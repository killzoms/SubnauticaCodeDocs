using System;

namespace Sentry
{
    [Serializable]
    public class Device
    {
        public string name;

        public string family;

        public string model;

        public string model_id;

        public string arch;

        public string cpu_description;

        public float battery_level;

        public string battery_status;

        public string orientation;

        public bool simulator;

        public long memory_size;

        public DateTimeOffset? boot_time;

        public string timezone;

        public string device_type;
    }
}
