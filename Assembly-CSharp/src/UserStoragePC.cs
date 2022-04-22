using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class UserStoragePC : UserStorage
    {
        private class Wrapper
        {
            public readonly UserStorageUtils.AsyncOperation operation;

            public readonly string savePath;

            public readonly string containerName;

            public readonly List<string> files;

            public readonly object state;

            public Wrapper(UserStorageUtils.AsyncOperation operation, string savePath, string containerName, List<string> files = null, object state = null)
            {
                this.operation = operation;
                this.savePath = savePath;
                this.containerName = containerName;
                this.files = files;
                this.state = state;
            }
        }

        private static readonly WorkerThread ioThread = ThreadUtils.StartWorkerThread("I/O", "UserStoragePC", System.Threading.ThreadPriority.BelowNormal, -2, 10);

        private string savePath;

        private static string GetSaveFilePath(string savePath, string containerName, string relativePath)
        {
            return Path.Combine(Path.Combine(savePath, containerName), relativePath);
        }

        public UserStoragePC(string _savePath)
        {
            savePath = _savePath;
        }

        public UserStorageUtils.AsyncOperation InitializeAsync()
        {
            UserStorageUtils.AsyncOperation asyncOperation = new UserStorageUtils.AsyncOperation();
            ioThread.Enqueue(InitializeAsyncImpl, this, new Wrapper(asyncOperation, savePath, null));
            return asyncOperation;
        }

        private static void InitializeAsyncImpl(object owner, object state)
        {
            Wrapper wrapper = (Wrapper)state;
            UserStorageUtils.AsyncOperation operation = wrapper.operation;
            UserStorageUtils.Result result = UserStorageUtils.Result.Success;
            string errorMessage = null;
            if (!Directory.Exists(wrapper.savePath))
            {
                try
                {
                    Directory.CreateDirectory(wrapper.savePath);
                }
                catch (Exception ex)
                {
                    result = UserStorageUtils.GetResultForException(ex);
                    errorMessage = ex.Message;
                }
            }
            operation.SetComplete(result, errorMessage);
        }

        public UserStorageUtils.QueryOperation GetContainerNamesAsync()
        {
            UserStorageUtils.QueryOperation queryOperation = new UserStorageUtils.QueryOperation();
            ioThread.Enqueue(GetContainerNamesAsyncImpl, this, new Wrapper(queryOperation, savePath, null));
            return queryOperation;
        }

        public static void GetContainerNamesAsyncImpl(object owner, object state)
        {
            Wrapper wrapper = (Wrapper)state;
            UserStorageUtils.QueryOperation queryOperation = (UserStorageUtils.QueryOperation)wrapper.operation;
            UserStorageUtils.Result result = UserStorageUtils.Result.Success;
            string errorMessage = null;
            if (Directory.Exists(wrapper.savePath))
            {
                try
                {
                    string[] directories = Directory.GetDirectories(wrapper.savePath, "*", SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < directories.Length; i++)
                    {
                        string fileName = Path.GetFileName(directories[i]);
                        queryOperation.results.Add(fileName);
                    }
                }
                catch (Exception ex)
                {
                    result = UserStorageUtils.GetResultForException(ex);
                    errorMessage = ex.Message;
                }
            }
            queryOperation.SetComplete(result, errorMessage);
        }

        public UserStorageUtils.QueryOperation GetFileNamesAsync(string containerName)
        {
            UserStorageUtils.QueryOperation queryOperation = new UserStorageUtils.QueryOperation();
            ioThread.Enqueue(GetFileNamesAsync, this, new Wrapper(queryOperation, savePath, containerName));
            return queryOperation;
        }

        public static void GetFileNamesAsync(object owner, object state)
        {
            Wrapper obj = (Wrapper)state;
            UserStorageUtils.QueryOperation queryOperation = (UserStorageUtils.QueryOperation)obj.operation;
            string path = GetSaveFilePath(containerName: obj.containerName, savePath: obj.savePath, relativePath: string.Empty);
            UserStorageUtils.Result result = UserStorageUtils.Result.Success;
            string errorMessage = null;
            try
            {
                string[] files = Directory.GetFiles(path);
                for (int i = 0; i < files.Length; i++)
                {
                    string fileName = Path.GetFileName(files[i]);
                    queryOperation.results.Add(fileName);
                }
            }
            catch (Exception ex)
            {
                result = UserStorageUtils.GetResultForException(ex);
                errorMessage = ex.Message;
            }
            queryOperation.SetComplete(result, errorMessage);
        }

        public UserStorageUtils.LoadOperation LoadFilesAsync(string containerName, List<string> fileNames)
        {
            UserStorageUtils.LoadOperation loadOperation = new UserStorageUtils.LoadOperation();
            ioThread.Enqueue(LoadFilesAsyncImpl, this, new Wrapper(loadOperation, savePath, containerName, fileNames));
            return loadOperation;
        }

        public static void LoadFilesAsyncImpl(object owner, object state)
        {
            Wrapper obj = (Wrapper)state;
            UserStorageUtils.LoadOperation loadOperation = (UserStorageUtils.LoadOperation)obj.operation;
            string path = obj.savePath;
            string containerName = obj.containerName;
            List<string> files = obj.files;
            UserStorageUtils.Result result = UserStorageUtils.Result.Success;
            string errorMessage = null;
            if (!Directory.Exists(Path.Combine(path, containerName)))
            {
                result = UserStorageUtils.Result.UnknownError;
                errorMessage = $"Container {containerName} doesn't exist";
            }
            else
            {
                foreach (string item in files)
                {
                    byte[] value = null;
                    try
                    {
                        value = File.ReadAllBytes(GetSaveFilePath(path, containerName, item));
                    }
                    catch (Exception ex)
                    {
                        result = UserStorageUtils.GetResultForException(ex);
                        errorMessage = ex.Message;
                    }
                    loadOperation.files[item] = value;
                }
            }
            loadOperation.SetComplete(result, errorMessage);
        }

        public UserStorageUtils.SlotsOperation LoadSlotsAsync(List<string> containerNames, List<string> fileNames)
        {
            return UserStorageUtils.LoadFilesAsync(this, containerNames, fileNames);
        }

        public UserStorageUtils.SaveOperation SaveFilesAsync(string containerName, Dictionary<string, byte[]> files)
        {
            UserStorageUtils.SaveOperation saveOperation = new UserStorageUtils.SaveOperation();
            ioThread.Enqueue(SaveFilesAsyncImpl, this, new Wrapper(saveOperation, savePath, containerName, null, files));
            return saveOperation;
        }

        public static void SaveFilesAsyncImpl(object owner, object state)
        {
            Wrapper obj = (Wrapper)state;
            UserStorageUtils.SaveOperation saveOperation = (UserStorageUtils.SaveOperation)obj.operation;
            Dictionary<string, byte[]> dictionary = (Dictionary<string, byte[]>)obj.state;
            string path = obj.savePath;
            string containerName = obj.containerName;
            string path2 = Path.Combine(path, containerName);
            if (!Directory.Exists(path2))
            {
                Directory.CreateDirectory(path2);
            }
            UserStorageUtils.Result result = UserStorageUtils.Result.Success;
            string errorMessage = null;
            Dictionary<string, byte[]>.Enumerator enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                try
                {
                    string saveFilePath = GetSaveFilePath(path, containerName, enumerator.Current.Key);
                    byte[] value = enumerator.Current.Value;
                    File.WriteAllBytes(saveFilePath, value);
                }
                catch (Exception ex)
                {
                    result = UserStorageUtils.GetResultForException(ex);
                    errorMessage = ex.Message;
                }
            }
            saveOperation.SetComplete(result, errorMessage);
        }

        public UserStorageUtils.SaveOperation SaveFilesAsync(string containerName, string sourcePath, List<string> fileNames)
        {
            UserStorageUtils.SaveOperation saveOperation = new UserStorageUtils.SaveOperation();
            ioThread.Enqueue(CopyFilesAsyncImpl, this, new Wrapper(saveOperation, savePath, containerName, fileNames, sourcePath));
            return saveOperation;
        }

        public static void CopyFilesAsyncImpl(object owner, object state)
        {
            Wrapper obj = (Wrapper)state;
            UserStorageUtils.SaveOperation saveOperation = (UserStorageUtils.SaveOperation)obj.operation;
            string path = (string)obj.state;
            string text = obj.savePath;
            string containerName = obj.containerName;
            List<string> files = obj.files;
            UserStorageUtils.Result result = UserStorageUtils.Result.Success;
            string errorMessage = null;
            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    string sourceFileName = Path.Combine(path, files[i]);
                    string saveFilePath = GetSaveFilePath(text, containerName, files[i]);
                    string directoryName = Path.GetDirectoryName(saveFilePath);
                    if (!Directory.Exists(directoryName))
                    {
                        Directory.CreateDirectory(directoryName);
                    }
                    File.Copy(sourceFileName, saveFilePath, overwrite: true);
                }
                catch (Exception ex)
                {
                    result = UserStorageUtils.GetResultForException(ex);
                    errorMessage = ex.Message;
                }
            }
            saveOperation.SetComplete(result, errorMessage);
        }

        public UserStorageUtils.AsyncOperation CreateContainerAsync(string containerName)
        {
            UserStorageUtils.AsyncOperation asyncOperation = new UserStorageUtils.AsyncOperation();
            ioThread.Enqueue(CreateContainerAsyncImpl, this, new Wrapper(asyncOperation, savePath, containerName));
            return asyncOperation;
        }

        public static void CreateContainerAsyncImpl(object owner, object state)
        {
            Wrapper obj = (Wrapper)state;
            UserStorageUtils.AsyncOperation operation = obj.operation;
            string path = obj.savePath;
            string containerName = obj.containerName;
            string path2 = Path.Combine(path, containerName);
            UserStorageUtils.Result result = UserStorageUtils.Result.Success;
            string errorMessage = null;
            if (!Directory.Exists(path2))
            {
                try
                {
                    Directory.CreateDirectory(path2);
                }
                catch (Exception ex)
                {
                    result = UserStorageUtils.GetResultForException(ex);
                    errorMessage = ex.Message;
                }
            }
            operation.SetComplete(result, errorMessage);
        }

        public UserStorageUtils.AsyncOperation DeleteContainerAsync(string containerName)
        {
            UserStorageUtils.AsyncOperation asyncOperation = new UserStorageUtils.AsyncOperation();
            ioThread.Enqueue(DeleteContainerAsyncImpl, this, new Wrapper(asyncOperation, savePath, containerName));
            return asyncOperation;
        }

        public static void DeleteContainerAsyncImpl(object owner, object state)
        {
            Wrapper obj = (Wrapper)state;
            UserStorageUtils.AsyncOperation operation = obj.operation;
            string path = obj.savePath;
            string containerName = obj.containerName;
            string path2 = Path.Combine(path, containerName);
            if (Directory.Exists(path2))
            {
                Directory.Delete(path2, recursive: true);
            }
            operation.SetComplete(UserStorageUtils.Result.Success, null);
        }

        public UserStorageUtils.AsyncOperation DeleteFilesAsync(string containerName, List<string> filePaths)
        {
            UserStorageUtils.AsyncOperation asyncOperation = new UserStorageUtils.AsyncOperation();
            ioThread.Enqueue(DeleteFilesAsyncImpl, this, new Wrapper(asyncOperation, savePath, containerName, filePaths));
            return asyncOperation;
        }

        public static void DeleteFilesAsyncImpl(object owner, object state)
        {
            Wrapper obj = (Wrapper)state;
            UserStorageUtils.AsyncOperation operation = obj.operation;
            string path = obj.savePath;
            string containerName = obj.containerName;
            foreach (string file in obj.files)
            {
                string path2 = Path.Combine(Path.Combine(path, containerName), file);
                if (File.Exists(path2))
                {
                    File.Delete(path2);
                }
            }
            operation.SetComplete(UserStorageUtils.Result.Success, null);
        }

        public UserStorageUtils.CopyOperation CopyFilesFromContainerAsync(string containerName, string destinationPath)
        {
            UserStorageUtils.CopyOperation copyOperation = new UserStorageUtils.CopyOperation();
            ioThread.Enqueue(CopyFilesFromContainerAsyncImpl, this, new Wrapper(copyOperation, savePath, containerName, null, destinationPath));
            return copyOperation;
        }

        public static void CopyFilesFromContainerAsyncImpl(object owner, object state)
        {
            Wrapper obj = (Wrapper)state;
            UserStorageUtils.CopyOperation copyOperation = (UserStorageUtils.CopyOperation)obj.operation;
            string destinationPath = (string)obj.state;
            string path = obj.savePath;
            string containerName = obj.containerName;
            UserStorageUtils.Result result = UserStorageUtils.Result.Success;
            string errorMessage = null;
            string sourcePath = Path.Combine(path, containerName);
            try
            {
                global::UWE.Utils.CopyDirectory(sourcePath, destinationPath);
            }
            catch (Exception ex)
            {
                result = UserStorageUtils.GetResultForException(ex);
                errorMessage = ex.Message;
            }
            copyOperation.SetComplete(result, errorMessage);
        }

        private static bool MoveDirectoryAtomic(string oldPath, string newPath)
        {
            bool flag = false;
            try
            {
                global::UWE.Utils.CopyDirectory(oldPath, newPath);
                Directory.Delete(oldPath, recursive: true);
                flag = true;
            }
            catch (IOException message)
            {
                Debug.LogWarning(message);
            }
            if (!flag)
            {
                try
                {
                    Directory.Delete(newPath);
                    return flag;
                }
                catch (IOException message2)
                {
                    Debug.LogWarning(message2);
                    return flag;
                }
            }
            return flag;
        }

        public bool MigrateSaveData(string oldDir)
        {
            if (Directory.Exists(oldDir))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(oldDir);
                if (!Directory.Exists(savePath))
                {
                    try
                    {
                        Directory.CreateDirectory(savePath);
                    }
                    catch (IOException message)
                    {
                        Debug.LogWarning(message);
                    }
                }
                DirectoryInfo[] directories = directoryInfo.GetDirectories();
                FileInfo[] files = directoryInfo.GetFiles();
                foreach (FileInfo fileInfo in files)
                {
                    string text = Path.Combine(savePath, fileInfo.Name);
                    string sourceFileName = Path.Combine(oldDir, fileInfo.Name);
                    if (!File.Exists(text))
                    {
                        try
                        {
                            File.Move(sourceFileName, text);
                        }
                        catch (IOException message2)
                        {
                            Debug.LogWarning(message2);
                        }
                    }
                }
                DirectoryInfo[] array = directories;
                foreach (DirectoryInfo directoryInfo2 in array)
                {
                    string text2 = Path.Combine(oldDir, directoryInfo2.Name);
                    if (directoryInfo2.Name.Equals("options"))
                    {
                        try
                        {
                            Directory.Delete(text2, recursive: true);
                        }
                        catch (IOException message3)
                        {
                            Debug.LogWarning(message3);
                        }
                    }
                    else
                    {
                        string path = "migrated_" + directoryInfo2.Name;
                        string newPath = Path.Combine(savePath, path);
                        MoveDirectoryAtomic(text2, newPath);
                    }
                }
                if (global::UWE.Utils.IsDirectoryEmpty(oldDir))
                {
                    try
                    {
                        Directory.Delete(oldDir, recursive: true);
                        return true;
                    }
                    catch (IOException message4)
                    {
                        Debug.LogWarning(message4);
                    }
                }
            }
            return false;
        }
    }
}
