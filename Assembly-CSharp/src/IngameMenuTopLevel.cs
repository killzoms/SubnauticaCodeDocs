namespace AssemblyCSharp
{
    public class IngameMenuTopLevel : uGUI_NavigableControlGrid, uGUI_IButtonReceiver
    {
        public bool OnButtonDown(GameInput.Button button)
        {
            if (button == GameInput.Button.UICancel)
            {
                IngameMenu.main.Close();
                return true;
            }
            return false;
        }
    }
}
