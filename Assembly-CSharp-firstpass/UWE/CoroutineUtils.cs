using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace UWE
{
    public static class CoroutineUtils
    {
        public class PumpCoroutineStateMachine : StateMachineBase<object>
        {
            private readonly Stopwatch workingWatch = new Stopwatch();

            private readonly Stack<IEnumerator> workingStack = new Stack<IEnumerator>();

            private string profileSampleName;

            private float maxFrameMs;

            public void Initialize(IEnumerator coroutine, string profileSampleName, float maxFrameMs)
            {
                this.profileSampleName = profileSampleName;
                this.maxFrameMs = maxFrameMs;
                workingStack.Clear();
                workingStack.Push(coroutine);
            }

            public void SetMaxFrameMs(float maxFrameMs)
            {
                this.maxFrameMs = maxFrameMs;
            }

            public override bool MoveNext()
            {
                workingWatch.Restart();
                while (workingStack.Count > 0)
                {
                    if (Utils.GetTimeElapsedMS(workingWatch) > maxFrameMs)
                    {
                        current = null;
                        return true;
                    }
                    IEnumerator enumerator = workingStack.Peek();
                    if (!enumerator.SafeMoveNext(profileSampleName))
                    {
                        workingStack.Pop();
                        continue;
                    }
                    object obj = enumerator.Current;
                    IEnumerator enumerator2 = obj as IEnumerator;
                    if (enumerator2 != null)
                    {
                        workingStack.Push(enumerator2);
                        continue;
                    }
                    YieldInstruction yieldInstruction = obj as YieldInstruction;
                    if (yieldInstruction == null && obj != waitForNextFrame)
                    {
                        continue;
                    }
                    current = yieldInstruction;
                    return true;
                }
                return false;
            }

            public override void Reset()
            {
                workingWatch.Stop();
                workingStack.Clear();
            }
        }

        public static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

        public static readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

        public static readonly object waitForNextFrame = new object();

        private static readonly StateMachinePool<PumpCoroutineStateMachine, object> timedPumpCoroutines = new StateMachinePool<PumpCoroutineStateMachine, object>();

        public static void PumpCoroutine(IEnumerator coroutine)
        {
            if (coroutine == null)
            {
                return;
            }
            Stack<IEnumerator> stack = new Stack<IEnumerator>();
            stack.Push(coroutine);
            while (stack.Count > 0)
            {
                IEnumerator enumerator = stack.Peek();
                ISkippableRequest skippableRequest = enumerator as ISkippableRequest;
                if (skippableRequest != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Debug.LogWarningFormat("PumpCoroutine force syncing skippable request ({0}, '{1}')", skippableRequest.GetType(), skippableRequest);
                    }
                    skippableRequest.LazyInitializeSyncRequest();
                }
                if (!enumerator.SafeMoveNext())
                {
                    stack.Pop();
                    continue;
                }
                object current = enumerator.Current;
                IEnumerator enumerator2 = current as IEnumerator;
                if (enumerator2 != null)
                {
                    stack.Push(enumerator2);
                }
                else if (current is YieldInstruction || current == waitForNextFrame)
                {
                    UnityEngine.Debug.LogErrorFormat("PumpCoroutine encountered YieldInstruction ({0}). Resulting in undefined behavior!", current.GetType());
                }
            }
        }

        public static PooledStateMachine<PumpCoroutineStateMachine> PumpCoroutine(IEnumerator coroutine, string profileSampleName, float maxFrameMs)
        {
            PooledStateMachine<PumpCoroutineStateMachine> pooledStateMachine = timedPumpCoroutines.Get(null);
            pooledStateMachine.stateMachine.Initialize(coroutine, profileSampleName, maxFrameMs);
            return pooledStateMachine;
        }
    }
}
