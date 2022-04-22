using System;
using System.Collections.Generic;
using AssemblyCSharp.Exploder;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    [ProtoInclude(1010, typeof(CreepvineSeed))]
    public class Pickupable : HandTarget, IProtoEventListener, IProtoTreeEventListener, IHandTarget
    {
        private static readonly Type[] dontDisableOnAttach = new Type[1] { typeof(EcoTarget) };

        private const int currentVersion = 4;

        [NonSerialized]
        [ProtoMember(1)]
        public TechType overrideTechType;

        [NonSerialized]
        [ProtoMember(2)]
        public bool overrideTechUsed;

        [NonSerialized]
        [ProtoMember(3)]
        public bool isLootCube;

        [ProtoMember(4)]
        public bool isPickupable = true;

        [ProtoMember(5)]
        public bool destroyOnDeath = true;

        [NonSerialized]
        [ProtoMember(6)]
        public bool _attached;

        [NonSerialized]
        [ProtoMember(7)]
        public bool _isInSub;

        [NonSerialized]
        [ProtoMember(8)]
        public int version;

        [NonSerialized]
        [ProtoMember(9)]
        public PickupableKinematicState isKinematic = PickupableKinematicState.NoKinematicStateSet;

        public bool cubeOnPickup;

        public bool randomizeRotationWhenDropped = true;

        [NonSerialized]
        public Event<Pickupable> pickedUpEvent = new Event<Pickupable>();

        [NonSerialized]
        public Event<Pickupable> droppedEvent = new Event<Pickupable>();

        public bool usePackUpIcon;

        private InventoryItem inventoryItem;

        private float timeDropped;

        private ItemPrefabData prefabData;

        private List<Behaviour> disabledBehaviours;

        private List<Collider> disabledColliders;

        private List<Rigidbody> disabledRigidbodies;

        public bool attached
        {
            get
            {
                return _attached;
            }
            set
            {
                if (_attached != value)
                {
                    _attached = value;
                    if (_attached)
                    {
                        pickedUpEvent.Trigger(this);
                    }
                }
            }
        }

        public bool isInSub => _isInSub;

        public event OnTechTypeChanged onTechTypeChanged;

        public override void Awake()
        {
            base.Awake();
            prefabData = GetComponent<ItemPrefabData>();
        }

        public void SetInventoryItem(InventoryItem newInventoryItem)
        {
            if (inventoryItem != newInventoryItem)
            {
                if (inventoryItem != null)
                {
                    inventoryItem.container?.RemoveItem(inventoryItem, forced: true, verbose: false);
                }
                inventoryItem = newInventoryItem;
            }
        }

        public void SetTechTypeOverride(TechType techType, bool lootCube = false)
        {
            isLootCube = lootCube;
            ChangeTechTypeOverride(techType, useTechTypeOverride: true);
        }

        public void ResetTechTypeOverride()
        {
            ChangeTechTypeOverride(TechType.None, useTechTypeOverride: false);
        }

        private void ChangeTechTypeOverride(TechType techType, bool useTechTypeOverride)
        {
            TechType techType2 = GetTechType();
            overrideTechType = techType;
            overrideTechUsed = useTechTypeOverride;
            if (this.onTechTypeChanged != null)
            {
                this.onTechTypeChanged(this, techType2);
            }
        }

        public void PlayPickupSound()
        {
            FMODUWE.PlayOneShot(CraftData.GetPickupSound(GetTechType()), Player.main.transform.position);
        }

        public Pickupable Pickup(bool events = true)
        {
            TechType techType = GetTechType();
            GetTechName();
            Pickupable result = Initialize();
            if (events)
            {
                ProfilingUtils.BeginSample("Trigger Goal");
                GoalManager.main.OnCustomGoalEvent("Pickup_" + techType.AsString());
                ProfilingUtils.EndSample();
                PlayPickupSound();
            }
            return result;
        }

        public Pickupable Initialize()
        {
            Pickupable pickupable = this;
            TechType techType = GetTechType();
            pickupable.SendMessage("OnExamine", SendMessageOptions.DontRequireReceiver);
            if (cubeOnPickup)
            {
                GameObject prefabForTechType = CraftData.GetPrefabForTechType(techType, verbose: false);
                GameObject obj = ((prefabForTechType != null) ? Utils.SpawnFromPrefab(prefabForTechType, null) : Utils.CreateGenericLoot(techType));
                obj.transform.position = base.gameObject.transform.position;
                pickupable = obj.GetComponent<Pickupable>();
                Fragment component = GetComponent<Fragment>();
                if ((bool)component)
                {
                    component.Deactivate();
                    global::UnityEngine.Object.Destroy(this);
                }
                else
                {
                    global::UnityEngine.Object.Destroy(base.gameObject);
                }
            }
            int num = pickupable.gameObject.GetComponentsInChildren<Rigidbody>(includeInactive: true).Length;
            if (num == 0)
            {
                pickupable.gameObject.AddComponent<Rigidbody>();
            }
            else if (num > 1)
            {
                Debug.Log("WARNING: pickupable " + base.gameObject.name + " has more than 1 rigidbody component!");
            }
            pickupable.Deactivate();
            pickupable.attached = true;
            if (pickupable._isInSub)
            {
                Unplace();
                pickupable._isInSub = false;
            }
            return pickupable;
        }

        private void Activate(bool registerEntity)
        {
            base.gameObject.SetActive(value: true);
            isPickupable = true;
            base.isValidHandTarget = true;
            PlayerTool component = GetComponent<PlayerTool>();
            if (component != null && component.mainCollider != null)
            {
                component.mainCollider.isTrigger = false;
            }
            if ((bool)LargeWorld.main && registerEntity)
            {
                LargeWorld.main.streamer.cellManager.RegisterEntity(base.gameObject);
            }
        }

        private void Deactivate()
        {
            base.gameObject.SetActive(value: false);
            isPickupable = false;
            base.isValidHandTarget = false;
            PlayerTool component = GetComponent<PlayerTool>();
            if (component != null && component.mainCollider != null)
            {
                component.mainCollider.isTrigger = true;
            }
            if (LargeWorld.main != null && LargeWorld.main.streamer != null && LargeWorld.main.streamer.cellManager != null)
            {
                base.transform.parent = null;
                LargeWorld.main.streamer.cellManager.UnregisterEntity(base.gameObject);
            }
        }

        public void SetVisible(bool visible)
        {
            if (attached)
            {
                if (visible)
                {
                    base.gameObject.SetActive(value: true);
                    DisableBehaviours();
                    DisableColliders();
                    DisableRigidbodies();
                }
                else
                {
                    EnableRigidbodies();
                    EnableColliders();
                    EnableBehaviours();
                    base.gameObject.SetActive(value: false);
                }
            }
        }

        public void Reparent(Transform parent)
        {
            base.transform.parent = parent;
            if (parent != null)
            {
                if (prefabData == null)
                {
                    base.transform.localPosition = Vector3.zero;
                    base.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    base.transform.localPosition = prefabData.localPosition;
                    base.transform.localRotation = Quaternion.Euler(prefabData.localRotation);
                }
            }
        }

        public void Drop()
        {
            Drop(base.transform.position);
        }

        public void Drop(Vector3 dropPosition, Vector3 pushVelocity = default(Vector3), bool activateRigidbody = true)
        {
            if (inventoryItem != null)
            {
                IItemsContainer container = inventoryItem.container;
                if (container != null)
                {
                    container.RemoveItem(inventoryItem, forced: true, verbose: false);
                    if (container == Inventory.main.container || container == Inventory.main.equipment)
                    {
                        FMODUWE.PlayOneShot(CraftData.GetDropSound(GetTechType()), Player.main.transform.position);
                    }
                }
                inventoryItem = null;
            }
            Player component = Utils.GetLocalPlayer().GetComponent<Player>();
            WaterPark currentWaterPark = component.currentWaterPark;
            bool flag = currentWaterPark != null;
            SetVisible(visible: false);
            Reparent(null);
            base.transform.position = dropPosition;
            Activate(!flag);
            if (flag)
            {
                currentWaterPark.AddItem(this);
            }
            timeDropped = 0f;
            droppedEvent.Trigger(this);
            base.gameObject.SendMessage("OnDrop", SendMessageOptions.DontRequireReceiver);
            _isInSub = component.IsInSub() && !flag;
            GenericLoot.SetLayer(base.gameObject, _isInSub);
            Rigidbody component2 = GetComponent<Rigidbody>();
            attached = false;
            if (!flag)
            {
                component2.AddForce(pushVelocity, ForceMode.VelocityChange);
                Smell smell = base.gameObject.GetComponent<Smell>();
                if (smell == null)
                {
                    smell = base.gameObject.AddComponent<Smell>();
                }
                smell.owner = component.gameObject;
                smell.strength = 1f;
                smell.falloff = 0.05f;
            }
            if (activateRigidbody)
            {
                component2.isKinematic = false;
            }
            if (_isInSub)
            {
                Place();
            }
        }

        private void Place()
        {
            DisableColliders();
            DisableRigidbodies();
        }

        private void Unplace()
        {
            EnableRigidbodies();
            EnableColliders();
        }

        private void DisableBehaviours()
        {
            disabledBehaviours = new List<Behaviour>();
            Behaviour[] componentsInChildren = GetComponentsInChildren<Behaviour>();
            foreach (Behaviour behaviour in componentsInChildren)
            {
                if (behaviour == null)
                {
                    Debug.LogWarning("Discarded missing behaviour on a Pickupable gameObject", this);
                    continue;
                }
                Type type = behaviour.GetType();
                if (!behaviour.enabled)
                {
                    continue;
                }
                bool flag = true;
                for (int j = 0; j < dontDisableOnAttach.Length; j++)
                {
                    if (type.Equals(dontDisableOnAttach[j]))
                    {
                        flag = false;
                    }
                }
                if (flag)
                {
                    behaviour.enabled = false;
                    disabledBehaviours.Add(behaviour);
                }
            }
        }

        private void DisableColliders()
        {
            disabledColliders = new List<Collider>();
            Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
            foreach (Collider collider in componentsInChildren)
            {
                if (collider.enabled && !collider.isTrigger)
                {
                    collider.gameObject.layer = LayerID.Useable;
                    collider.isTrigger = true;
                    disabledColliders.Add(collider);
                }
            }
        }

        private void DisableRigidbodies()
        {
            disabledRigidbodies = new List<Rigidbody>();
            Rigidbody[] componentsInChildren = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rigidbody in componentsInChildren)
            {
                rigidbody.isKinematic = true;
                disabledRigidbodies.Add(rigidbody);
            }
        }

        private void EnableColliders()
        {
            if (disabledColliders != null)
            {
                for (int i = 0; i < disabledColliders.Count; i++)
                {
                    Collider collider = disabledColliders[i];
                    collider.isTrigger = false;
                    collider.gameObject.layer = LayerID.Default;
                }
                disabledColliders = null;
            }
        }

        private void EnableBehaviours()
        {
            if (disabledBehaviours != null)
            {
                for (int i = 0; i < disabledBehaviours.Count; i++)
                {
                    disabledBehaviours[i].enabled = true;
                }
                disabledBehaviours = null;
            }
        }

        private void EnableRigidbodies()
        {
            if (disabledRigidbodies != null)
            {
                for (int i = 0; i < disabledRigidbodies.Count; i++)
                {
                    disabledRigidbodies[i].isKinematic = false;
                }
                disabledRigidbodies = null;
            }
        }

        public TechType GetTechType()
        {
            if (!overrideTechUsed)
            {
                return CraftData.GetTechType(base.gameObject);
            }
            return overrideTechType;
        }

        public string GetTechName()
        {
            return GetTechType().AsString();
        }

        private bool AllowedToPickUp()
        {
            if (isPickupable && Time.time - timeDropped > 1f)
            {
                return Player.main.HasInventoryRoom(this);
            }
            return false;
        }

        public void OnHandClick(GUIHand hand)
        {
            if (!hand.IsFreeToInteract() || !AllowedToPickUp())
            {
                return;
            }
            if (!Inventory.Get().Pickup(this))
            {
                ErrorMessage.AddWarning(Language.main.Get("InventoryFull"));
                return;
            }
            Player.main.PlayGrab();
            WaterParkItem component = GetComponent<WaterParkItem>();
            if (component != null)
            {
                component.SetWaterPark(null);
            }
        }

        public void OnHandHover(GUIHand hand)
        {
            ProfilingUtils.BeginSample("Pickupable.OnHandHover");
            HandReticle main = HandReticle.main;
            if (hand.IsFreeToInteract())
            {
                TechType techType = GetTechType();
                ProfilingUtils.BeginSample("AllowedToPickup");
                bool num = AllowedToPickUp();
                ProfilingUtils.EndSample();
                if (num)
                {
                    string text = string.Empty;
                    string text2 = string.Empty;
                    ProfilingUtils.BeginSample("Exo Claw");
                    Exosuit exosuit = Player.main.GetVehicle() as Exosuit;
                    bool flag = exosuit == null || exosuit.HasClaw();
                    ProfilingUtils.EndSample();
                    if (flag)
                    {
                        ProfilingUtils.BeginSample("GetPickupText");
                        ISecondaryTooltip component = base.gameObject.GetComponent<ISecondaryTooltip>();
                        if (component != null)
                        {
                            text2 = component.GetSecondaryTooltip();
                        }
                        text = (usePackUpIcon ? LanguageCache.GetPackUpText(techType) : LanguageCache.GetPickupText(techType));
                        main.SetIcon(usePackUpIcon ? HandReticle.IconType.PackUp : HandReticle.IconType.Hand);
                        ProfilingUtils.EndSample();
                    }
                    ProfilingUtils.BeginSample("SetInteractText");
                    if ((bool)exosuit)
                    {
                        HandReticle.Hand hand2 = (flag ? HandReticle.Hand.Left : HandReticle.Hand.None);
                        if (exosuit.leftArmType != TechType.ExosuitClawArmModule)
                        {
                            hand2 = HandReticle.Hand.Right;
                        }
                        main.SetInteractText(text, text2, translate1: false, translate2: false, hand2);
                    }
                    else
                    {
                        main.SetInteractText(text, text2, translate1: false, translate2: false, HandReticle.Hand.Left);
                    }
                    ProfilingUtils.EndSample();
                }
                else if (isPickupable && !Player.main.HasInventoryRoom(this))
                {
                    main.SetInteractInfo(techType.AsString(), "InventoryFull");
                }
                else
                {
                    main.SetInteractInfo(techType.AsString());
                }
            }
            ProfilingUtils.EndSample();
        }

        private void OnDestroy()
        {
            SetInventoryItem(null);
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            version = 4;
            Rigidbody component = GetComponent<Rigidbody>();
            if ((bool)component)
            {
                isKinematic = ((!component.isKinematic) ? PickupableKinematicState.NonKinematic : PickupableKinematicState.Kinematic);
            }
            else
            {
                isKinematic = PickupableKinematicState.Invalid;
            }
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            GenericLoot.SetLayer(base.gameObject, _isInSub);
            if (version < 3 && _attached && _isInSub && GetComponent<WaterParkItem>() != null && GetComponentInParent<WaterPark>() != null)
            {
                _attached = false;
                _isInSub = false;
            }
            if (_isInSub)
            {
                Place();
            }
            if (isLootCube && overrideTechUsed)
            {
                GameObject prefabForTechType = CraftData.GetPrefabForTechType(overrideTechType, verbose: false);
                if ((bool)prefabForTechType)
                {
                    Eatable component = prefabForTechType.GetComponent<Eatable>();
                    if ((bool)component)
                    {
                        Eatable eatable = base.gameObject.EnsureComponent<Eatable>();
                        eatable.timeDecayStart = component.timeDecayStart;
                        eatable.foodValue = component.foodValue;
                        eatable.waterValue = component.waterValue;
                        eatable.decomposes = component.decomposes;
                        eatable.kDecayRate = component.kDecayRate;
                    }
                }
            }
            if (_attached)
            {
                if ((bool)LargeWorld.main)
                {
                    LargeWorld.main.streamer.cellManager.UnregisterEntity(base.gameObject);
                }
                pickedUpEvent.Trigger(this);
            }
            Rigidbody component2 = GetComponent<Rigidbody>();
            if ((bool)component2)
            {
                switch (isKinematic)
                {
                    case PickupableKinematicState.NoKinematicStateSet:
                        isKinematic = ((!component2.isKinematic) ? PickupableKinematicState.NonKinematic : PickupableKinematicState.Kinematic);
                        break;
                    case PickupableKinematicState.Kinematic:
                        component2.isKinematic = true;
                        break;
                    case PickupableKinematicState.NonKinematic:
                        component2.isKinematic = false;
                        break;
                    case PickupableKinematicState.Invalid:
                        break;
                }
            }
            else
            {
                isKinematic = PickupableKinematicState.Invalid;
            }
        }

        public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            if (attached)
            {
                bool activeSelf = base.gameObject.activeSelf;
                Transform parent = base.transform.parent;
                base.gameObject.SetActive(value: true);
                base.transform.SetParent(null);
                base.transform.SetParent(parent);
                base.gameObject.SetActive(activeSelf);
            }
        }
    }
}
