using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_RadiationWarning : MonoBehaviour
    {
        [AssertNotNull]
        public GameObject warning;

        [AssertNotNull]
        public Text text;

        private bool _initialized;

        private void Update()
        {
            Initialize();
            bool flag = IsRadiated();
            if (warning.activeSelf != flag)
            {
                warning.SetActive(flag);
            }
        }

        private void OnDisable()
        {
            Deinitialize();
        }

        private void Initialize()
        {
            if (!_initialized)
            {
                Language main = Language.main;
                if (!(main == null))
                {
                    _initialized = true;
                    OnLanguageChanged();
                    main.OnLanguageChanged += OnLanguageChanged;
                }
            }
        }

        private void Deinitialize()
        {
            if (_initialized)
            {
                _initialized = false;
                Language main = Language.main;
                if (main != null)
                {
                    main.OnLanguageChanged -= OnLanguageChanged;
                }
            }
        }

        private void OnLanguageChanged()
        {
            Language main = Language.main;
            if (!(main == null))
            {
                text.text = main.Get("RadiationDetected");
            }
        }

        private bool IsRadiated()
        {
            if (!_initialized)
            {
                return false;
            }
            Player main = Player.main;
            if (main == null)
            {
                return false;
            }
            PDA pDA = main.GetPDA();
            if (pDA != null && pDA.isInUse)
            {
                return false;
            }
            return main.radiationAmount > 0f;
        }
    }
}
