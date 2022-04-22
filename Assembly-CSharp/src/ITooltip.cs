using System.Collections.Generic;

namespace AssemblyCSharp
{
    public interface ITooltip
    {
        void GetTooltip(out string tooltipText, List<TooltipIcon> tooltipIcons);
    }
}
