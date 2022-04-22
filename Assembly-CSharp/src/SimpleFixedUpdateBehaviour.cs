using UnityEngine;

namespace AssemblyCSharp
{
    public class SimpleFixedUpdateBehaviour : SimpleCounter
    {
        private float lastTime;

        private void Start()
        {
            lastTime = Time.time;
        }

        private void FixedUpdate()
        {
            while (Time.time - lastTime >= SimpleCounter.delay)
            {
                lastTime += SimpleCounter.delay;
                Do();
            }
        }
    }
}
