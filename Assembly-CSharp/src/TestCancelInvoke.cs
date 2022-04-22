using UnityEngine;

namespace AssemblyCSharp
{
    public class TestCancelInvoke : MonoBehaviour
    {
        public bool bCancelFlag;

        private void OnEnable()
        {
            ProfilingUtils.BeginSample("Invoke-ByName");
            Invoke("Something", 5f);
            ProfilingUtils.EndSample();
        }

        private void OnDisable()
        {
            if (bCancelFlag)
            {
                ProfilingUtils.BeginSample("CancelInvoke-ByName");
                CancelInvoke("Something");
                ProfilingUtils.EndSample();
            }
            else
            {
                ProfilingUtils.BeginSample("CancelInvoke-All");
                CancelInvoke();
                ProfilingUtils.EndSample();
            }
        }

        private void Something()
        {
        }
    }
}
