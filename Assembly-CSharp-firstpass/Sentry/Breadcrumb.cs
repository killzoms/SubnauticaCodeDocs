using System;
using System.Collections.Generic;

namespace Sentry
{
    [Serializable]
    public class Breadcrumb
    {
        public const int MaxBreadcrumbs = 100;

        public string timestamp;

        public string message;

        public Breadcrumb(string timestamp, string message)
        {
            this.timestamp = timestamp;
            this.message = message;
        }

        public static List<Breadcrumb> CombineBreadcrumbs(Breadcrumb[] breadcrumbs, int index, int number)
        {
            List<Breadcrumb> list = new List<Breadcrumb>(number);
            int num = (index + 100 - number) % 100;
            for (int i = 0; i < number; i++)
            {
                list.Add(breadcrumbs[(i + num) % 100]);
            }
            return list;
        }
    }
}
