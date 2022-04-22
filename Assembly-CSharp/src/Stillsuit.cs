using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class Stillsuit : MonoBehaviour, IEquippable
    {
        public Eatable waterPrefab;

        private Pickupable pickupableWaterPrefab;

        public PDANotification stillsuitEquippedNotification;

        [NonSerialized]
        [ProtoMember(1)]
        public float waterCaptured;

        private void Start()
        {
            pickupableWaterPrefab = waterPrefab.gameObject.GetComponent<Pickupable>();
        }

        public void OnEquip(GameObject sender, string slot)
        {
            stillsuitEquippedNotification.Play();
        }

        public void OnUnequip(GameObject sender, string slot)
        {
        }

        public void UpdateEquipped(GameObject sender, string slot)
        {
            if (GameModeUtils.RequiresSurvival() && !Player.main.GetComponent<Survival>().freezeStats)
            {
                float num = Time.deltaTime / 1800f * 100f;
                waterCaptured += num * 0.75f;
                if (waterCaptured >= waterPrefab.waterValue)
                {
                    Pickupable component = global::UnityEngine.Object.Instantiate(waterPrefab.gameObject).GetComponent<Pickupable>();
                    Inventory.main.ForcePickup(component);
                    waterCaptured -= waterPrefab.waterValue;
                }
            }
        }
    }
}
