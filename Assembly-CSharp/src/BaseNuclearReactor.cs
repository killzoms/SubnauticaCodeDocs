using System;
using System.Collections;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class BaseNuclearReactor : MonoBehaviour, IBaseModule, IProtoEventListener, IProtoTreeEventListener
    {
        private static string[] slotIDs = new string[4] { "NuclearReactor1", "NuclearReactor2", "NuclearReactor3", "NuclearReactor4" };

        private const float powerPerSecond = 4.16666651f;

        private static readonly Dictionary<TechType, float> charge = new Dictionary<TechType, float>(TechTypeExtensions.sTechTypeComparer) { 
        {
            TechType.ReactorRod,
            20000f
        } };

        private const float storageMaxDistance = 4f;

        private const int _currentVersion = 3;

        [NonSerialized]
        [ProtoMember(1)]
        public int _protoVersion = 3;

        [NonSerialized]
        [ProtoMember(2)]
        public Base.Face _moduleFace;

        [NonSerialized]
        [ProtoMember(3)]
        public float _constructed = 1f;

        [NonSerialized]
        [Obsolete("Obsolete since v2")]
        [ProtoMember(4, OverwriteList = true)]
        public byte[] _serializedEquipment;

        [NonSerialized]
        [ProtoMember(5, OverwriteList = true)]
        public Dictionary<string, string> _serializedEquipmentSlots;

        [NonSerialized]
        [ProtoMember(6)]
        public float _toConsume;

        [AssertNotNull]
        public ChildObjectIdentifier storageRoot;

        private Equipment _equipment;

        private PowerRelay _powerRelay;

        private PowerSource _powerSource;

        public bool producingPower
        {
            get
            {
                if (_constructed >= 1f)
                {
                    return CountActiveRods() > 0;
                }
                return false;
            }
        }

        private Equipment equipment
        {
            get
            {
                if (_equipment == null)
                {
                    _equipment = new Equipment(base.gameObject, storageRoot.transform);
                    _equipment.SetLabel("BaseNuclearReactorStorageLabel");
                    _equipment.isAllowedToAdd = IsAllowedToAdd;
                    _equipment.isAllowedToRemove = IsAllowedToRemove;
                    _equipment.compatibleSlotDelegate = GetCompatibleSlot;
                    _equipment.onEquip += OnEquip;
                    _equipment.onUnequip += OnUnequip;
                    UnlockDefaultEquipmentSlots();
                }
                return _equipment;
            }
        }

        public Base.Face moduleFace
        {
            get
            {
                return _moduleFace;
            }
            set
            {
                _moduleFace = value;
            }
        }

        public float constructed
        {
            get
            {
                return _constructed;
            }
            set
            {
                value = Mathf.Clamp01(value);
                if (_constructed != value)
                {
                    _constructed = value;
                    if (!(_constructed >= 1f) && _constructed <= 0f)
                    {
                        global::UnityEngine.Object.Destroy(base.gameObject);
                    }
                }
            }
        }

        private void Start()
        {
            _powerRelay = base.gameObject.GetComponentInParent<PowerRelay>();
            if (_powerRelay == null)
            {
                Debug.LogError("BaseNuclearReactor could not find PowerRelay", this);
            }
            _powerSource = GetComponent<PowerSource>();
            if (_powerSource == null)
            {
                Debug.LogError("BaseNuclearReactor could not find PowerSource", this);
            }
        }

        private void Update()
        {
            if (!(_constructed >= 1f))
            {
                return;
            }
            float num = 4.16666651f * DayNightCycle.main.deltaTime;
            float num2 = _powerSource.maxPower - _powerSource.power;
            if (num2 > 0f)
            {
                if (num2 < num)
                {
                    num = num2;
                }
                float amount = ProducePower(num);
                _powerSource.AddEnergy(amount, out var _);
            }
        }

        public void OnHover()
        {
            HandReticle main = HandReticle.main;
            main.SetInteractText(Language.main.GetFormat("UseBaseNuclearReactor", Mathf.RoundToInt(_powerSource.GetPower()), Mathf.RoundToInt(_powerSource.GetMaxPower())), "Tooltip_UseBaseNuclearReactor", translate1: false, translate2: true, addInstructions: true);
            main.SetIcon(HandReticle.IconType.Hand);
        }

        public void OnUse(BaseNuclearReactorGeometry model)
        {
            PDA pDA = Player.main.GetPDA();
            Inventory.main.SetUsedStorage(equipment);
            pDA.Open(PDATab.Inventory, model.storagePivot, null, 4f);
        }

        private void UnlockDefaultEquipmentSlots()
        {
            _equipment.AddSlots(slotIDs);
        }

        private void OnEquip(string slot, InventoryItem item)
        {
            if (charge.ContainsKey(item.item.GetTechType()))
            {
                item.isEnabled = false;
            }
            BaseNuclearReactorGeometry model = GetModel();
            if (model != null)
            {
                model.SetState(producingPower);
            }
        }

        private void OnUnequip(string slot, InventoryItem item)
        {
            BaseNuclearReactorGeometry model = GetModel();
            if (model != null)
            {
                model.SetState(producingPower);
            }
        }

        private bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            bool flag = false;
            if (pickupable != null)
            {
                TechType techType = pickupable.GetTechType();
                flag = charge.ContainsKey(techType);
            }
            if (!flag && verbose)
            {
                ErrorMessage.AddMessage(Language.main.Get("BaseNuclearReactorCantAddItem"));
            }
            return flag;
        }

        private bool IsAllowedToRemove(Pickupable pickupable, bool verbose)
        {
            bool flag = true;
            if (pickupable != null)
            {
                flag = !charge.ContainsKey(pickupable.GetTechType());
            }
            if (!flag && verbose)
            {
                ErrorMessage.AddMessage(Language.main.Get("BaseNuclearReactorCantRemoveItem"));
            }
            return flag;
        }

        private bool GetCompatibleSlot(EquipmentType itemType, out string result)
        {
            if (itemType == EquipmentType.NuclearReactor)
            {
                int num = slotIDs.Length;
                for (int i = 0; i < num; i++)
                {
                    string text = slotIDs[i];
                    InventoryItem itemInSlot = equipment.GetItemInSlot(text);
                    if (itemInSlot == null)
                    {
                        result = text;
                        return true;
                    }
                    Pickupable item = itemInSlot.item;
                    if (item != null && item.GetTechType() == TechType.DepletedReactorRod)
                    {
                        result = text;
                        return true;
                    }
                }
            }
            result = null;
            return false;
        }

        private BaseNuclearReactorGeometry GetModel()
        {
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null)
            {
                IBaseModuleGeometry moduleGeometry = componentInParent.GetModuleGeometry(moduleFace);
                if (moduleGeometry != null)
                {
                    return moduleGeometry as BaseNuclearReactorGeometry;
                }
            }
            return null;
        }

        private float ProducePower(float requested)
        {
            float num = 0f;
            if (requested > 0f)
            {
                _toConsume += requested;
                num = requested;
                int num2 = 0;
                for (int i = 0; i < slotIDs.Length; i++)
                {
                    string slot = slotIDs[i];
                    InventoryItem itemInSlot = equipment.GetItemInSlot(slot);
                    if (itemInSlot == null)
                    {
                        continue;
                    }
                    Pickupable item = itemInSlot.item;
                    if (!(item != null))
                    {
                        continue;
                    }
                    TechType techType = item.GetTechType();
                    float value = 0f;
                    if (charge.TryGetValue(techType, out value))
                    {
                        if (_toConsume < value)
                        {
                            num2++;
                            continue;
                        }
                        _toConsume -= value;
                        global::UnityEngine.Object.Destroy(equipment.RemoveItem(slot, forced: true, verbose: false).item.gameObject);
                        equipment.AddItem(slot, SpawnDepletedRod(), forced: true);
                    }
                }
                if (num2 == 0)
                {
                    num -= _toConsume;
                    _toConsume = 0f;
                }
            }
            return num;
        }

        private InventoryItem SpawnDepletedRod()
        {
            return new InventoryItem(global::UnityEngine.Object.Instantiate(CraftData.GetPrefabForTechType(TechType.DepletedReactorRod)).GetComponent<Pickupable>().Pickup(events: false));
        }

        private int CountActiveRods()
        {
            int num = 0;
            for (int i = 0; i < slotIDs.Length; i++)
            {
                string slot = slotIDs[i];
                InventoryItem itemInSlot = equipment.GetItemInSlot(slot);
                if (itemInSlot != null)
                {
                    Pickupable item = itemInSlot.item;
                    if (charge.ContainsKey(item.GetTechType()))
                    {
                        num++;
                    }
                }
            }
            return num;
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            _serializedEquipmentSlots = _equipment.SaveEquipment();
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            equipment.Clear();
            if (_protoVersion < 2)
            {
                if (_serializedEquipment != null && _serializedEquipmentSlots != null)
                {
                    StorageHelper.RestoreEquipment(serializer, _serializedEquipment, _serializedEquipmentSlots, equipment);
                    _serializedEquipment = null;
                    _serializedEquipmentSlots = null;
                }
                _protoVersion = 2;
            }
        }

        public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            if (_serializedEquipmentSlots != null)
            {
                StorageHelper.TransferEquipment(storageRoot.gameObject, _serializedEquipmentSlots, equipment);
                _serializedEquipmentSlots = null;
            }
            UnlockDefaultEquipmentSlots();
            if (_protoVersion < 3)
            {
                CoroutineHost.StartCoroutine(CleanUpDuplicatedStorage());
            }
        }

        private IEnumerator CleanUpDuplicatedStorage()
        {
            yield return StorageHelper.DestroyDuplicatedItems(storageRoot.transform.parent.gameObject);
            _protoVersion = Mathf.Max(_protoVersion, 3);
        }
    }
}
