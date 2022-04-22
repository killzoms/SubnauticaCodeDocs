using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class uGUI_MainMenu : uGUI_InputGroup
    {
        public static uGUI_MainMenu main;

        public MainMenuRightSide rightSide;

        public MainMenuPrimaryOptionsMenu primaryOptions;

        private uGUI_INavigableIconGrid subMenu;

        private string lastGroup;

        private bool isStartingNewGame;

        protected override void Awake()
        {
            main = this;
            base.Awake();
            isStartingNewGame = false;
            subMenu = primaryOptions;
        }

        private IEnumerator Start()
        {
            Select();
            PlatformUtils.main.GetServices().SetRichPresence("PresenceMainMenu");
            yield return null;
            if (QuickLaunchHelper.IsQuickLaunching())
            {
                if (QuickLaunchHelper.ForceNewGame())
                {
                    OnButtonSurvival();
                }
                else
                {
                    StartMostRecentSaveOrNewGame();
                }
            }
        }

        private void StartMostRecentSaveOrNewGame()
        {
            if (!HasSavedGames())
            {
                OnButtonSurvival();
            }
            else
            {
                LoadMostRecentSavedGame();
            }
        }

        protected override void Update()
        {
        }

        public uGUI_INavigableIconGrid GetCurrentSubMenu()
        {
            return subMenu;
        }

        private bool HasSavedGames()
        {
            string[] activeSlotNames = SaveLoadManager.main.GetActiveSlotNames();
            int i = 0;
            for (int num = activeSlotNames.Length; i < num; i++)
            {
                if (SaveLoadManager.main.GetGameInfo(activeSlotNames[i]) != null)
                {
                    return true;
                }
            }
            return false;
        }

        private void LoadMostRecentSavedGame()
        {
            string[] activeSlotNames = SaveLoadManager.main.GetActiveSlotNames();
            long num = 0L;
            SaveLoadManager.GameInfo gameInfo = null;
            string saveGame = string.Empty;
            int i = 0;
            for (int num2 = activeSlotNames.Length; i < num2; i++)
            {
                SaveLoadManager.GameInfo gameInfo2 = SaveLoadManager.main.GetGameInfo(activeSlotNames[i]);
                if (gameInfo2.dateTicks > num)
                {
                    gameInfo = gameInfo2;
                    num = gameInfo2.dateTicks;
                    saveGame = activeSlotNames[i];
                }
            }
            if (gameInfo != null)
            {
                CoroutineHost.StartCoroutine(LoadGameAsync(saveGame, gameInfo.changeSet, gameInfo.gameMode));
            }
        }

        private IEnumerator StartNewGame(GameMode gameMode)
        {
            if (isStartingNewGame)
            {
                yield break;
            }
            isStartingNewGame = true;
            Guid.NewGuid().ToString();
            PlatformUtils.main.GetServices().ShowUGCRestrictionMessageIfNecessary();
            Utils.SetContinueMode(mode: false);
            Utils.SetLegacyGameMode(gameMode);
            CoroutineTask<SaveLoadManager.CreateResult> createSlotTask = ((!PlatformUtils.isPS4Platform) ? SaveLoadManager.main.CreateSlotAsync() : SaveLoadManager.main.SetupSlotPS4Async());
            yield return createSlotTask;
            SaveLoadManager.CreateResult result = createSlotTask.GetResult();
            if (!result.success)
            {
                if (result.slotName == SaveLoadManager.Error.OutOfSpace.ToString())
                {
                    string descriptionText = Language.main.Get("SaveFailedSpace");
                    uGUI.main.confirmation.Show(descriptionText, null);
                }
                else if (result.slotName == SaveLoadManager.Error.OutOfSlots.ToString())
                {
                    string descriptionText2 = Language.main.Get("SaveFailedSlot");
                    uGUI.main.confirmation.Show(descriptionText2, null);
                }
                isStartingNewGame = false;
                yield break;
            }
            SaveLoadManager.main.SetCurrentSlot(result.slotName);
            VRLoadingOverlay.Show();
            if (!PlatformUtils.isPS4Platform)
            {
                UserStorageUtils.AsyncOperation clearSlotTask = SaveLoadManager.main.ClearSlotAsync(result.slotName);
                yield return clearSlotTask;
                if (!clearSlotTask.GetSuccessful())
                {
                    Debug.LogError("Clearing save data failed. But we ignore it.");
                }
            }
            GamepadInputModule.current.SetCurrentGrid(null);
            uGUI.main.loading.BeginAsyncSceneLoad("Main");
        }

        public IEnumerator LoadGameAsync(string saveGame, int changeSet, GameMode gameMode)
        {
            if (isStartingNewGame)
            {
                yield break;
            }
            isStartingNewGame = true;
            FPSInputModule.SelectGroup(null);
            uGUI.main.loading.ShowLoadingScreen();
            yield return BatchUpgrade.UpgradeBatches(saveGame, changeSet);
            Utils.SetContinueMode(mode: true);
            Utils.SetLegacyGameMode(gameMode);
            SaveLoadManager.main.SetCurrentSlot(Path.GetFileName(saveGame));
            VRLoadingOverlay.Show();
            CoroutineTask<SaveLoadManager.LoadResult> task = SaveLoadManager.main.LoadAsync();
            yield return task;
            SaveLoadManager.LoadResult result = task.GetResult();
            if (!result.success)
            {
                yield return new WaitForSecondsRealtime(1f);
                isStartingNewGame = false;
                uGUI.main.loading.End(fade: false);
                string descriptionText = Language.main.GetFormat("LoadFailed", result.errorMessage);
                if (result.error == SaveLoadManager.Error.OutOfSpace)
                {
                    descriptionText = Language.main.Get("LoadFailedSpace");
                }
                uGUI.main.confirmation.Show(descriptionText, delegate(bool confirmed)
                {
                    OnErrorConfirmed(confirmed, saveGame, changeSet, gameMode);
                });
            }
            else
            {
                FPSInputModule.SelectGroup(null);
                uGUI.main.loading.BeginAsyncSceneLoad("Main");
            }
        }

        private void OnErrorConfirmed(bool confirmed, string saveGame, int changeSet, GameMode gameMode)
        {
            if (confirmed)
            {
                CoroutineHost.StartCoroutine(LoadGameAsync(saveGame, changeSet, gameMode));
            }
            else
            {
                main.Select();
            }
        }

        public void OnButtonLoad()
        {
            rightSide.OpenGroup("SavedGames");
        }

        public void OnButtonNew()
        {
            rightSide.OpenGroup("NewGame");
        }

        public void OnButtonHelp()
        {
            PlatformUtils.main.GetServices().ShowHelp();
        }

        public void OnButtonOptions()
        {
            ShowPrimaryOptions(show: false);
            rightSide.OpenGroup("Options");
        }

        public void OnButtonSwitchUser()
        {
            PlatformUtils.main.LogOffUser();
            Deselect();
            SceneCleaner.Open();
        }

        public void OnButtonFeedback()
        {
            uGUI_FeedbackCollector.main.Open();
        }

        public void OnButtonStore()
        {
            Application.OpenURL("https://unknownworlds.com/sn_store");
        }

        public void OnButtonQuit()
        {
            Application.Quit();
        }

        public void OnButtonFreedom()
        {
            CoroutineHost.StartCoroutine(StartNewGame(GameMode.Freedom));
        }

        public void OnButtonSurvival()
        {
            CoroutineHost.StartCoroutine(StartNewGame(GameMode.Survival));
        }

        public void OnButtonHardcore()
        {
            CoroutineHost.StartCoroutine(StartNewGame(GameMode.Hardcore));
        }

        public void OnButtonCreative()
        {
            CoroutineHost.StartCoroutine(StartNewGame(GameMode.Creative));
        }

        public void OnHome()
        {
            rightSide.OpenGroup("Home");
        }

        public void ShowPrimaryOptions(bool show)
        {
            primaryOptions.gameObject.SetActive(show);
        }

        public void OnRightSideOpened(GameObject root)
        {
            lastGroup = root.name;
            subMenu = root.GetComponentInChildren<uGUI_INavigableIconGrid>();
            if (subMenu == null)
            {
                subMenu = primaryOptions;
            }
            GamepadInputModule.current.SetCurrentGrid(GetCurrentSubMenu());
        }

        public void CloseRightSide()
        {
            MainMenuRightSide.main.OpenGroup("Home");
        }

        public override void OnSelect(bool lockMovement)
        {
            base.OnSelect(lockMovement);
            ShowPrimaryOptions(!string.Equals(lastGroup, "Options", StringComparison.OrdinalIgnoreCase));
            rightSide.OpenGroup(string.IsNullOrEmpty(lastGroup) ? "Home" : lastGroup);
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            GamepadInputModule.current.SetCurrentGrid(null);
        }
    }
}
