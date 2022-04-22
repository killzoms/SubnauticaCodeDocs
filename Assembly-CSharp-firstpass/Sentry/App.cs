using System;

namespace Sentry
{
    [Serializable]
    public class App
    {
        public string app_identifier;

        public string app_start_time;

        public string device_app_hash;

        public string build_type;

        public string app_name;

        public string app_version;

        public string app_build;
    }
}
