using System.Collections.Generic;

namespace AssemblyCSharp
{
    public interface uGUI_IIconManager
    {
        void GetTooltip(uGUI_ItemIcon icon, out string tooltipText, List<TooltipIcon> tooltipIcons);

        void OnPointerEnter(uGUI_ItemIcon icon);

        void OnPointerExit(uGUI_ItemIcon icon);

        bool OnPointerClick(uGUI_ItemIcon icon, int button);

        bool OnBeginDrag(uGUI_ItemIcon icon);

        void OnEndDrag(uGUI_ItemIcon icon);

        void OnDrop(uGUI_ItemIcon icon);

        void OnDragHoverEnter(uGUI_ItemIcon icon);

        void OnDragHoverStay(uGUI_ItemIcon icon);

        void OnDragHoverExit(uGUI_ItemIcon icon);
    }
}
