using System.Collections.Generic;

namespace AssemblyCSharp
{
    public interface uGUI_IToolbarManager
    {
        void GetToolbarTooltip(int index, out string tooltipText, List<TooltipIcon> tooltipIcons);

        void OnToolbarClick(int index, int button);
    }
}
