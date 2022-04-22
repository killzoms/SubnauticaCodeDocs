using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(uGUI_Block))]
    public class uGUI_SimpleButton : Button
    {
        public override void OnSelect(BaseEventData eventData)
        {
        }
    }
}
