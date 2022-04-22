using UnityEngine;

namespace AssemblyCSharp
{
    public class performanceMonitor : MonoBehaviour
    {
        private int[] frames;

        private int frame = 1;

        private int totalTime;

        public int frameTimeMean30;

        private void Start()
        {
            frames = new int[30];
        }

        private void Update()
        {
            frames[frame] = (int)(Time.deltaTime * 1000f);
            if (frame > 28)
            {
                int[] array = frames;
                foreach (int num in array)
                {
                    totalTime += num;
                }
                frameTimeMean30 = totalTime / 30;
                frame = 0;
                totalTime = 0;
            }
            else
            {
                frame++;
            }
        }
    }
}
