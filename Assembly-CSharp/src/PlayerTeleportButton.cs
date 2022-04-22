using UnityEngine;

namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class PlayerTeleportButton : HandTarget, IHandTarget
    {
        public Transform destination;

        public bool isForEscapePod;

        public bool enterOnly;

        [AssertNotNull]
        public string hoverText = "";

        public string customGoal = "";

        public void OnHandHover(GUIHand hand)
        {
            if (hand.IsFreeToInteract())
            {
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                HandReticle.main.SetInteractText(hoverText);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            if (!hand.IsFreeToInteract())
            {
                return;
            }
            Player component = hand.GetComponent<Player>();
            component.SetPosition(destination.position);
            if (isForEscapePod)
            {
                if (enterOnly)
                {
                    component.escapePod.Update(newValue: true);
                }
                else
                {
                    component.escapePod.Update(newValue: false);
                }
            }
            if (customGoal != "")
            {
                GoalManager.main.OnCustomGoalEvent(customGoal);
            }
        }
    }
}
