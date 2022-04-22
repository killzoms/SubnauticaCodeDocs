using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    public class ScreenshotManager : MonoBehaviour, IScreenshotClient
    {
        private sealed class WaitForNextFrame : YieldInstruction
        {
        }

        public class Thumbnail
        {
            public string filename;

            public Texture2D texture;

            public DateTime lastWriteTimeUtc;
        }

        private class LoadingRequest
        {
            public string fileName;

            public float progress;

            public HashSet<IScreenshotClient> clients = new HashSet<IScreenshotClient>();

            private static List<IScreenshotClient> clientsTemp = new List<IScreenshotClient>();

            public Texture2D texture;

            public DateTime lastWriteTimeUtc;

            public string path { get; private set; }

            public string url { get; private set; }

            public LoadingRequest(string fileName, string url)
            {
                this.fileName = fileName;
                path = Combine(savePath, fileName);
                this.url = url;
            }

            public void AddClient(IScreenshotClient client)
            {
                if (client != null && !clients.Contains(client))
                {
                    clients.Add(client);
                }
            }

            public void RemoveClient(IScreenshotClient client)
            {
                if (client != null)
                {
                    clients.Remove(client);
                }
            }

            public bool HasClient(IScreenshotClient client)
            {
                if (client != null)
                {
                    return clients.Contains(client);
                }
                return false;
            }

            public void NotifyProgress(float progress)
            {
                clientsTemp.Clear();
                clientsTemp.AddRange(clients);
                foreach (IScreenshotClient item in clientsTemp)
                {
                    item?.OnProgress(fileName, progress);
                }
                clientsTemp.Clear();
            }

            public void NotifyDone()
            {
                clientsTemp.Clear();
                clientsTemp.AddRange(clients);
                foreach (IScreenshotClient item in clientsTemp)
                {
                    item?.OnDone(fileName, texture);
                }
                clientsTemp.Clear();
            }

            public void NotifyRemoved()
            {
                clientsTemp.Clear();
                clientsTemp.AddRange(clients);
                foreach (IScreenshotClient item in clientsTemp)
                {
                    item?.OnRemoved(fileName);
                }
                clientsTemp.Clear();
            }
        }

        public delegate void OnThumbnailAdd(string fileName, Thumbnail thumbnail);

        public delegate void OnThumbnailUpdate(string fileName, Thumbnail thumbnail);

        public delegate void OnThumbnailRemove(string fileName);

        public delegate void OnScreenshotTaken(string fileName);

        public delegate void OnScreenshotDenied();

        public const string fileProtocol = "file:///";

        public const string httpProtocol = "http://";

        public const string httpsProtocol = "https://";

        public const string screenshotExtension = "jpg";

        public const string screenshotsFolderName = "screenshots";

        public const string timeCapsuleFolderName = "timecapsules";

        private const int screenshotQuality = 100;

        private const int thumbnailWidth = 256;

        private const float loadFromDiskTimeout = 10f;

        private const float loadFromWebTimeout = 60f;

        public int maxScreenshotsConsole = 40;

        private static ScreenshotManager instance;

        private static Dictionary<string, Thumbnail> screenshotDatabase = new Dictionary<string, Thumbnail>();

        private static bool forceLinearTexture = PlatformUtils.isPS4Platform;

        private static string screenshotPattern = string.Empty;

        private static DirectoryInfo dirInfo = null;

        private static Dictionary<string, FileInfo> filesOnDisk = new Dictionary<string, FileInfo>();

        private List<string> unusedToRemove = new List<string>();

        private static List<string> loadingRequests = new List<string>();

        private static Dictionary<string, LoadingRequest> loadingResults = new Dictionary<string, LoadingRequest>();

        private static Queue<string> thumbnailingQueue = new Queue<string>();

        private Coroutine updateRoutine;

        private Coroutine screenshotRoutine;

        private bool thumbnailAddCompleted = true;

        public static string savePath { get; private set; }

        public static event OnThumbnailAdd onThumbnailAdd;

        public static event OnThumbnailUpdate onThumbnailUpdate;

        public static event OnThumbnailRemove onThumbnailRemove;

        public static event OnScreenshotTaken onScreenshotTaken;

        public static event OnScreenshotDenied onScreenshotDenied;

        private void Start()
        {
            updateRoutine = StartCoroutine(UpdateRoutine());
        }

        private IEnumerator UpdateRoutine()
        {
            IEnumerator load = LoadRoutine();
            IEnumerator thumbnailer = ThumbnailerRoutine();
            while (true)
            {
                load.MoveNext();
                if (load.Current is WaitForNextFrame)
                {
                    yield return null;
                }
                thumbnailer.MoveNext();
                yield return null;
            }
        }

        private void LateUpdate()
        {
            if (GameInput.GetButtonDown(GameInput.Button.TakePicture) && !uGUI.isLoading && !SaveLoadManager.main.isSaving)
            {
                TakeScreenshot();
            }
        }

        private void OnDestroy()
        {
            if (screenshotRoutine != null)
            {
                StopCoroutine(screenshotRoutine);
            }
            if (updateRoutine != null)
            {
                StopCoroutine(updateRoutine);
            }
            ClearThumbnailsDB();
            ScreenshotManager.onThumbnailAdd = null;
            ScreenshotManager.onThumbnailUpdate = null;
            ScreenshotManager.onThumbnailRemove = null;
            ScreenshotManager.onScreenshotTaken = null;
            loadingRequests.Clear();
            loadingResults.Clear();
            thumbnailingQueue.Clear();
            dirInfo = null;
            filesOnDisk.Clear();
            unusedToRemove.Clear();
            instance = null;
        }

        public static void Initialize(string _savePath)
        {
            if (instance != null || PlatformUtils.isSwitchPlatform)
            {
                return;
            }
            screenshotPattern = string.Format("*.{0}", "jpg");
            instance = new GameObject("ScreenshotManager").AddComponent<ScreenshotManager>();
            savePath = _savePath;
            Dictionary<string, FileInfo> screenshotFiles = GetScreenshotFiles();
            if (screenshotFiles == null)
            {
                return;
            }
            foreach (KeyValuePair<string, FileInfo> item in screenshotFiles)
            {
                AddRequest(item.Key, instance);
            }
        }

        public static string Combine(string path1, string path2)
        {
            if (path1 == null || path2 == null || HasInvalidPathChars(path1) || HasInvalidPathChars(path2))
            {
                return null;
            }
            if (path2.Length == 0)
            {
                return path1;
            }
            if (path1.Length == 0)
            {
                return path2;
            }
            if (Path.IsPathRooted(path2))
            {
                return path2;
            }
            char c = path1[path1.Length - 1];
            if (c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar && c != Path.VolumeSeparatorChar)
            {
                return $"{path1}/{path2}";
            }
            return $"{path1}{path2}";
        }

        private static bool HasInvalidPathChars(string path)
        {
            foreach (int num in path)
            {
                if (num == 34 || num == 60 || num == 62 || num == 124 || num < 32)
                {
                    return true;
                }
            }
            return false;
        }

        private static int GetRequestIndex(string fileName)
        {
            int i = 0;
            for (int count = loadingRequests.Count; i < count; i++)
            {
                if (string.Equals(loadingRequests[i], fileName))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool IsRequestCompleted(string fileName)
        {
            return GetRequestIndex(fileName) < 0;
        }

        public static void AddRequest(string fileName, IScreenshotClient client, bool highPriority = false)
        {
            AddRequest(fileName, null, client, highPriority, forceReload: false);
        }

        public static void AddRequest(string fileName, string url, IScreenshotClient client, bool highPriority = false)
        {
            AddRequest(fileName, url, client, highPriority, forceReload: false);
        }

        private static void AddRequest(string fileName, string url, IScreenshotClient client, bool highPriority, bool forceReload)
        {
            if (string.IsNullOrEmpty(fileName) || client == null)
            {
                return;
            }
            if (!loadingResults.TryGetValue(fileName, out var value))
            {
                value = new LoadingRequest(fileName, url);
                loadingResults.Add(fileName, value);
                if (highPriority)
                {
                    loadingRequests.Insert(0, fileName);
                }
                else
                {
                    loadingRequests.Add(fileName);
                }
            }
            value.AddClient(client);
            int requestIndex = GetRequestIndex(fileName);
            if (requestIndex >= 0)
            {
                if (highPriority && requestIndex != 0)
                {
                    loadingRequests.RemoveAt(requestIndex);
                    loadingRequests.Insert(0, fileName);
                }
            }
            else if (forceReload)
            {
                if (highPriority)
                {
                    loadingRequests.Insert(0, fileName);
                }
                else
                {
                    loadingRequests.Add(fileName);
                }
            }
            else
            {
                client.OnDone(fileName, value.texture);
            }
        }

        public static void RemoveRequest(string fileName, IScreenshotClient client)
        {
            if (fileName != null && client != null && loadingResults.TryGetValue(fileName, out var value))
            {
                value.RemoveClient(client);
            }
        }

        public static bool HasPendingRequests(IScreenshotClient client)
        {
            if (client == null)
            {
                return false;
            }
            Dictionary<string, LoadingRequest>.Enumerator enumerator = loadingResults.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, LoadingRequest> current = enumerator.Current;
                string key = current.Key;
                if (current.Value.HasClient(client) && !IsRequestCompleted(key))
                {
                    return true;
                }
            }
            return false;
        }

        public static void RemoveAllRequests(IScreenshotClient client)
        {
            if (client != null)
            {
                Dictionary<string, LoadingRequest>.Enumerator enumerator = loadingResults.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    enumerator.Current.Value.RemoveClient(client);
                }
            }
        }

        public static Dictionary<string, Thumbnail>.Enumerator GetThumbnails()
        {
            return screenshotDatabase.GetEnumerator();
        }

        public static Texture2D GetThumbnail(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && screenshotDatabase.TryGetValue(fileName, out var value))
            {
                return value.texture;
            }
            return null;
        }

        public static void Delete(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }
            string path = Combine(savePath, fileName);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    RemoveScreenshot(fileName);
                }
                catch (IOException ex)
                {
                    Debug.LogError("ScreenshotManager : An exception has occured on attempt to delete '" + fileName + "'\n" + ex.ToString());
                }
            }
        }

        public static bool TakeSelfie()
        {
            if ((bool)instance)
            {
                return instance.TakeScreenshot();
            }
            return false;
        }

        private static void NotifyThumbnailAdd(string fileName, Thumbnail thumbnail)
        {
            if (ScreenshotManager.onThumbnailAdd != null)
            {
                ScreenshotManager.onThumbnailAdd(fileName, thumbnail);
            }
        }

        private static void NotifyThumbnailUpdate(string fileName, Thumbnail thumbnail)
        {
            if (ScreenshotManager.onThumbnailUpdate != null)
            {
                ScreenshotManager.onThumbnailUpdate(fileName, thumbnail);
            }
        }

        private static void NotifyThumbnailRemove(string fileName)
        {
            NotificationManager.main.Remove(NotificationManager.Group.Gallery, fileName);
            if (ScreenshotManager.onThumbnailRemove != null)
            {
                ScreenshotManager.onThumbnailRemove(fileName);
            }
        }

        void IScreenshotClient.OnProgress(string fileName, float progress)
        {
        }

        void IScreenshotClient.OnDone(string fileName, Texture2D texture)
        {
            if (!thumbnailingQueue.Contains(fileName))
            {
                thumbnailingQueue.Enqueue(fileName);
            }
        }

        void IScreenshotClient.OnRemoved(string fileName)
        {
        }

        private bool TakeScreenshot()
        {
            if (!CanTakeScreenshot())
            {
                return false;
            }
            screenshotRoutine = StartCoroutine(MakeScreenshot());
            return true;
        }

        private static void EnsureScreenshotDirectory()
        {
            if (dirInfo == null)
            {
                dirInfo = new DirectoryInfo(Combine(savePath, "screenshots"));
            }
            else
            {
                dirInfo.Refresh();
            }
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
                dirInfo.Refresh();
            }
        }

        private IEnumerator MakeScreenshot()
        {
            GUIController.HidePhase hidePhase = GUIController.HidePhase.None;
            if (GUIController.main != null)
            {
                hidePhase = GUIController.main.GetHidePhase();
            }
            HideForScreenshots.Hide(HideForScreenshots.HideType.Mask | HideForScreenshots.HideType.HUD);
            EnsureScreenshotDirectory();
            yield return null;
            yield return new WaitForEndOfFrame();
            string fileName2;
            if (Application.platform == RuntimePlatform.PS4)
            {
                fileName2 = Guid.NewGuid().ToString() + ".jpg";
            }
            else
            {
                string prefix = DateTime.Now.ToString("yyyy-MM-dd_");
                fileName2 = MathExtensions.GetUniqueFileName(dirInfo, prefix, "jpg", 5, startFromOne: true, dense: true);
            }
            if (fileName2 != null)
            {
                fileName2 = Combine("screenshots", fileName2);
                string screenshotPath = Combine(savePath, fileName2);
                Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: true);
                tex.name = "ScreenshotManager.MakeScreenshot";
                tex.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
                tex.Apply();
                GUIController.SetHidePhase(hidePhase);
                if (tex != null)
                {
                    yield return null;
                    byte[] array = tex.EncodeToJPG(100);
                    if (array.Length != 0)
                    {
                        File.WriteAllBytes(screenshotPath, array);
                        Debug.Log("Captured screenshot and saved to '" + screenshotPath + "'");
                    }
                    global::UnityEngine.Object.Destroy(tex);
                }
                NotificationManager.main.Add(NotificationManager.Group.Gallery, fileName2, 3f);
                AddRequest(fileName2, this);
                if (ScreenshotManager.onScreenshotTaken != null)
                {
                    ScreenshotManager.onScreenshotTaken(fileName2);
                }
                thumbnailAddCompleted = false;
            }
            else
            {
                GUIController.SetHidePhase(hidePhase);
            }
            screenshotRoutine = null;
        }

        public static bool HasScreenshotForFile(string filename)
        {
            if (filesOnDisk.ContainsKey(filename))
            {
                return true;
            }
            if (screenshotDatabase.ContainsKey(filename))
            {
                return true;
            }
            return false;
        }

        private static Dictionary<string, FileInfo> GetScreenshotFiles()
        {
            ProfilingUtils.BeginSample("GetScreenshotFiles");
            try
            {
                EnsureScreenshotDirectory();
                if (!dirInfo.Exists)
                {
                    return null;
                }
                filesOnDisk.Clear();
                ProfilingUtils.BeginSample("GetScreenshotFiles-GetFiles");
                FileInfo[] files = dirInfo.GetFiles(screenshotPattern, SearchOption.TopDirectoryOnly);
                ProfilingUtils.EndSample();
                int i = 0;
                for (int num = files.Length; i < num; i++)
                {
                    FileInfo fileInfo = files[i];
                    string key = Combine("screenshots", fileInfo.Name);
                    filesOnDisk.Add(key, fileInfo);
                }
                files = null;
                return filesOnDisk;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        private static void RemoveScreenshot(string filename)
        {
            if (screenshotDatabase.TryGetValue(filename, out var value))
            {
                Texture2D texture = value.texture;
                if (texture != null)
                {
                    global::UnityEngine.Object.Destroy(texture);
                }
                value.texture = null;
                screenshotDatabase.Remove(filename);
                filesOnDisk.Remove(filename);
                NotifyThumbnailRemove(filename);
            }
            if (loadingResults.TryGetValue(filename, out var value2))
            {
                value2.NotifyRemoved();
                loadingResults.Remove(filename);
            }
            loadingRequests.Remove(filename);
        }

        private IEnumerator LoadRoutine()
        {
            while (true)
            {
                ProfilingUtils.BeginSample("ReleaseUnused");
                ReleaseUnused();
                ProfilingUtils.EndSample();
                if (loadingRequests.Count == 0)
                {
                    yield return null;
                    continue;
                }
                string fileName = loadingRequests[0];
                if (loadingResults.TryGetValue(fileName, out var request))
                {
                    string url = null;
                    float num = 10f;
                    bool saveToDisk = false;
                    FileInfo fileInfo = new FileInfo(request.path);
                    if (fileInfo.Exists)
                    {
                        url = "file:///" + request.path;
                        num = 10f;
                        saveToDisk = PlatformUtils.isPS4Platform;
                    }
                    else if (!string.IsNullOrEmpty(request.url))
                    {
                        url = request.url;
                        num = 60f;
                        string extension = Path.GetExtension(fileName);
                        saveToDisk = extension == ".jpg" || extension == ".jpeg" || extension == ".png";
                    }
                    if (!string.IsNullOrEmpty(url))
                    {
                        float unscaledTime = Time.unscaledTime;
                        float timeEnd = unscaledTime + num;
                        using WWW www = new WWW(url);
                        bool timedOut = false;
                        while (!www.isDone)
                        {
                            yield return null;
                            if (Time.unscaledTime > timeEnd)
                            {
                                timedOut = true;
                                break;
                            }
                            request.NotifyProgress(www.progress);
                        }
                        request.NotifyProgress(www.progress);
                        if (!timedOut && string.IsNullOrEmpty(www.error) && www.bytesDownloaded > 0)
                        {
                            if (saveToDisk)
                            {
                                yield return null;
                                string directoryName = Path.GetDirectoryName(fileName);
                                DirectoryInfo directoryInfo = new DirectoryInfo(Combine(savePath, directoryName));
                                if (!directoryInfo.Exists)
                                {
                                    directoryInfo.Create();
                                    directoryInfo.Refresh();
                                }
                                File.WriteAllBytes(request.path, www.bytes);
                                fileInfo.Refresh();
                            }
                            yield return null;
                            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGB24, mipChain: false, forceLinearTexture);
                            texture2D.name = "ScreenshotManager.LoadRoutine";
                            texture2D.wrapMode = TextureWrapMode.Clamp;
                            texture2D.LoadImage(www.bytes, markNonReadable: false);
                            request.texture = texture2D;
                        }
                        else
                        {
                            Debug.LogError($"Failed to read '{url}' with error '{www.error}'");
                        }
                    }
                    if (fileInfo.Exists)
                    {
                        request.lastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                    }
                }
                loadingRequests.Remove(fileName);
                if (!loadingResults.ContainsKey(fileName))
                {
                    loadingResults.Add(fileName, request);
                }
                request.NotifyDone();
                yield return new WaitForNextFrame();
                request = null;
            }
        }

        private IEnumerator ThumbnailerRoutine()
        {
            while (true)
            {
                if (thumbnailingQueue.Count == 0)
                {
                    yield return null;
                    continue;
                }
                ProfilingUtils.BeginSample("ProcessThumbnail");
                string text = thumbnailingQueue.Dequeue();
                Thumbnail value;
                bool num = screenshotDatabase.TryGetValue(text, out value);
                if (!num)
                {
                    value = new Thumbnail();
                }
                Texture2D texture = value.texture;
                if (texture != null)
                {
                    global::UnityEngine.Object.Destroy(texture);
                }
                value.texture = null;
                value.filename = text;
                bool flag = false;
                if (loadingResults.TryGetValue(text, out var value2))
                {
                    value.lastWriteTimeUtc = value2.lastWriteTimeUtc;
                    Texture2D texture2 = value2.texture;
                    if (texture2 != null)
                    {
                        value.texture = MathExtensions.ScaleTexture(texture2, 256, mipmap: false, forceLinearTexture);
                        flag = true;
                    }
                }
                if (num)
                {
                    NotifyThumbnailUpdate(text, value);
                }
                else
                {
                    screenshotDatabase.Add(text, value);
                    filesOnDisk.Remove(text);
                    NotifyThumbnailAdd(text, value);
                    instance.thumbnailAddCompleted = true;
                }
                RemoveRequest(text, this);
                ProfilingUtils.EndSample();
                yield return flag ? new WaitForNextFrame() : null;
            }
        }

        private void ClearThumbnailsDB()
        {
            RemoveAllRequests(this);
            Dictionary<string, Thumbnail>.Enumerator enumerator = screenshotDatabase.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, Thumbnail> current = enumerator.Current;
                string key = current.Key;
                Thumbnail value = current.Value;
                NotifyThumbnailRemove(key);
                Texture2D texture = value.texture;
                if (texture != null)
                {
                    global::UnityEngine.Object.Destroy(texture);
                }
                value.texture = null;
            }
            screenshotDatabase.Clear();
        }

        private void ReleaseUnused()
        {
            if (loadingResults.Count == 0)
            {
                return;
            }
            Dictionary<string, LoadingRequest>.Enumerator enumerator = loadingResults.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<string, LoadingRequest> current = enumerator.Current;
                LoadingRequest value = current.Value;
                if (value.clients.Count == 0)
                {
                    Texture2D texture = value.texture;
                    if (texture != null)
                    {
                        global::UnityEngine.Object.Destroy(texture);
                    }
                    value.texture = null;
                    unusedToRemove.Add(current.Key);
                }
            }
            int i = 0;
            for (int count = unusedToRemove.Count; i < count; i++)
            {
                string text = unusedToRemove[i];
                loadingResults.Remove(text);
                loadingRequests.Remove(text);
            }
            unusedToRemove.Clear();
        }

        public static bool ShareScreenshot(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }
            string fileName2 = Combine(savePath, fileName);
            return PlatformUtils.main.GetServices().ShareScreenshot(fileName2);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        private static void DebugPrintDatabase()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("ScreenshotDatabase {0} entries\n", screenshotDatabase.Count);
            foreach (KeyValuePair<string, Thumbnail> item in screenshotDatabase)
            {
                stringBuilder.AppendFormat("    Key {0} / filename {1}  has texture {2} \n", item.Key, item.Value.filename, item.Value.texture ? "Y" : "N");
            }
            stringBuilder.AppendFormat("filesOnDisk {0} entries\n", filesOnDisk.Count);
            foreach (KeyValuePair<string, FileInfo> item2 in filesOnDisk)
            {
                stringBuilder.AppendFormat("    Key {0} / FileInfo name {1} \n", item2.Key, item2.Value.Name);
            }
            Debug.Log(stringBuilder.ToString());
        }

        private string GetDebugInfo()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("filesOnDisk.Count = {0}\n", filesOnDisk.Count.ToString());
            foreach (KeyValuePair<string, FileInfo> item in filesOnDisk)
            {
                stringBuilder.Append(item.Key);
                stringBuilder.Append('\n');
            }
            stringBuilder.Append('\n');
            stringBuilder.AppendFormat("thumbnailingQueue.Count = {0}\n", thumbnailingQueue.Count.ToString());
            foreach (string item2 in thumbnailingQueue)
            {
                stringBuilder.Append(item2);
                stringBuilder.Append('\n');
            }
            stringBuilder.Append('\n');
            stringBuilder.AppendFormat("loadingRequests.Count = {0}\n", loadingRequests.Count.ToString());
            foreach (string loadingRequest in loadingRequests)
            {
                stringBuilder.Append(loadingRequest);
                stringBuilder.Append('\n');
            }
            stringBuilder.Append('\n');
            stringBuilder.AppendFormat("loadingResults.Count = {0}\n", loadingResults.Count.ToString());
            foreach (KeyValuePair<string, LoadingRequest> loadingResult in loadingResults)
            {
                stringBuilder.Append(loadingResult.Key);
                stringBuilder.Append('\n');
            }
            stringBuilder.Append('\n');
            stringBuilder.AppendFormat("screenshotDatabase.Count = {0}\n", screenshotDatabase.Count.ToString());
            foreach (KeyValuePair<string, Thumbnail> item3 in screenshotDatabase)
            {
                Thumbnail value = item3.Value;
                stringBuilder.AppendFormat("{0} {1}\n", item3.Key, value.lastWriteTimeUtc.ToString());
            }
            return stringBuilder.ToString();
        }

        public static int GetNumScreenshots()
        {
            if (screenshotDatabase != null)
            {
                return screenshotDatabase.Count;
            }
            return 0;
        }

        public static int GetMaxNumScreenshots()
        {
            return instance.maxScreenshotsConsole;
        }

        public static bool CanTakeScreenshot()
        {
            if (uGUI.isIntro)
            {
                return false;
            }
            if (instance.screenshotRoutine != null || !instance.thumbnailAddCompleted)
            {
                return false;
            }
            if (IsLimitingScreenhots() && GetNumScreenshots() >= GetMaxNumScreenshots())
            {
                if (ScreenshotManager.onScreenshotDenied != null)
                {
                    ScreenshotManager.onScreenshotDenied();
                }
                return false;
            }
            return true;
        }

        public static bool IsLimitingScreenhots()
        {
            return PlatformUtils.isConsolePlatform;
        }

        public static bool IsScreenshotBeingRequested(string fileName)
        {
            return !IsRequestCompleted(fileName);
        }
    }
}
