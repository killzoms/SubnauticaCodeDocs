using System;
using System.IO;

namespace AssemblyCSharp
{
    public class PlatformServicesOculus : PlatformServicesNull
    {
        private static string GetSavePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Subnautica");
        }

        public PlatformServicesOculus()
            : base(GetSavePath())
        {
        }

        public override string GetName()
        {
            return "Oculus";
        }
    }
}
