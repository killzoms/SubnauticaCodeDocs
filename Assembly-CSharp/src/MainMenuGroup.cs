using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [SuppressMessage("Subnautica.Rules", "AvoidDoubleInitializationRule")]
    public class MainMenuGroup : MonoBehaviour
    {
        [Tooltip("Should this group change the legend when opened?")]
        public bool ChangeLegendOnOpen = true;

        public LegendButtonData[] legendButtons = new LegendButtonData[0];

        public void SyncLegendToGroup()
        {
            uGUI_LegendBar.ClearButtons();
            for (int i = 0; i < legendButtons.Length; i++)
            {
                LegendButtonData legendButtonData = legendButtons[i];
                uGUI_LegendBar.ChangeButton(legendButtonData.legendButtonIdx, uGUI.FormatButton(legendButtonData.button, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat(legendButtonData.buttonDescription));
            }
        }
    }
}
