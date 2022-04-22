using ProtoBuf;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class SeaEmperorJuvenile : Creature
    {
        public override void Start()
        {
            base.Start();
            friend = Player.main.gameObject;
        }
    }
}
