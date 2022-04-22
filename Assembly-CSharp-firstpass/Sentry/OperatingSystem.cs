using System;

namespace Sentry
{
    [Serializable]
    public class OperatingSystem
    {
        public string name;

        public string version;

        public string raw_description;

        public string build;

        public string kernel_version;
    }
}
