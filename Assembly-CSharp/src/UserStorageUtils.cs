using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace AssemblyCSharp
{
    public static class UserStorageUtils
    {
        public enum Result
        {
            Success,
            UnknownError,
            OutOfSpace,
            NoAccess,
            NotFound,
            InvalidFormat,
            OutOfSlots
        }

        public class AsyncOperation : IEnumerator
        {
            public Result result;

            public string errorMessage;

            public bool done;

            public object Current => null;

            public bool MoveNext()
            {
                return !done;
            }

            public void SetComplete(Result _result, string _errorMessage)
            {
                result = _result;
                errorMessage = _errorMessage;
                done = true;
            }

            public void Reset()
            {
            }

            public bool GetSuccessful()
            {
                return result == Result.Success;
            }
        }

        public class QueryOperation : AsyncOperation
        {
            public readonly List<string> results = new List<string>();
        }

        public class LoadOperation : AsyncOperation
        {
            public readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        }

        public class SlotsOperation : AsyncOperation
        {
            public readonly Dictionary<string, LoadOperation> slots = new Dictionary<string, LoadOperation>();
        }

        public class SaveOperation : AsyncOperation
        {
            public int saveDataSize;
        }

        public class CopyOperation : AsyncOperation
        {
        }

        public static Result GetResultForException(Exception exception)
        {
            if (global::UWE.Utils.GetDiskFull(exception))
            {
                return Result.OutOfSpace;
            }
            if (exception is UnauthorizedAccessException)
            {
                return Result.NoAccess;
            }
            if (exception is DirectoryNotFoundException)
            {
                return Result.NotFound;
            }
            return Result.UnknownError;
        }

        public static SlotsOperation LoadFilesAsync(UserStorage userStorage, List<string> containerNames, List<string> fileNames)
        {
            SlotsOperation slotsOperation = new SlotsOperation();
            foreach (string containerName in containerNames)
            {
                slotsOperation.slots[containerName] = userStorage.LoadFilesAsync(containerName, fileNames);
            }
            slotsOperation.SetComplete(Result.Success, null);
            return slotsOperation;
        }
    }
}
