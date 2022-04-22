using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [SuppressMessage("Subnautica.Rules", "AvoidDoubleInitializationRule")]
    public class mGUI_Change_Legend_On_Select : MonoBehaviour
    {
        public LegendButtonData[] legendButtonConfiguration = new LegendButtonData[0];

        public void SyncLegendBarToGUISelection()
        {
            uGUI_LegendBar.ClearButtons();
            for (int i = 0; i < legendButtonConfiguration.Length; i++)
            {
                LegendButtonData legendButtonData = legendButtonConfiguration[i];
                uGUI_LegendBar.ChangeButton(legendButtonData.legendButtonIdx, uGUI.FormatButton(legendButtonData.button, allBindingSets: false, " / ", gamePadOnly: true), Language.main.GetFormat(legendButtonData.buttonDescription));
            }
        }
    }
}
