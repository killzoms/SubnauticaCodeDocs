using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class DeferredSpawner : MonoBehaviour
    {
        public class Task : IEnumerator
        {
            public GameObject prefab;

            public GameObject result;

            public Vector3 position;

            public Quaternion rotation;

            public bool instantiateDeactivated;

            public object Current => null;

            public bool MoveNext()
            {
                return result == null;
            }

            public void Reset()
            {
            }

            public void Clear()
            {
                result = null;
                prefab = null;
            }
        }

        public int maxSpawnsPerFrame = 1;

        private readonly ObjectPool<Task> taskPool = ObjectPoolHelper.CreatePool<Task>(128);

        private readonly Queue<Task> lowPriorityQueue = new Queue<Task>();

        private readonly Queue<Task> highPriorityQueue = new Queue<Task>();

        public static DeferredSpawner instance { get; private set; }

        public bool enableDeferredSpawning { get; set; }

        public int InstantiateQueueCount => lowPriorityQueue.Count + highPriorityQueue.Count;

        private void Awake()
        {
            instance = this;
            enableDeferredSpawning = true;
        }

        public void Reset()
        {
            lowPriorityQueue.Clear();
            highPriorityQueue.Clear();
        }

        private void OnDestroy()
        {
            Reset();
            instance = null;
        }

        public Task InstantiateAsync(GameObject prefab, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion), bool instantiateDeactivated = false, bool highPriority = false)
        {
            Task task = taskPool.Get();
            task.prefab = prefab;
            task.position = position;
            task.rotation = rotation;
            task.instantiateDeactivated = instantiateDeactivated;
            if (enableDeferredSpawning)
            {
                (highPriority ? highPriorityQueue : lowPriorityQueue).Enqueue(task);
            }
            else
            {
                Spawn(task);
            }
            return task;
        }

        public void ReturnTask(Task task)
        {
            task.Clear();
            taskPool.Return(task);
        }

        private void Update()
        {
            int num = Process(highPriorityQueue, maxSpawnsPerFrame);
            Process(lowPriorityQueue, maxSpawnsPerFrame - num);
        }

        private int Process(Queue<Task> queue, int maxSpawned)
        {
            int i;
            for (i = 0; i < maxSpawned; i++)
            {
                if (queue.Count <= 0)
                {
                    break;
                }
                Task task = queue.Dequeue();
                if (task != null)
                {
                    Spawn(task);
                }
            }
            return i;
        }

        private void Spawn(Task task)
        {
            task.result = EditorModifications.Instantiate(task.prefab, null, task.position, task.rotation, !task.instantiateDeactivated);
        }
    }
}
