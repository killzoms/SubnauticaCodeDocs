using System.Collections.Generic;
using System.Diagnostics;

namespace AssemblyCSharp.Exploder
{
    public static class Profiler
    {
        private static readonly Dictionary<string, Stopwatch> timeSegments = new Dictionary<string, Stopwatch>();

        public static void Start(string key)
        {
            Stopwatch value = null;
            if (timeSegments.TryGetValue(key, out value))
            {
                value.Reset();
                value.Start();
            }
            else
            {
                value = new Stopwatch();
                value.Start();
                timeSegments.Add(key, value);
            }
        }

        public static void End(string key)
        {
            timeSegments[key].Stop();
        }

        public static string[] PrintResults()
        {
            string[] array = new string[timeSegments.Count];
            int num = 0;
            foreach (KeyValuePair<string, Stopwatch> timeSegment in timeSegments)
            {
                array[num++] = timeSegment.Key + " " + timeSegment.Value.ElapsedMilliseconds + " [ms]";
            }
            return array;
        }
    }
}
