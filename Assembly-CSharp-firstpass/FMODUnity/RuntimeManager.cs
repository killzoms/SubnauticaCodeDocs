using System;
using System.Collections.Generic;
using System.Text;
using AOT;
using FMOD;
using FMOD.Studio;
using UnityEngine;

namespace FMODUnity
{
    [AddComponentMenu("")]
    public class RuntimeManager : MonoBehaviour
    {
        private struct LoadedBank
        {
            public Bank Bank;

            public int RefCount;
        }

        private class GuidComparer : IEqualityComparer<Guid>
        {
            bool IEqualityComparer<Guid>.Equals(Guid x, Guid y)
            {
                return x.Equals(y);
            }

            int IEqualityComparer<Guid>.GetHashCode(Guid obj)
            {
                return obj.GetHashCode();
            }
        }

        private class AttachedInstance
        {
            public EventInstance instance;

            public Transform transform;

            public Rigidbody rigidBody;

            public Rigidbody2D rigidBody2D;
        }

        private static SystemNotInitializedException initException = null;

        private static RuntimeManager instance;

        [SerializeField]
        private FMODPlatform fmodPlatform;

        private FMOD.Studio.System studioSystem;

        private FMOD.System lowlevelSystem;

        private DSP mixerHead;

        [SerializeField]
        private long[] cachedPointers = new long[2];

        private Dictionary<string, LoadedBank> loadedBanks = new Dictionary<string, LoadedBank>();

        private Dictionary<string, uint> loadedPlugins = new Dictionary<string, uint>();

        private Dictionary<Guid, EventDescription> cachedDescriptions = new Dictionary<Guid, EventDescription>(new GuidComparer());

        private List<AttachedInstance> attachedInstances = new List<AttachedInstance>(128);

        private bool listenerWarningIssued;

        private Rect windowRect = new Rect(10f, 10f, 300f, 100f);

        private string lastDebugText;

        private float lastDebugUpdate;

        public static bool[] HasListener = new bool[8];

        private static RuntimeManager Instance
        {
            get
            {
                if (initException != null)
                {
                    throw initException;
                }
                if (instance == null)
                {
                    RESULT rESULT = RESULT.OK;
                    RuntimeManager[] array = UnityEngine.Object.FindObjectsOfType(typeof(RuntimeManager)) as RuntimeManager[];
                    RuntimeManager[] array2 = array;
                    foreach (RuntimeManager runtimeManager in array2)
                    {
                        if (array != null)
                        {
                            if (runtimeManager.cachedPointers[0] != 0L)
                            {
                                instance = runtimeManager;
                                instance.studioSystem.handle = (IntPtr)instance.cachedPointers[0];
                                instance.lowlevelSystem.handle = (IntPtr)instance.cachedPointers[1];
                            }
                            UnityEngine.Object.DestroyImmediate(runtimeManager);
                        }
                    }
                    GameObject gameObject = new GameObject("FMOD.UnityIntegration.RuntimeManager");
                    instance = gameObject.AddComponent<RuntimeManager>();
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    }
                    gameObject.hideFlags = HideFlags.HideAndDontSave;
                    try
                    {
                        RuntimeUtils.EnforceLibraryOrder();
                        rESULT = instance.Initialize();
                    }
                    catch (Exception ex)
                    {
                        initException = ex as SystemNotInitializedException;
                        if (initException == null)
                        {
                            initException = new SystemNotInitializedException(ex);
                        }
                        throw initException;
                    }
                    if (rESULT != 0)
                    {
                        throw new SystemNotInitializedException(rESULT, "Output forced to NO SOUND mode");
                    }
                }
                return instance;
            }
        }

        public static FMOD.Studio.System StudioSystem => Instance.studioSystem;

        public static FMOD.System LowlevelSystem => Instance.lowlevelSystem;

        public static IDictionary<Guid, EventDescription> CachedDescriptions => Instance.cachedDescriptions;

        public static bool IsInitialized
        {
            get
            {
                if (instance != null)
                {
                    return instance.studioSystem.isValid();
                }
                return false;
            }
        }

        public static bool HasBanksLoaded => Instance.loadedBanks.Count > 1;

