using System.Collections.Generic;
using UnityEngine;

namespace UWE
{
    public static class GameObjectPool
    {
        public interface IPooledObject
        {
            void Spawn(float time = 0f, bool active = true);

            void Despawn(float time = 0f);
        }

        public class QueueInfo
        {
            public Transform parent;

            public Queue<GameObject> queue;

            public string name;

            public int id;

            public int instanceCount;

            public int hitCount;

            public float lastHitTime;

            public string prefabId;

            public LinkedListNode<QueueInfo> listNode;
        }

        public const string ObjectPoolAssetPath = "Assets/Resources/GameObjectPools.asset";

        private const int skMinObjectForPool = 5;

        private const float skStalePoolTime = 120f;

        private static Dictionary<int, QueueInfo> sPoolMap = new Dictionary<int, QueueInfo>();

        private static int sHitCount;

        private static int sInstanceCount;

        private static bool sDumpEnabled = true;

        private static GameObject sPoolParentObject;

        private static int sCurrentObjectCount = 0;

        private static List<IPooledObject> sTempObjCache = new List<IPooledObject>();

        private static List<PooledMonoBehaviour> sTempComponentCache = new List<PooledMonoBehaviour>();

        private static LinkedList<QueueInfo> sQueueList = new LinkedList<QueueInfo>();

        public static Dictionary<int, QueueInfo> PoolMap => sPoolMap;

        public static int HitCount
        {
            get
            {
                return sHitCount;
            }
            set
            {
                sHitCount = value;
            }
        }

        public static int InstanceCount => sInstanceCount;

        public static bool DumpEnabled
        {
            get
            {
                return sDumpEnabled;
            }
            set
            {
                sDumpEnabled = value;
            }
        }

