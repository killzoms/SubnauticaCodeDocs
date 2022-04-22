using UnityEngine;

namespace AssemblyCSharp
{
    public class GUIController : MonoBehaviour
    {
        public enum HidePhase
        {
            None,
            Mask,
            HUD,
            MaskHUD,
            All
        }

        public static GUIController main;

        private HidePhase hidePhase;

        private void Awake()
        {
            main = this;
        }

        public HidePhase GetHidePhase()
        {
            return hidePhase;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                hidePhase++;
                if (hidePhase > HidePhase.All)
                {
                    hidePhase = HidePhase.None;
                }
                SetHidePhase(hidePhase);
            }
        }

        public static void SetHidePhase(HidePhase hidePhase)
        {
            switch (hidePhase)
            {
                case HidePhase.None:
                    HideForScreenshots.Hide(HideForScreenshots.HideType.None);
                    break;
                case HidePhase.Mask:
                    HideForScreenshots.Hide(HideForScreenshots.HideType.Mask);
                    break;
                case HidePhase.HUD:
                    HideForScreenshots.Hide(HideForScreenshots.HideType.HUD);
                    break;
                case HidePhase.MaskHUD:
                    HideForScreenshots.Hide(HideForScreenshots.HideType.Mask | HideForScreenshots.HideType.HUD);
                    break;
                case HidePhase.All:
                    HideForScreenshots.Hide(HideForScreenshots.HideType.Mask | HideForScreenshots.HideType.HUD | HideForScreenshots.HideType.ViewModel);
                    break;
                default:
                    Debug.LogErrorFormat("undefined hide phase {0}", hidePhase);
                    break;
            }
        }
    }
}
