using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class TimeCapsuleItem
    {
        [NonSerialized]
        [ProtoMember(1)]
        public TechType techType;

        [NonSerialized]
        [ProtoMember(2)]
        public bool hasBattery;

        [NonSerialized]
        [ProtoMember(3)]
        public TechType batteryType;

        [NonSerialized]
        [ProtoMember(4)]
        public float batteryCharge = -1f;

        public bool IsValid()
        {
            return true & (techType != TechType.None);
        }

        public Pickupable Spawn()
        {
            GameObject prefabForTechType = CraftData.GetPrefabForTechType(techType);
            if (prefabForTechType == null)
            {
                return null;
            }
            GameObject gameObject = global::UnityEngine.Object.Instantiate(prefabForTechType);
            Pickupable component = gameObject.GetComponent<Pickupable>();
            if (component != null)
            {
                if (hasBattery)
                {
                    EnergyMixin component2 = gameObject.GetComponent<EnergyMixin>();
                    if (component2 != null)
                    {
                        component2.SetBattery(batteryType, batteryCharge);
                    }
                    else
                    {
                        Debug.LogErrorFormat("Time Capsule item deserialization error - deserialized item TechType.{0} is supposed to have battery but EnergyMixin component was not found on spawned GameObject", techType.AsString());
                    }
                }
            }
            else
            {
                global::UnityEngine.Object.Destroy(gameObject);
                Debug.LogErrorFormat("Time Capsule item deserialization failed - Pickupable component missing on spawned prefab for TechType.{0}", techType.AsString());
            }
            return component;
        }
    }
}
