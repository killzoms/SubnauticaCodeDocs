using UnityEngine;

namespace AssemblyCSharp
{
    public class SimpleUpdateBehaviour : SimpleCounter
    {
        private float lastTime;

        private void Start()
        {
            lastTime = Time.time;
        }

        private void Update()
        {
            while (Time.time - lastTime >= SimpleCounter.delay)
            {
                lastTime += SimpleCounter.delay;
                Do();
            }
        }
    }
}
