using UnityEngine;

namespace AssemblyCSharp
{
    public class TimeDelegate
    {
        public delegate float TimeDelegateFunction();

        private TimeDelegateFunction timeDelegate;

        public float GetTime()
        {
            ProfilingUtils.BeginSample("TimeDelegate.GetTime");
            float result = ((DayNightCycle.main != null) ? DayNightCycle.main.timePassedAsFloat : Time.time);
            if (timeDelegate != null)
            {
                result = timeDelegate();
            }
            ProfilingUtils.EndSample();
            return result;
        }

        public void SetTimeDelegate(TimeDelegateFunction tdf)
        {
            timeDelegate = tdf;
        }
    }
}
