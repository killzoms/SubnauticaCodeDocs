using System.ComponentModel;

namespace AssemblyCSharp.Oculus.Platform
{
    public enum KeyValuePairType : uint
    {
        [Description("STRING")]
        String,
        [Description("INT")]
        Int,
        [Description("DOUBLE")]
        Double
    }
}
