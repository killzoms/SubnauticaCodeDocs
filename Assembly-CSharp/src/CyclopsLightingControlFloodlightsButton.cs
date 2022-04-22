using UnityEngine;

namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class CyclopsLightingControlFloodlightsButton : HandTarget, IHandTarget
    {
        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText("CyclopsFloodlightsToggle");
            HandReticle.main.SetIcon(HandReticle.IconType.Interact);
        }

        public void OnHandClick(GUIHand guiHand)
        {
            SendMessageUpwards("ToggleFloodlights", null, SendMessageOptions.RequireReceiver);
        }
    }
}
