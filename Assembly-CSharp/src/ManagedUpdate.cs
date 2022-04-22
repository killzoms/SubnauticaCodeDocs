using System;
using System.Collections.Generic;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    public class ManagedUpdate : MonoBehaviour
    {
        public delegate void OnUpdate();

        [SuppressMessage("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
        public enum Queue
        {
            First,
            UpdateBeforeInput,
            UpdateInput,
            UpdateAfterInput,
            LateUpdateBeforeInput,
            LateUpdateInput,
            LateUpdateCamera,
            LateUpdateAfterInput,
            Canvas,
            Drag,
            Ping,
            Selection,
            Last
        }

        public class QueueComparer : IEqualityComparer<Queue>
        {
            public bool Equals(Queue x, Queue y)
            {
                int num = (int)x;
                return num.Equals((int)y);
            }

            public int GetHashCode(Queue obj)
            {
                return (int)obj;
            }
        }

        private static ManagedUpdate _main;

        public static readonly QueueComparer sQueueComparer = new QueueComparer();

        private Dictionary<Queue, List<OnUpdate>> subscribers = new Dictionary<Queue, List<OnUpdate>>(sQueueComparer);

        public static ManagedUpdate main
        {
            get
            {
                if (_main == null)
                {
                    GameObject obj = new GameObject("ManagedUpdate");
                    _main = obj.AddComponent<ManagedUpdate>();
                    global::UnityEngine.Object.DontDestroyOnLoad(obj);
                }
                return _main;
            }
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases += OnWillRenderCanvases;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
        }

        private void Update()
        {
            Execute(Queue.UpdateBeforeInput);
            Execute(Queue.UpdateInput);
            Execute(Queue.UpdateAfterInput);
        }

        private void LateUpdate()
        {
            Execute(Queue.LateUpdateBeforeInput);
            Execute(Queue.LateUpdateInput);
            Execute(Queue.LateUpdateCamera);
            Execute(Queue.LateUpdateAfterInput);
        }

        public static void Subscribe(Queue queue, OnUpdate action)
        {
            if (action != null)
            {
                if (!main.subscribers.TryGetValue(queue, out var value))
                {
                    value = new List<OnUpdate>();
                    main.subscribers.Add(queue, value);
                }
                value.Add(action);
            }
        }

        public static void Unsubscribe(Queue queue, OnUpdate action)
        {
            if (main.subscribers.TryGetValue(queue, out var value) && value.Remove(action) && value.Count == 0)
            {
                main.subscribers.Remove(queue);
            }
        }

        public static void Unsubscribe(OnUpdate action)
        {
            int i = 0;
            for (int num = 12; i < num; i++)
            {
                Unsubscribe((Queue)i, action);
            }
        }

        private void OnWillRenderCanvases()
        {
            int i = 8;
            for (int num = 12; i < num; i++)
            {
                Queue queue = (Queue)i;
                Execute(queue);
            }
        }

        private void Execute(Queue queue)
        {
            if (!subscribers.TryGetValue(queue, out var value))
            {
                return;
            }
            for (int num = value.Count - 1; num >= 0; num--)
            {
                OnUpdate onUpdate = value[num];
                if (onUpdate != null)
                {
                    try
                    {
                        onUpdate();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex.ToString());
                    }
                }
                else
                {
                    value.RemoveAt(num);
                }
            }
            if (value.Count == 0)
            {
                subscribers.Remove(queue);
            }
        }
    }
}
