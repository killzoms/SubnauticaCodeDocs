using System;

namespace Sentry
{
    [Serializable]
    public class Gpu
    {
        public string name;

        public int id;

        public int vendor_id;

        public string vendor_name;

        public int memory_size;

        public string api_type;

        public bool multi_threaded_rendering;

        public string version;

        public string npot_support;
    }
}