        public static GameObject Instantiate(GameObject pooledInstancePrefab, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion), bool active = true)
        {
            ProfilingUtils.BeginSample("[GameObjectPool::Instantiate]");
            GameObject gameObject = null;
            CheckPoolValid();
            PooledMonoBehaviour component = pooledInstancePrefab.GetComponent<PooledMonoBehaviour>();
            int num = Animator.StringToHash(pooledInstancePrefab.name);
            if (component != null)
            {
                PooledMonoBehaviour pooledMonoBehaviour = null;
                ProfilingUtils.BeginSample("[GameObjectPool::Instantiate::SearchQueueMap]");
                QueueInfo value = null;
                if (sPoolMap.TryGetValue(num, out value) && value.queue.Count > 0)
                {
                    gameObject = value.queue.Dequeue();
                    while (gameObject == null && value.queue.Count > 0)
                    {
                        gameObject = value.queue.Dequeue();
                    }
                    if (gameObject != null)
                    {
                        sHitCount++;
                        value.hitCount++;
                        value.lastHitTime = Time.timeSinceLevelLoad;
                        sQueueList.Remove(value.listNode);
                        sQueueList.AddFirst(value);
                        value.listNode = sQueueList.First;
                        if (Application.isEditor)
                        {
                            value.parent.SetAsFirstSibling();
                        }
                        pooledMonoBehaviour = gameObject.GetComponent<PooledMonoBehaviour>();
                    }
                }
                ProfilingUtils.EndSample();
                if (gameObject == null)
                {
                    ProfilingUtils.BeginSample("[GameObjectPool::Instantiate::CreateNewObject]");
                    gameObject = CreateNewInstance(pooledInstancePrefab, position, rotation, active);
                    pooledMonoBehaviour = gameObject.GetComponent<PooledMonoBehaviour>();
                    if (pooledMonoBehaviour != null && (pooledMonoBehaviour.AlwaysPool || GameObjectPoolPrefabMap.Map.Count == 0 || GameObjectPoolPrefabMap.Map.ContainsKey(num)))
                    {
                        sInstanceCount++;
                        pooledMonoBehaviour.PoolQueueID = num;
                        pooledMonoBehaviour.CacheComponents();
                        if (value != null)
                        {
                            value.instanceCount++;
                            value.lastHitTime = Time.timeSinceLevelLoad;
                            value.parent.SetAsFirstSibling();
                            sQueueList.Remove(value.listNode);
                            sQueueList.AddFirst(value);
                            value.listNode = sQueueList.First;
                        }
                        else
                        {
                            ProfilingUtils.BeginSample("[GameObjectPool::Instantiate::CreateNewQueue]");
                            string name = pooledMonoBehaviour.gameObject.name.Replace("(Clone)", string.Empty);
                            PrefabIdentifier component2 = pooledInstancePrefab.GetComponent<PrefabIdentifier>();
                            value = new QueueInfo();
                            value.instanceCount = 1;
                            value.name = name;
                            value.id = num;
                            value.queue = new Queue<GameObject>();
                            value.parent = new GameObject(value.name).transform;
                            value.parent.gameObject.SetActive(value: false);
                            value.parent.parent = sPoolParentObject.transform;
                            value.lastHitTime = Time.timeSinceLevelLoad;
                            value.prefabId = ((component2 != null) ? component2.ClassId : value.name);
                            if (Application.isEditor)
                            {
                                value.parent.SetAsFirstSibling();
                            }
                            sPoolMap[num] = value;
                            sQueueList.AddFirst(value);
                            value.listNode = sQueueList.First;
                            ProfilingUtils.EndSample();
                        }
                    }
                    ProfilingUtils.EndSample();
                }
                gameObject.transform.SetParent(null, worldPositionStays: false);
                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
                ProfilingUtils.BeginSample("[GameObjectPool::Instantiate::NotifySpawn]");
                if (pooledMonoBehaviour != null && pooledMonoBehaviour.PooledObjectCache != null)
                {
                    for (int i = 0; i < pooledMonoBehaviour.PooledObjectCache.Count; i++)
                    {
                        pooledMonoBehaviour.PooledObjectCache[i].Spawn(0f, active);
                    }
                }
                ProfilingUtils.EndSample();
            }
            else
            {
                gameObject = CreateNewInstance(pooledInstancePrefab, position, rotation, active);
            }
            if (sQueueList.Last != null && sDumpEnabled && Time.timeSinceLevelLoad - sQueueList.Last.Value.lastHitTime > 120f)
            {
                ProfilingUtils.BeginSample("[GameObjectPool::Instantiate::DestroyOldQueue]");
                QueueInfo value2 = sQueueList.Last.Value;
                sPoolMap.Remove(value2.id);
                sQueueList.Remove(value2.listNode);
                Object.Destroy(value2.parent.gameObject);
                ProfilingUtils.EndSample();
            }
            ProfilingUtils.EndSample();
            return gameObject;
        }

