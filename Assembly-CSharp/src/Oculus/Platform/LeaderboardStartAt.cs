using System.ComponentModel;

namespace AssemblyCSharp.Oculus.Platform
{
    public enum LeaderboardStartAt : uint
    {
        [Description("TOP")]
        Top,
        [Description("CENTERED_ON_VIEWER")]
        CenteredOnViewer
    }
}
