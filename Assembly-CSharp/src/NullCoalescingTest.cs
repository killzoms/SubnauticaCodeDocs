using UnityEngine;

namespace AssemblyCSharp
{
    public class NullCoalescingTest : MonoBehaviour
    {
        private int[] array;

        private int[] array2 = new int[0];

        private void Update()
        {
            int[] array = this.array;
            this.array = array2;
            array2 = array;
            ProfilingUtils.BeginSample("operator?? [0]");
            if (this.array == null)
            {
                _ = new int[0];
            }
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("operator?? [10]");
            if (this.array == null)
            {
                _ = new int[10];
            }
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("if null [10]");
            if (this.array == null)
            {
                _ = new int[10];
            }
            ProfilingUtils.EndSample();
        }
    }
}
