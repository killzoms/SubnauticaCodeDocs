using UnityEngine;

namespace AssemblyCSharp
{
    public class VehicleHatch : HandTarget, IHandTarget
    {
        public GameObject vehicleModel;

        public Vector3 GetDiverSpawnPosition()
        {
            return base.transform.position + new Vector3(0f, 2f, 0f);
        }

        public void OnVehicleReturned()
        {
            vehicleModel.SetActive(value: true);
        }

        public void OnVehicleLaunched()
        {
            vehicleModel.SetActive(value: false);
        }

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText("DriveVehicle");
            HandReticle.main.SetIcon(HandReticle.IconType.Hand);
        }

        public void OnHandClick(GUIHand hand)
        {
        }
    }
}