        [MonoPInvokeCallback(typeof(DEBUG_CALLBACK))]
        private static RESULT DEBUG_CALLBACK(DEBUG_FLAGS flags, StringWrapper file, int line, StringWrapper func, StringWrapper message)
        {
            if ((flags & DEBUG_FLAGS.ERROR) != 0)
            {
                UnityEngine.Debug.LogErrorFormat("[FMOD] {0} : {1}", (string)func, (string)message);
            }
            else if ((flags & DEBUG_FLAGS.WARNING) != 0)
            {
                UnityEngine.Debug.LogWarningFormat("[FMOD] {0} : {1}", (string)func, (string)message);
            }
            else if ((flags & DEBUG_FLAGS.LOG) != 0)
            {
                UnityEngine.Debug.LogFormat("[FMOD] {0} : {1}", (string)func, (string)message);
            }
            return RESULT.OK;
        }

        private void CheckInitResult(RESULT result, string cause)
        {
            if (result != 0)
            {
                if (studioSystem.isValid())
                {
                    studioSystem.release();
                    studioSystem.clearHandle();
                }
                throw new SystemNotInitializedException(result, cause);
            }
        }

        private RESULT Initialize()
        {
            RESULT rESULT = RESULT.OK;
            RESULT rESULT2 = RESULT.OK;
            Settings settings = Settings.Instance;
            fmodPlatform = RuntimeUtils.GetCurrentPlatform();
            int sampleRate = settings.GetSampleRate(fmodPlatform);
            int num = Math.Min(settings.GetRealChannels(fmodPlatform), 256);
            int virtualChannels = settings.GetVirtualChannels(fmodPlatform);
            SPEAKERMODE speakerMode = (SPEAKERMODE)settings.GetSpeakerMode(fmodPlatform);
            OUTPUTTYPE output = OUTPUTTYPE.AUTODETECT;
            FMOD.ADVANCEDSETTINGS settings2 = default(FMOD.ADVANCEDSETTINGS);
            settings2.randomSeed = (uint)DateTime.Now.Ticks;
            settings2.maxVorbisCodecs = num;
            SetThreadAffinity();
            FMOD.Studio.INITFLAGS iNITFLAGS = FMOD.Studio.INITFLAGS.DEFERRED_CALLBACKS;
            if (settings.IsLiveUpdateEnabled(fmodPlatform))
            {
                iNITFLAGS |= FMOD.Studio.INITFLAGS.LIVEUPDATE;
                settings2.profilePort = settings.LiveUpdatePort;
            }
            while (true)
            {
                rESULT = FMOD.Studio.System.create(out studioSystem);
                CheckInitResult(rESULT, "FMOD.Studio.System.create");
                rESULT = studioSystem.getLowLevelSystem(out lowlevelSystem);
                CheckInitResult(rESULT, "FMOD.Studio.System.getLowLevelSystem");
                rESULT = lowlevelSystem.setOutput(output);
                CheckInitResult(rESULT, "FMOD.System.setOutput");
                rESULT = lowlevelSystem.setSoftwareChannels(num);
                CheckInitResult(rESULT, "FMOD.System.setSoftwareChannels");
                rESULT = lowlevelSystem.setSoftwareFormat(sampleRate, speakerMode, 0);
                CheckInitResult(rESULT, "FMOD.System.setSoftwareFormat");
                rESULT = lowlevelSystem.setAdvancedSettings(ref settings2);
                CheckInitResult(rESULT, "FMOD.System.setAdvancedSettings");
                rESULT = studioSystem.initialize(virtualChannels, iNITFLAGS, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
                if (rESULT != 0 && rESULT2 == RESULT.OK)
                {
                    rESULT2 = rESULT;
                    output = OUTPUTTYPE.NOSOUND;
                    UnityEngine.Debug.LogErrorFormat("[FMOD] Studio::System::initialize returned {0}, defaulting to no-sound mode.", rESULT.ToString());
                    continue;
                }
                CheckInitResult(rESULT, "Studio::System::initialize");
                if ((iNITFLAGS & FMOD.Studio.INITFLAGS.LIVEUPDATE) == 0)
                {
                    break;
                }
                studioSystem.flushCommands();
                rESULT = studioSystem.update();
                if (rESULT != RESULT.ERR_NET_SOCKET_ERROR)
                {
                    break;
                }
                iNITFLAGS &= ~FMOD.Studio.INITFLAGS.LIVEUPDATE;
                UnityEngine.Debug.LogWarning("[FMOD] Cannot open network port for Live Update (in-use), restarting with Live Update disabled.");
                rESULT = studioSystem.release();
                CheckInitResult(rESULT, "FMOD.Studio.System.Release");
            }
            LoadPlugins(settings);
            LoadBanks(settings);
            return rESULT2;
        }

        private void Update()
        {
            if (!studioSystem.isValid())
            {
                return;
            }
            bool flag = false;
            bool flag2 = false;
            int numListeners = 0;
            for (int num = 7; num >= 0; num--)
            {
                if (!flag && HasListener[num])
                {
                    numListeners = num + 1;
                    flag = true;
                    flag2 = true;
                }
                if (!HasListener[num] && flag)
                {
                    flag2 = false;
                }
            }
            if (flag)
            {
                studioSystem.setNumListeners(numListeners);
            }
            if (!flag2 && !listenerWarningIssued)
            {
                listenerWarningIssued = true;
                UnityEngine.Debug.LogWarning("[FMOD] Please add an 'FMOD Studio Listener' component to your a camera in the scene for correct 3D positioning of sounds");
            }
            for (int i = 0; i < attachedInstances.Count; i++)
            {
                PLAYBACK_STATE state = PLAYBACK_STATE.STOPPED;
                attachedInstances[i].instance.getPlaybackState(out state);
                if (!attachedInstances[i].instance.isValid() || state == PLAYBACK_STATE.STOPPED || attachedInstances[i].transform == null)
                {
                    attachedInstances.RemoveAt(i);
                    i--;
                }
                else if ((bool)attachedInstances[i].rigidBody)
                {
                    attachedInstances[i].instance.set3DAttributes(RuntimeUtils.To3DAttributes(attachedInstances[i].transform, attachedInstances[i].rigidBody));
                }
                else
                {
                    attachedInstances[i].instance.set3DAttributes(RuntimeUtils.To3DAttributes(attachedInstances[i].transform, attachedInstances[i].rigidBody2D));
                }
            }
            studioSystem.update();
        }

        public static void AttachInstanceToGameObject(EventInstance instance, Transform transform, Rigidbody rigidBody)
        {
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform, rigidBody));
            AttachedInstance attachedInstance = new AttachedInstance();
            attachedInstance.transform = transform;
            attachedInstance.instance = instance;
            attachedInstance.rigidBody = rigidBody;
            Instance.attachedInstances.Add(attachedInstance);
        }

        public static void AttachInstanceToGameObject(EventInstance instance, Transform transform, Rigidbody2D rigidBody2D)
        {
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform, rigidBody2D));
            AttachedInstance attachedInstance = new AttachedInstance();
            attachedInstance.transform = transform;
            attachedInstance.instance = instance;
            attachedInstance.rigidBody2D = rigidBody2D;
            attachedInstance.rigidBody = null;
            Instance.attachedInstances.Add(attachedInstance);
        }

        public static void DetachInstanceFromGameObject(EventInstance instance)
        {
            RuntimeManager runtimeManager = Instance;
            for (int i = 0; i < runtimeManager.attachedInstances.Count; i++)
            {
                if (runtimeManager.attachedInstances[i].instance.handle == instance.handle)
                {
                    runtimeManager.attachedInstances.RemoveAt(i);
                    break;
                }
            }
        }

        private void OnGUI()
        {
            if (studioSystem.isValid() && Settings.Instance.IsOverlayEnabled(fmodPlatform))
            {
                windowRect = GUI.Window(0, windowRect, DrawDebugOverlay, "FMOD Studio Debug");
            }
        }

        private void DrawDebugOverlay(int windowID)
        {
            if (lastDebugUpdate + 0.25f < Time.unscaledTime)
            {
                if (initException != null)
                {
                    lastDebugText = initException.Message;
                }
                else
                {
                    if (!mixerHead.hasHandle())
                    {
                        lowlevelSystem.getMasterChannelGroup(out var channelgroup);
                        channelgroup.getDSP(0, out mixerHead);
                        mixerHead.setMeteringEnabled(inputEnabled: false, outputEnabled: true);
                    }
                    StringBuilder stringBuilder = new StringBuilder();
                    studioSystem.getCPUUsage(out var usage);
                    stringBuilder.AppendFormat("CPU: dsp = {0:F1}%, studio = {1:F1}%\n", usage.dspusage, usage.studiousage);
                    Memory.GetStats(out var currentalloced, out var maxalloced);
                    stringBuilder.AppendFormat("MEMORY: cur = {0}MB, max = {1}MB\n", currentalloced >> 20, maxalloced >> 20);
                    lowlevelSystem.getChannelsPlaying(out var channels, out var realchannels);
                    stringBuilder.AppendFormat("CHANNELS: real = {0}, total = {1}\n", realchannels, channels);
                    mixerHead.getMeteringInfo(IntPtr.Zero, out var outputInfo);
                    float num = 0f;
                    for (int i = 0; i < outputInfo.numchannels; i++)
                    {
                        num += outputInfo.rmslevel[i] * outputInfo.rmslevel[i];
                    }
                    num = Mathf.Sqrt(num / (float)outputInfo.numchannels);
                    float num2 = ((num > 0f) ? (20f * Mathf.Log10(num * Mathf.Sqrt(2f))) : (-80f));
                    if (num2 > 10f)
                    {
                        num2 = 10f;
                    }
                    stringBuilder.AppendFormat("VOLUME: RMS = {0:f2}db\n", num2);
                    lastDebugText = stringBuilder.ToString();
                    lastDebugUpdate = Time.unscaledTime;
                }
            }
            GUI.Label(new Rect(10f, 20f, 290f, 100f), lastDebugText);
            GUI.DragWindow();
        }

        private void OnDisable()
        {
            cachedPointers[0] = (long)studioSystem.handle;
            cachedPointers[1] = (long)lowlevelSystem.handle;
        }

        private void OnDestroy()
        {
            if (studioSystem.isValid())
            {
                studioSystem.release();
                studioSystem.clearHandle();
            }
            initException = null;
            instance = null;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (studioSystem.isValid())
            {
                PauseAllEvents(pauseStatus);
                if (pauseStatus)
                {
                    lowlevelSystem.mixerSuspend();
                }
                else
                {
                    lowlevelSystem.mixerResume();
                }
            }
        }

        private void loadedBankRegister(LoadedBank loadedBank, string bankPath, string bankName, bool loadSamples, RESULT loadResult)
        {
            switch (loadResult)
            {
            case RESULT.OK:
                loadedBank.RefCount = 1;
                if (loadSamples)
                {
                    loadedBank.Bank.loadSampleData();
                }
                Instance.loadedBanks.Add(bankName, loadedBank);
                break;
            case RESULT.ERR_EVENT_ALREADY_LOADED:
                loadedBank.RefCount = 2;
                Instance.loadedBanks.Add(bankName, loadedBank);
                break;
            default:
                throw new BankLoadException(bankPath, loadResult);
            }
        }

        public static void LoadBank(string bankName, bool loadSamples = false)
        {
            if (Instance.loadedBanks.ContainsKey(bankName))
            {
                LoadedBank value = Instance.loadedBanks[bankName];
                value.RefCount++;
                if (loadSamples)
                {
                    value.Bank.loadSampleData();
                }
                Instance.loadedBanks[bankName] = value;
            }
            else
            {
                string bankPath = RuntimeUtils.GetBankPath(bankName);
                LoadedBank loadedBank = default(LoadedBank);
                RESULT loadResult = Instance.studioSystem.loadBankFile(bankPath, LOAD_BANK_FLAGS.NORMAL, out loadedBank.Bank);
                Instance.loadedBankRegister(loadedBank, bankPath, bankName, loadSamples, loadResult);
            }
        }

        public static void LoadBank(TextAsset asset, bool loadSamples = false)
        {
            string text = asset.name;
            if (Instance.loadedBanks.ContainsKey(text))
            {
                LoadedBank loadedBank = Instance.loadedBanks[text];
                loadedBank.RefCount++;
                if (loadSamples)
                {
                    loadedBank.Bank.loadSampleData();
                }
                return;
            }
            LoadedBank value = default(LoadedBank);
            RESULT rESULT = Instance.studioSystem.loadBankMemory(asset.bytes, LOAD_BANK_FLAGS.NORMAL, out value.Bank);
            switch (rESULT)
            {
            case RESULT.OK:
                value.RefCount = 1;
                Instance.loadedBanks.Add(text, value);
                if (loadSamples)
                {
                    value.Bank.loadSampleData();
                }
                break;
            case RESULT.ERR_EVENT_ALREADY_LOADED:
                value.RefCount = 2;
                Instance.loadedBanks.Add(text, value);
                break;
            default:
                throw new BankLoadException(text, rESULT);
            }
        }

        private void LoadBanks(Settings fmodSettings)
        {
            if (fmodSettings.ImportType != 0)
            {
                return;
            }
            try
            {
                foreach (string masterBank in fmodSettings.MasterBanks)
                {
                    LoadBank(masterBank + ".strings", fmodSettings.AutomaticSampleLoading);
                    if (fmodSettings.AutomaticEventLoading)
                    {
                        LoadBank(masterBank, fmodSettings.AutomaticSampleLoading);
                    }
                }
                if (!fmodSettings.AutomaticEventLoading)
                {
                    return;
                }
                foreach (string bank in fmodSettings.Banks)
                {
                    LoadBank(bank, fmodSettings.AutomaticSampleLoading);
                }
                WaitForAllLoads();
            }
            catch (BankLoadException exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }

        public static void UnloadBank(string bankName)
        {
            if (Instance.loadedBanks.TryGetValue(bankName, out var value))
            {
                value.RefCount--;
                if (value.RefCount == 0)
                {
                    value.Bank.unload();
                    Instance.loadedBanks.Remove(bankName);
                }
                else
                {
                    Instance.loadedBanks[bankName] = value;
                }
            }
        }

        public static bool AnyBankLoading()
        {
            bool flag = false;
            foreach (LoadedBank value in Instance.loadedBanks.Values)
            {
                value.Bank.getSampleLoadingState(out var state);
                flag = flag || state == LOADING_STATE.LOADING;
            }
            return flag;
        }

        public static void WaitForAllLoads()
        {
            Instance.studioSystem.flushSampleLoading();
        }

        public static Guid PathToGUID(string path)
        {
            Guid id = Guid.Empty;
            if (path.StartsWith("{"))
            {
                Util.ParseID(path, out id);
            }
            else if (Instance.studioSystem.lookupID(path, out id) == RESULT.ERR_EVENT_NOTFOUND)
            {
                throw new EventNotFoundException(path);
            }
            return id;
        }

        public static EventInstance CreateInstance(string path)
        {
            try
            {
                return CreateInstance(PathToGUID(path));
            }
            catch (EventNotFoundException)
            {
                throw new EventNotFoundException(path);
            }
        }

        public static EventInstance CreateInstance(Guid guid)
        {
            GetEventDescription(guid).createInstance(out var result);
            return result;
        }

        public static void PlayOneShot(string path, Vector3 position = default(Vector3))
        {
            try
            {
                PlayOneShot(PathToGUID(path), position);
            }
            catch (EventNotFoundException)
            {
                UnityEngine.Debug.LogWarning("[FMOD] Event not found: " + path);
            }
        }

        public static void PlayOneShot(Guid guid, Vector3 position = default(Vector3))
        {
            EventInstance eventInstance = CreateInstance(guid);
            eventInstance.set3DAttributes(position.To3DAttributes());
            eventInstance.start();
            eventInstance.release();
        }

        public static void PlayOneShotAttached(string path, GameObject gameObject)
        {
            try
            {
                PlayOneShotAttached(PathToGUID(path), gameObject);
            }
            catch (EventNotFoundException)
            {
                UnityEngine.Debug.LogWarning("[FMOD] Event not found: " + path);
            }
        }

        public static void PlayOneShotAttached(Guid guid, GameObject gameObject)
        {
            EventInstance eventInstance = CreateInstance(guid);
            AttachInstanceToGameObject(eventInstance, gameObject.transform, gameObject.GetComponent<Rigidbody>());
            eventInstance.start();
            eventInstance.release();
        }

        public static EventDescription GetEventDescription(string path)
        {
            try
            {
                return GetEventDescription(PathToGUID(path));
            }
            catch (EventNotFoundException)
            {
                throw new EventNotFoundException(path);
            }
        }

        public static EventDescription GetEventDescription(Guid guid)
        {
            if (Instance.cachedDescriptions.ContainsKey(guid) && Instance.cachedDescriptions[guid].isValid())
            {
                return Instance.cachedDescriptions[guid];
            }
            if (Instance.studioSystem.getEventByID(guid, out var _event) != 0)
            {
                throw new EventNotFoundException(guid);
            }
            if (_event.isValid())
            {
                Instance.cachedDescriptions[guid] = _event;
            }
            return _event;
        }

        public static void SetListenerLocation(GameObject gameObject, Rigidbody rigidBody = null)
        {
            Instance.studioSystem.setListenerAttributes(0, RuntimeUtils.To3DAttributes(gameObject, rigidBody));
        }

        public static void SetListenerLocation(GameObject gameObject, Rigidbody2D rigidBody2D)
        {
            Instance.studioSystem.setListenerAttributes(0, RuntimeUtils.To3DAttributes(gameObject, rigidBody2D));
        }

        public static void SetListenerLocation(Transform transform)
        {
            Instance.studioSystem.setListenerAttributes(0, transform.To3DAttributes());
        }

        public static void SetListenerLocation(int listenerIndex, GameObject gameObject, Rigidbody rigidBody = null)
        {
            Instance.studioSystem.setListenerAttributes(listenerIndex, RuntimeUtils.To3DAttributes(gameObject, rigidBody));
        }

        public static void SetListenerLocation(int listenerIndex, GameObject gameObject, Rigidbody2D rigidBody2D)
        {
            Instance.studioSystem.setListenerAttributes(listenerIndex, RuntimeUtils.To3DAttributes(gameObject, rigidBody2D));
        }

        public static void SetListenerLocation(int listenerIndex, Transform transform)
        {
            Instance.studioSystem.setListenerAttributes(listenerIndex, transform.To3DAttributes());
        }

        public static Bus GetBus(string path)
        {
            if (StudioSystem.getBus(path, out var bus) != 0)
            {
                throw new BusNotFoundException(path);
            }
            return bus;
        }

        public static VCA GetVCA(string path)
        {
            if (StudioSystem.getVCA(path, out var vca) != 0)
            {
                throw new VCANotFoundException(path);
            }
            return vca;
        }

        public static void PauseAllEvents(bool paused)
        {
            if (HasBanksLoaded && StudioSystem.getBus("bus:/", out var bus) == RESULT.OK)
            {
                bus.setPaused(paused);
            }
        }

        public static void MuteAllEvents(bool muted)
        {
            if (HasBanksLoaded && StudioSystem.getBus("bus:/", out var bus) == RESULT.OK)
            {
                bus.setMute(muted);
            }
        }

        public static bool HasBankLoaded(string loadedBank)
        {
            return instance.loadedBanks.ContainsKey(loadedBank);
        }

        private void LoadPlugins(Settings fmodSettings)
        {
            foreach (string plugin in fmodSettings.Plugins)
            {
                if (!string.IsNullOrEmpty(plugin))
                {
                    string pluginPath = RuntimeUtils.GetPluginPath(plugin);
                    uint handle;
                    RESULT rESULT = lowlevelSystem.loadPlugin(pluginPath, out handle);
                    if (rESULT == RESULT.ERR_FILE_BAD || rESULT == RESULT.ERR_FILE_NOTFOUND)
                    {
                        string pluginPath2 = RuntimeUtils.GetPluginPath(plugin + "64");
                        rESULT = lowlevelSystem.loadPlugin(pluginPath2, out handle);
                    }
                    CheckInitResult(rESULT, $"Loading plugin '{plugin}' from '{pluginPath}'");
                    loadedPlugins.Add(plugin, handle);
                }
            }
        }

        private void SetThreadAffinity()
        {
        }
    }
}
