using UnityEngine;

namespace AssemblyCSharp
{
    [SkipProtoContractCheck]
    public class IntroFireExtinguisherHandTarget : HandTarget, IHandTarget
    {
        public GameObject extinguisherModel;

        private void Start()
        {
            if (Utils.GetContinueMode())
            {
                extinguisherModel.SetActive(value: false);
                Object.Destroy(base.gameObject);
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractText("Pickup_FireExtinguisher");
            HandReticle.main.SetIcon(HandReticle.IconType.Hand);
        }

        public void OnHandClick(GUIHand hand)
        {
            UseVolume();
        }

        private void UseVolume()
        {
            if ((bool)extinguisherModel)
            {
                extinguisherModel.SetActive(value: false);
            }
            CraftData.AddToInventory(TechType.FireExtinguisher);
            Inventory.main.SecureItems(verbose: false);
            Object.Destroy(base.gameObject);
        }
    }
}
