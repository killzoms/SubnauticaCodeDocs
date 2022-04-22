using System.Collections;
using UnityEngine;

namespace AssemblyCSharp
{
    public class SimpleCoroutineBehaviour : SimpleCounter
    {
        public IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSeconds(SimpleCounter.delay);
                Do();
            }
        }
    }
}
