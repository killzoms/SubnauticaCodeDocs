using UnityEngine;

namespace AssemblyCSharp
{
    public class ExploderSlowMotion : MonoBehaviour
    {
        public float slowMotionTime = 1f;

        public ExploderObject Exploder;

        private float slowMotionSpeed = 1f;

        private bool slowmo;

        public void EnableSlowMotion(bool status)
        {
            slowmo = status;
            if (slowmo)
            {
                slowMotionSpeed = 0.05f;
                if ((bool)Exploder)
                {
                    Exploder.MeshColliders = true;
                }
            }
            else
            {
                slowMotionSpeed = 1f;
                if ((bool)Exploder)
                {
                    Exploder.MeshColliders = false;
                }
            }
            slowMotionTime = slowMotionSpeed;
        }

        public void Update()
        {
            slowMotionSpeed = slowMotionTime;
            Time.timeScale = slowMotionSpeed;
            Time.fixedDeltaTime = slowMotionSpeed * 0.02f;
            if (Input.GetKeyDown(KeyCode.T))
            {
                slowmo = !slowmo;
                EnableSlowMotion(slowmo);
            }
        }
    }
}
