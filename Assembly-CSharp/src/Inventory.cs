using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class Inventory : MonoBehaviour, IProtoEventListener
    {
        public Transform toolSocket;

        public Transform cameraSocket;

        public PDANotification inventorySecured;

        public float throwForce = 1.5f;

        public bool debugQuickSlots;

        public static Inventory main;

        private static int layerMask;

        private ItemsContainer _container;

        private List<IItemsContainer> usedStorage = new List<IItemsContainer>();

        private GameObject equipmentRoot;

        private Equipment _equipment;

        private Transform pendingItemsRoot;

        private List<Pickupable> _pendingItems;

        private bool tryFlushPendingItems;

        private bool isTerminating;

        private const int currentVersion = 6;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 6;

        [NonSerialized]
        [ProtoMember(2, OverwriteList = true)]
        public byte[] serializedStorage;

        [NonSerialized]
        [ProtoMember(3, OverwriteList = true)]
        public string[] serializedQuickSlots;

        [NonSerialized]
        [ProtoMember(4, OverwriteList = true)]
        public byte[] serializedEquipment;

        [NonSerialized]
        [ProtoMember(5, OverwriteList = true)]
        public Dictionary<string, string> serializedEquipmentSlots;

        [NonSerialized]
        [ProtoMember(7, OverwriteList = true)]
        public byte[] serializedPendingItems;

        public ItemsContainer container => _container;

        public Equipment equipment => _equipment;

        public QuickSlots quickSlots { get; private set; }

        public GameObject storageRoot { get; private set; }

        public static Inventory Get()
        {
            return main;
        }

        private void Awake()
        {
            if (main != null)
            {
                global::UnityEngine.Object.DestroyImmediate(this);
                return;
            }
            main = this;
            Player component = GetComponent<Player>();
            Transform component2 = component.GetComponent<Transform>();
            component.currentSubChangedEvent.AddHandler(base.gameObject, OnCurrentSubRootChanged);
            storageRoot = new GameObject("Inventory Storage");
            Transform component3 = storageRoot.GetComponent<Transform>();
            component3.parent = component2;
            global::UWE.Utils.ZeroTransform(component3);
            storageRoot.AddComponent<StoreInformationIdentifier>();
            _container = new ItemsContainer(6, 8, component3, "InventoryStorageLabel", null);
            _container.onAddItem += OnAddItem;
            _container.onRemoveItem += OnRemoveItem;
            quickSlots = new QuickSlots(base.gameObject, toolSocket, cameraSocket, this, component.rightHandSlot, 5);
            equipmentRoot = new GameObject("Equipment Storage");
            Transform component4 = equipmentRoot.GetComponent<Transform>();
            component4.parent = component2;
            global::UWE.Utils.ZeroTransform(component4);
            equipmentRoot.AddComponent<StoreInformationIdentifier>();
            _equipment = new Equipment(component.gameObject, component4);
            _equipment.SetLabel("EquipmentLabel");
            _equipment.onEquip += OnEquip;
            _equipment.onUnequip += OnUnequip;
            UnlockDefaultEquipmentSlots();
            GameObject gameObject = new GameObject("Pending Items");
            pendingItemsRoot = gameObject.GetComponent<Transform>();
            pendingItemsRoot.parent = component2;
            global::UWE.Utils.ZeroTransform(pendingItemsRoot);
            gameObject.AddComponent<StoreInformationIdentifier>();
            _pendingItems = new List<Pickupable>();
            layerMask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("TouchScreen")));
            DevConsole.RegisterConsoleCommand(this, "logpendingitems");
        }

        public void UpdateContainers()
        {
            ((IItemsContainer)_container).UpdateContainer();
            int count = usedStorage.Count;
            if (count == 0)
            {
                ((IItemsContainer)_equipment).UpdateContainer();
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    usedStorage[i]?.UpdateContainer();
                }
            }
            quickSlots.Update();
            FlushPendingItems();
        }

        private void OnDestroy()
        {
            _equipment.onEquip -= OnEquip;
            _equipment.onUnequip -= OnUnequip;
        }

        private void OnApplicationQuit()
        {
            isTerminating = true;
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            int activeSlot = quickSlots.activeSlot;
            quickSlots.DeselectImmediate();
            serializedStorage = StorageHelper.Save(serializer, storageRoot);
            serializedQuickSlots = quickSlots.SaveBinding();
            serializedEquipment = StorageHelper.Save(serializer, equipmentRoot);
            serializedEquipmentSlots = _equipment.SaveEquipment();
            serializedPendingItems = StorageHelper.Save(serializer, pendingItemsRoot.gameObject);
            quickSlots.SelectImmediate(activeSlot);
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            ResetInventory();
            StorageHelper.RestoreItems(serializer, serializedStorage, _container);
            if (serializedEquipment != null && serializedEquipmentSlots != null)
            {
                StorageHelper.RestoreEquipment(serializer, serializedEquipment, serializedEquipmentSlots, _equipment);
            }
            UnlockDefaultEquipmentSlots();
            StorageHelper.RestorePendingItems(serializer, serializedPendingItems, pendingItemsRoot, _pendingItems);
            if (serializedQuickSlots != null)
            {
                quickSlots.RestoreBinding(serializedQuickSlots);
            }
            CraftTree.Initialize();
        }

        public void ResetInventory()
        {
            quickSlots.DeselectImmediate();
            _pendingItems.Clear();
            _container.Clear();
            _equipment.Clear();
            StorageHelper.RenewIdentifier(storageRoot);
            StorageHelper.RenewIdentifier(equipmentRoot);
            StorageHelper.RenewIdentifier(pendingItemsRoot.gameObject);
        }

        public void SetUsedStorage(IItemsContainer container, bool append = false)
        {
            if (!append)
            {
                ClearUsedStorage();
            }
            usedStorage.Add(container);
        }

        public IItemsContainer GetUsedStorage(int index)
        {
            if (index < 0 || index >= usedStorage.Count)
            {
                return null;
            }
            return usedStorage[index];
        }

        public int GetUsedStorageCount()
        {
            return usedStorage.Count;
        }

        public void ClearUsedStorage()
        {
            usedStorage.Clear();
        }

        public bool IsUsingStorage(IItemsContainer container)
        {
            return usedStorage.Contains(container);
        }

        public int GetPickupCount(TechType pickupType)
        {
            return _container.GetCount(pickupType);
        }

        public int GetTotalItemCount()
        {
            return _container.count;
        }

        public bool HasRoomFor(Pickupable item)
        {
            return _container.HasRoomFor(item);
        }

        public bool HasRoomFor(int width, int height)
        {
            return _container.HasRoomFor(width, height);
        }

        public bool Contains(Pickupable p)
        {
            return _container.Contains(p);
        }

        public bool DestroyItem(TechType destroyTechType, bool allowGenerics = false)
        {
            return _container.DestroyItem(destroyTechType);
        }

        public bool TryRemoveItem(Pickupable pickupable)
        {
            if (!_container.RemoveItem(pickupable))
            {
                return _equipment.RemoveItem(pickupable);
            }
            return true;
        }

        public void SecureItems(bool verbose)
        {
            _container.SecureItems();
            _equipment.SecureItems();
        }

        public bool LoseItems()
        {
            List<InventoryItem> list = new List<InventoryItem>();
            foreach (InventoryItem item in (IEnumerable<InventoryItem>)main.container)
            {
                if (item.item.destroyOnDeath)
                {
                    list.Add(item);
                }
            }
            foreach (InventoryItem item2 in (IItemsContainer)main.equipment)
            {
                if (item2.item.destroyOnDeath)
                {
                    list.Add(item2);
                }
            }
            bool result = false;
            for (int i = 0; i < list.Count - 1; i++)
            {
                if (InternalDropItem(list[i].item, notify: false))
                {
                    result = true;
                }
            }
            return result;
        }

        public void OnCurrentSubRootChanged(SubRoot subRoot)
        {
            if (!(subRoot == null))
            {
                SecureItems(verbose: true);
            }
        }

        public bool GetCanBindItem(InventoryItem item)
        {
            if (item == null || !item.isBindable)
            {
                return false;
            }
            return item.container == _container;
        }

        public ItemAction GetUseItemAction(InventoryItem item)
        {
            if (item == null)
            {
                return ItemAction.None;
            }
            Pickupable item2 = item.item;
            TechType techType = item2.GetTechType();
            IItemsContainer itemsContainer = item.container;
            IItemsContainer oppositeContainer = GetOppositeContainer(item);
            Equipment obj = itemsContainer as Equipment;
            Equipment equipment = oppositeContainer as Equipment;
            bool flag = obj != null;
            bool flag2 = equipment != null;
            if (itemsContainer == _container)
            {
                if (item2.gameObject.GetComponent<Eatable>() != null && GameModeUtils.RequiresSurvival())
                {
                    return ItemAction.Eat;
                }
                if (CraftData.IsInvUseable(techType))
                {
                    return ItemAction.Use;
                }
                if (item.isBindable)
                {
                    return ItemAction.Assign;
                }
            }
            if (oppositeContainer != null)
            {
                if (flag)
                {
                    if (!flag2 && item.CanDrag(verbose: false) && oppositeContainer.HasRoomFor(item2, null))
                    {
                        return ItemAction.Unequip;
                    }
                }
                else if (flag2)
                {
                    EquipmentType equipmentType = CraftData.GetEquipmentType(techType);
                    if (equipment.GetFreeSlot(equipmentType, out var result))
                    {
                        return ItemAction.Equip;
                    }
                    if (equipment.GetCompatibleSlot(equipmentType, out result))
                    {
                        return ItemAction.Swap;
                    }
                }
            }
            return ItemAction.None;
        }

        public ItemAction GetAltUseItemAction(InventoryItem item)
        {
            if (item == null)
            {
                return ItemAction.None;
            }
            IItemsContainer itemsContainer = item.container;
            IItemsContainer oppositeContainer = GetOppositeContainer(item);
            bool flag = itemsContainer != null && itemsContainer is Equipment;
            bool flag2 = oppositeContainer != null && oppositeContainer is Equipment;
            if (oppositeContainer != null && !flag2 && !flag)
            {
                if (item.CanDrag(verbose: false))
                {
                    return ItemAction.Switch;
                }
            }
            else if (itemsContainer == _container && CanDropItemHere(item.item))
            {
                return ItemAction.Drop;
            }
            return ItemAction.None;
        }

        public void UseItem(InventoryItem item)
        {
            ExecuteItemAction(GetUseItemAction(item), item);
        }

        public void AltUseItem(InventoryItem item)
        {
            ExecuteItemAction(GetAltUseItemAction(item), item);
        }

        private void ExecuteItemAction(ItemAction action, InventoryItem item)
        {
            if (action == ItemAction.None || item == null)
            {
                return;
            }
            Survival component = Player.main.GetComponent<Survival>();
            Pickupable item2 = item.item;
            _ = item.container;
            IItemsContainer oppositeContainer = main.GetOppositeContainer(item);
            switch (action)
            {
                case ItemAction.Eat:
                    if (component.Eat(item2.gameObject))
                    {
                        global::UnityEngine.Object.Destroy(item2.gameObject);
                    }
                    break;
                case ItemAction.Use:
                    if (component.Use(item2.gameObject))
                    {
                        global::UnityEngine.Object.Destroy(item2.gameObject);
                    }
                    break;
                case ItemAction.Assign:
                    quickSlots.Assign(item);
                    break;
                case ItemAction.Equip:
                case ItemAction.Unequip:
                case ItemAction.Switch:
                    AddOrSwap(item, oppositeContainer);
                    break;
                case ItemAction.Swap:
                    AddOrSwap(item, oppositeContainer);
                    break;
                case ItemAction.Drop:
                    InternalDropItem(item2);
                    break;
            }
        }

        public static bool CanSwap(InventoryItem itemA, InventoryItem itemB)
        {
            if (itemA == null || itemB == null || itemA == itemB)
            {
                return false;
            }
            Pickupable item = itemA.item;
            Pickupable item2 = itemB.item;
            if (item == null || item2 == null)
            {
                return false;
            }
            IItemsContainer itemsContainer = itemA.container;
            IItemsContainer itemsContainer2 = itemB.container;
            if (itemsContainer == null || itemsContainer2 == null)
            {
                return false;
            }
            Equipment equipment = itemsContainer as Equipment;
            Equipment equipment2 = itemsContainer2 as Equipment;
            bool flag = equipment != null;
            bool flag2 = equipment2 != null;
            string slot = string.Empty;
            if (flag && !equipment.GetItemSlot(item, ref slot))
            {
                return false;
            }
            string slot2 = string.Empty;
            if (flag2 && !equipment2.GetItemSlot(item2, ref slot2))
            {
                return false;
            }
            if (flag)
            {
                if (flag2)
                {
                    if (equipment == equipment2 && slot == slot2)
                    {
                        return false;
                    }
                    if (equipment2.AllowedToAdd(slot2, item, verbose: false))
                    {
                        return equipment.AllowedToAdd(slot, item2, verbose: false);
                    }
                    return false;
                }
                if (equipment.AllowedToAdd(slot, item2, verbose: false) && itemsContainer2.AllowedToAdd(item, verbose: false))
                {
                    return itemsContainer2.HasRoomFor(item, itemB);
                }
                return false;
            }
            if (flag2)
            {
                if (equipment2.AllowedToAdd(slot2, item, verbose: false) && itemsContainer.AllowedToAdd(item2, verbose: false))
                {
                    return itemsContainer.HasRoomFor(item2, itemA);
                }
                return false;
            }
            if (itemsContainer == itemsContainer2)
            {
                return false;
            }
            if (itemsContainer.AllowedToAdd(item2, verbose: false) && itemsContainer2.AllowedToAdd(item, verbose: false) && itemsContainer.HasRoomFor(item2, itemA))
            {
                return itemsContainer2.HasRoomFor(item, itemB);
            }
            return false;
        }

        public static bool AddOrSwap(InventoryItem itemA, IItemsContainer containerB)
        {
            return AddOrSwap(itemA, containerB, null);
        }

        public static bool AddOrSwap(InventoryItem itemA, InventoryItem itemB)
        {
            return AddOrSwap(itemA, null, itemB);
        }

        private static bool AddOrSwap(InventoryItem itemA, IItemsContainer containerB, InventoryItem itemB)
        {
            if (itemA == null || !itemA.CanDrag(verbose: true))
            {
                return false;
            }
            Pickupable item = itemA.item;
            if (item == null)
            {
                return false;
            }
            IItemsContainer itemsContainer = itemA.container;
            if (itemsContainer == null)
            {
                return false;
            }
            if (itemB != null)
            {
                containerB = itemB.container;
            }
            if (containerB == null)
            {
                return false;
            }
            Equipment equipment = itemsContainer as Equipment;
            Equipment equipment2 = containerB as Equipment;
            bool flag = equipment != null;
            bool num = equipment2 != null;
            CraftData.GetEquipmentType(item.GetTechType());
            if (num)
            {
                string slot = string.Empty;
                if (itemB != null && !equipment2.GetItemSlot(itemB.item, ref slot))
                {
                    return false;
                }
                return AddOrSwap(itemA, equipment2, slot);
            }
            string slot2 = string.Empty;
            if (flag && !equipment.GetItemSlot(item, ref slot2))
            {
                return false;
            }
            if (itemsContainer == containerB)
            {
                return false;
            }
            if (itemB != null && !containerB.RemoveItem(itemB, forced: false, verbose: true))
            {
                return false;
            }
            if (containerB.AddItem(itemA))
            {
                if (itemB == null)
                {
                    return true;
                }
                if ((flag && equipment.AddItem(slot2, itemB)) || (!flag && itemsContainer.AddItem(itemB)))
                {
                    return true;
                }
                if (flag)
                {
                    equipment.AddItem(slot2, itemA, forced: true);
                }
                else
                {
                    itemsContainer.AddItem(itemA);
                }
                if (itemB != null)
                {
                    containerB.AddItem(itemB);
                }
            }
            else if (itemB != null)
            {
                containerB.AddItem(itemB);
            }
            return false;
        }

        public static bool AddOrSwap(InventoryItem itemA, Equipment equipmentB, string slotB)
        {
            if (itemA == null || !itemA.CanDrag(verbose: true) || equipmentB == null)
            {
                return false;
            }
            IItemsContainer itemsContainer = itemA.container;
            if (itemsContainer == null)
            {
                return false;
            }
            Pickupable item = itemA.item;
            if (item == null)
            {
                return false;
            }
            Equipment equipment = itemsContainer as Equipment;
            bool flag = equipment != null;
            string slot = string.Empty;
            if (flag && !equipment.GetItemSlot(item, ref slot))
            {
                return false;
            }
            EquipmentType equipmentType = CraftData.GetEquipmentType(item.GetTechType());
            if (string.IsNullOrEmpty(slotB))
            {
                equipmentB.GetCompatibleSlot(equipmentType, out slotB);
            }
            if (string.IsNullOrEmpty(slotB))
            {
                return false;
            }
            if (itemsContainer == equipmentB && slot == slotB)
            {
                return false;
            }
            if (!Equipment.IsCompatible(equipmentType, Equipment.GetSlotType(slotB)))
            {
                return false;
            }
            InventoryItem inventoryItem = equipmentB.RemoveItem(slotB, forced: false, verbose: true);
            if (inventoryItem == null)
            {
                if (equipmentB.AddItem(slotB, itemA))
                {
                    return true;
                }
            }
            else if (equipmentB.AddItem(slotB, itemA))
            {
                if ((flag && equipment.AddItem(slot, inventoryItem)) || (!flag && itemsContainer.AddItem(inventoryItem)))
                {
                    return true;
                }
                if (flag)
                {
                    equipment.AddItem(slot, itemA, forced: true);
                }
                else
                {
                    itemsContainer.AddItem(itemA);
                }
                equipmentB.AddItem(slotB, inventoryItem, forced: true);
            }
            else
            {
                equipmentB.AddItem(slotB, inventoryItem, forced: true);
            }
            return false;
        }

        private bool InternalDropItem(Pickupable pickupable, bool notify = true)
        {
            bool result = false;
            if (CanDropItemHere(pickupable, notify))
            {
                Transform transform = MainCameraControl.main.transform;
                Vector3 dropPosition = RayCast(transform.position, transform.forward, 10f, 0.75f, 1.5f);
                pickupable.Drop(dropPosition);
                if (pickupable.randomizeRotationWhenDropped)
                {
                    pickupable.transform.rotation = global::UnityEngine.Random.rotation;
                }
                SkyEnvironmentChanged.Send(pickupable.gameObject, Player.main.GetSkyEnvironment());
                result = true;
            }
            return result;
        }

        public static bool CanDropItemHere(Pickupable item, bool notify = false)
        {
            Player player = Player.main;
            bool flag = true;
            if (player.currentSub != null && player.currentWaterPark == null)
            {
                flag = false;
            }
            else if (player.escapePod.value)
            {
                flag = false;
            }
            else if (player.isPiloting)
            {
                flag = false;
            }
            else if (player.currentWaterPark != null && !WaterPark.CanDropItemInside(item))
            {
                flag = false;
            }
            else if (player.precursorOutOfWater)
            {
                flag = false;
            }
            if (notify && !flag)
            {
                ErrorMessage.AddError(Language.main.Get("InventoryCantDropHere"));
            }
            return flag;
        }

        public IItemsContainer GetOppositeContainer(InventoryItem item)
        {
            if (item.container == _container)
            {
                int count = usedStorage.Count;
                if (count > 0)
                {
                    Pickupable item2 = item.item;
                    for (int i = 0; i < count; i++)
                    {
                        IItemsContainer itemsContainer = usedStorage[i];
                        if (itemsContainer != null && itemsContainer.HasRoomFor(item2, null))
                        {
                            return itemsContainer;
                        }
                    }
                    return usedStorage[0];
                }
                return _equipment;
            }
            return _container;
        }

        public bool ForcePickup(Pickupable pickupable)
        {
            if (Pickup(pickupable))
            {
                return true;
            }
            ErrorMessage.AddWarning(Language.main.GetFormat("InventoryOverflow", Language.main.Get(pickupable.GetTechName())));
            AddPending(pickupable);
            return false;
        }

        public bool Pickup(Pickupable pickupable, bool noMessage = false)
        {
            if (!_container.HasRoomFor(pickupable))
            {
                return false;
            }
            _ = pickupable.gameObject.transform.position;
            TechType techType = pickupable.GetTechType();
            pickupable = pickupable.Pickup();
            InventoryItem item = new InventoryItem(pickupable);
            if (!((IItemsContainer)_equipment).AddItem(item))
            {
                _container.UnsafeAdd(item);
            }
            if (!noMessage)
            {
                UniqueIdentifier component = pickupable.GetComponent<UniqueIdentifier>();
                if (component != null)
                {
                    NotificationManager.main.Add(NotificationManager.Group.Inventory, component.Id, 4f);
                }
            }
            KnownTech.Analyze(pickupable.GetTechType());
            if (Utils.GetSubRoot() != null)
            {
                pickupable.destroyOnDeath = false;
            }
            if (!noMessage)
            {
                uGUI_IconNotifier.main.Play(techType, uGUI_IconNotifier.AnimationType.From);
            }
            SkyEnvironmentChanged.Send(pickupable.gameObject, Player.main.GetSkyEnvironment());
            return true;
        }

        public void ConsumeResourcesForRecipe(TechType techType, uGUI_IconNotifier.AnimationDone endFunc = null)
        {
            ITechData techData = CraftData.Get(techType);
            if (techData == null)
            {
                return;
            }
            int i = 0;
            for (int ingredientCount = techData.ingredientCount; i < ingredientCount; i++)
            {
                IIngredient ingredient = techData.GetIngredient(i);
                TechType techType2 = ingredient.techType;
                int j = 0;
                for (int amount = ingredient.amount; j < amount; j++)
                {
                    bool flag = i == techData.ingredientCount - 1 && j == ingredient.amount - 1;
                    if (!DestroyItem(techType2, allowGenerics: true) && GameModeUtils.RequiresIngredients())
                    {
                        Debug.LogErrorFormat(this, "Unable to remove one '{0}' from player inventory to craft {1}.", techType2, techType);
                    }
                    uGUI_IconNotifier.main.Play(techType2, uGUI_IconNotifier.AnimationType.To, flag ? endFunc : null);
                }
            }
        }

        public Pickupable GetHeld()
        {
            return quickSlots.heldItem?.item;
        }

        public GameObject GetHeldObject()
        {
            Pickupable held = GetHeld();
            if (held == null)
            {
                return null;
            }
            return held.gameObject;
        }

        public PlayerTool GetHeldTool()
        {
            Pickupable held = GetHeld();
            if (held == null)
            {
                return null;
            }
            return held.GetComponent<PlayerTool>();
        }

        public bool ReturnHeld(bool immediate = true)
        {
            InventoryItem heldItem = quickSlots.heldItem;
            if (heldItem == null)
            {
                return true;
            }
            PlayerTool component = heldItem.item.GetComponent<PlayerTool>();
            if (component != null && component.isInUse)
            {
                return false;
            }
            if (immediate)
            {
                quickSlots.DeselectImmediate();
            }
            else
            {
                quickSlots.Deselect();
            }
            return true;
        }

        public void DropHeldItem(bool applyForce)
        {
            Vector3 velocity = Player.main.GetComponent<Rigidbody>().velocity;
            if (applyForce)
            {
                velocity += Player.main.viewModelCamera.transform.forward * throwForce;
            }
            quickSlots.Drop(velocity);
        }

        public void SetViewModelVis(bool state)
        {
            quickSlots.SetViewModelVis(state);
        }

        public static Vector3 RayCast(Vector3 origin, Vector3 direction, float rndAngle, float minRadius, float maxRadius)
        {
            direction.Normalize();
            float num = global::UnityEngine.Random.Range(minRadius, maxRadius);
            Vector2 insideUnitCircle = global::UnityEngine.Random.insideUnitCircle;
            Vector3 vector = Quaternion.Euler(rndAngle * insideUnitCircle.x, rndAngle * insideUnitCircle.y, 0f) * direction;
            float num2 = 0.2f;
            if (Physics.Raycast(origin, vector, out var hitInfo, num + num2, layerMask))
            {
                num = Mathf.Max(0f, hitInfo.distance - num2);
            }
            return origin + vector * num;
        }

        private void OnAddItem(InventoryItem item)
        {
            TryUpdateOxygen();
        }

        private void OnRemoveItem(InventoryItem item)
        {
            if (!isTerminating)
            {
                ChangeOxygen(item, add: false);
                tryFlushPendingItems = true;
            }
            RemoveNotificationsFor(item);
        }

        private void OnEquip(string slot, InventoryItem item)
        {
        }

        private void OnUnequip(string slot, InventoryItem item)
        {
            RemoveNotificationsFor(item);
        }

        private void RemoveNotificationsFor(InventoryItem item)
        {
            if (item == null)
            {
                return;
            }
            Pickupable item2 = item.item;
            if (!(item2 == null))
            {
                UniqueIdentifier component = item2.GetComponent<UniqueIdentifier>();
                if (!(component == null))
                {
                    NotificationManager.main.Remove(NotificationManager.Group.Inventory, component.Id);
                }
            }
        }

        private void ChangeOxygen(InventoryItem item, bool add)
        {
            if (item == null)
            {
                return;
            }
            Pickupable item2 = item.item;
            if (item2 == null)
            {
                return;
            }
            Oxygen component = item2.GetComponent<Oxygen>();
            if (component == null)
            {
                return;
            }
            OxygenManager oxygenMgr = Player.main.oxygenMgr;
            if (!(oxygenMgr == null))
            {
                if (add)
                {
                    oxygenMgr.RegisterSource(component);
                }
                else
                {
                    oxygenMgr.UnregisterSource(component);
                }
            }
        }

        private void TryUpdateOxygen()
        {
            InventoryItem itemInSlot = equipment.GetItemInSlot("Tank");
            if (itemInSlot == null)
            {
                return;
            }
            Oxygen component = itemInSlot.item.GetComponent<Oxygen>();
            if (!(component == null))
            {
                OxygenManager oxygenMgr = Player.main.oxygenMgr;
                if (!(oxygenMgr == null))
                {
                    oxygenMgr.RegisterSource(component);
                }
            }
        }

        public void AddPending(Pickupable pickupable)
        {
            if (!(pickupable == null))
            {
                pickupable.gameObject.SetActive(value: false);
                pickupable.transform.parent = pendingItemsRoot;
                _pendingItems.Add(pickupable);
            }
        }

        private void FlushPendingItems()
        {
            if (!tryFlushPendingItems)
            {
                return;
            }
            for (int num = _pendingItems.Count - 1; num >= 0; num--)
            {
                Pickupable pickupable = _pendingItems[num];
                if (!pickupable || Pickup(pickupable))
                {
                    _pendingItems.RemoveAt(num);
                }
            }
            tryFlushPendingItems = false;
        }

        private void UnlockDefaultEquipmentSlots()
        {
            string[] slots = new string[7] { "Head", "Body", "Gloves", "Foots", "Chip1", "Chip2", "Tank" };
            _equipment.AddSlots(slots);
        }

        private void OnConsoleCommand_logpendingitems()
        {
            StringBuilder stringBuilder = new StringBuilder();
            int count = _pendingItems.Count;
            stringBuilder.AppendFormat("pending items: {0}\n", count);
            for (int i = 0; i < count; i++)
            {
                stringBuilder.AppendFormat("item: {0}\n", _pendingItems[i].name);
            }
            Debug.LogError(stringBuilder.ToString());
        }
    }
}
