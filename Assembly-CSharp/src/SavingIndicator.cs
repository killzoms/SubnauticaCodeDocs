using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class SavingIndicator : MonoBehaviour
    {
        public GameObject logo;

        public Text text;

        private void OnEnable()
        {
            OnLanguageChanged();
            Language.main.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Language.main.OnLanguageChanged -= OnLanguageChanged;
        }

        private void Update()
        {
            bool isSaving = SaveLoadManager.main.isSaving;
            logo.SetActive(isSaving);
            text.enabled = isSaving;
        }

        private void OnLanguageChanged()
        {
            text.text = Language.main.Get("SavingGame");
        }
    }
}
