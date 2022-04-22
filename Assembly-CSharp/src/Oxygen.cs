using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class Oxygen : MonoBehaviour, IProtoEventListener, IEquippable, ISecondaryTooltip
    {
        public float oxygenCapacity;

        public bool isPlayer;

        [NonSerialized]
        [ProtoMember(1)]
        public float oxygenAvailable;

        public float oxygenValue => oxygenAvailable;

        private void Awake()
        {
            if (oxygenAvailable < 0f)
            {
                oxygenAvailable = oxygenCapacity;
            }
        }

        private void Start()
        {
            if (isPlayer)
            {
                OxygenManager oxygenMgr = Player.main.oxygenMgr;
                if (oxygenMgr != null)
                {
                    oxygenMgr.RegisterSource(this);
                }
            }
        }

        private void OnDestroy()
        {
            OxygenManager oxygenMgr = Player.main.oxygenMgr;
            if (oxygenMgr != null)
            {
                oxygenMgr.UnregisterSource(this);
            }
        }

        public float AddOxygen(float amount)
        {
            float num = Mathf.Min(amount, oxygenCapacity - oxygenAvailable);
            oxygenAvailable += num;
            return num;
        }

        public float RemoveOxygen(float amount)
        {
            float num = Mathf.Min(amount, oxygenAvailable);
            oxygenAvailable = Mathf.Max(0f, oxygenAvailable - num);
            return num;
        }

        public int GetSecondsLeft()
        {
            if (oxygenValue > 0.5f)
            {
                return Mathf.RoundToInt(oxygenValue);
            }
            return 0;
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            if (oxygenAvailable < 0f)
            {
                oxygenAvailable = oxygenCapacity;
            }
        }

        public void OnEquip(GameObject sender, string slot)
        {
            OxygenManager oxygenMgr = Player.main.oxygenMgr;
            if (oxygenMgr != null)
            {
                oxygenMgr.RegisterSource(this);
            }
        }

        public void OnUnequip(GameObject sender, string slot)
        {
            OxygenManager oxygenMgr = Player.main.oxygenMgr;
            if (oxygenMgr != null)
            {
                oxygenMgr.UnregisterSource(this);
            }
        }

        public void UpdateEquipped(GameObject sender, string slot)
        {
        }

        public string GetSecondaryTooltip()
        {
            return LanguageCache.GetOxygenText(GetSecondsLeft());
        }
    }
}
