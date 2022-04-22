using UnityEngine.XR;

namespace AssemblyCSharp
{
    public class GameOptions
    {
        public static bool enableVrAnimations;

        public static bool GetVrAnimationMode()
        {
            if (XRSettings.enabled)
            {
                return true;
            }
            return enableVrAnimations;
        }
    }
}
