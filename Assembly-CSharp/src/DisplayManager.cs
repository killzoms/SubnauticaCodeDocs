using System;
using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    public class DisplayManager : MonoBehaviour
    {
        private static DisplayManager instance;

        private static bool changed;

        private Resolution resolution;

        private bool fullscreen;

        private int vSyncCount;

        private float vrRenderScale;

        public static event Action OnDisplayChanged;

        public static void SetResolution(int width, int height, bool fullscreen)
        {
            Screen.SetResolution(width, height, fullscreen);
            changed = true;
        }

        private void Awake()
        {
            if (instance != null)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            instance = this;
            Initialize();
        }

        private void Initialize()
        {
            resolution = Screen.currentResolution;
            fullscreen = Screen.fullScreen;
            vSyncCount = QualitySettings.vSyncCount;
            vrRenderScale = XRSettings.eyeTextureResolutionScale;
        }

        private void Update()
        {
            if (Screen.currentResolution.width != resolution.width || Screen.currentResolution.height != resolution.height || Screen.fullScreen != fullscreen || QualitySettings.vSyncCount != vSyncCount || XRSettings.eyeTextureResolutionScale != vrRenderScale)
            {
                changed = true;
            }
            if (changed)
            {
                changed = false;
                resolution = Screen.currentResolution;
                fullscreen = Screen.fullScreen;
                vSyncCount = QualitySettings.vSyncCount;
                vrRenderScale = XRSettings.eyeTextureResolutionScale;
                if (DisplayManager.OnDisplayChanged != null)
                {
                    DisplayManager.OnDisplayChanged();
                }
            }
        }
    }
}
