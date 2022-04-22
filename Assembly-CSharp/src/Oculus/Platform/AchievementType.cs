using System.ComponentModel;

namespace AssemblyCSharp.Oculus.Platform
{
    public enum AchievementType : uint
    {
        [Description("UNKNOWN")]
        Unknown,
        [Description("SIMPLE")]
        Simple,
        [Description("BITFIELD")]
        Bitfield,
        [Description("COUNT")]
        Count
    }
}
