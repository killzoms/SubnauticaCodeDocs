using UnityEngine;

namespace AssemblyCSharp
{
    public class DisableWaterRenderingVolume : MonoBehaviour
    {
        public enum WaterStates
        {
            Disable,
            Enable
        }

        public WaterStates changeOnEnter = WaterStates.Enable;

        private void OnTriggerEnter(Collider col)
        {
            GameObject entityRoot = global::UWE.Utils.GetEntityRoot(col.gameObject);
            if (!entityRoot)
            {
                entityRoot = col.gameObject;
            }
            if (global::UWE.Utils.GetComponentInHierarchy<Player>(entityRoot) != null)
            {
                Player.main.SetDisplaySurfaceWater(changeOnEnter == WaterStates.Enable);
            }
        }
    }
}
