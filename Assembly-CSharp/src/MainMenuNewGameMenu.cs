namespace AssemblyCSharp
{
    public class MainMenuNewGameMenu : uGUI_NavigableControlGrid, uGUI_IButtonReceiver
    {
        public bool OnButtonDown(GameInput.Button button)
        {
            if (button == GameInput.Button.UICancel)
            {
                OnBack();
                return true;
            }
            return false;
        }

        public void OnBack()
        {
            MainMenuRightSide.main.OpenGroup("Home");
        }
    }
}
