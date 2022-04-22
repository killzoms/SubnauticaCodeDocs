using UWE;

namespace AssemblyCSharp
{
    public class IngameMenuPanel : uGUI_NavigableControlGrid, uGUI_IButtonReceiver
    {
        public IngameMenu menu;

        public uGUI_TabbedControlsPanel tabbedPanel;

        public bool OnButtonDown(GameInput.Button button)
        {
            if (button == GameInput.Button.UICancel)
            {
                if (tabbedPanel != null && tabbedPanel.tabOpen)
                {
                    tabbedPanel.HighlightCurrentTab();
                    uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Back"));
                    uGUI_LegendBar.ChangeButton(1, uGUI.FormatButton(GameInput.Button.UISubmit, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("ItelSelectorSelect"));
                    return true;
                }
                OnBack();
            }
            return false;
        }

        public void OnBack()
        {
            CoroutineHost.StartCoroutine(GameSettings.SaveAsync());
            menu.ChangeSubscreen("Main");
        }
    }
}
