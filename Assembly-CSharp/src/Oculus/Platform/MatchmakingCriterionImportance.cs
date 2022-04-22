using System.ComponentModel;

namespace AssemblyCSharp.Oculus.Platform
{
    public enum MatchmakingCriterionImportance : uint
    {
        [Description("REQUIRED")]
        Required,
        [Description("HIGH")]
        High,
        [Description("MEDIUM")]
        Medium,
        [Description("LOW")]
        Low
    }
}
