using System;
using System.Collections;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class PickPrefab : HandTarget, IProtoEventListener, IHandTarget, ICompileTimeCheckable
    {
        public TechType pickTech;

        public bool destroyOnPicked;

        private bool isAddingToInventory;

        [NonSerialized]
        [ProtoMember(1)]
        public bool pickedState;

        [NonSerialized]
        public readonly Event<PickPrefab> pickedEvent = new Event<PickPrefab>();

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            SetPickedState(pickedState);
        }

        public void Start()
        {
        }

        private bool AllowedToPickUp()
        {
            Vector2int itemSize = CraftData.GetItemSize(pickTech);
            return Player.main.HasInventoryRoom(itemSize.x, itemSize.y);
        }

        public void OnHandHover(GUIHand hand)
        {
            if (base.gameObject.activeInHierarchy)
            {
                if (AllowedToPickUp())
                {
                    string pickupText = LanguageCache.GetPickupText(pickTech);
                    HandReticle.main.SetInteractText(pickupText, translate: false, HandReticle.Hand.Left);
                    HandReticle.main.SetIcon(HandReticle.IconType.Hand);
                }
                else
                {
                    HandReticle.main.SetInteractText(pickTech.AsString(), "InventoryFull", translate1: true, translate2: false, HandReticle.Hand.None);
                }
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            if (base.gameObject.activeInHierarchy && AllowedToPickUp() && !isAddingToInventory)
            {
                isAddingToInventory = true;
                StartCoroutine(AddToInventoryRoutine());
            }
        }

        public void SetPickedUp()
        {
            pickedEvent.Trigger(this);
            SetPickedState(newPickedState: true);
        }

        public bool AddToContainer(ItemsContainer container)
        {
            GameObject gameObject = CraftData.InstantiateFromPrefab(pickTech);
            if ((bool)gameObject)
            {
                Pickupable component = gameObject.GetComponent<Pickupable>();
                if (!component)
                {
                    global::UnityEngine.Object.Destroy(gameObject);
                    return false;
                }
                if (!container.HasRoomFor(component))
                {
                    global::UnityEngine.Object.Destroy(gameObject);
                    return false;
                }
                component = component.Initialize();
                InventoryItem item = new InventoryItem(component);
                container.UnsafeAdd(item);
                return true;
            }
            return false;
        }

        public void SetPickedState(bool newPickedState)
        {
            pickedState = newPickedState;
            base.gameObject.SetActive(!newPickedState);
            if (newPickedState && destroyOnPicked)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        public bool GetPickedState()
        {
            return pickedState;
        }

        public string CompileTimeCheck()
        {
            if (pickTech != 0)
            {
                return null;
            }
            return "Tech type not set";
        }

        private IEnumerator AddToInventoryRoutine()
        {
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            yield return CraftData.AddToInventoryRoutine(pickTech, 1, noMessage: false, spawnIfCantAdd: false, result);
            if ((bool)result.Get())
            {
                SetPickedUp();
            }
            isAddingToInventory = false;
        }
    }
}
