using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(RectTransform))]
    public class SimpleTooltip : MonoBehaviour, ITooltip
    {
        public bool translate = true;

        public string text = "Tooltip";

        void ITooltip.GetTooltip(out string tooltipText, List<TooltipIcon> tooltipIcons)
        {
            tooltipText = (translate ? Language.main.Get(text) : text);
        }
    }
}
