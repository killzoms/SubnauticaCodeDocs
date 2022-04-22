using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry
{
    [Serializable]
    public class SentryEvent
    {
        public string event_id;

        public string message;

        public string timestamp;

        public string logger;

        public string level;

        public string platform = "csharp";

        public string release;

        public Context contexts;

        public SdkVersion sdk = new SdkVersion();

        public List<Breadcrumb> breadcrumbs;

        public User user = new User();

        public Tags tags;

        public Extra extra;

        public SentryEvent(string message, List<Breadcrumb> breadcrumbs = null)
        {
            event_id = Guid.NewGuid().ToString("N");
            this.message = message;
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            level = "error";
            this.breadcrumbs = breadcrumbs;
            contexts = new Context();
            release = Application.version;
            tags = new Tags();
            extra = new Extra();
        }
    }
}
