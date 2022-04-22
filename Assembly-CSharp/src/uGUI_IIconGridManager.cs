using System.Collections.Generic;

namespace AssemblyCSharp
{
    public interface uGUI_IIconGridManager
    {
        void GetTooltip(string id, out string tooltipText, List<TooltipIcon> tooltipIcons);

        void OnPointerEnter(string id);

        void OnPointerExit(string id);

        void OnPointerClick(string id, int button);

        void OnSortRequested();
    }
}
