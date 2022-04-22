using UnityEngine;

namespace AssemblyCSharp
{
    public class ConstructorClimb : HandTarget, IHandTarget
    {
        [AssertNotNull]
        public string handText = "Climb";

        [AssertNotNull]
        public Transform target;

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText(handText);
            HandReticle.main.SetIcon(HandReticle.IconType.Hand);
        }

        public void OnHandClick(GUIHand hand)
        {
            Player component = hand.gameObject.GetComponent<Player>();
            component.SetPosition(target.position);
            component.GetComponent<Rigidbody>().velocity = Vector3.zero;
            component.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
}
