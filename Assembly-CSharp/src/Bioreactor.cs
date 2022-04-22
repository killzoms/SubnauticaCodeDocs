using ProtoBuf;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class Bioreactor : PowerCrafter, IHandTarget
    {
        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractInfo("Deprecated", "DeprecatedBuildableInstructions");
        }

        public void OnHandClick(GUIHand hand)
        {
        }
    }
}
