using UnityEngine;

namespace AssemblyCSharp
{
    public class CyclopsExternalCamsButton : MonoBehaviour
    {
        [AssertNotNull]
        public CyclopsExternalCams cyclopsExternalCams;

        [AssertNotNull]
        public SubRoot subRoot;

        [AssertNotNull]
        public LiveMixin liveMixin;

        private bool mouseHover;

        private void Update()
        {
            if (mouseHover)
            {
                _ = HandReticle.main;
            }
        }

        public void OnMouseEnter()
        {
            if (!(Player.main.currentSub != subRoot))
            {
                mouseHover = true;
            }
        }

        public void OnMouseExit()
        {
            if (!(Player.main.currentSub != subRoot))
            {
                HandReticle.main.SetIcon(HandReticle.IconType.Default);
                mouseHover = false;
            }
        }

        public void CameraButtonActivated()
        {
            if (!(Player.main.currentSub != subRoot) && liveMixin.IsAlive())
            {
                mouseHover = false;
                cyclopsExternalCams.EnterCameraView();
            }
        }
    }
}
