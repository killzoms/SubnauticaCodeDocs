using UnityEngine;

namespace AssemblyCSharp
{
    public class HintSwimToSurface : MonoBehaviour
    {
        public float oxygenThreshold = 10f;

        public int maxNumToShow = 3;

        private bool initialized;

        private int numShown;

        private string message;

        private int messageHash;

        private bool show;

        private void OnDisable()
        {
            Deinitialize();
        }

        private void Update()
        {
            Initialize();
            Track();
        }

        private void Initialize()
        {
            if (!initialized)
            {
                Language main = Language.main;
                if (!(main == null))
                {
                    initialized = true;
                    OnLanguageChanged();
                    main.OnLanguageChanged += OnLanguageChanged;
                }
            }
        }

        private void Deinitialize()
        {
            if (initialized)
            {
                initialized = false;
                numShown = 0;
                Language main = Language.main;
                if (main != null)
                {
                    main.OnLanguageChanged -= OnLanguageChanged;
                }
            }
        }

        private void OnLanguageChanged()
        {
            message = Language.main.Get("SwimToSurface");
            messageHash = message.GetHashCode();
        }

        private void Track()
        {
            if (!initialized)
            {
                return;
            }
            Hint main = Hint.main;
            if (main == null)
            {
                return;
            }
            bool num = show;
            show = ShouldShowWarning();
            uGUI_PopupMessage uGUI_PopupMessage2 = main.message;
            if (show)
            {
                uGUI_PopupMessage2.anchor = TextAnchor.UpperCenter;
                if (!uGUI_PopupMessage2.isShowingMessage || uGUI_PopupMessage2.messageHash != messageHash)
                {
                    uGUI_PopupMessage2.SetText(message, TextAnchor.MiddleLeft);
                    uGUI_PopupMessage2.Show(-1f);
                }
            }
            else if (uGUI_PopupMessage2.isShowingMessage && uGUI_PopupMessage2.messageHash == messageHash)
            {
                uGUI_PopupMessage2.Hide();
            }
            if (num && !show)
            {
                numShown++;
            }
        }

        private bool ShouldShowWarning()
        {
            if (GameModeUtils.IsOptionActive(GameModeOption.NoHints))
            {
                return false;
            }
            Player main = Player.main;
            if (main == null)
            {
                return false;
            }
            if (numShown >= maxNumToShow)
            {
                return false;
            }
            Ocean main2 = Ocean.main;
            if (main2 == null)
            {
                return false;
            }
            float oxygenAvailable = main.GetOxygenAvailable();
            float depthOf = main2.GetDepthOf(main.gameObject);
            Vehicle vehicle = main.GetVehicle();
            if ((oxygenAvailable < oxygenThreshold && depthOf > 0f && main.IsSwimming()) || (vehicle != null && !vehicle.IsPowered()))
            {
                return true;
            }
            return false;
        }
    }
}
