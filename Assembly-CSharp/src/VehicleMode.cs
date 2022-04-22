using UnityEngine;

namespace AssemblyCSharp
{
    public class VehicleMode : MonoBehaviour
    {
        private VehicleHatch srcHatch;

        public void Deactivate()
        {
            if (srcHatch != null)
            {
                base.transform.position = srcHatch.GetDiverSpawnPosition();
                srcHatch.OnVehicleReturned();
            }
            srcHatch = null;
        }
    }
}
