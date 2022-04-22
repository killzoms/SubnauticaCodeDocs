using System.ComponentModel;

namespace AssemblyCSharp.Oculus.Platform
{
    public enum SendPolicy : uint
    {
        [Description("UNRELIABLE")]
        Unreliable,
        [Description("RELIABLE")]
        Reliable
    }
}
