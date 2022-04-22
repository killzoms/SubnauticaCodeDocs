using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Text))]
    public class uGUI_Text : MonoBehaviour
    {
        protected Text text;

        protected virtual void Awake()
        {
            text = GetComponent<Text>();
        }

        protected virtual void Start()
        {
            SetText(text.text, translate: true);
        }

        public void SetText(string s, bool translate = false)
        {
            if (translate)
            {
                s = Language.main.Get(s);
            }
            text.text = s;
        }

        public void SetAlignment(TextAnchor anchor)
        {
            text.alignment = anchor;
        }

        public void SetColor(Color color)
        {
            text.color = new Color(color.r, color.g, color.b, text.color.a);
        }
    }
}
