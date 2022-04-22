using UnityEngine;

namespace AssemblyCSharp
{
    public class DiveModeParticles : MonoBehaviour
    {
        public ParticleSystem particles;

        private bool prevShouldPlay;

        private void Start()
        {
            particles.Stop();
        }

        private void Update()
        {
            bool flag = Ocean.main.GetDepthOf(base.gameObject) > 3f;
            bool flag2 = Player.main.motorMode == Player.MotorMode.Dive && flag;
            if (flag2 != prevShouldPlay)
            {
                if (flag2)
                {
                    particles.Play();
                }
                else
                {
                    particles.Stop();
                }
            }
            prevShouldPlay = flag2;
        }
    }
}
