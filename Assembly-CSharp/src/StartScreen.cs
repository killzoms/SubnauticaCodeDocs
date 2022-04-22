using System;
using System.Collections;
using Gendarme;
using UnityEngine;
using UnityEngine.SceneManagement;
using UWE;

namespace AssemblyCSharp
{
    public class StartScreen : MonoBehaviour
    {
        public StartScreenPressStart pressStart;

        public CanvasRenderer splashScreensBackground;

        public CanvasRenderer chineseHealthAndSafetyWarning;

        public GameObject earlyAccessWarning;

        public GameObject mainMenuFaderRef;

        [SuppressMessage("Gendarme.Rules.Performance", "AvoidUnusedPrivateFieldsRule")]
        private StartScreenFade startScreenFade;

        private bool splashScreensShown;

        private bool menuInputEnabled;

        private bool splashScreensActive;

        private int loginGamepadIndex = -1;

        public void SetStartMenuInputActive(bool active)
        {
            menuInputEnabled = active;
        }

        private static IEnumerator FadeGraphicIn(CanvasRenderer renderer, float fadeOutTime = 0.25f)
        {
            renderer.gameObject.SetActive(value: true);
            float alpha = 0f;
            while (alpha <= 1f)
            {
                alpha += Time.unscaledDeltaTime / fadeOutTime;
                renderer.SetAlpha(alpha);
                yield return null;
            }
        }

        private static IEnumerator FadeGraphicOut(CanvasRenderer renderer, float fadeOutTime = 0.25f)
        {
            float alpha = 1f;
            while (alpha >= 0f)
            {
                alpha -= Time.unscaledDeltaTime / fadeOutTime;
                renderer.SetAlpha(alpha);
                yield return null;
            }
            renderer.gameObject.SetActive(value: false);
        }

        private IEnumerator Start()
        {
            UnityUWE.Return123Test();
            uGUI.Initialize();
            AsyncOperation enviornmentMenuAsyncLoadOp = SceneManager.LoadSceneAsync("MenuEnvironment", LoadSceneMode.Additive);
            if (PlatformUtils.isConsolePlatform)
            {
                menuInputEnabled = false;
                startScreenFade = mainMenuFaderRef.GetComponent<StartScreenFade>();
                startScreenFade.NotifyMenuEnviornmentLoadStart(enviornmentMenuAsyncLoadOp);
            }
            if (!splashScreensShown)
            {
                splashScreensShown = true;
            }
            yield break;
        }

        private bool GetPressedController(out int controllerIndex)
        {
            controllerIndex = -1;
            if (menuInputEnabled)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (Input.GetKeyUp((KeyCode)(357 + i * 20)))
                    {
                        controllerIndex = i;
                        return true;
                    }
                }
            }
            return false;
        }

        private void Update()
        {
            if (splashScreensActive)
            {
                return;
            }
            PlatformServices services = PlatformUtils.main.GetServices();
            if (services == null)
            {
                return;
            }
            bool supportsDynamicLogOn = services.GetSupportsDynamicLogOn();
            bool flag = true;
            int controllerIndex = -1;
            if (supportsDynamicLogOn || PlatformUtils.isConsolePlatform)
            {
                pressStart.gameObject.SetActive(value: true);
                flag = GetPressedController(out controllerIndex);
            }
            if (!pressStart.IsLoading && flag)
            {
                pressStart.SetLoading(_loading: true);
                loginGamepadIndex = controllerIndex;
                if (supportsDynamicLogOn)
                {
                    PlatformUtils main = PlatformUtils.main;
                    main.OnLoginFinished = (PlatformUtils.LoginFinishedDelegate)Delegate.Combine(main.OnLoginFinished, new PlatformUtils.LoginFinishedDelegate(OnLoginFinished));
                    PlatformUtils.main.StartLogOnUserAsync(loginGamepadIndex);
                }
                else
                {
                    StartLoadingSavedata();
                }
            }
        }

        private void OnLoginFinished(bool success)
        {
            PlatformUtils main = PlatformUtils.main;
            main.OnLoginFinished = (PlatformUtils.LoginFinishedDelegate)Delegate.Remove(main.OnLoginFinished, new PlatformUtils.LoginFinishedDelegate(OnLoginFinished));
            if (success)
            {
                StartLoadingSavedata();
            }
            else
            {
                pressStart.SetLoading(_loading: false);
            }
        }

        private void StartLoadingSavedata()
        {
            Invoke("OnGuiInitialized", 0f);
        }

        private void OnGuiInitialized()
        {
            CoroutineHost.StartCoroutine(Load());
        }

        private IEnumerator Load()
        {
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            UserStorageUtils.AsyncOperation initTask = userStorage.InitializeAsync();
            yield return initTask;
            if (!initTask.GetSuccessful())
            {
                Debug.LogErrorFormat("Save data init failed ({0})", initTask.result);
                yield break;
            }
            CoroutineTask<SaveLoadManager.LoadResult> loadSlotsTask = SaveLoadManager.main.LoadSlotsAsync();
            yield return loadSlotsTask;
            SaveLoadManager.LoadResult result = loadSlotsTask.GetResult();
            if (!result.success)
            {
                string format = Language.main.GetFormat("LoadFailed", result.errorMessage);
                uGUI.main.confirmation.Show(format, OnLoadErrorConfirmed);
                yield break;
            }
            CoroutineTask<bool> loadOptionsTask = GameSettings.LoadAsync();
            yield return loadOptionsTask;
            if (!loadOptionsTask.GetResult())
            {
                string descriptionText = Language.main.Get("LoadOptionsFailed");
                uGUI.main.confirmation.Show(descriptionText, OnLoadErrorConfirmed);
            }
            else
            {
                yield return LoadMainMenu();
            }
        }

        private void OnLoadErrorConfirmed(bool confirmed)
        {
            if (confirmed)
            {
                StartLoadingSavedata();
            }
            else
            {
                CoroutineHost.StartCoroutine(LoadMainMenu());
            }
        }

        private IEnumerator LoadMainMenu()
        {
            yield return SceneManager.LoadSceneAsync("XMenu", LoadSceneMode.Additive);
            global::UnityEngine.Object.Destroy(base.gameObject);
        }
    }
}
