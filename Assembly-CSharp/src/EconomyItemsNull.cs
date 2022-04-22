using System.Collections;

namespace AssemblyCSharp
{
    public class EconomyItemsNull : EconomyItems
    {
        public bool HasItem(string itemId)
        {
            return false;
        }

        public string GetItemProperty(string itemId, string property)
        {
            return string.Empty;
        }

        public IEnumerator RefreshAsync()
        {
            yield break;
        }
    }
}
