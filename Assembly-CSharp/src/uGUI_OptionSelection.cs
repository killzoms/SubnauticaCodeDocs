using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_OptionSelection : MonoBehaviour, ISelectHandler, IEventSystemHandler, IDeselectHandler
    {
        public GameObject selectionBackground;

        public void OnSelect(BaseEventData data)
        {
            if (GameInput.GetPrimaryDevice() == GameInput.Device.Controller)
            {
                selectionBackground.SetActive(value: true);
            }
            uGUI_LegendBar.ClearButtons();
            if (GetComponent<Toggle>() != null)
            {
                uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Back"));
                uGUI_LegendBar.ChangeButton(1, uGUI.FormatButton(GameInput.Button.UISubmit, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Toggle"));
                return;
            }
            Slider component = GetComponent<Slider>();
            uGUI_Choice component2 = GetComponent<uGUI_Choice>();
            if (component != null || component2 != null)
            {
                uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Back"));
                uGUI_LegendBar.ChangeButton(1, uGUI.GetDisplayTextForBinding("ControllerRightStick"), Language.main.GetFormat("Modify"));
            }
            else
            {
                uGUI_LegendBar.ChangeButton(0, uGUI.FormatButton(GameInput.Button.UICancel, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("Back"));
                uGUI_LegendBar.ChangeButton(1, uGUI.FormatButton(GameInput.Button.UISubmit, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat("ItelSelectorSelect"));
            }
        }

        public void OnDeselect(BaseEventData data)
        {
            selectionBackground.SetActive(value: false);
        }
    }
}
