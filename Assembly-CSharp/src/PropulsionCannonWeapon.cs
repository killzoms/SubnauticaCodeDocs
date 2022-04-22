using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PropulsionCannonWeapon : PlayerTool, IEquippable
    {
        [AssertNotNull]
        public Animator animator;

        [AssertNotNull]
        public PropulsionCannon propulsionCannon;

        private GameObject cachedObjectForCustomUseText;

        private string cachedCustomUseText = string.Empty;

        private bool firstUse;

        public override string GetCustomUseText()
        {
            if (propulsionCannon.grabbedObject != null)
            {
                if (propulsionCannon.grabbedObject != cachedObjectForCustomUseText)
                {
                    cachedObjectForCustomUseText = propulsionCannon.grabbedObject;
                    cachedCustomUseText = LanguageCache.GetButtonFormat("PropulsionCannonToRelease", GameInput.Button.AltTool);
                }
                return cachedCustomUseText;
            }
            cachedObjectForCustomUseText = null;
            return base.GetCustomUseText();
        }

        public override void OnDraw(Player p)
        {
            TechType techType = pickupable.GetTechType();
            firstUse = !p.IsToolUsed(techType);
            base.OnDraw(p);
        }

        public override void OnHolster()
        {
            base.OnHolster();
            propulsionCannon.ReleaseGrabbedObject();
        }

        public override void OnToolBleederHitAnim(GUIHand guiHand)
        {
            if (usingPlayer != null)
            {
                Bleeder bleeder = usingPlayer.GetComponentInChildren<BleederAttachTarget>().bleeder;
                if (bleeder != null)
                {
                    bleeder.attachAndSuck.SetDetached();
                    propulsionCannon.ReleaseGrabbedObject();
                    propulsionCannon.GrabObject(bleeder.gameObject);
                }
            }
        }

        public override FMODAsset GetBleederHitSound(FMODAsset defaultSound)
        {
            return null;
        }

        public override bool OnExitDown()
        {
            if (usingPlayer != null && !usingPlayer.IsInSub())
            {
                propulsionCannon.ReleaseGrabbedObject();
                return true;
            }
            return false;
        }

        public override bool OnAltDown()
        {
            if (usingPlayer != null && usingPlayer.IsInSub())
            {
                return false;
            }
            if (propulsionCannon.IsGrabbingObject())
            {
                propulsionCannon.ReleaseGrabbedObject();
            }
            else if (propulsionCannon.HasChargeForShot() && !propulsionCannon.OnReload(new List<IItemsContainer> { Inventory.main.container }))
            {
                ErrorMessage.AddMessage(Language.main.Get("PropulsionCannonNoItems"));
            }
            return true;
        }

        public override bool OnRightHandDown()
        {
            if (usingPlayer != null && usingPlayer.IsInSub())
            {
                return false;
            }
            return propulsionCannon.OnShoot();
        }

        public override void OnToolReloadBeginAnim(GUIHand guiHand)
        {
            base.OnToolReloadBeginAnim(guiHand);
            propulsionCannon.ReleaseGrabbedObject();
        }

        public void OnEquip(GameObject sender, string slot)
        {
            if (base.isDrawn && firstUse)
            {
                propulsionCannon.PlayFirstUseFxAndSound();
                animator.SetBool("using_tool_first", value: true);
            }
        }

        public void OnUnequip(GameObject sender, string slot)
        {
            if (firstUse)
            {
                propulsionCannon.StopFirstUseFxAndSound();
                animator.SetBool("using_tool_first", value: false);
            }
        }

        public void UpdateEquipped(GameObject sender, string slot)
        {
            if (usingPlayer != null && !usingPlayer.IsInSub())
            {
                propulsionCannon.usingCannon = GameInput.GetButtonHeld(GameInput.Button.RightHand);
                propulsionCannon.UpdateActive();
                SafeAnimator.SetBool(Player.main.armsController.GetComponent<Animator>(), "cangrab_propulsioncannon", propulsionCannon.canGrab || propulsionCannon.grabbedObject != null);
            }
        }
    }
}
