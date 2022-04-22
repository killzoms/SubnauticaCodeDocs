using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class LanguageUpdater : MonoBehaviour
    {
        public bool debug;

        private TextMesh[] textMeshes;

        private GUIText[] guiTextStrings;

        private Text[] uguiTextStrings;

        private Dictionary<string, string> reverseStrings = new Dictionary<string, string>();

        private void OnEnable()
        {
            Language.main.OnLanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
        }

        private void OnDisable()
        {
            Language.main.OnLanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            ReverseChildrenTranslation(base.gameObject);
            TranslateChildren(base.gameObject);
        }

        public void TranslateChildren(GameObject parent)
        {
            textMeshes = parent.GetComponentsInChildren<TextMesh>(includeInactive: true);
            TextMesh[] array = textMeshes;
            foreach (TextMesh textMesh in array)
            {
                textMesh.text = GetReversible(textMesh.text);
            }
            guiTextStrings = parent.GetComponentsInChildren<GUIText>(includeInactive: true);
            GUIText[] array2 = guiTextStrings;
            foreach (GUIText gUIText in array2)
            {
                gUIText.text = GetReversible(gUIText.text);
            }
            uguiTextStrings = parent.GetComponentsInChildren<Text>(includeInactive: true);
            Text[] array3 = uguiTextStrings;
            foreach (Text text in array3)
            {
                text.text = GetReversible(text.text);
            }
        }

        public void ReverseChildrenTranslation(GameObject parent)
        {
            textMeshes = parent.GetComponentsInChildren<TextMesh>(includeInactive: true);
            TextMesh[] array = textMeshes;
            foreach (TextMesh textMesh in array)
            {
                textMesh.text = GetReverse(textMesh.text);
            }
            uguiTextStrings = parent.GetComponentsInChildren<Text>(includeInactive: true);
            Text[] array2 = uguiTextStrings;
            foreach (Text text in array2)
            {
                text.text = GetReverse(text.text);
            }
        }

        private string GetReversible(string key)
        {
            string text = Language.main.Get(key);
            reverseStrings[text] = key;
            return text;
        }

        private string GetReverse(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "";
            }
            if (!reverseStrings.TryGetValue(key, out var value))
            {
                if (debug)
                {
                    Debug.LogWarningFormat(this, "no reverse translation for key: '{0}'", key);
                }
                return key;
            }
            return value;
        }
    }
}
