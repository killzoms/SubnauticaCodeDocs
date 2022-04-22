using UnityEngine;

namespace AssemblyCSharp
{
    public class uGUI_SceneLoading : uGUI_Scene
    {
        public uGUI_TextFade loadingText;

        public uGUI_Fader loadingBackground;

        private int loadingTextCounter;

        private bool loadingTextOverriden;

        private bool isLoading;

        private AsyncOperation levelLoadingOp;

        private string levelNameToLoad;

        public bool IsLoading => isLoading;

        public static bool IsLoadingScreenFinished { get; private set; }

        private void Init()
        {
            isLoading = true;
            loadingTextOverriden = false;
            loadingTextCounter = 0;
            levelLoadingOp = null;
            levelNameToLoad = null;
            if (!IsInvoking("AnimateLoadingText"))
            {
                InvokeRepeating("AnimateLoadingText", 0f, 0.1f);
            }
        }

        private void AnimateLoadingText()
        {
            if (!loadingTextOverriden)
            {
                if (levelLoadingOp == null)
                {
                    loadingText.SetText(Language.main.Get("SceneLoading") + new string('.', loadingTextCounter % 4));
                    loadingTextCounter++;
                    return;
                }
                loadingText.SetText(Language.main.Get("SceneLoading") + " (" + Mathf.RoundToInt(levelLoadingOp.progress * 100f) + "%)");
            }
        }

        public void DelayedBegin(SequenceCallback callback = null)
        {
            Init();
            loadingBackground.DelayedFadeIn(0.3f, callback);
        }

        public void Begin(bool withoutFadeIn = false)
        {
            if (!isLoading)
            {
                Init();
                if (withoutFadeIn)
                {
                    loadingBackground.SetState(enabled: true);
                    OnFadeInFinished();
                }
                else
                {
                    loadingBackground.FadeIn(OnFadeInFinished);
                }
            }
        }

        public void ShowLoadingScreen()
        {
            Init();
            loadingBackground.DelayedFadeIn(0.3f, OnFadeInFinished);
        }

        private static void OnFadeInFinished()
        {
            MainCameraV2 main = MainCameraV2.main;
            if ((bool)main)
            {
                main.OverrideCullingMask(0);
            }
        }

        public void BeginAsyncSceneLoad(string sceneName)
        {
            Init();
            levelNameToLoad = sceneName;
            loadingBackground.DelayedFadeIn(0.3f, DelayedSceneLoad);
        }

        private void DelayedSceneLoad()
        {
            levelLoadingOp = Application.LoadLevelAsync(levelNameToLoad);
        }

        public void End(bool fade = true)
        {
            if (isLoading)
            {
                MainCameraV2 main = MainCameraV2.main;
                if ((bool)main)
                {
                    main.RestoreCullingMask();
                }
                IsLoadingScreenFinished = true;
                isLoading = false;
                loadingTextOverriden = true;
                loadingText.SetText("");
                if (fade)
                {
                    loadingBackground.FadeOut();
                }
                else
                {
                    loadingBackground.FadeOut(0f, null);
                }
            }
        }

        public void SetLoadingText(string text)
        {
            loadingTextOverriden = true;
            loadingText.SetText(text);
        }
    }
}
