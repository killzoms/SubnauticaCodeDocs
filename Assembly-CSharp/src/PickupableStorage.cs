using UnityEngine;

namespace AssemblyCSharp
{
    public class PickupableStorage : MonoBehaviour, IHandTarget
    {
        [AssertNotNull]
        public Pickupable pickupable;

        [AssertNotNull]
        public StorageContainer storageContainer;

        public string cantPickupHoverText = "LuggageBagNotEmptyCantPickup";

        public string cantPickupClickText = "LuggageBagNotEmptyCantPickup";

        public void OnHandHover(GUIHand hand)
        {
            if (storageContainer.IsEmpty())
            {
                pickupable.OnHandHover(hand);
            }
            else if (!string.IsNullOrEmpty(cantPickupHoverText))
            {
                HandReticle.main.SetInteractText(cantPickupHoverText, string.Empty, translate1: true, translate2: false, addInstructions: false);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            if (storageContainer.IsEmpty())
            {
                pickupable.OnHandClick(hand);
            }
            else if (!string.IsNullOrEmpty(cantPickupClickText))
            {
                ErrorMessage.AddError(Language.main.Get(cantPickupClickText));
            }
        }
    }
}
