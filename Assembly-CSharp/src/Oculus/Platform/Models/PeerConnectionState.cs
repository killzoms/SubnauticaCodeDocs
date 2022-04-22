using System.ComponentModel;

namespace AssemblyCSharp.Oculus.Platform.Models
{
    public enum PeerConnectionState : uint
    {
        [Description("UNKNOWN")]
        Unknown,
        [Description("CONNECTED")]
        Connected,
        [Description("TIMEOUT")]
        Timeout
    }
}
