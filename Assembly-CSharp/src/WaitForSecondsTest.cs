using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public class WaitForSecondsTest : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(LoopForSeconds());
            StartCoroutine(LoopForRealSeconds());
        }

        private IEnumerator LoopForSeconds()
        {
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForSeconds(1f);
                Debug.LogFormat(this, "LoopForSeconds {0}", i);
            }
        }

        private IEnumerator LoopForRealSeconds()
        {
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForSecondsRealtime(1f);
                Debug.LogFormat(this, "LoopForRealSeconds {0}", i);
            }
        }
    }
}
