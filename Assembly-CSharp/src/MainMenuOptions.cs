using UWE;

namespace AssemblyCSharp
{
    public class MainMenuOptions : uGUI_NavigableControlGrid, uGUI_IButtonReceiver
    {
        public uGUI_MainMenu mainMenu;

        public uGUI_TabbedControlsPanel tabbedPanel;

        public void Apply()
        {
            OnBack();
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            if (button == GameInput.Button.UICancel)
            {
                if (tabbedPanel != null && tabbedPanel.tabOpen)
                {
                    tabbedPanel.HighlightCurrentTab();
                    return true;
                }
                OnBack();
                return true;
            }
            return false;
        }

        public void OnBack()
        {
            CoroutineHost.StartCoroutine(GameSettings.SaveAsync());
            mainMenu.ShowPrimaryOptions(show: true);
            mainMenu.OnHome();
        }
    }
}
