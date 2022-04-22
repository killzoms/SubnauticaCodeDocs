using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_SceneConfirmation : uGUI_InputGroup, uGUI_IButtonReceiver
    {
        public delegate void ConfirmationFinishedDelegate(bool confirmed);

        public Text description;

        public Button yes;

        public Button no;

        private ConfirmationFinishedDelegate OnConfirmationFinished;

        public uGUI_NavigableControlGrid panel;

        private void Start()
        {
            base.gameObject.SetActive(value: false);
        }

        public override void OnSelect(bool lockMovement)
        {
            base.OnSelect(lockMovement);
            GamepadInputModule.current.SetCurrentGrid(panel);
        }

        public void Show(string descriptionText, ConfirmationFinishedDelegate callback)
        {
            OnConfirmationFinished = callback;
            description.text = descriptionText;
            base.gameObject.SetActive(value: true);
            Select(lockMovement: true);
        }

        private void Close(bool result)
        {
            Deselect();
            base.gameObject.SetActive(value: false);
            if (OnConfirmationFinished != null)
            {
                OnConfirmationFinished(result);
            }
        }

        public bool OnButtonDown(GameInput.Button button)
        {
            if (button == GameInput.Button.UICancel)
            {
                Close(result: false);
                return true;
            }
            return false;
        }

        public void OnYes()
        {
            Close(result: true);
        }

        public void OnNo()
        {
            Close(result: false);
        }
    }
}
