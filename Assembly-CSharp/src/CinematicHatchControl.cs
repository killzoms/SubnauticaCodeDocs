using UnityEngine;

namespace AssemblyCSharp
{
    public class CinematicHatchControl : MonoBehaviour
    {
        public Openable hatch;

        private void OnCyclopsHatchOpen(AnimationEvent e)
        {
            hatch.Open();
        }
    }
}
