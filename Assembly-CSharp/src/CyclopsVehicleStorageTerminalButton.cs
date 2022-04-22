using UnityEngine;
using UnityEngine.EventSystems;

namespace AssemblyCSharp
{
    public class CyclopsVehicleStorageTerminalButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
    {
        public CyclopsVehicleStorageTerminalManager.VehicleStorageType vehicleStorageType;

        public int slotID;

        [AssertNotNull]
        public CyclopsVehicleStorageTerminalManager terminalManager;

        [AssertNotNull(AssertNotNullAttribute.Options.IgnorePrefabs)]
        public Animator animator;

        public void OnPointerEnter(PointerEventData data)
        {
            HandReticle.main.SetIcon(HandReticle.IconType.Interact);
            animator.SetTrigger("Highlighted");
        }

        public void OnPointerExit(PointerEventData data)
        {
            HandReticle.main.SetIcon(HandReticle.IconType.Default);
            animator.SetTrigger("Normal");
        }

        public void OnPointerClick(PointerEventData data)
        {
            animator.SetTrigger("Pressed");
            if (vehicleStorageType == CyclopsVehicleStorageTerminalManager.VehicleStorageType.Module)
            {
                terminalManager.ModuleButtonClick();
            }
            else
            {
                terminalManager.StorageButtonClick(vehicleStorageType, slotID);
            }
        }
    }
}
