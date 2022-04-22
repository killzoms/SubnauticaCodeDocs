using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class PrecursorDoorKeyColumn : HandTarget, IHandTarget
    {
        public GameObject glowFX;

        public BoxCollider boxCollider;

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public bool unlocked;

        public void SlotKey(GameObject keyObject)
        {
            if (!unlocked)
            {
                unlocked = true;
                if ((bool)boxCollider)
                {
                    boxCollider.enabled = false;
                }
                base.gameObject.BroadcastMessage("ToggleDoor", true, SendMessageOptions.RequireReceiver);
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            if (!unlocked)
            {
                HandReticle.main.SetInteractText("Insert_Precursor_Key");
            }
        }

        public void OnHandClick(GUIHand hand)
        {
        }
    }
}
