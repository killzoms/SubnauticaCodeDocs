using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class MainMenuLoadPanel : MonoBehaviour
    {
        public GameObject saveInstance;

        public GameObject savedGameArea;

        public GameObject upgradeWarning;

        private void Start()
        {
            Language.main.OnLanguageChanged += OnLanguageChanged;
            GameInput.OnPrimaryDeviceChanged += OnPrimaryDeviceChanged;
            string[] possibleSlotNames = SaveLoadManager.main.GetPossibleSlotNames();
            GameMode[] possibleSlotGameModes = SaveLoadManager.main.GetPossibleSlotGameModes();
            bool active = !GameInput.IsPrimaryDeviceGamepad();
            for (int i = 0; i < possibleSlotNames.Length; i++)
            {
                string saveGame = possibleSlotNames[i];
                GameObject obj = Object.Instantiate(saveInstance);
                obj.transform.SetParent(savedGameArea.transform);
                obj.transform.localPosition = new Vector3(0f, 0f, 0f);
                obj.transform.localScale = new Vector3(1f, 1f, 1f);
                obj.transform.localRotation = Quaternion.identity;
                MainMenuLoadButton component = obj.GetComponent<MainMenuLoadButton>();
                component.saveGame = saveGame;
                component.gameMode = possibleSlotGameModes[i];
                component.deleteButton.SetActive(active);
                UpdateLoadButtonState(component);
            }
            SortSavesByDate();
        }

        private void OnDestroy()
        {
            Language.main.OnLanguageChanged -= OnLanguageChanged;
            GameInput.OnPrimaryDeviceChanged -= OnPrimaryDeviceChanged;
        }

        private void OnLanguageChanged()
        {
            MainMenuLoadButton[] allComponentsInChildren = savedGameArea.GetAllComponentsInChildren<MainMenuLoadButton>();
            foreach (MainMenuLoadButton lb in allComponentsInChildren)
            {
                UpdateLoadButtonState(lb);
            }
        }

        private void OnPrimaryDeviceChanged()
        {
            bool active = !GameInput.IsPrimaryDeviceGamepad();
            MainMenuLoadButton[] componentsInChildren = savedGameArea.GetComponentsInChildren<MainMenuLoadButton>(includeInactive: true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].deleteButton.SetActive(active);
            }
        }

        private void UpdateLoadButtonState(MainMenuLoadButton lb)
        {
            string text = Language.main.Get(lb.gameMode.ToString());
            lb.load.FindChild("SaveGameMode").GetComponent<Text>().text = text;
            SaveLoadManager.GameInfo gameInfo = SaveLoadManager.main.GetGameInfo(lb.saveGame);
            if (gameInfo == null)
            {
                lb.load.FindChild("DeleteButton").gameObject.SetActive(value: false);
                lb.load.FindChild("SaveGameTime").gameObject.SetActive(value: false);
                lb.load.FindChild("SaveIcons").gameObject.SetActive(value: false);
                lb.load.FindChild("SaveGameScreenshot").gameObject.SetActive(value: false);
                lb.load.FindChild("SaveGameLength").GetComponent<Text>().text = Language.main.Get("SlotEmpty");
                Debug.Log("Save / Load: Came across a directory that doesn't appear to contain a save game, skipping it.");
                return;
            }
            string text2 = Utils.PrettifyTime(gameInfo.gameTime);
            string text3 = Utils.PrettifyDate(gameInfo.dateTicks);
            lb.gameObject.name = "Saved" + gameInfo.dateTicks;
            bool cyclopsPresent = gameInfo.cyclopsPresent;
            bool seamothPresent = gameInfo.seamothPresent;
            bool exosuitPresent = gameInfo.exosuitPresent;
            bool rocketPresent = gameInfo.rocketPresent;
            Texture2D screenshot = gameInfo.GetScreenshot();
            lb.changeSet = gameInfo.changeSet;
            lb.gameMode = gameInfo.gameMode;
            if (screenshot != null)
            {
                lb.load.FindChild("SaveGameScreenshot").GetComponent<RawImage>().texture = screenshot;
            }
            lb.load.FindChild("SaveGameTime").GetComponent<Text>().text = text3;
            lb.load.FindChild("SaveGameLength").GetComponent<Text>().text = text2;
            lb.load.FindChild("SaveGameMode").GetComponent<Text>().text = text;
            lb.load.FindChild("SaveIcons").FindChild("SavedCyclops").gameObject.SetActive(cyclopsPresent);
            lb.load.FindChild("SaveIcons").FindChild("SavedSeamoth").gameObject.SetActive(seamothPresent);
            lb.load.FindChild("SaveIcons").FindChild("SavedExo").gameObject.SetActive(exosuitPresent);
            lb.load.FindChild("SaveIcons").FindChild("SavedRocket").gameObject.SetActive(rocketPresent);
            if (gameInfo.isFallback)
            {
                lb.load.FindChild("SaveGameLength").GetComponent<Text>().text = Language.main.Get("DamagedSavedGame");
            }
            if (!gameInfo.IsValid())
            {
                lb.load.GetComponent<Image>().color = Color.red;
                lb.load.FindChild("LoadButton").SetActive(value: false);
                lb.load.FindChild("SaveGameLength").GetComponent<Text>().text = $"<color=#ff0000ff>Changeset {gameInfo.changeSet} is newer than the current version!</color>";
            }
        }

        public void raiseWarning()
        {
            upgradeWarning.SetActive(value: true);
        }

        private void SortSavesByDate()
        {
            List<Transform> list = new List<Transform>();
            for (int num = savedGameArea.transform.childCount - 1; num >= 0; num--)
            {
                Transform child = savedGameArea.transform.GetChild(num);
                if (child.name != "NewGame")
                {
                    list.Add(child);
                    child.SetParent(null);
                }
            }
            list.Sort((Transform t1, Transform t2) => t1.name.CompareTo(t2.name));
            list.Reverse();
            foreach (Transform item in list)
            {
                item.SetParent(savedGameArea.transform);
            }
        }
    }
}
