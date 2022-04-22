using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class FiltrationMachine : MonoBehaviour, IBaseModule, IProtoEventListener
    {
        private const float spawnWaterInterval = 840f;

        private const float spawnSaltInterval = 420f;

        private const float powerPerSecond = 0.85f;

        private const float filterInterval = 1f;

        public StorageContainer storageContainer;

        public GameObject waterModel;

        public GameObject saltModel;

        public VFXController vfxController;

        public FMODAsset spawnSaltSound;

        public FMODAsset spawnWaterSound;

        public Pickupable waterPrefab;

        public Pickupable saltPrefab;

        public FMOD_CustomLoopingEmitter workSound;

        public int maxWater = 2;

        public int maxSalt = 2;

        public Transform spawnPoint;

        public float atmosphericWaterScalar;

        public float atmosphericSaltScalar;

        private GameObject shownModel;

        private VFXScan shownModelVfxScan;

        private bool modelUpdatePending;

        private float timeModelSpawned = -1f;

        private float scanDuration = 1.5f;

        private bool working;

        private PowerRelay powerRelay;

        private bool fastFiltering;

        private int lastUpdateWater;

        private int lastUpdateSalt;

        private BaseFiltrationMachineGeometry filtrationGeo;

        private const int currentVersion = 2;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 2;

        [NonSerialized]
        [ProtoMember(2)]
        public float timeRemainingWater = -1f;

        [NonSerialized]
        [ProtoMember(3)]
        public float timeRemainingSalt = -1f;

        [NonSerialized]
        [ProtoMember(4)]
        public Base.Face _moduleFace;

        [NonSerialized]
        [ProtoMember(5)]
        public float _constructed = 1f;

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

        private void OnEnable()
        {
            storageContainer.enabled = true;
            storageContainer.container.onAddItem += AddItem;
            storageContainer.container.onRemoveItem += RemoveItem;
            storageContainer.container.isAllowedToAdd = IsAllowedToAdd;
        }

        private void OnDisable()
        {
            storageContainer.container.onAddItem -= AddItem;
            storageContainer.container.onRemoveItem -= RemoveItem;
            storageContainer.container.isAllowedToAdd = null;
            storageContainer.enabled = false;
        }

        private bool IsUnderwater()
        {
            return base.transform.position.y < -1f;
        }

        private void Start()
        {
            DevConsole.RegisterConsoleCommand(this, "filterwater");
            DevConsole.RegisterConsoleCommand(this, "filtersalt");
            DevConsole.RegisterConsoleCommand(this, "filterfast");
            powerRelay = base.gameObject.GetComponentInParent<PowerRelay>();
            lastUpdateWater = storageContainer.container.GetCount(TechType.BigFilteredWater);
            lastUpdateSalt = storageContainer.container.GetCount(TechType.Salt);
            if (lastUpdateWater > 0)
            {
                AssignModel(waterModel);
            }
            else if (lastUpdateSalt > 0)
            {
                AssignModel(saltModel);
            }
            modelUpdatePending = false;
            Invoke("DelayedStart", 0.5f);
            InvokeRepeating("UpdateFiltering", 1f, 1f);
        }

        private void Update()
        {
            bool flag = timeModelSpawned != -1f && timeModelSpawned + scanDuration > Time.time;
            if (filtrationGeo == null)
            {
                filtrationGeo = GetModel();
            }
            if ((bool)filtrationGeo)
            {
                float yPos = 0f;
                if (shownModel != null && shownModelVfxScan != null)
                {
                    yPos = shownModelVfxScan.GetCurrentYPos();
                }
                filtrationGeo.SetWorking(flag, yPos);
            }
        }

        private void LateUpdate()
        {
            if (modelUpdatePending && Player.main.IsInBase() && (Player.main.transform.position - base.transform.position).sqrMagnitude <= 36f)
            {
                UpdateModel();
            }
        }

        public void OnHover(HandTargetEventData eventData)
        {
            if (!(constructed < 1f))
            {
                string text = Language.main.Get("FiltrationComplete");
                if (GameModeUtils.RequiresPower() && powerRelay.GetPower() < 0.85f)
                {
                    text = Language.main.Get("unpowered");
                }
                else if (timeRemainingWater >= 0f || timeRemainingSalt >= 0f)
                {
                    float arg = 1f - Mathf.Clamp01(timeRemainingWater / 840f);
                    float arg2 = 1f - Mathf.Clamp01(timeRemainingSalt / 420f);
                    text = Language.main.GetFormat("FiltrationProgress", arg, arg2);
                }
                HandReticle.main.SetInteractText("UseFiltrationMachine", text, translate1: true, translate2: false, addInstructions: true);
                HandReticle.main.SetIcon(HandReticle.IconType.Interact);
            }
        }

        public void OnUse(BaseFiltrationMachineGeometry model)
        {
            if (!(constructed < 1f))
            {
                storageContainer.Open();
            }
        }

        private BaseFiltrationMachineGeometry GetModel()
        {
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null)
            {
                IBaseModuleGeometry moduleGeometry = componentInParent.GetModuleGeometry(moduleFace);
                if (moduleGeometry != null)
                {
                    return moduleGeometry as BaseFiltrationMachineGeometry;
                }
            }
            return null;
        }

        private void DelayedStart()
        {
            TryFilterWater();
            TryFilterSalt();
        }

        private void AssignModel(GameObject model)
        {
            shownModel = global::UnityEngine.Object.Instantiate(model);
            shownModel.transform.parent = spawnPoint;
            shownModel.transform.localRotation = Quaternion.identity;
            shownModel.transform.localPosition = Vector3.zero;
            shownModelVfxScan = shownModel.GetComponent<VFXScan>();
        }

        private void SpawnModel(GameObject model)
        {
            AssignModel(model);
            timeModelSpawned = Time.time;
            shownModelVfxScan.StartScan(scanDuration);
        }

        private void SpawnWaterDrops()
        {
            vfxController.Play(0);
        }

        private void UpdateModel()
        {
            int count = storageContainer.container.GetCount(TechType.BigFilteredWater);
            int count2 = storageContainer.container.GetCount(TechType.Salt);
            CancelInvoke("SpawnWaterDrops");
            vfxController.Stop(0);
            if ((bool)shownModel)
            {
                global::UnityEngine.Object.Destroy(shownModel);
                shownModel = null;
                shownModelVfxScan = null;
            }
            if (count > lastUpdateWater)
            {
                SpawnModel(waterModel);
                if (!DayNightCycle.main.IsInSkipTimeMode())
                {
                    Utils.PlayFMODAsset(spawnWaterSound, spawnPoint);
                    Invoke("SpawnWaterDrops", scanDuration);
                }
            }
            else if (count2 > lastUpdateSalt)
            {
                SpawnModel(saltModel);
                if (!DayNightCycle.main.IsInSkipTimeMode())
                {
                    Utils.PlayFMODAsset(spawnSaltSound, spawnPoint);
                }
            }
            lastUpdateWater = count;
            lastUpdateSalt = count2;
            modelUpdatePending = false;
        }

        private void UpdateFiltering()
        {
            bool flag = false;
            if (timeRemainingWater > 0f || timeRemainingSalt > 0f)
            {
                PowerRelay powerRelay = this.powerRelay;
                float num = 1f * DayNightCycle.main.dayNightSpeed;
                if (!GameModeUtils.RequiresPower() || (powerRelay != null && powerRelay.GetPower() >= 0.85f * num))
                {
                    if (GameModeUtils.RequiresPower())
                    {
                        powerRelay.ConsumeEnergy(0.85f * num, out var _);
                    }
                    if (timeRemainingWater > 0f)
                    {
                        float num2 = num;
                        if (fastFiltering)
                        {
                            num2 *= 80f;
                        }
                        if (!IsUnderwater())
                        {
                            num2 *= atmosphericWaterScalar;
                        }
                        if (num2 > 0f)
                        {
                            timeRemainingWater = Mathf.Max(0f, timeRemainingWater - num2);
                            flag = true;
                        }
                        if (timeRemainingWater == 0f)
                        {
                            timeRemainingWater = -1f;
                            Spawn(waterPrefab);
                            TryFilterWater();
                        }
                    }
                    if (timeRemainingSalt > 0f)
                    {
                        float num3 = num;
                        if (fastFiltering)
                        {
                            num3 *= 80f;
                        }
                        if (!IsUnderwater())
                        {
                            num3 *= atmosphericSaltScalar;
                        }
                        if (num3 > 0f)
                        {
                            timeRemainingSalt = Mathf.Max(0f, timeRemainingSalt - num3);
                            flag = true;
                        }
                        if (timeRemainingSalt == 0f)
                        {
                            timeRemainingSalt = -1f;
                            Spawn(saltPrefab);
                            TryFilterSalt();
                        }
                    }
                    if (timeRemainingWater == -1f && timeRemainingSalt == -1f)
                    {
                        flag = false;
                    }
                }
            }
            bool flag2 = working;
            working = flag;
            if (flag && !flag2)
            {
                workSound.Play();
                vfxController.Play(1);
            }
            else if (!flag && flag2)
            {
                workSound.Stop();
                vfxController.Stop(1);
            }
        }

        private bool Spawn(Pickupable prefab)
        {
            Vector2int itemSize = CraftData.GetItemSize(prefab.GetComponent<Pickupable>().GetTechType());
            if (!storageContainer.container.HasRoomFor(itemSize.x, itemSize.y))
            {
                Debug.Log("no room in filtration machine!");
                return false;
            }
            InventoryItem item = new InventoryItem(global::UnityEngine.Object.Instantiate(prefab.gameObject).GetComponent<Pickupable>().Pickup(events: false));
            storageContainer.container.UnsafeAdd(item);
            return true;
        }

        private void TryFilterWater()
        {
            if (!(timeRemainingWater > 0f) && (IsUnderwater() || atmosphericWaterScalar != 0f) && storageContainer.container.GetCount(TechType.BigFilteredWater) < maxWater)
            {
                timeRemainingWater = 840f;
            }
        }

        private void TryFilterSalt()
        {
            if (!(timeRemainingSalt > 0f) && (IsUnderwater() || atmosphericSaltScalar != 0f) && storageContainer.container.GetCount(TechType.Salt) < maxSalt)
            {
                timeRemainingSalt = 420f;
            }
        }

        private void AddItem(InventoryItem item)
        {
            modelUpdatePending = true;
        }

        private void RemoveItem(InventoryItem item)
        {
            TryFilterWater();
            TryFilterSalt();
            modelUpdatePending = true;
        }

        private bool IsAllowedToAdd(Pickupable pickupable, bool verbose)
        {
            return false;
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            if (version >= 2)
            {
                return;
            }
            version = 2;
            Constructable component = GetComponent<Constructable>();
            if (component != null)
            {
                constructed = component.amount;
                global::UnityEngine.Object.Destroy(component);
            }
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent != null)
            {
                Vector3 point = base.transform.position + base.transform.right * Base.cellSize.x;
                Int3 cell = componentInParent.WorldToGrid(point);
                Base.Direction[] horizontalDirections = Base.HorizontalDirections;
                foreach (Base.Direction direction in horizontalDirections)
                {
                    Base.Face face = new Base.Face(cell, direction);
                    if (componentInParent.GetFaceRaw(face) == Base.FaceType.FiltrationMachine)
                    {
                        face.cell -= componentInParent.GetAnchor();
                        _moduleFace = face;
                        return;
                    }
                }
            }
            Debug.LogError("Failed to upgrade savegame data. FiltrationMachine IBaseModule is not found", this);
        }

        private void OnConsoleCommand_filterfast()
        {
            fastFiltering = !fastFiltering;
            ErrorMessage.AddDebug("fast filtering " + fastFiltering);
        }

        private void OnConsoleCommand_filterwater()
        {
            if (storageContainer.container.GetCount(TechType.BigFilteredWater) < maxWater)
            {
                timeRemainingWater = -1f;
                Spawn(waterPrefab);
                TryFilterWater();
                ErrorMessage.AddDebug("filtered water, water amount" + storageContainer.container.GetCount(TechType.BigFilteredWater));
            }
        }

        private void OnConsoleCommand_filtersalt()
        {
            if (storageContainer.container.GetCount(TechType.Salt) < maxSalt)
            {
                timeRemainingSalt = -1f;
                Spawn(saltPrefab);
                TryFilterSalt();
                ErrorMessage.AddDebug("filtered salt, salt amount:" + storageContainer.container.GetCount(TechType.Salt));
            }
        }
    }
}
