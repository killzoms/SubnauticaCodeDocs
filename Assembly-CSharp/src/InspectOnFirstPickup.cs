using UnityEngine;

namespace AssemblyCSharp
{
    public class InspectOnFirstPickup : MonoBehaviour
    {
        [AssertNotNull]
        public Pickupable pickupAble;

        [AssertNotNull]
        public Collider collision;

        [AssertNotNull]
        public Rigidbody rigidBody;

        public string animParam;

        public bool restoreQuickSlot = true;

        public float inspectDuration = 4.34f;

        private Transform inventoryTransform;

        private void Start()
        {
            pickupAble.pickedUpEvent.AddHandler(base.gameObject, OnPickedUp);
        }

        private void OnPickedUp(Pickupable p)
        {
            if (!GameOptions.GetVrAnimationMode() && !Player.main.isPiloting && Player.main.AddUsedTool(pickupAble.GetTechType()))
            {
                Player.main.armsController.StartInspectObjectAsync(this);
            }
        }

        public void OnInspectObjectBegin()
        {
            rigidBody.isKinematic = true;
            collision.enabled = false;
            inventoryTransform = base.transform.parent;
            base.transform.SetParent(Inventory.main.toolSocket);
            base.transform.localPosition = Vector3.zero;
            base.transform.localRotation = Quaternion.identity;
        }

        public void OnInspectObjectDone()
        {
            collision.enabled = true;
            base.transform.SetParent(inventoryTransform);
        }
    }
}
