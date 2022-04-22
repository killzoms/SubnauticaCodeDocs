using UnityEngine;
using UnityEngine.UI;
using UWE;

namespace AssemblyCSharp
{
    public class uGUI_HardcoreGameOver : uGUI_InputGroup
    {
        private const string freezerName = "HardcoreGameOver";

        public GameObject inputBlocker;

        public GameObject message;

        public Text text;

        public uGUI_NavigableControlGrid mainGrid;

        protected override void Awake()
        {
            inputBlocker.SetActive(value: false);
            base.Awake();
            message.SetActive(value: false);
        }

        protected override void Update()
        {
        }

        public override void OnSelect(bool lockMovement)
        {
            message.SetActive(value: true);
            text.text = Language.main.Get("HardcoreGameOver");
            FreezeTime.Begin("HardcoreGameOver");
            inputBlocker.SetActive(value: true);
            base.OnSelect(lockMovement);
            GamepadInputModule.current.SetCurrentGrid(mainGrid);
        }

        public override void OnDeselect()
        {
        }

        public void Show()
        {
            uGUI.main.overlays.gameObject.SetActive(value: false);
            Select(lockMovement: true);
        }

        public void OnOkClick()
        {
            FreezeTime.End("HardcoreGameOver");
            SceneCleaner.Open();
        }
    }
}
