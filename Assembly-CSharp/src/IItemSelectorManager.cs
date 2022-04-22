using System.Collections.Generic;

namespace AssemblyCSharp
{
    public interface IItemSelectorManager
    {
        bool Filter(InventoryItem item);

        int Sort(List<InventoryItem> items);

        string GetText(InventoryItem item);

        void Select(InventoryItem item);
    }
}
