using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class SaveLoadManager : MonoBehaviour
    {
        [ProtoContract]
        public class OptionsCache
        {
            [NonSerialized]
            [ProtoMember(1)]
            public readonly Dictionary<string, float> _floats = new Dictionary<string, float>();

            [NonSerialized]
            [ProtoMember(2)]
            public readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

            [NonSerialized]
            [ProtoMember(3)]
            public readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>();

            [NonSerialized]
            [ProtoMember(4)]
            public readonly Dictionary<string, int> _ints = new Dictionary<string, int>();

            public void Clear()
            {
                _floats.Clear();
                _strings.Clear();
                _bools.Clear();
                _ints.Clear();
            }

            public void SetInt(string name, int value)
            {
                _ints[name] = value;
            }

            public int GetInt(string name, int defaultValue)
            {
                return _ints.GetOrDefault(name, defaultValue);
            }

            public void SetFloat(string name, float value)
            {
                _floats[name] = value;
            }

            public float GetFloat(string name, float defaultValue)
            {
                return _floats.GetOrDefault(name, defaultValue);
            }

            public void SetBool(string name, bool value)
            {
                _bools[name] = value;
            }

            public bool GetBool(string name, bool defaultValue)
            {
                return _bools.GetOrDefault(name, defaultValue);
            }

            public void SetString(string name, string value)
            {
                _strings[name] = value;
            }

            public string GetString(string name, string defaultValue)
            {
                return _strings.GetOrDefault(name, defaultValue);
            }
        }

        public enum Error
        {
            None,
            InvalidCall,
            UnknownError,
            OutOfSpace,
            NoAccess,
            NotFound,
            InvalidFormat,
            OutOfSlots
        }

        public abstract class AsyncResult
        {
            public readonly bool success;

            public AsyncResult(bool success)
            {
                this.success = success;
            }
        }

        public class LoadResult : AsyncResult
        {
            public readonly Error error;

            public readonly string errorMessage;

            public LoadResult(bool success, Error error, string errorMessage)
                : base(success)
            {
                this.error = error;
                this.errorMessage = errorMessage;
            }
        }

        public class SaveResult : AsyncResult
        {
            public readonly Error error;

            public readonly string errorMessage;

            public SaveResult(bool success, Error error, string errorMessage)
                : base(success)
            {
                this.error = error;
                this.errorMessage = errorMessage;
            }
        }

        public class CreateResult : AsyncResult
        {
            public readonly string slotName;

            public CreateResult(bool success, string slotName)
                : base(success)
            {
                this.slotName = slotName;
            }
        }

        public class GameInfo
        {
            private const int screenshotWidth = 200;

            private const int screenshotQuality = 75;

            public int version;

            public int gameTime;

            public long dateTicks;

            public long startTicks;

            public int changeSet;

            public string userName;

            public string machineName;

            public string session;

            public GameMode gameMode = GameMode.None;

            public bool isFallback;

            public bool cyclopsPresent;

            public bool seamothPresent;

            public bool exosuitPresent;

            public bool rocketPresent;

            public bool basePresent;

            private Texture2D screenshot;

            private static void SaveFile(string fileName, byte[] bytes)
            {
                global::Platform.IO.File.WriteAllBytes(global::Platform.IO.Path.Combine(GetTemporarySavePath(), fileName), bytes);
            }

            public void Initialize(float timePlayed, long firstStart, string sessionId, Texture2D tex)
            {
                version = 2;
                gameTime = Mathf.FloorToInt(timePlayed);
                dateTicks = DateTime.Now.Ticks;
                startTicks = firstStart;
                changeSet = SNUtils.GetPlasticChangeSetOfBuild(0);
                userName = Environment.UserName;
                machineName = Environment.MachineName;
                session = sessionId;
                gameMode = Utils.GetLegacyGameMode();
                screenshot = tex;
            }

            public override string ToString()
            {
                return JsonUtility.ToJson(this);
            }

            public static void SaveIntoCurrentSlot(GameInfo info)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    string value = JsonUtility.ToJson(info);
                    using (StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
                    {
                        streamWriter.WriteLine(value);
                    }
                    SaveFile("gameinfo.json", memoryStream.ToArray());
                }
                using MemoryStream memoryStream2 = new MemoryStream();
                Texture2D texture2D = info.screenshot;
                if (texture2D != null)
                {
                    info.screenshot = MathExtensions.ScaleTexture(texture2D, 200, mipmap: false);
                    byte[] array = info.screenshot.EncodeToJPG(75);
                    if (array.Length != 0)
                    {
                        memoryStream2.Write(array, 0, array.Length);
                        SaveFile("screenshot.jpg", memoryStream2.ToArray());
                    }
                    global::UnityEngine.Object.Destroy(texture2D);
                }
            }

            public static GameInfo LoadFromBytes(byte[] jsonData, byte[] screenshotData)
            {
                try
                {
                    if (jsonData == null)
                    {
                        throw new ArgumentNullException("jsonData", "No gameinfo data");
                    }
                    GameInfo gameInfo = null;
                    using (StreamReader streamReader = new StreamReader(new MemoryStream(jsonData)))
                    {
                        gameInfo = JsonUtility.FromJson<GameInfo>(streamReader.ReadToEnd());
                    }
                    if (screenshotData != null && screenshotData.Length != 0)
                    {
                        Texture2D texture2D = (gameInfo.screenshot = MathExtensions.LoadTexture(screenshotData));
                    }
                    return gameInfo;
                }
                catch (Exception ex)
                {
                    Debug.LogWarningFormat("Exception while parsing: {0}. Using fallback gameinfo instead.", ex);
                    DateTime dateTime = new DateTime(2015, 1, 1);
                    return new GameInfo
                    {
                        version = 2,
                        gameTime = 0,
                        dateTicks = dateTime.Ticks,
                        startTicks = dateTime.Ticks,
                        changeSet = SNUtils.GetPlasticChangeSetOfBuild(0),
                        userName = Environment.UserName,
                        machineName = Environment.MachineName,
                        session = Guid.NewGuid().ToString(),
                        gameMode = GameMode.Survival,
                        screenshot = null,
                        isFallback = true
                    };
                }
            }

            public bool IsValid()
            {
                int plasticChangeSetOfBuild = SNUtils.GetPlasticChangeSetOfBuild(0);
                if (plasticChangeSetOfBuild > 0 && changeSet > plasticChangeSetOfBuild)
                {
                    Debug.LogWarning("Savegame from the future! " + changeSet + " vs. " + plasticChangeSetOfBuild);
                    return false;
                }
                return true;
            }

            public Texture2D GetScreenshot()
            {
                return screenshot;
            }
        }

        private const int currentVersion = 2;

        private long firstStart;

        private string sessionId;

        private const string gameInfoFileName = "gameinfo.json";

        private const string screenshotFileName = "screenshot.jpg";

        private string currentSlot = "test";

        private bool allowWritingFiles = true;

        private float startTimeRealSeconds;

        private float loadedGameTime;

        private DateTime lastSaveTime;

        private readonly Dictionary<string, GameInfo> gameInfoCache = new Dictionary<string, GameInfo>();

        private static SaveLoadManager _main;

        private static string temporarySavePath;

        public bool isSaving { get; private set; }

        public bool isLoading { get; private set; }

        public float timePlayedTotal => Time.realtimeSinceStartup - startTimeRealSeconds + loadedGameTime;

        private int MaxSlotsAllowed
        {
            get
            {
                if (Application.isConsolePlatform)
                {
                    return 10;
                }
                return 10000;
            }
        }

        public static SaveLoadManager main => _main;

        public void CancelSave()
        {
            if (PlatformUtils.isXboxOnePlatform)
            {
                isSaving = false;
            }
            else
            {
                Debug.LogWarning("CancelSave should not be called for any other platform.");
            }
        }

        public void SetCurrentSlot(string _currentSlot)
        {
            currentSlot = _currentSlot;
        }

        public string GetCurrentSlot()
        {
            return currentSlot;
        }

        public bool GetAllowWritingFiles()
        {
            return allowWritingFiles;
        }

        private void Awake()
        {
            if (_main != null)
            {
                Debug.LogError("Multiple SaveLoadManager instances found in scene!", this);
                Debug.Break();
                global::UnityEngine.Object.DestroyImmediate(base.gameObject);
            }
            else
            {
                _main = this;
            }
        }

        private void OnDestroy()
        {
            ClearTemporarySave();
        }

        public static string GetTemporarySavePath()
        {
            return temporarySavePath;
        }

        public static string GetTemporarySavePathRoot()
        {
            return global::Platform.IO.Path.Combine(PlatformUtils.temporaryCachePath, "TempSave");
        }

        public string StartNewSession()
        {
            sessionId = Guid.NewGuid().ToString();
            return sessionId;
        }

        public void InitializeNewGame()
        {
            startTimeRealSeconds = Time.realtimeSinceStartup;
            loadedGameTime = 0f;
            firstStart = DateTime.Now.Ticks;
            GameInfo gameInfo = GetGameInfo(currentSlot);
            if (gameInfo != null)
            {
                loadedGameTime = gameInfo.gameTime;
                firstStart = gameInfo.startTicks;
                sessionId = gameInfo.session;
            }
            ScreenshotManager.Initialize(GetTemporarySavePath());
        }

        public void Deinitialize()
        {
            gameInfoCache.Clear();
        }

        private bool SanityCheck(out string error)
        {
            if (!Application.isPlaying)
            {
                error = "Not playing";
                return false;
            }
            if (isSaving)
            {
                Debug.LogError("Attempting use while save operation in progress");
                error = "Currently saving";
                return false;
            }
            if (isLoading)
            {
                Debug.LogError("Attempting use while load operation in progress");
                error = "Currently loading";
                return false;
            }
            error = null;
            return true;
        }

        public CoroutineTask<SaveResult> SaveToDeepStorageAsync()
        {
            TaskResult<SaveResult> result = new TaskResult<SaveResult>();
            return new CoroutineTask<SaveResult>(SaveToDeepStorageAsync(result), result);
        }

        public CoroutineTask<SaveResult> SaveToTemporaryStorageAsync(Texture2D screenshot)
        {
            TaskResult<SaveResult> result = new TaskResult<SaveResult>();
            return new CoroutineTask<SaveResult>(SaveToTemporaryStorageAsync(result, screenshot), result);
        }

        private IEnumerator SaveToTemporaryStorageAsync(IOut<SaveResult> result, Texture2D screenshot)
        {
            if (!SanityCheck(out var error))
            {
                result.Set(new SaveResult(success: false, Error.InvalidCall, error));
                yield break;
            }
            isSaving = true;
            GameInfo gameInfo = new GameInfo();
            gameInfo.Initialize(timePlayedTotal, firstStart, sessionId, screenshot);
            gameInfo.seamothPresent = GameInfoIcon.Has(TechType.Seamoth);
            gameInfo.exosuitPresent = GameInfoIcon.Has(TechType.Exosuit);
            gameInfo.rocketPresent = GameInfoIcon.Has(TechType.RocketBase);
            gameInfo.cyclopsPresent = GameInfoIcon.Has(TechType.Cyclops);
            gameInfo.basePresent = GameInfoIcon.Has(TechType.BaseCorridor);
            try
            {
                GameInfo.SaveIntoCurrentSlot(gameInfo);
            }
            catch (Exception ex)
            {
                result.Set(new SaveResult(success: false, GetResultForException(ex), ex.Message));
                isSaving = false;
                yield break;
            }
            gameInfoCache[currentSlot] = gameInfo;
            LargeWorldStreamer streamer = LargeWorldStreamer.main;
            while (!streamer.IsWorldSettled())
            {
                yield return CoroutineUtils.waitForNextFrame;
            }
            streamer.frozen = true;
            CellManager cellManager = streamer.cellManager;
            yield return cellManager.IncreaseFreezeCount();
            if (Application.isEditor && PlatformUtils.isConsolePlatform)
            {
                yield return new WaitForSecondsRealtime(5f);
            }
            Player player = Player.main;
            PilotingChair playerChair = player.GetPilotingChair();
            if ((bool)playerChair)
            {
                player.ExitPilotingMode(keepCinematicState: true);
            }
            Transform playerParent = player.transform.parent;
            player.transform.parent = null;
            try
            {
                LargeWorldStreamer.main.SaveSceneObjectsIntoCurrentSlot();
            }
            catch (Exception ex2)
            {
                result.Set(new SaveResult(success: false, GetResultForException(ex2), ex2.Message));
                isSaving = false;
                yield break;
            }
            yield return null;
            try
            {
                LargeWorldStreamer.main.SaveGlobalRootIntoCurrentSlot();
            }
            catch (Exception ex3)
            {
                result.Set(new SaveResult(success: false, GetResultForException(ex3), ex3.Message));
                isSaving = false;
                yield break;
            }
            yield return null;
            try
            {
                LargeWorldStreamer.main.cellManager.SaveAllBatchCells();
            }
            catch (Exception ex4)
            {
                result.Set(new SaveResult(success: false, GetResultForException(ex4), ex4.Message));
                isSaving = false;
                yield break;
            }
            yield return null;
            player.transform.parent = playerParent;
            if ((bool)playerChair)
            {
                player.EnterPilotingMode(playerChair, keepCinematicState: true);
            }
            cellManager.DecreaseFreezeCount();
            streamer.frozen = false;
            isSaving = false;
            result.Set(new SaveResult(success: true, Error.None, null));
        }

        private IEnumerator SaveToDeepStorageAsync(IOut<SaveResult> result)
        {
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            if (!SanityCheck(out var error))
            {
                result.Set(new SaveResult(success: false, Error.InvalidCall, error));
                yield break;
            }
            isSaving = true;
            allowWritingFiles = false;
            string text = GetTemporarySavePath();
            List<string> list = new List<string>();
            try
            {
                string[] files = global::Platform.IO.Directory.GetFiles(text, "*", SearchOption.AllDirectories);
                foreach (string text2 in files)
                {
                    bool flag = PlatformUtils.isPS4Platform && text2.Contains(".jpg");
                    if (global::Platform.IO.File.GetLastWriteTime(text2) > lastSaveTime || flag)
                    {
                        string item = text2.Substring(text.Length + 1);
                        list.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                result.Set(new SaveResult(success: false, Error.UnknownError, ex.ToString()));
                allowWritingFiles = true;
                isSaving = false;
                yield break;
            }
            lastSaveTime = DateTime.Now;
            UserStorageUtils.SaveOperation saveOperation;
            try
            {
                saveOperation = userStorage.SaveFilesAsync(currentSlot, text, list);
            }
            catch (Exception ex2)
            {
                result.Set(new SaveResult(success: false, Error.UnknownError, ex2.ToString()));
                allowWritingFiles = true;
                isSaving = false;
                yield break;
            }
            yield return saveOperation;
            if (Application.isEditor && PlatformUtils.isConsolePlatform)
            {
                yield return new WaitForSecondsRealtime(5f);
            }
            if (PlatformUtils.isXboxOnePlatform)
            {
                float a = (float)saveOperation.saveDataSize / (5270718f / (float)System.Math.PI);
                a = Mathf.Max(a, 30f);
                yield return new WaitForSecondsRealtime(a);
            }
            allowWritingFiles = true;
            isSaving = false;
            Error error2 = ConvertResult(saveOperation.result);
            result.Set(new SaveResult(saveOperation.result == UserStorageUtils.Result.Success, error2, saveOperation.errorMessage));
        }

        public CoroutineTask<LoadResult> LoadSlotsAsync()
        {
            TaskResult<LoadResult> result = new TaskResult<LoadResult>();
            return new CoroutineTask<LoadResult>(LoadSlotsAsync(result), result);
        }

        private void RegisterSaveGame(string slotName, UserStorageUtils.LoadOperation loadOperation)
        {
            if (loadOperation.GetSuccessful())
            {
                byte[] value = null;
                byte[] value2 = null;
                loadOperation.files.TryGetValue("gameinfo.json", out value);
                loadOperation.files.TryGetValue("screenshot.jpg", out value2);
                GameInfo gameInfo = GameInfo.LoadFromBytes(value, value2);
                if (gameInfo == null)
                {
                    Debug.LogFormat("Skipping save directory because GameInfo failed to load: {0}", slotName);
                }
                else
                {
                    gameInfoCache[slotName] = gameInfo;
                }
            }
        }

        private IEnumerator LoadSlotsAsync(IOut<LoadResult> result)
        {
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            if (!SanityCheck(out var error))
            {
                result.Set(new LoadResult(success: false, Error.InvalidCall, error));
                yield break;
            }
            isLoading = true;
            gameInfoCache.Clear();
            UserStorageUtils.QueryOperation queryOperation = userStorage.GetContainerNamesAsync();
            yield return queryOperation;
            List<string> list = new List<string>();
            list.Add("gameinfo.json");
            list.Add("screenshot.jpg");
            UserStorageUtils.SlotsOperation slotsOperation = userStorage.LoadSlotsAsync(queryOperation.results, list);
            yield return slotsOperation;
            if (slotsOperation.GetSuccessful())
            {
                foreach (KeyValuePair<string, UserStorageUtils.LoadOperation> slot in slotsOperation.slots)
                {
                    string slotName = slot.Key;
                    UserStorageUtils.LoadOperation loadOperation = slot.Value;
                    yield return loadOperation;
                    RegisterSaveGame(slotName, loadOperation);
                }
            }
            isLoading = false;
            result.Set(new LoadResult(slotsOperation.GetSuccessful(), ConvertResult(slotsOperation.result), slotsOperation.errorMessage));
        }

        public CoroutineTask<LoadResult> LoadAsync()
        {
            TaskResult<LoadResult> result = new TaskResult<LoadResult>();
            return new CoroutineTask<LoadResult>(LoadAsync(result), result);
        }

        private IEnumerator LoadAsync(IOut<LoadResult> result)
        {
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            if (!SanityCheck(out var error))
            {
                result.Set(new LoadResult(success: false, Error.InvalidCall, error));
                yield break;
            }
            isLoading = true;
            ClearTemporarySave();
            CreateTemporarySave();
            string sourcePath = GetTemporarySavePath();
            UserStorageUtils.AsyncOperation copyOperation = userStorage.CopyFilesFromContainerAsync(currentSlot, sourcePath);
            yield return copyOperation;
            lastSaveTime = DateTime.Now;
            isLoading = false;
            result.Set(new LoadResult(copyOperation.GetSuccessful(), ConvertResult(copyOperation.result), copyOperation.errorMessage));
        }

        public GameInfo GetGameInfo(string slotName)
        {
            if (gameInfoCache.TryGetValue(slotName, out var value))
            {
                return value;
            }
            return null;
        }

        private void ClearTemporarySave()
        {
            string temporarySavePathRoot = GetTemporarySavePathRoot();
            temporarySavePath = null;
            try
            {
                if (global::Platform.IO.Directory.Exists(temporarySavePathRoot))
                {
                    global::Platform.IO.Directory.Delete(temporarySavePathRoot, recursive: true);
                }
            }
            catch (IOException exception)
            {
                Debug.LogException(exception);
            }
            catch (UnauthorizedAccessException exception2)
            {
                Debug.LogException(exception2);
            }
        }

        private static void CreateTemporarySave()
        {
            try
            {
                string temporarySavePathRoot = GetTemporarySavePathRoot();
                global::Platform.IO.Directory.CreateDirectory(temporarySavePathRoot);
                HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string[] directories = global::Platform.IO.Directory.GetDirectories(temporarySavePathRoot, "tmp*", SearchOption.TopDirectoryOnly);
                for (int i = 0; i < directories.Length; i++)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(directories[i]);
                    hashSet.Add(directoryInfo.Name);
                }
                new System.Random();
                for (int j = 0; j < 10000; j++)
                {
                    int num = global::UnityEngine.Random.Range(0, 1000000);
                    string text = $"tmp{num:000000}";
                    if (!hashSet.Contains(text))
                    {
                        temporarySavePath = global::Platform.IO.Path.Combine(temporarySavePathRoot, text);
                        global::Platform.IO.Directory.CreateDirectory(temporarySavePath);
                        global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(temporarySavePath, "BatchObjects"));
                        return;
                    }
                }
                Debug.LogErrorFormat("Failed to find unused tmp slot in '{0}' ({1} existing tmp slots)", temporarySavePathRoot, hashSet.Count);
                temporarySavePath = global::Platform.IO.Path.Combine(temporarySavePathRoot, "tmp_error");
                global::Platform.IO.Directory.CreateDirectory(temporarySavePath);
            }
            catch (IOException exception)
            {
                Debug.LogException(exception);
            }
            catch (UnauthorizedAccessException exception2)
            {
                Debug.LogException(exception2);
            }
        }

        public CoroutineTask<CreateResult> CreateSlotAsync()
        {
            TaskResult<CreateResult> result = new TaskResult<CreateResult>();
            return new CoroutineTask<CreateResult>(CreateSlotAsync(result), result);
        }

        private IEnumerator CreateSlotAsync(IOut<CreateResult> result)
        {
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            UserStorageUtils.QueryOperation queryOperation = userStorage.GetContainerNamesAsync();
            yield return queryOperation;
            if (queryOperation.results.Count >= MaxSlotsAllowed)
            {
                result.Set(new CreateResult(success: false, Error.OutOfSlots.ToString()));
                yield break;
            }
            string slotName = null;
            for (int i = 0; i < MaxSlotsAllowed; i++)
            {
                string testSlotName = $"slot{i:0000}";
                if (!queryOperation.results.Contains(testSlotName))
                {
                    UserStorageUtils.AsyncOperation createOperation = userStorage.CreateContainerAsync(testSlotName);
                    yield return createOperation;
                    if (createOperation.GetSuccessful())
                    {
                        slotName = testSlotName;
                        break;
                    }
                    if (createOperation.result == UserStorageUtils.Result.OutOfSpace)
                    {
                        break;
                    }
                }
            }
            if (slotName == null)
            {
                Debug.LogError("Could not create new slot");
            }
            ClearTemporarySave();
            CreateTemporarySave();
            lastSaveTime = DateTime.Now;
            result.Set(new CreateResult(slotName != null, slotName));
        }

        public CoroutineTask<CreateResult> SetupSlotPS4Async()
        {
            TaskResult<CreateResult> result = new TaskResult<CreateResult>();
            return new CoroutineTask<CreateResult>(SetupSlotPS4Async(result), result);
        }

        private IEnumerator SetupSlotPS4Async(IOut<CreateResult> result)
        {
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            UserStorageUtils.QueryOperation queryOperation = userStorage.GetContainerNamesAsync();
            yield return queryOperation;
            string text = null;
            if (queryOperation.GetSuccessful())
            {
                if (queryOperation.results.Count >= MaxSlotsAllowed)
                {
                    result.Set(new CreateResult(success: false, Error.OutOfSlots.ToString()));
                    yield break;
                }
                for (int i = 0; i < MaxSlotsAllowed; i++)
                {
                    string text2 = $"slot{i:0000}";
                    if (!queryOperation.results.Contains(text2))
                    {
                        ClearTemporarySave();
                        CreateTemporarySave();
                        lastSaveTime = DateTime.Now;
                        text = text2;
                        break;
                    }
                }
            }
            result.Set(new CreateResult(text != null, text));
        }

        public string[] GetActiveSlotNames()
        {
            return gameInfoCache.Keys.OrderBy((string p) => p).ToArray();
        }

        public string[] GetPossibleSlotNames()
        {
            return GetActiveSlotNames();
        }

        public GameMode[] GetPossibleSlotGameModes()
        {
            string[] activeSlotNames = GetActiveSlotNames();
            GameMode[] array = new GameMode[activeSlotNames.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = gameInfoCache[activeSlotNames[i]].gameMode;
            }
            return array;
        }

        public UserStorageUtils.AsyncOperation ClearSlotAsync(string slotName)
        {
            gameInfoCache.Remove(slotName);
            return PlatformUtils.main.GetUserStorage().DeleteContainerAsync(slotName);
        }

        public UserStorageUtils.AsyncOperation DeleteFilesInSlot(string slotName, List<string> filePaths)
        {
            return PlatformUtils.main.GetUserStorage().DeleteFilesAsync(slotName, filePaths);
        }

        private static Error ConvertResult(UserStorageUtils.Result result)
        {
            return result switch
            {
                UserStorageUtils.Result.Success => Error.None, 
                UserStorageUtils.Result.UnknownError => Error.UnknownError, 
                UserStorageUtils.Result.OutOfSpace => Error.OutOfSpace, 
                UserStorageUtils.Result.NoAccess => Error.NoAccess, 
                UserStorageUtils.Result.NotFound => Error.NotFound, 
                UserStorageUtils.Result.InvalidFormat => Error.InvalidFormat, 
                _ => Error.UnknownError, 
            };
        }

        protected static Error GetResultForException(Exception exception)
        {
            if (global::UWE.Utils.GetDiskFull(exception))
            {
                return Error.OutOfSpace;
            }
            if (exception is UnauthorizedAccessException)
            {
                return Error.NoAccess;
            }
            if (exception is DirectoryNotFoundException)
            {
                return Error.NotFound;
            }
            return Error.UnknownError;
        }
    }
}
