using UnityEngine;

namespace AssemblyCSharp
{
    public class PDASounds : MonoBehaviour
    {
        public static SoundQueue queue;

        private void Awake()
        {
            if (queue == null)
            {
                queue = new SoundQueue();
            }
            else
            {
                Object.Destroy(this);
            }
        }

        public static void Deinitialize()
        {
            queue = null;
        }

        private void LateUpdate()
        {
            queue.Update();
        }
    }
}
