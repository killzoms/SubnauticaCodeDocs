using System.Collections;
using UnityEngine;

namespace UWE
{
    public class LoadingPrefabRequest : IPrefabRequest, IEnumerator
    {
        private readonly string filename;

        private readonly ResourceRequest request;

        public object Current => request;

        public LoadingPrefabRequest(string filename, ResourceRequest request)
        {
            this.filename = filename;
            this.request = request;
        }

        public bool TryGetPrefab(out GameObject result)
        {
            result = (GameObject)request.asset;
            PrefabDatabase.AddToCache(filename, result);
            return result != null;
        }

        public bool MoveNext()
        {
            return !request.isDone;
        }

        public void Reset()
        {
        }
    }
}
