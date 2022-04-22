using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class SupplyCrate : HandTarget, IHandTarget
    {
        [AssertNotNull]
        public string openClipName = "crate_treasure_chest_open_anim";

        [AssertNotNull]
        public string closeClipName = "crate_treasure_chest_close_anim";

        [AssertNotNull]
        public string openText = "Open_SupplyCrate";

        [AssertNotNull]
        public string closeText = "Close_SupplyCrate";

        [AssertNotNull]
        public string snapOpenOnLoad = "crate_treasure_chest_open_static";

        public GameObject setActiveOnOpen;

        [AssertNotNull]
        public FMODAsset openSound;

        private Sealed sealedComp;

        private Pickupable itemInside;

        [NonSerialized]
        [ProtoMember(1)]
        public bool open;

        private void Start()
        {
            sealedComp = GetComponent<Sealed>();
            if (sealedComp != null)
            {
                sealedComp.openedEvent.AddHandler(base.gameObject, OnSealedOpened);
            }
            Animation componentInChildren = GetComponentInChildren<Animation>();
            if (componentInChildren != null && open)
            {
                componentInChildren.Play(snapOpenOnLoad);
            }
        }

        private void FindInsideItemAfterStart()
        {
            itemInside = base.transform.GetComponentInChildren<Pickupable>();
        }

        public void OnHandHover(GUIHand hand)
        {
            FindInsideItemAfterStart();
            bool flag = false;
            if (!open)
            {
                if (sealedComp == null || !sealedComp.IsSealed())
                {
                    HandReticle.main.SetInteractText(openText);
                }
                else
                {
                    HandReticle.main.SetInteractInfo("Sealed_SupplyCrate", "SealedInstructions");
                }
                flag = true;
            }
            else if (itemInside != null)
            {
                HandReticle.main.SetInteractText("TakeItem_SupplyCrate");
                flag = true;
            }
            if (flag)
            {
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        public void OnHandClick(GUIHand guiHand)
        {
            FindInsideItemAfterStart();
            if (sealedComp == null || !sealedComp.IsSealed())
            {
                if (!open)
                {
                    ToggleOpenState();
                }
                else if (itemInside != null)
                {
                    Inventory.main.Pickup(itemInside);
                    itemInside = null;
                }
            }
        }

        private void ToggleOpenState()
        {
            Animation componentInChildren = GetComponentInChildren<Animation>();
            if (componentInChildren != null)
            {
                componentInChildren.Play((!open) ? openClipName : closeClipName);
                open = !open;
                if (open && (bool)setActiveOnOpen)
                {
                    setActiveOnOpen.SetActive(value: true);
                }
                if (open)
                {
                    Utils.PlayFMODAsset(openSound, base.transform);
                }
            }
        }

        private void OnSealedOpened(Sealed sealedComp)
        {
            ToggleOpenState();
        }
    }
}
