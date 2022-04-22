namespace AssemblyCSharp
{
    public class ButtonHandTarget : HandTarget, IHandTarget
    {
        public delegate void ButtonHandler();

        public FMOD_StudioEventEmitter pressSound;

        public ButtonHandler buttonHandler;

        public void OnHandHover(GUIHand hand)
        {
            if (hand.IsFreeToInteract())
            {
                HandReticle.main.SetIcon(HandReticle.IconType.Interact);
                HandReticle.main.SetInteractText("PressButton");
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            if (hand.IsFreeToInteract())
            {
                if (pressSound != null)
                {
                    Utils.PlayEnvSound(pressSound);
                }
                if (buttonHandler != null)
                {
                    buttonHandler();
                }
            }
        }
    }
}
