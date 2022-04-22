using System.Collections;
using System.Threading;
using UnityEngine;

namespace UWE
{
    public static class ThreadUtils
    {
        private sealed class WaitHandle : CustomYieldInstruction
        {
            private bool executed;

            public override bool keepWaiting => !executed;

            public static void Execute(object owner, object state)
            {
                ((WaitHandle)owner).executed = true;
            }
        }

        private sealed class Throttle
        {
            private readonly IThread thread;

            private readonly int timeoutMilliseconds;

            public Throttle(IThread thread, float frequency)
            {
                this.thread = thread;
                timeoutMilliseconds = (int)(1000f / frequency);
            }

            public void Execute()
            {
                Thread.Sleep(timeoutMilliseconds);
                thread.Enqueue(Step, this, null);
            }

            public static void Step(object owner, object state)
            {
                ((Throttle)owner).Execute();
            }
        }

        public static void StepCoroutine(object owner, object state)
        {
            IThread thread = (IThread)owner;
            IEnumerator enumerator = (IEnumerator)state;
            if (enumerator.MoveNext())
            {
                CallbackAwaiter callbackAwaiter = enumerator.Current as CallbackAwaiter;
                if (callbackAwaiter != null)
                {
                    callbackAwaiter.Initialize(thread, enumerator);
                }
                else
                {
                    thread.Enqueue(StepCoroutine, thread, enumerator);
                }
            }
        }

        public static void ThrottleThread(this IThread thread, float frequency)
        {
            Throttle owner = new Throttle(thread, frequency);
            thread.Enqueue(Throttle.Step, owner, null);
        }

        public static CustomYieldInstruction Wait(this IThread thread)
        {
            WaitHandle waitHandle = new WaitHandle();
            thread.Enqueue(WaitHandle.Execute, waitHandle, null);
            return waitHandle;
        }

        public static void StartCoroutine(this IThread thread, IEnumerator coroutine)
        {
            thread.Enqueue(StepCoroutine, thread, coroutine);
        }

        public static WorkerThread StartThrottledThread(string group, string name, System.Threading.ThreadPriority priority, int coreAffinityMask, int initialCapacity, float frequency)
        {
            WorkerThread workerThread = StartWorkerThread(group, name, priority, coreAffinityMask, initialCapacity);
            workerThread.ThrottleThread(frequency);
            return workerThread;
        }

        public static WorkerThread StartWorkerThread(string group, string name, System.Threading.ThreadPriority priority, int coreAffinityMask, int initialCapacity)
        {
            WorkerThread workerThread = new WorkerThread(group, name, priority, coreAffinityMask, initialCapacity);
            workerThread.Start();
            return workerThread;
        }

        public static UnityThread StartUnityThread(string name, int initialCapacity, MonoBehaviour host)
        {
            UnityThread unityThread = new UnityThread(name, initialCapacity);
            host.StartCoroutine(unityThread.Pump());
            return unityThread;
        }
    }
}
