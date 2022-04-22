using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    public class HideForPlatform : MonoBehaviour
    {
        public bool hideForDesktop;

        public bool hideForVr;

        public bool hideForConsole;

        public bool hideForPerfectWorldChina;

        public bool hideForPS4;

        public bool hideForXboxOne;

        private void Start()
        {
            bool flag = false;
            if (!PlatformUtils.isConsolePlatform)
            {
                flag = ((!XRSettings.enabled) ? (flag || hideForDesktop) : (flag || hideForVr));
            }
            else
            {
                if (PlatformUtils.isPS4Platform)
                {
                    flag = flag || hideForPS4;
                }
                else if (PlatformUtils.isXboxOnePlatform)
                {
                    flag = flag || hideForXboxOne;
                }
                flag = flag || hideForConsole;
            }
            if (flag)
            {
                base.gameObject.SetActive(value: false);
            }
        }
    }
}
