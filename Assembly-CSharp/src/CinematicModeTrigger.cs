namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class CinematicModeTrigger : CinematicModeTriggerBase
    {
        [AssertLocalization(AssertLocalizationAttribute.Options.AllowEmptyString)]
        public string handText;

        public override void OnHandHover(GUIHand hand)
        {
            if (showIconOnHandHover && PlayerCinematicController.cinematicModeCount <= 0 && !string.IsNullOrEmpty(handText))
            {
                HandReticle.main.SetInteractText(handText);
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }
    }
}
