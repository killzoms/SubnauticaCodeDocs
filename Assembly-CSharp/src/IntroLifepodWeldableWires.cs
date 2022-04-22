namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class IntroLifepodWeldableWires : HandTarget, IHandTarget
    {
        public LiveMixin liveMixin;

        private void Start()
        {
        }

        public void OnHandHover(GUIHand hand)
        {
            if (liveMixin.health < liveMixin.maxHealth)
            {
                HandReticle.main.SetInteractInfo("DamagedWires", "WeldToFix");
            }
        }

        public void OnHandClick(GUIHand hand)
        {
        }
    }
}
