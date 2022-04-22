using UnityEngine;

namespace AssemblyCSharp
{
    public class StartScreenFade : MonoBehaviour
    {
        public string menuEnviornmentWaterScapeName;

        public StartScreen startScreenScript;

        public Texture splashImageOnFirstEntry;

        public Texture splashImageOnSubsequentEntries;

        public float fadeTime = 1f;

        private float elapsedFadeDelayTime;

        private float delayInSecondsBeforeFading = 0.8f;

        private Texture currentSplashScreen;

        private Texture2D singlePixel;

        private bool fadeInStarted;

        private float elapsedFadeTime;

        private float fadeTimeInv;

        private bool fadingInDone;

        private WaterSurface waterSurface;

        private float overlayFadeValue;

        private AsyncOperation asyncLoadMenuEnv;

        private Camera camera;

        private bool menuEnvAlreadyLoaded;

        private void Start()
        {
            if (!PlatformUtils.isConsolePlatform)
            {
                base.enabled = false;
                return;
            }
            if (currentSplashScreen == null)
            {
                currentSplashScreen = splashImageOnFirstEntry;
            }
            else
            {
                currentSplashScreen = splashImageOnSubsequentEntries;
            }
            camera = MainCamera.camera;
            singlePixel = new Texture2D(1, 1, TextureFormat.RGB24, mipChain: false);
            fadeTimeInv = 1f / fadeTime;
        }

        private void Update()
        {
            if (asyncLoadMenuEnv == null || !asyncLoadMenuEnv.isDone || fadingInDone)
            {
                return;
            }
            if (!menuEnvAlreadyLoaded)
            {
                waterSurface = GameObject.Find(menuEnviornmentWaterScapeName).GetComponent<WaterSurface>();
                menuEnvAlreadyLoaded = true;
            }
            if (waterSurface.IsLoadingDisplacementTextures())
            {
                return;
            }
            elapsedFadeDelayTime += Time.unscaledDeltaTime;
            if (!(elapsedFadeDelayTime >= delayInSecondsBeforeFading))
            {
                return;
            }
            if (!fadeInStarted)
            {
                fadeInStarted = true;
                elapsedFadeTime = 0f;
            }
            elapsedFadeTime += Time.unscaledDeltaTime;
            overlayFadeValue = Mathf.Lerp(0f, 1f, elapsedFadeTime * fadeTimeInv);
            if (overlayFadeValue == 1f)
            {
                if ((bool)startScreenScript)
                {
                    startScreenScript.SetStartMenuInputActive(active: true);
                }
                fadingInDone = true;
            }
        }

        public void NotifyMenuEnviornmentLoadStart(AsyncOperation enviornmentMenuAsyncLoadOp)
        {
            asyncLoadMenuEnv = enviornmentMenuAsyncLoadOp;
        }

        private void OnGUI()
        {
            if (!fadingInDone)
            {
                Color color = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 1f - overlayFadeValue);
                GUI.DrawTexture(new Rect(0f, 0f, camera.pixelWidth, camera.pixelHeight), singlePixel, ScaleMode.StretchToFill);
                GUI.color = new Color(1f, 1f, 1f, 1f - overlayFadeValue);
                GUI.DrawTexture(new Rect(0f, 0f, camera.pixelWidth, camera.pixelHeight), currentSplashScreen, ScaleMode.ScaleAndCrop);
                GUI.color = color;
            }
        }
    }
}
