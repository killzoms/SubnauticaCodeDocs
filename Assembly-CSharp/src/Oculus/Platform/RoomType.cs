using System.ComponentModel;

namespace AssemblyCSharp.Oculus.Platform
{
    public enum RoomType : uint
    {
        [Description("UNKNOWN")]
        Unknown,
        [Description("MATCHMAKING")]
        Matchmaking,
        [Description("MODERATED")]
        Moderated,
        [Description("PRIVATE")]
        Private,
        [Description("SOLO")]
        Solo
    }
}
