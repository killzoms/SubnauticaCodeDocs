using System;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class MainMenuContinueGameHandler : MonoBehaviour
    {
        public Text continueTooltip;

        public Button continueButton;

        public GameObject continue3D;

        public GameObject continueTooltipPanel;

        private SaveLoadManager.GameInfo gameInfo;

        private void Start()
        {
            gameInfo = SaveLoadManager.main.GetGameInfo("test");
            if (gameInfo != null && gameInfo.IsValid())
            {
                Debug.Log("Main Menu: There is a save game available, showing continue button.");
                continue3D.SetActive(value: true);
                continueButton.interactable = true;
                int num = gameInfo.gameTime / 60;
                DateTime dateTime = new DateTime(gameInfo.dateTicks);
                continueTooltip.text = $"{gameInfo.gameMode}, {num} minutes, {dateTime}";
            }
            else if (gameInfo != null && !gameInfo.IsValid())
            {
                Debug.LogWarning("Main Menu: There is a save game available, but it is invalid.");
                continue3D.SetActive(value: true);
                continueButton.interactable = false;
                continue3D.GetComponentInChildren<TextMesh>().color = Color.gray;
                int num2 = gameInfo.gameTime / 60;
                DateTime dateTime2 = new DateTime(gameInfo.dateTicks);
                continueTooltip.text = $"<color=#ff0000ff>INVALID SAVEGAME</color>\nChangeset {gameInfo.changeSet} is newer than the current version!\n{gameInfo.gameMode}, {num2} minutes, {dateTime2}";
            }
            else
            {
                Debug.Log("Main Menu: There is no save game available, hiding continue button.");
                base.gameObject.SetActive(value: false);
                continue3D.SetActive(value: false);
            }
        }

        public void HandleClick()
        {
            uGUI.main.loading.DelayedBegin(ContinueGame);
        }

        private void ContinueGame()
        {
            Utils.SetLegacyGameMode(gameInfo.gameMode);
            Utils.SetContinueMode(mode: true);
            Application.LoadLevel("main");
        }
    }
}
