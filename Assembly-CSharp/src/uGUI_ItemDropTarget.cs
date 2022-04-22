using UnityEngine;
using UnityEngine.EventSystems;

namespace AssemblyCSharp
{
    public class uGUI_ItemDropTarget : MonoBehaviour, IDropHandler, IEventSystemHandler, IDragHoverHandler
    {
        [AssertNotNull]
        public RectTransform dropTarget;

        public void OnDrop(PointerEventData eventData)
        {
            if (!ItemDragManager.isDragging)
            {
                return;
            }
            InventoryItem draggedItem = ItemDragManager.draggedItem;
            if (draggedItem != null)
            {
                Inventory main = Inventory.main;
                if (main.GetAltUseItemAction(draggedItem) == ItemAction.Drop)
                {
                    main.AltUseItem(draggedItem);
                }
                else if (main.GetUseItemAction(draggedItem) == ItemAction.Drop)
                {
                    main.UseItem(draggedItem);
                }
            }
            ItemDragManager.DragStop();
        }

        public void OnDragHoverEnter(PointerEventData eventData)
        {
        }

        public void OnDragHoverStay(PointerEventData eventData)
        {
            if (!ItemDragManager.isDragging)
            {
                return;
            }
            InventoryItem draggedItem = ItemDragManager.draggedItem;
            if (draggedItem != null)
            {
                Inventory main = Inventory.main;
                if (main.GetAltUseItemAction(draggedItem) == ItemAction.Drop || main.GetUseItemAction(draggedItem) == ItemAction.Drop)
                {
                    ItemDragManager.SetActionHint(ItemActionHint.Drop);
                }
            }
        }

        public void OnDragHoverExit(PointerEventData eventData)
        {
        }
    }
}
