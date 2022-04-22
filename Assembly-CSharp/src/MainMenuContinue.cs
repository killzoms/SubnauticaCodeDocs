using System;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class MainMenuContinue : MonoBehaviour
    {
        public Text label;

        public Text tooltip;

        private SaveLoadManager.GameInfo gameInfo;

        private void Start()
        {
            Application.targetFrameRate = 144;
            gameInfo = SaveLoadManager.main.GetGameInfo("test");
            if (gameInfo != null)
            {
                base.gameObject.GetComponent<Button>().interactable = true;
                int num = gameInfo.gameTime / 60;
                DateTime dateTime = new DateTime(gameInfo.dateTicks);
                tooltip.text = $"{gameInfo.gameMode}, {num} minutes, {dateTime}";
                tooltip.gameObject.SetActive(value: true);
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
