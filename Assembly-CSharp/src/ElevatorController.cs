namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class ElevatorController : HandTarget, IHandTarget
    {
        [AssertNotNull]
        public Rocket rocket;

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText("StartElevator");
            HandReticle.main.SetIcon(HandReticle.IconType.Interact);
        }

        public void OnHandClick(GUIHand hand)
        {
            rocket.ElevatorControlButtonActivate();
        }
    }
}
