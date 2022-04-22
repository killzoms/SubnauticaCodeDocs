namespace AssemblyCSharp
{
    public class PlatformServicesArc : PlatformServicesNull
    {
        public PlatformServicesArc()
            : base(PlatformServicesNull.DefaultSavePath)
        {
        }

        public static bool IsPresent()
        {
            return PlatformServicesUtils.IsRuntimePluginDllPresent("ArcSDK");
        }

        public override string GetName()
        {
            return "Arc";
        }
    }
}
