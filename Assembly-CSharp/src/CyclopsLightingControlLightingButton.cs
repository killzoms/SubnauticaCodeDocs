using UnityEngine;

namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class CyclopsLightingControlLightingButton : HandTarget, IHandTarget
    {
        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText("CyclopsLightingToggle");
            HandReticle.main.SetIcon(HandReticle.IconType.Interact);
        }

        public void OnHandClick(GUIHand guiHand)
        {
            SendMessageUpwards("ToggleInternalLighting", null, SendMessageOptions.RequireReceiver);
        }
    }
}
