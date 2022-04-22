using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public class LockTest : MonoBehaviour
    {
        private readonly object valueLock = new object();

        private int value;

        private void Start()
        {
            StartCoroutine(LockThenFor());
            StartCoroutine(ForThenLock());
            StartCoroutine(NestedLock());
        }

        private IEnumerator LockThenFor()
        {
            while (true)
            {
                lock (valueLock)
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        value += i;
                    }
                }
                yield return null;
            }
        }

        private IEnumerator ForThenLock()
        {
            while (true)
            {
                for (int i = 0; i < 10000; i++)
                {
                    lock (valueLock)
                    {
                        value += i;
                    }
                }
                yield return null;
            }
        }

        private IEnumerator NestedLock()
        {
            while (true)
            {
                lock (valueLock)
                {
                    for (int i = 0; i < 10000; i++)
                    {
                        lock (valueLock)
                        {
                            value += i;
                        }
                    }
                }
                yield return null;
            }
        }
    }
}