        private static GameObject CreateNewInstance(GameObject prefab, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion), bool active = true)
        {
            ProfilingUtils.BeginSample("[GameObjectPool::CreateNewInstance]");
            GameObject result = EditorModifications.Instantiate(prefab, null, position, rotation, active);
            ProfilingUtils.EndSample();
            return result;
        }

        public static void Return(GameObject Instance, float time = 0f)
        {
            PooledMonoBehaviour component = Instance.GetComponent<PooledMonoBehaviour>();
            Instance.GetComponentsInChildren(sTempComponentCache);
            for (int i = 0; i < sTempComponentCache.Count; i++)
            {
                Return(sTempComponentCache[i], time);
            }
            if (component == null)
            {
                Object.Destroy(Instance, time);
            }
        }

        public static void Return(PooledMonoBehaviour Instance, float time = 0f)
        {
            ProfilingUtils.BeginSample("[GameObjectPool::Return]");
            CheckPoolValid();
            if (Instance != null)
            {
                Instance.GetComponents(sTempObjCache);
                for (int i = 0; i < sTempObjCache.Count; i++)
                {
                    sTempObjCache[i].Despawn(time);
                }
                if (time <= 0f)
                {
                    if (Instance.PoolQueueID == -1 || !sPoolMap.TryGetValue(Instance.PoolQueueID, out var value))
                    {
                        Object.Destroy(Instance.gameObject);
                    }
                    else
                    {
                        value.queue.Enqueue(Instance.gameObject);
                        value.lastHitTime = Time.timeSinceLevelLoad;
                        if (Application.isEditor && value.parent != null)
                        {
                            value.parent.SetAsFirstSibling();
                        }
                        Instance.transform.parent = value.parent;
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GameObjectPool::Return] trying to return null component!");
            }
            ProfilingUtils.EndSample();
        }

        public static void ClearPools()
        {
            foreach (KeyValuePair<int, QueueInfo> item in sPoolMap)
            {
                while (item.Value.queue != null && item.Value.queue.Count > 0)
                {
                    GameObject gameObject = item.Value.queue.Dequeue();
                    if (gameObject != null && (bool)gameObject)
                    {
                        Object.Destroy(gameObject);
                    }
                }
                if (item.Value.parent != null)
                {
                    Object.Destroy(item.Value.parent.gameObject);
                }
            }
            sPoolMap.Clear();
            sQueueList.Clear();
            sInstanceCount = 0;
            Object.Destroy(sPoolParentObject);
            sPoolParentObject = null;
        }

        public static void WarmPools()
        {
            CheckPoolValid();
            Dictionary<int, GameObjectPoolPrefabMap.prefabInfo>.Enumerator enumerator = GameObjectPoolPrefabMap.Map.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Value.count <= 5)
                {
                    continue;
                }
                PrefabDatabase.TryGetPrefab(enumerator.Current.Value.prefabId, out var prefab);
                if (!(prefab != null) || !(prefab.GetComponent<PooledMonoBehaviour>() != null))
                {
                    continue;
                }
                int num = Animator.StringToHash(prefab.name);
                PooledMonoBehaviour pooledMonoBehaviour = null;
                if (sPoolMap.TryGetValue(num, out var value))
                {
                    if (value.queue.Count >= enumerator.Current.Value.count)
                    {
                        continue;
                    }
                    int num2 = enumerator.Current.Value.count - value.queue.Count;
                    for (int i = 0; i < num2; i++)
                    {
                        pooledMonoBehaviour = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<PooledMonoBehaviour>();
                        if (pooledMonoBehaviour != null)
                        {
                            sInstanceCount++;
                            pooledMonoBehaviour.PoolQueueID = num;
                            pooledMonoBehaviour.CacheComponents();
                            Return(pooledMonoBehaviour);
                        }
                    }
                    continue;
                }
                PrefabIdentifier component = prefab.GetComponent<PrefabIdentifier>();
                value = new QueueInfo();
                value.instanceCount = 1;
                value.name = prefab.name;
                value.id = num;
                value.queue = new Queue<GameObject>();
                value.parent = new GameObject(value.name).transform;
                value.parent.parent = sPoolParentObject.transform;
                value.parent.gameObject.SetActive(value: false);
                value.lastHitTime = Time.timeSinceLevelLoad;
                value.prefabId = ((component != null) ? component.ClassId : value.name);
                sPoolMap[num] = value;
                sQueueList.AddFirst(value);
                value.listNode = sQueueList.First;
                for (int j = 0; j < enumerator.Current.Value.count; j++)
                {
                    pooledMonoBehaviour = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<PooledMonoBehaviour>();
                    if (pooledMonoBehaviour != null)
                    {
                        sInstanceCount++;
                        pooledMonoBehaviour.PoolQueueID = num;
                        pooledMonoBehaviour.CacheComponents();
                        Return(pooledMonoBehaviour);
                    }
                }
            }
        }

        private static void CheckPoolValid()
        {
            if (sPoolParentObject == null)
            {
                sPoolParentObject = new GameObject("[ObjectPool]");
            }
        }
    }
}
