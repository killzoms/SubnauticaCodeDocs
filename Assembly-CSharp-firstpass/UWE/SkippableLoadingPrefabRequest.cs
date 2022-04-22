using System.Collections;
using UnityEngine;

namespace UWE
{
    public class SkippableLoadingPrefabRequest : IPrefabRequest, IEnumerator, ISkippableRequest
    {
        private readonly string filename;

        private IPrefabRequest request;

        public object Current
        {
            get
            {
                LazyInitializeAsyncRequest();
                return request.Current;
            }
        }

        public SkippableLoadingPrefabRequest(string filename)
        {
            this.filename = filename;
        }

        public bool TryGetPrefab(out GameObject prefab)
        {
            LazyInitializeSyncRequest();
            return request.TryGetPrefab(out prefab);
        }

        public bool MoveNext()
        {
            LazyInitializeAsyncRequest();
            return request.MoveNext();
        }

        public void Reset()
        {
        }

        public void LazyInitializeSyncRequest()
        {
            if (request == null)
            {
                GameObject prefab = Resources.Load<GameObject>(filename);
                request = PrefabDatabase.AddToCache(filename, prefab);
            }
        }

        public void LazyInitializeAsyncRequest()
        {
            if (request == null)
            {
                ResourceRequest resourceRequest = Resources.LoadAsync<GameObject>(filename);
                request = new LoadingPrefabRequest(filename, resourceRequest);
            }
        }

        public override string ToString()
        {
            return $"Load prefab '{filename}'";
        }
    }
}
