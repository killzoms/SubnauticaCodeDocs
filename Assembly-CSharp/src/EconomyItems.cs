using System.Collections;

namespace AssemblyCSharp
{
    public interface EconomyItems
    {
        bool HasItem(string itemId);

        string GetItemProperty(string itemId, string property);

        IEnumerator RefreshAsync();
    }
}
