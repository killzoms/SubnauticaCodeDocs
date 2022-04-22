using UnityEngine;

namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class BaseEntranceLadder : HandTarget, IHandTarget
    {
        public Transform targetTransform;

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText("ClimbUp");
            HandReticle.main.SetIcon(HandReticle.IconType.Hand);
        }

        public void OnHandClick(GUIHand hand)
        {
            if (base.enabled)
            {
                hand.player.SetPosition(targetTransform.position);
                hand.player.SetCurrentSub(GetComponentInParent<SubRoot>());
            }
        }
    }
}
