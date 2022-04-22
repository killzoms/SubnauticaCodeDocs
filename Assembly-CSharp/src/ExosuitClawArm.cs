using UnityEngine;

namespace AssemblyCSharp
{
    public class ExosuitClawArm : MonoBehaviour, IExosuitArm
    {
        public const float kGrabDistance = 6f;

        public Animator animator;

        public FMODAsset hitTerrainSound;

        public FMODAsset hitFishSound;

        public FMODAsset pickupSound;

        public Transform front;

        public VFXEventTypes vfxEventType;

        public VFXController fxControl;

        public float cooldownPunch = 1f;

        public float cooldownPickup = 1.533f;

        private const float attackDist = 6.5f;

        private const float damage = 50f;

        private const DamageType damageType = DamageType.Normal;

        private float timeUsed = float.NegativeInfinity;

        private float cooldownTime;

        private bool shownNoRoomNotification;

        private Exosuit exosuit;

        GameObject IExosuitArm.GetGameObject()
        {
            return base.gameObject;
        }

        void IExosuitArm.SetSide(Exosuit.Arm arm)
        {
            exosuit = GetComponentInParent<Exosuit>();
            if (arm == Exosuit.Arm.Right)
            {
                base.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else
            {
                base.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }

        bool IExosuitArm.OnUseDown(out float cooldownDuration)
        {
            return TryUse(out cooldownDuration);
        }

        bool IExosuitArm.OnUseHeld(out float cooldownDuration)
        {
            return TryUse(out cooldownDuration);
        }

        bool IExosuitArm.OnUseUp(out float cooldownDuration)
        {
            cooldownDuration = 0f;
            return true;
        }

        bool IExosuitArm.OnAltDown()
        {
            return false;
        }

        void IExosuitArm.Update(ref Quaternion aimDirection)
        {
        }

        void IExosuitArm.Reset()
        {
        }

        private bool TryUse(out float cooldownDuration)
        {
            if (Time.time - timeUsed >= cooldownTime)
            {
                Pickupable pickupable = null;
                PickPrefab pickPrefab = null;
                if ((bool)exosuit.GetActiveTarget())
                {
                    pickupable = exosuit.GetActiveTarget().GetComponent<Pickupable>();
                    pickPrefab = exosuit.GetActiveTarget().GetComponent<PickPrefab>();
                }
                if (!(pickupable != null) || !pickupable.isPickupable)
                {
                    if (pickPrefab != null)
                    {
                        animator.SetTrigger("use_tool");
                        cooldownTime = (cooldownDuration = cooldownPickup);
                        return true;
                    }
                    animator.SetTrigger("bash");
                    cooldownTime = (cooldownDuration = cooldownPunch);
                    fxControl.Play(0);
                    return true;
                }
                if (exosuit.storageContainer.container.HasRoomFor(pickupable))
                {
                    animator.SetTrigger("use_tool");
                    cooldownTime = (cooldownDuration = cooldownPickup);
                    shownNoRoomNotification = false;
                    return true;
                }
                if (!shownNoRoomNotification)
                {
                    ErrorMessage.AddMessage(Language.main.Get("ContainerCantFit"));
                    shownNoRoomNotification = true;
                }
            }
            cooldownDuration = 0f;
            return false;
        }

        public void OnPickup()
        {
            Exosuit componentInParent = GetComponentInParent<Exosuit>();
            if ((bool)componentInParent.GetActiveTarget())
            {
                Pickupable component = componentInParent.GetActiveTarget().GetComponent<Pickupable>();
                PickPrefab component2 = componentInParent.GetActiveTarget().GetComponent<PickPrefab>();
                if (component != null && component.isPickupable && componentInParent.storageContainer.container.HasRoomFor(component))
                {
                    component = component.Initialize();
                    InventoryItem item = new InventoryItem(component);
                    componentInParent.storageContainer.container.UnsafeAdd(item);
                    Utils.PlayFMODAsset(pickupSound, front, 5f);
                }
                else if (component2 != null && component2.AddToContainer(componentInParent.storageContainer.container))
                {
                    component2.SetPickedUp();
                }
            }
        }

        public void OnHit()
        {
            Exosuit componentInParent = GetComponentInParent<Exosuit>();
            if (!componentInParent.CanPilot() || !componentInParent.GetPilotingMode())
            {
                return;
            }
            Vector3 position = default(Vector3);
            GameObject closestObj = null;
            global::UWE.Utils.TraceFPSTargetPosition(componentInParent.gameObject, 6.5f, ref closestObj, ref position);
            if (closestObj == null)
            {
                InteractionVolumeUser component = Player.main.gameObject.GetComponent<InteractionVolumeUser>();
                if (component != null && component.GetMostRecent() != null)
                {
                    closestObj = component.GetMostRecent().gameObject;
                }
            }
            if ((bool)closestObj)
            {
                LiveMixin liveMixin = closestObj.FindAncestor<LiveMixin>();
                if ((bool)liveMixin)
                {
                    liveMixin.IsAlive();
                    liveMixin.TakeDamage(50f, position);
                    Utils.PlayFMODAsset(hitFishSound, front, 50f);
                }
                else
                {
                    Utils.PlayFMODAsset(hitTerrainSound, front, 50f);
                }
                VFXSurface component2 = closestObj.GetComponent<VFXSurface>();
                Vector3 euler = MainCameraControl.main.transform.eulerAngles + new Vector3(300f, 90f, 0f);
                VFXSurfaceTypeManager.main.Play(component2, vfxEventType, position, Quaternion.Euler(euler), componentInParent.gameObject.transform);
                closestObj.SendMessage("BashHit", this, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
