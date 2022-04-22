using System.Collections.Generic;

namespace AssemblyCSharp
{
    public class UserStoragePS4 : UserStorage
    {
        public UserStorageUtils.CopyOperation CopyFilesFromContainerAsync(string containerName, string destPath)
        {
            UserStorageUtils.CopyOperation copyOperation = new UserStorageUtils.CopyOperation();
            copyOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return copyOperation;
        }

        public UserStorageUtils.AsyncOperation CreateContainerAsync(string containerName)
        {
            UserStorageUtils.AsyncOperation asyncOperation = new UserStorageUtils.AsyncOperation();
            asyncOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return asyncOperation;
        }

        public UserStorageUtils.AsyncOperation DeleteContainerAsync(string containerName)
        {
            UserStorageUtils.AsyncOperation asyncOperation = new UserStorageUtils.AsyncOperation();
            asyncOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return asyncOperation;
        }

        public UserStorageUtils.AsyncOperation DeleteFilesAsync(string containerName, List<string> filePaths)
        {
            UserStorageUtils.AsyncOperation asyncOperation = new UserStorageUtils.AsyncOperation();
            asyncOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return asyncOperation;
        }

        public UserStorageUtils.QueryOperation GetContainerNamesAsync()
        {
            UserStorageUtils.QueryOperation queryOperation = new UserStorageUtils.QueryOperation();
            queryOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return queryOperation;
        }

        public UserStorageUtils.QueryOperation GetFileNamesAsync(string containerName)
        {
            UserStorageUtils.QueryOperation queryOperation = new UserStorageUtils.QueryOperation();
            queryOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return queryOperation;
        }

        public UserStorageUtils.AsyncOperation InitializeAsync()
        {
            UserStorageUtils.AsyncOperation asyncOperation = new UserStorageUtils.AsyncOperation();
            asyncOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return asyncOperation;
        }

        public UserStorageUtils.LoadOperation LoadFilesAsync(string containerName, List<string> fileNames)
        {
            UserStorageUtils.LoadOperation loadOperation = new UserStorageUtils.LoadOperation();
            loadOperation.SetComplete(UserStorageUtils.Result.NotFound, "File not found");
            return loadOperation;
        }

        public UserStorageUtils.SlotsOperation LoadSlotsAsync(List<string> containerNames, List<string> fileNames)
        {
            UserStorageUtils.SlotsOperation slotsOperation = new UserStorageUtils.SlotsOperation();
            slotsOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return slotsOperation;
        }

        public UserStorageUtils.SaveOperation SaveFilesAsync(string containerName, Dictionary<string, byte[]> files)
        {
            UserStorageUtils.SaveOperation saveOperation = new UserStorageUtils.SaveOperation();
            saveOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return saveOperation;
        }

        public UserStorageUtils.SaveOperation SaveFilesAsync(string containerName, string sourcePath, List<string> fileNames)
        {
            UserStorageUtils.SaveOperation saveOperation = new UserStorageUtils.SaveOperation();
            saveOperation.SetComplete(UserStorageUtils.Result.Success, null);
            return saveOperation;
        }
    }
}
