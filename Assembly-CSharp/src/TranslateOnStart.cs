using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class TranslateOnStart : MonoBehaviour
    {
        private void Start()
        {
            Text[] componentsInChildren = GetComponentsInChildren<Text>(includeInactive: true);
            foreach (Text text in componentsInChildren)
            {
                text.text = Language.main.Get(text.text);
            }
        }
    }
}
