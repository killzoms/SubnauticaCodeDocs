using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class TranslationLiveUpdate : MonoBehaviour
    {
        public string translationKey;

        [AssertNotNull]
        public Text textComponent;

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
            if (!string.IsNullOrEmpty(textComponent.text))
            {
                if (string.IsNullOrEmpty(translationKey))
                {
                    translationKey = textComponent.text;
                }
                textComponent.text = Language.main.Get(translationKey);
            }
        }
    }
}
