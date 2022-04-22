using UnityEngine;

namespace AssemblyCSharp
{
    public class LayerMaskTest : MonoBehaviour
    {
        public string[] layerNames;

        [ReadOnly]
        public int layerMask;

        [ReadOnly]
        public int[] layers;

        private void Update()
        {
            if (layerNames != null)
            {
                ProfilingUtils.BeginSample("GetMask");
                layerMask = LayerMask.GetMask(layerNames);
                ProfilingUtils.EndSample();
                layers = new int[layerNames.Length];
                ProfilingUtils.BeginSample("NameToLayer");
                for (int i = 0; i < layerNames.Length; i++)
                {
                    layers[i] = LayerMask.NameToLayer(layerNames[i]);
                }
                ProfilingUtils.EndSample();
            }
        }
    }
}
