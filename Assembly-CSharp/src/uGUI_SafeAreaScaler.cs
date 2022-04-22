using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    public class uGUI_SafeAreaScaler : MonoBehaviour
    {
        public Rect vrSafeRect = new Rect(0.15f, 0.25f, 0.7f, 0.5f);

        public Rect consoleSafeRect = new Rect(0.01f, 0.05f, 0.98f, 0.9f);

        private void Update()
        {
            RectTransform component = GetComponent<RectTransform>();
            Rect rect = new Rect(0f, 0f, 1f, 1f);
            if (XRSettings.enabled)
            {
                rect = vrSafeRect;
            }
            else if (PlatformUtils.isConsolePlatform)
            {
                rect = consoleSafeRect;
            }
            component.anchorMin = rect.min;
            component.anchorMax = rect.max;
        }
    }
}
