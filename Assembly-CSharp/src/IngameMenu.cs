using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UWE;

namespace AssemblyCSharp
{
    public class IngameMenu : uGUI_InputGroup, uGUI_IButtonReceiver
    {
        public static IngameMenu main;

        [AssertNotNull]
        public Button saveButton;

        [AssertNotNull]
        public Button feedbackButton;

        [AssertNotNull]
        public Button quitToMainMenuButton;

        [AssertNotNull]
        public Button helpButton;

        [AssertNotNull]
        public Button developerButton;

        [AssertNotNull]
        public Text quitToMainMenuText;

        [AssertNotNull]
        public Text quitLastSaveText;

        public float maxSecondsToBeRecentlySaved;

        public GameObject pleaseWaitPanel;

        public GameObject mainPanel;

        private bool developerMode;

        private float lastSavedStateTime;

        public GameObject currentScreen { get; set; }

        protected override void Awake()
        {
            main = this;
            base.Awake();
        }

        private void OnEnable()
        {
            uGUI_LegendBar.ClearButtons();
            uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Back"));
            uGUI_LegendBar.ChangeButton(1, uGUI.FormatButton(GameInput.Button.UISubmit, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("ItelSelectorSelect"));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            uGUI_LegendBar.ClearButtons();
        }

        private void Start()
        {
            if (!uGUI_FeedbackCollector.main.IsEnabled())
            {
                feedbackButton.gameObject.SetActive(value: false);
                Transform transform = base.transform.Find("Main/ButtonLayout/ButtonFeedback");
                if (transform != null)
                {
                    transform.gameObject.SetActive(value: false);
                }
            }
            base.gameObject.SetActive(value: false);
            Reset();
            if (developerMode && !PlatformUtils.isShippingRelease)
            {
                developerButton.enabled = true;
            }
            lastSavedStateTime = Time.timeSinceLevelLoad;
        }

        public void ActivateDeveloperMode()
        {
            developerMode = true;
            developerButton.gameObject.SetActive(value: true);
        }

        protected override void Update()
        {
            base.Update();
            if (!developerMode && GameInput.GetEnableDeveloperMode())
            {
                ActivateDeveloperMode();
            }
            bool allowSaving = GetAllowSaving();
            saveButton.interactable = allowSaving;
            quitToMainMenuButton.interactable = allowSaving || !GameModeUtils.IsPermadeath();
        }

        private void Reset()
        {
            foreach (Transform item in base.gameObject.transform)
            {
                if (item.gameObject.name == "Main")
                {
                    item.gameObject.SetActive(value: true);
                    currentScreen = item.gameObject;
                }
                else if (item.gameObject.name == "Legend")
                {
                    item.gameObject.SetActive(value: true);
                }
                else
                {
                    item.gameObject.SetActive(value: false);
                }
            }
        }

        public void Open()
        {
            if (!(Time.timeSinceLevelLoad < 1f))
            {
                if (!base.gameObject.activeInHierarchy)
                {
                    base.gameObject.SetActive(value: true);
                    Select();
                }
                if (XRSettings.enabled)
                {
                    HideForScreenshots.Hide(HideForScreenshots.HideType.Mask | HideForScreenshots.HideType.HUD | HideForScreenshots.HideType.ViewModel);
                }
                ChangeSubscreen("Main");
            }
        }

        public void Close()
        {
            Deselect();
        }

        private bool GetAllowSaving()
        {
            if (IntroVignette.isIntroActive || LaunchRocket.isLaunching)
            {
                return false;
            }
            if (PlayerCinematicController.cinematicModeCount > 0)
            {
                if (!(Time.time - PlayerCinematicController.cinematicActivityStart > 30f))
                {
                    return false;
                }
                Debug.LogError("cinematics have been blocking saves for an unusual length of time; assuming a bug and allowing saving");
            }
            return !SaveLoadManager.main.isSaving;
        }

        public override void OnSelect(bool lockMovement)
        {
            base.OnSelect(lockMovement);
            base.gameObject.SetActive(value: true);
            FreezeTime.Begin("IngameMenu");
            global::UWE.Utils.lockCursor = false;
            if (GameModeUtils.IsPermadeath())
            {
                quitToMainMenuText.text = Language.main.Get("SaveAndQuitToMainMenu");
                saveButton.gameObject.SetActive(value: false);
            }
            else
            {
                saveButton.interactable = GetAllowSaving();
                quitToMainMenuButton.interactable = true;
            }
            if (PlatformUtils.isXboxOnePlatform)
            {
                helpButton.gameObject.SetActive(value: true);
            }
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            Reset();
            FreezeTime.End("IngameMenu");
            uGUI_BasicColorSwap[] componentsInChildren = GetComponentsInChildren<uGUI_BasicColorSwap>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].makeTextWhite();
            }
            base.gameObject.SetActive(value: false);
            if (XRSettings.enabled)
            {
                HideForScreenshots.Hide(HideForScreenshots.HideType.None);
            }
        }

