using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    public static class InputEx
    {
        private static bool initialized = false;

        private static int frame = -1;

        private static float monitorAspect = 1.77f;

        private static Vector3 _mp = new Vector3(0f, 0f, 0f);

        public static Vector3 mp
        {
            get
            {
                UpdateMousePosition();
                return _mp;
            }
        }

        private static float GetAspect(float width, float height)
        {
            return width / height;
        }

        private static void Initialize()
        {
            if (initialized)
            {
                return;
            }
            Resolution currentResolution = Screen.currentResolution;
            Resolution[] resolutions = Screen.resolutions;
            int width = currentResolution.width;
            int height = currentResolution.height;
            int i = 0;
            for (int num = resolutions.Length; i < num; i++)
            {
                currentResolution = resolutions[i];
                if (width < currentResolution.width)
                {
                    width = currentResolution.width;
                }
                if (height < currentResolution.height)
                {
                    height = currentResolution.height;
                }
            }
            monitorAspect = GetAspect(width, height);
            Debug.Log($"InputEx : maxWidth:{width}, maxHeight:{height}, monitorAspect:{monitorAspect}");
            initialized = true;
        }

        private static void UpdateMousePosition()
        {
            if (frame == Time.frameCount)
            {
                return;
            }
            frame = Time.frameCount;
            Initialize();
            _mp = Input.mousePosition;
            if (Screen.fullScreen && monitorAspect != -1f && !XRSettings.enabled)
            {
                float num = Screen.width;
                float num2 = Screen.height;
                float aspect = GetAspect(num, num2);
                if (aspect != -1f && monitorAspect > aspect)
                {
                    float num3 = num2 * monitorAspect;
                    _mp.x = Mathf.Round(_mp.x / num3 * num);
                }
            }
        }
    }
}
