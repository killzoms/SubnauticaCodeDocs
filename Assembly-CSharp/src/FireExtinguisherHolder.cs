using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class FireExtinguisherHolder : MonoBehaviour, IHandTarget
    {
        [AssertNotNull]
        public GameObject tankObject;

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version;

        [NonSerialized]
        [ProtoMember(2)]
        public bool hasTank = true;

        [NonSerialized]
        [ProtoMember(3)]
        public float fuel = 100f;

        private void Start()
        {
            tankObject.SetActive(hasTank);
        }

        private void TakeTank()
        {
            GameObject gameObject = CraftData.AddToInventorySync(TechType.FireExtinguisher, 1, noMessage: false, spawnIfCantAdd: false);
            if (gameObject != null)
            {
                hasTank = false;
                tankObject.SetActive(value: false);
                gameObject.GetComponent<FireExtinguisher>().fuel = fuel;
            }
        }

        private void TryStoreTank()
        {
            Pickupable pickupable = Inventory.main.container.RemoveItem(TechType.FireExtinguisher);
            if (pickupable != null)
            {
                FireExtinguisher component = pickupable.GetComponent<FireExtinguisher>();
                if (component != null)
                {
                    fuel = component.fuel;
                }
                hasTank = true;
                tankObject.SetActive(value: true);
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            string interactText = (hasTank ? "TakeFireExtinguisher" : "ReplaceFireExtinguisher");
            HandReticle.main.SetInteractText(interactText);
            HandReticle.main.SetIcon(HandReticle.IconType.Hand);
        }

        public void OnHandClick(GUIHand hand)
        {
            if (hasTank)
            {
                TakeTank();
            }
            else
            {
                TryStoreTank();
            }
        }
    }
}
