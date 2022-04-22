using UnityEngine;

namespace AssemblyCSharp
{
    public class MainMenuNewGameConfirmationPopup : MonoBehaviour
    {
        public GameMode gameMode = GameMode.Freedom;

        public void StartNewGame()
        {
            uGUI.main.loading.DelayedBegin(uGUINewGame);
        }

        private void uGUINewGame()
        {
            Utils.SetContinueMode(mode: false);
            Utils.SetLegacyGameMode(gameMode);
            Application.LoadLevel("main");
        }
    }
}