        public void QuitSubscreen()
        {
            float num = Time.timeSinceLevelLoad - lastSavedStateTime;
            if (!GameModeUtils.IsPermadeath() && num > maxSecondsToBeRecentlySaved)
            {
                quitLastSaveText.text = Language.main.GetFormat("TimeSinceLastSave", Utils.PrettifyTime((int)num));
                ChangeSubscreen("QuitConfirmationWithSaveWarning");
            }
            else
            {
                ChangeSubscreen("QuitConfirmation");
            }
        }

        public void ChangeSubscreen(string newScreen)
        {
            if (currentScreen.name != newScreen)
            {
                currentScreen.SetActive(value: false);
                currentScreen = base.transform.Find(newScreen).gameObject;
                currentScreen.SetActive(value: true);
            }
            uGUI_INavigableIconGrid componentInChildren = currentScreen.GetComponentInChildren<uGUI_INavigableIconGrid>();
            GamepadInputModule.current.SetCurrentGrid(componentInChildren);
        }

        public IEnumerator QuitGameAsync(bool quitToDesktop)
        {
            if (SaveLoadManager.main.isSaving)
            {
                SetPleaseWaitVisible(visible: true);
                while (SaveLoadManager.main.isSaving)
                {
                    yield return null;
                }
                SetPleaseWaitVisible(visible: false);
            }
            if (GameModeUtils.IsPermadeath())
            {
                ChangeSubscreen("Main");
                mainPanel.SetActive(value: false);
                yield return SaveGameAsync();
            }
            if (!quitToDesktop)
            {
                global::UWE.Utils.lockCursor = false;
                SceneCleaner.Open();
            }
            else
            {
                Application.Quit();
            }
        }

        public void GiveFeedback()
        {
            OpenFeedbackForm();
        }

        public void OpenFeedbackForm()
        {
            Deselect();
            uGUI_FeedbackCollector.main.Open();
        }

        public void OpenFeedbackForums()
        {
            Application.OpenURL("http://playsubnautica.com");
        }

        public void ShowHelp()
        {
            PlatformUtils.main.GetServices().ShowHelp();
        }

        public void SaveGame()
        {
            mainPanel.SetActive(value: false);
            CoroutineHost.StartCoroutine(SaveGameAsync());
        }

        public void QuitGame(bool quitToDesktop)
        {
            CoroutineHost.StartCoroutine(QuitGameAsync(quitToDesktop));
        }

        private Texture2D CaptureSaveScreenshot()
        {
            Texture2D texture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: true);
            texture2D.name = "IngameMenu.CaptureSaveScreenshot";
            texture2D.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture2D.Apply();
            return texture2D;
        }

        private bool ReportSaveError(SaveLoadManager.SaveResult saveResult)
        {
            if (!saveResult.success)
            {
                FreezeTime.Begin("IngameMenu");
                string descriptionText = Language.main.GetFormat("SaveFailed", saveResult.errorMessage);
                if (saveResult.error == SaveLoadManager.Error.OutOfSpace)
                {
                    descriptionText = Language.main.Get("SaveFailedSpace");
                }
                uGUI.main.confirmation.Show(descriptionText, OnSaveErrorConfirmed);
                return true;
            }
            return false;
        }

        private IEnumerator SaveGameAsync()
        {
            yield return MainGameController.Instance.PerformGarbageAndAssetCollectionAsync();
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = CaptureSaveScreenshot();
            SetPleaseWaitVisible(visible: true);
            CoroutineTask<SaveLoadManager.SaveResult> saveToTemporaryTask = SaveLoadManager.main.SaveToTemporaryStorageAsync(screenshot);
            yield return saveToTemporaryTask;
            if (!GameModeUtils.IsPermadeath())
            {
                SetPleaseWaitVisible(visible: false);
                Close();
            }
            if (!ReportSaveError(saveToTemporaryTask.GetResult()))
            {
                CoroutineTask<SaveLoadManager.SaveResult> saveToDeepStorageTask = SaveLoadManager.main.SaveToDeepStorageAsync();
                yield return saveToDeepStorageTask;
                if (GameModeUtils.IsPermadeath())
                {
                    SetPleaseWaitVisible(visible: false);
                    Close();
                }
                if (!ReportSaveError(saveToDeepStorageTask.GetResult()))
                {
                    lastSavedStateTime = Time.timeSinceLevelLoad;
                }
            }
        }

        private void SetPleaseWaitVisible(bool visible)
        {
            pleaseWaitPanel.SetActive(visible);
        }

        private void OnSaveErrorConfirmed(bool confirmed)
        {
            FreezeTime.End("IngameMenu");
            if (confirmed)
            {
                SaveGame();
            }
            else
            {
                ErrorMessage.AddError(Language.main.Get("GameNotSaved"));
            }
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            if (button == GameInput.Button.UIMenu)
            {
                Close();
                return true;
            }
            return false;
        }
    }
}
