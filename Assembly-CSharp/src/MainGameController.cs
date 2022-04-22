using System;
using System.Collections;
using System.Diagnostics;
using Gendarme;
using UnityEngine;
using UnityEngine.SceneManagement;
using UWE;

namespace AssemblyCSharp
{
    public class MainGameController : MonoBehaviour
    {
        [SerializeField]
        private string[] additionalScenes;

        private static MainGameController instance;

        private float lastLookMoveTime;

        private float lastAssetCollectionTime;

        private float lastGarbageCollectionTime;

        private int lastGarbageCollectionFrame;

        private int lastFrameGCCount;

        private float pdaOpenTimer;

        private const float timeBeforeCheckingForAssetCollection1 = 600f;

        private const float timeBeforeCheckingForAssetCollection2 = 900f;

        private const float timeBeforeForcedAssetCollection = 1200f;

        private float autoGCInterval = 20f;

        private Stopwatch collectionTimer = new Stopwatch();

        private bool detailedMemoryLog;

        public static MainGameController Instance => instance;

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        private void OnDestroy()
        {
            VRUtil.OnRecenter -= ResetOrientation;
            instance = null;
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        private IEnumerator Start()
        {
            instance = this;
            return CoroutineUtils.PumpCoroutine(StartGame(), "StartGame", 30f);
        }

        private IEnumerator StartGame()
        {
            IAssetBundleWrapperCreateRequest baseRequest = Base.KickoffAssetBundleLoadRequest();
            Physics.autoSyncTransforms = false;
            Physics2D.autoSimulation = false;
            detailedMemoryLog = Environment.GetEnvironmentVariable("SN_DETAILED_MEMLOG") == "1";
            if (detailedMemoryLog && !global::UnityEngine.Debug.isDebugBuild)
            {
                global::UnityEngine.Debug.LogWarning("SN_DETAILED_MEMLOG was set, but this is not a debug/dev build. So the detailed mem readings will all be 0.");
            }
            float repeatRate = 60f;
            string environmentVariable = Environment.GetEnvironmentVariable("SN_HEARTBEAT_PERIOD_S");
            if (!string.IsNullOrEmpty(environmentVariable))
            {
                repeatRate = float.Parse(environmentVariable);
            }
            InvokeRepeating("DoHeartbeat", 0f, repeatRate);
            Language language = Language.main;
            WaitScreen.ManualWaitItem waitItem = WaitScreen.Add(language.Get("Loading"));
            waitItem.SetProgress(1, 10);
            for (int i = 0; i < additionalScenes.Length; i++)
            {
                string text = additionalScenes[i];
                AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(text, LoadSceneMode.Additive);
                WaitScreen.AsyncOperationItem sceneWaitItem = WaitScreen.Add(language.GetFormat("LoadingSceneFormat", language.Get(text)), asyncOperation);
                yield return asyncOperation;
                WaitScreen.Remove(sceneWaitItem);
            }
            waitItem.SetProgress(2, 10);
            while (LightmappedPrefabs.main.IsWaitingOnLoads())
            {
                yield return CoroutineUtils.waitForNextFrame;
            }
            waitItem.SetProgress(3, 10);
            WaitScreen.AsyncRequestItem baseWaitItem = WaitScreen.Add("PreloadingBase", baseRequest);
            yield return baseRequest;
            WaitScreen.Remove(baseWaitItem);
            waitItem.SetProgress(4, 10);
            PAXTerrainController main = PAXTerrainController.main;
            if (main != null)
            {
                yield return main.Initialize();
            }
            waitItem.SetProgress(8, 10);
            while (!LargeWorldStreamer.main || !LargeWorldStreamer.main.IsWorldSettled())
            {
                yield return CoroutineUtils.waitForNextFrame;
            }
            PerformGarbageAndAssetCollection();
            waitItem.SetProgress(9, 10);
            yield return LoadInitialInventoryAsync();
            waitItem.SetProgress(10, 10);
            WaitScreen.Remove(waitItem);
            Application.backgroundLoadingPriority = ThreadPriority.Normal;
            if (PlatformUtils.isConsolePlatform)
            {
                LargeWorld.main.GetComponent<Voxeland>().updateChunksEnabled = false;
                Time.fixedDeltaTime = 71f / (678f * (float)System.Math.PI);
                Cursor.lockState = CursorLockMode.Locked;
                Input.simulateMouseWithTouches = false;
            }
            DevConsole.RegisterConsoleCommand(this, "collect");
            DevConsole.RegisterConsoleCommand(this, "endsession");
            VRUtil.OnRecenter += ResetOrientation;
        }

        private void OnConsoleCommand_endsession()
        {
            global::UnityEngine.Debug.Log("endsession cmd");
        }

        public bool CanPerformAssetCollection()
        {
            return Time.time > lastAssetCollectionTime + 600f;
        }

        private bool WantsAutoGarbageCollection()
        {
            return Time.time > lastGarbageCollectionTime + autoGCInterval;
        }

        public void SetAutoGarbageCollectionInterval(float gcTime)
        {
            autoGCInterval = gcTime;
        }

        private void UpdateAutoGarbageCollection()
        {
            if (WantsAutoGarbageCollection())
            {
                PerformGarbageCollection();
            }
        }

        private void UpdateAutoAssetCollection()
        {
            if (!CanPerformAssetCollection())
            {
                return;
            }
            float time = Time.time;
            if (GameInput.GetLookDelta().sqrMagnitude > 0.1f)
            {
                lastLookMoveTime = time;
            }
            else if (time > lastAssetCollectionTime + 1200f)
            {
                PerformGarbageAndAssetCollection();
            }
            else if (time > lastAssetCollectionTime + 900f)
            {
                if (time > lastLookMoveTime + 0.5f)
                {
                    PerformGarbageAndAssetCollection();
                }
            }
            else if (time > lastAssetCollectionTime + 600f && Player.main.GetPDA().isOpen)
            {
                pdaOpenTimer += Time.deltaTime;
                if (pdaOpenTimer > 0.5f)
                {
                    PerformGarbageAndAssetCollection();
                    pdaOpenTimer = 0f;
                }
            }
        }

        public void PerformGarbageAndAssetCollection()
        {
            lastAssetCollectionTime = Time.time;
            global::UnityEngine.Debug.Log("PerformGarbageAndAssetCollection, Time.time=" + Time.time + ", Time.frameCount=" + Time.frameCount + ", DateTime.Now=" + DateTime.Now);
            StartCoroutine(PerformGarbageAndAssetCollectionAsyncInternal());
        }

        public IEnumerator PerformGarbageAndAssetCollectionAsync()
        {
            lastAssetCollectionTime = Time.time;
            global::UnityEngine.Debug.Log("PerformGarbageAndAssetCollection, Time.time=" + Time.time + ", Time.frameCount=" + Time.frameCount + ", DateTime.Now=" + DateTime.Now);
            yield return PerformGarbageAndAssetCollectionAsyncInternal();
        }

        private IEnumerator PerformGarbageAndAssetCollectionAsyncInternal()
        {
            collectionTimer.Restart();
            PerformGarbageCollection();
            collectionTimer.Stop();
            float gcTime = global::UWE.Utils.GetTimeElapsedMS(collectionTimer);
            yield return CoroutineUtils.waitForNextFrame;
            collectionTimer.Restart();
            yield return PrefabDatabase.UnloadUnusedAssets();
            collectionTimer.Stop();
            float timeElapsedMS = global::UWE.Utils.GetTimeElapsedMS(collectionTimer);
            global::UnityEngine.Debug.LogFormat("--- PerformGarbageAndAssetCollectionAsync: GC Time {0} Asset GC Time {1}", gcTime.ToString(), timeElapsedMS.ToString());
        }

        public void PerformGarbageCollection()
        {
            ProfilingUtils.BeginSample("PerformGarbageCollection");
            Timer.Begin("PerformGarbageCollection -> GC.Collect");
            GC.Collect();
            Timer.End();
            NotifyGarbageCollected();
            ProfilingUtils.EndSample();
        }

        public void NotifyGarbageCollected()
        {
            lastGarbageCollectionTime = Time.time;
            lastGarbageCollectionFrame = Time.frameCount;
        }

        public bool HasGarbageCollectedThisFrame()
        {
            return lastGarbageCollectionFrame == Time.frameCount;
        }

        private IEnumerator LoadInitialInventoryAsync()
        {
            if (GameModeUtils.SpawnsInitialItems())
            {
                WaitScreen.ManualWaitItem waitItem = WaitScreen.Add(Language.main.Get("LoadingEquipment"));
                int numTotal = Player.creativeEquipment.Length;
                int i = 0;
                Player.InitialEquipment[] creativeEquipment = Player.creativeEquipment;
                for (int j = 0; j < creativeEquipment.Length; j++)
                {
                    Player.InitialEquipment initialEquipment = creativeEquipment[j];
                    waitItem.SetProgress(i++, numTotal);
                    yield return CraftData.GetPrefabForTechTypeAsync(initialEquipment.techType);
                }
                WaitScreen.Remove(waitItem);
            }
        }

        private void OnConsoleCommand_collect()
        {
            PerformGarbageAndAssetCollection();
        }

        private void Update()
        {
            if (GC.CollectionCount(0) != lastFrameGCCount)
            {
                NotifyGarbageCollected();
            }
            UpdateAutoAssetCollection();
            lastFrameGCCount = GC.CollectionCount(0);
            if (global::UnityEngine.Debug.isDebugBuild && Input.GetKeyDown(KeyCode.F5) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            {
                if (!global::UnityEngine.Profiling.Profiler.enabled)
                {
                    global::UnityEngine.Profiling.Profiler.logFile = "profiling-" + Time.frameCount + ".log";
                    global::UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                    global::UnityEngine.Profiling.Profiler.enabled = true;
                    global::UnityEngine.Debug.Log("Started profiling, writing to " + global::UnityEngine.Profiling.Profiler.logFile);
                }
                else
                {
                    global::UnityEngine.Profiling.Profiler.enabled = false;
                    global::UnityEngine.Debug.Log("Stopped profiling");
                }
            }
            if (!PlatformUtils.isShippingRelease && (Input.GetKeyDown(KeyCode.F1) || (Input.GetKey(KeyCode.JoystickButton6) && Input.GetKeyDown(KeyCode.JoystickButton2))))
            {
                TerrainDebugGUI[] array = global::UnityEngine.Object.FindObjectsOfType<TerrainDebugGUI>();
                foreach (TerrainDebugGUI obj in array)
                {
                    obj.enabled = !obj.enabled;
                }
            }
            if (!PlatformUtils.isShippingRelease && Input.GetKeyDown(KeyCode.F3))
            {
                GraphicsDebugGUI[] array2 = global::UnityEngine.Object.FindObjectsOfType<GraphicsDebugGUI>();
                foreach (GraphicsDebugGUI graphicsDebugGUI in array2)
                {
                    if (graphicsDebugGUI != null)
                    {
                        graphicsDebugGUI.enabled = !graphicsDebugGUI.enabled;
                    }
                }
            }
            if (uGUI.main.intro.showing && HandReticle.main.gameObject.activeInHierarchy)
            {
                HandReticle.main.gameObject.SetActive(value: false);
            }
            else if (!uGUI.main.intro.showing && !HandReticle.main.gameObject.activeInHierarchy)
            {
                HandReticle.main.gameObject.SetActive(value: true);
            }
            if (PlatformUtils.isConsolePlatform)
            {
                Cursor.visible = false;
            }
            else if (!Cursor.visible && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.visible = true;
            }
        }

        private long CountTotalBytesUsedByResource<T>() where T : global::UnityEngine.Object
        {
            long num = 0L;
            T[] array = Resources.FindObjectsOfTypeAll<T>();
            foreach (T o in array)
            {
                num += global::UnityEngine.Profiling.Profiler.GetRuntimeMemorySize(o);
            }
            return num;
        }

        private void DoHeartbeat()
        {
            CellManager cellManager = LargeWorldStreamer.main.cellManager;
            Vector3 vector = Vector3.zero;
            if ((bool)Player.main)
            {
                vector = Player.main.transform.position;
            }
            long num = 0L;
            if ((bool)MonitorLauncher.main)
            {
                ProcessInfo processInfo = MonitorLauncher.main.GetProcessInfo();
                if (processInfo != null)
                {
                    num = processInfo.workingSet;
                }
            }
            string text = "";
            if (detailedMemoryLog)
            {
                text = ", totalMeshMBs," + (float)CountTotalBytesUsedByResource<Mesh>() / 1024f / 1024f + ", totalTextureMBs," + (float)CountTotalBytesUsedByResource<Texture>() / 1024f / 1024f;
            }
            global::UnityEngine.Debug.Log("Heartbeat CSV, time s," + Time.time + ", GC.GetTotalMemory MB," + (float)GC.GetTotalMemory(forceFullCollection: false) / 1024f / 1024f + ", OctNodes MB," + (float)VoxelandData.OctNode.GetPoolBytesTotal() / 1024f / 1024f + ", CompactOctrees MB," + (float)LargeWorldStreamer.main.EstimateCompactOctreeBytes() / 1024f / 1024f + ", CellManager MB," + (float)(cellManager?.EstimateBytes() ?? 0) / 1024f / 1024f + ", ClipMapManager MB," + (float)LargeWorldStreamer.main.EstimateClipMapManagerBytes() / 1024f / 1024f + ", GCCount," + GC.CollectionCount(0) + ", PlayerPos," + vector.x + "," + vector.y + "," + vector.z + ", WorkingSet MB," + (float)num / 1024f / 1024f + text);
        }

        public void ResetOrientation()
        {
            MainCameraControl.main.rotationY = 0f;
        }
    }
}
