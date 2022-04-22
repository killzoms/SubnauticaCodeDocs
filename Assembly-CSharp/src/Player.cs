using System;
using System.Collections;
using System.Collections.Generic;
using AssemblyCSharp.Story;
using ProtoBuf;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using UWE;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(OxygenManager))]
    [ProtoContract]
    public class Player : Living, IProtoEventListener, IObstacle, ICompileTimeCheckable, IOnTakeDamage
    {
        [Serializable]
        public struct EquipmentModel
        {
            public TechType techType;

            public GameObject model;
        }

        [Serializable]
        public struct EquipmentType
        {
            public string slot;

            public GameObject defaultModel;

            public EquipmentModel[] equipment;
        }

        public struct InitialEquipment
        {
            public TechType techType;

            public int count;
        }

        public enum Mode
        {
            Normal,
            Piloting,
            LockedPiloting,
            Sitting
        }

        public enum MotorMode
        {
            Walk,
            Dive,
            Seaglide,
            Vehicle,
            Mech,
            Run
        }

        public static readonly InitialEquipment[] creativeEquipment;

        private const string kCrashedShipBiome = "crashedShip";

        private const string kPrecursorGunBiome = "PrecursorGun";

        public static Player main;

        public static GameObject mainObject;

        public static Collider mainCollider;

        private static bool wantInterpolate;

        [HideInInspector]
        public Vehicle currentMountedVehicle;

        [AssertNotNull]
        public FMODAsset jumpSound;

        public EquipmentType[] equipmentModels = new EquipmentType[0];

        public float movementSpeed;

        public float depthLevel;

        [AssertNotNull]
        public SkinnedMeshRenderer head;

        public float playerSphereRadius = 0.5f;

        public SNCameraRoot camRoot;

        [AssertNotNull]
        public Transform camAnchor;

        [AssertNotNull]
        public PrefabSpawn surfaceFXSpawn;

        [AssertNotNull]
        public TemperatureDamage temperatureDamage;

        [AssertNotNull]
        public PrefabSpawn scubaMaskModelSpawn;

        [AssertNotNull]
        public GameObject fpParticleEmissionPoint;

        [AssertNotNull]
        public PrefabSpawn pdaSpawn;

        [AssertNotNull]
        public ArmsController armsController;

        [AssertNotNull]
        public Animator playerAnimator;

        [AssertNotNull]
        public Transform playerArrowTransform;

        [AssertNotNull]
        public OxygenManager oxygenMgr;

        public GUIStyle textStyle;

        public float crushDepth;

        [AssertNotNull]
        public PDAData pdaData;

        [AssertNotNull]
        public Transform rightHandSlot;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter radiateSound;

        [AssertNotNull]
        public FMOD_StudioEventEmitter deathMusic;

        [Tooltip("Time before you go unconscious when you're out of oxygen")]
        public float suffocationTime = 8f;

        [Tooltip("Time to recover consciousness after suffocating")]
        public float suffocationRecoveryTime = 4f;

        [AssertNotNull]
        public LiveMixin liveMixin;

        [AssertNotNull]
        public FootstepSounds footStepSounds;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter acidLoopingSound;

        [AssertNotNull]
        public GroundMotor groundMotor;

        [AssertNotNull]
        public Rigidbody rigidBody;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter teleportingLoopSound;

        [AssertNotNull]
        public Transform bottom;

        [AssertNotNull]
        public StoryGoal diveGoal;

        private bool _cinematicModeActive;

        [NonSerialized]
        public bool inExosuit;

        [NonSerialized]
        public bool inSeamoth;

        [NonSerialized]
        public MotorMode motorMode;

        public Event<Mode> playerModeChanged = new Event<Mode>();

        public Event<MotorMode> playerMotorModeChanged = new Event<MotorMode>();

        public Event<Player> tookBreathEvent = new Event<Player>();

        public Event<Player> swimFastEvent = new Event<Player>();

        [NonSerialized]
        public bool sitting;

        public Event<Player> playerDeathEvent = new Event<Player>();

        public Event<Player> playerRespawnEvent = new Event<Player>();

        public Utils.MonitoredValue<bool> isUnderwater = new Utils.MonitoredValue<bool>();

        public Utils.MonitoredValue<bool> isUnderwaterForSwimming = new Utils.MonitoredValue<bool>();

        public Utils.MonitoredValue<int> depthClass = new Utils.MonitoredValue<int>();

        public Utils.MonitoredValue<bool> escapePod = new Utils.MonitoredValue<bool>();

        [NonSerialized]
        public float timeGrabbed;

        [NonSerialized]
        public float timeBashed;

        [NonSerialized]
        public float mesmerizedSpeedMultiplier = 1f;

        [AssertNotNull]
        public InfectedMixin infectedMixin;

        public AnimationCurve infectionRevealCurve;

        public AnimationCurve infectionCureCurve;

        [AssertNotNull]
        public FMODAsset infectionRevealSound;

        [AssertNotNull]
        public Transform leftHandBone;

        private const int currentVersion = 5;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 5;

        [NonSerialized]
        [ProtoMember(2)]
        public bool serializedIsUnderwater;

        [NonSerialized]
        [ProtoMember(3)]
        public int serializedDepthClass;

        [NonSerialized]
        [ProtoMember(4)]
        public bool serializedEscapePod;

        [NonSerialized]
        [ProtoMember(5, OverwriteList = true)]
        public List<TechType> knownTech;

        [NonSerialized]
        [ProtoMember(6)]
        public string currentSubUID;

        [NonSerialized]
        [ProtoMember(7, OverwriteList = true)]
        public Dictionary<string, PDALog.Entry> journal;

        [NonSerialized]
        [ProtoMember(8, OverwriteList = true)]
        public Dictionary<string, PDAEncyclopedia.Entry> encyclopedia;

        [NonSerialized]
        [ProtoMember(9)]
        public PDAScanner.Data scanner;

        [NonSerialized]
        [ProtoMember(10)]
        public string currentWaterParkUID;

        [NonSerialized]
        [ProtoMember(11)]
        public readonly HashSet<TechType> usedTools = new HashSet<TechType>();

        [NonSerialized]
        [ProtoMember(12)]
        public bool precursorOutOfWater;

        [NonSerialized]
        [ProtoMember(13, OverwriteList = true)]
        public HashSet<TechType> analyzedTech;

        [NonSerialized]
        [ProtoMember(14)]
        public bool isSick;

        [NonSerialized]
        [ProtoMember(15)]
        public NotificationManager.SerializedData notifications;

        [NonSerialized]
        [ProtoMember(16)]
        public bool _displaySurfaceWater = true;

        [NonSerialized]
        [ProtoMember(17)]
        public float timeLastSleep;

        [NonSerialized]
        [ProtoMember(18)]
        public bool infectionRevealed;

        [NonSerialized]
        [ProtoMember(19, OverwriteList = true)]
        public Dictionary<string, TimeCapsuleContent> timeCapsules;

        private float timeLastCleanup = -1f;

        private int restoreQuickSlot = -1;

        private Utils.ScalarMonitor timeMonitor;

        private GameObject scubaMaskModel;

        private VFXController surfaceFX;

        private Mode mode;

        private float timeSpawned;

        private string biomeString = "";

        private SubRoot _currentSub;

        private SubRoot initialSub;

        private EscapePod _currentEscapePod;

        private WaterPark _currentWaterPark;

        private float _radiationAmount;

        private bool isNewBorn = true;

        private float escapePodRadius = 15f;

        private SubRoot lastValidSub;

        private EscapePod lastEscapePod;

        private Sequence suffocation = new Sequence(initialState: true);

        private static readonly GameInput.Button[] quickSlotButtons;

        public static readonly int quickSlotButtonsCount;

        private const float fallDamageThreshholdSpeed = -10f;

        private const float fallDamage = 2.5f;

        private int cachedDepth;

        private const string bodyStr = "Body";

        private const string handStr = "Gloves";

        private int fmodIndexIntensity = -1;

        private float timeStartRealSeconds;

        private Vector3 lastPosition;

        private float maxDepth;

        private float distanceTraveled;

        private const float infectionRevealCinematicLength = 12f;

        private const float infectionRevealDuration = 8f;

        private const float infectionCureDuration = 20f;

        private float timePlayerInfectionRevealed;

        private float timePlayerInfectionCured;

        private int freezeStatsCount;

        private float timeFallingBegan = -1f;

        private bool wasFalling;

        private const float kFallingAnimDelay = 0.45f;

        public const float exitWaterOffset = 0.8f;

        public const float enterWaterOffset = -0.1f;

        private bool shotgunMode;

        public Event<SubRoot> currentSubChangedEvent = new Event<SubRoot>();

        private PilotingChair currChair;

        private float timeLastShotgun;

        public static bool interpolate => wantInterpolate;

        public bool forceCinematicMode
        {
            set
            {
                _cinematicModeActive = value;
            }
        }

        public bool cinematicModeActive
        {
            get
            {
                return _cinematicModeActive;
            }
            set
            {
                _cinematicModeActive = value;
                MainCameraControl.main.cinematicMode = value;
                playerController.SetEnabled(!value);
            }
        }

        public bool justSpawned => timeSpawned + 8f > Time.time;

        public float radiationAmount => _radiationAmount;

        public SubRoot currentSub
        {
            get
            {
                return _currentSub;
            }
            private set
            {
                if (!(_currentSub == value))
                {
                    if (_currentSub != null)
                    {
                        _currentSub.damagedEvent.RemoveHandlers(base.gameObject);
                        _currentSub.OnPlayerExited(this);
                    }
                    _currentSub = value;
                    if (_currentSub != null)
                    {
                        _currentSub.damagedEvent.AddHandler(base.gameObject, OnSubDamaged);
                        _currentSub.OnPlayerEntered(this);
                        lastValidSub = _currentSub;
                    }
                    UpdateIsUnderwater();
                    if (!CheckSubValid(lastValidSub))
                    {
                        lastValidSub = null;
                    }
                    currentSubChangedEvent.Trigger(_currentSub);
                    SkyEnvironmentChanged.Broadcast(base.gameObject, _currentSub);
                }
            }
        }

        public EscapePod currentEscapePod
        {
            get
            {
                return _currentEscapePod;
            }
            set
            {
                if (!(_currentEscapePod == value))
                {
                    _currentEscapePod = value;
                    if (_currentEscapePod != null)
                    {
                        lastEscapePod = _currentEscapePod;
                    }
                    SkyEnvironmentChanged.Broadcast(base.gameObject, _currentEscapePod);
                }
            }
        }

        public WaterPark currentWaterPark
        {
            get
            {
                return _currentWaterPark;
            }
            set
            {
                _currentWaterPark = value;
                if (_currentWaterPark != null)
                {
                    InvokeRepeating("ValidateCurrentWaterPark", 0f, 1f);
                }
                else
                {
                    CancelInvoke("ValidateCurrentWaterPark");
                }
            }
        }

        public bool displaySurfaceWater => _displaySurfaceWater;

        public bool isPiloting
        {
            get
            {
                if (mode != Mode.Piloting)
                {
                    return mode == Mode.LockedPiloting;
                }
                return true;
            }
        }

        public Camera viewModelCamera => MainCamera.camera;

        public PlayerController playerController { get; private set; }

        public PDA GetPDA()
        {
            return pdaSpawn.spawnedObj.GetComponent<PDA>();
        }

        public void OnConsoleCommand_warpme()
        {
            SubRoot subRoot = GetCurrentSub();
            if (subRoot != null)
            {
                RespawnPoint componentInChildren = subRoot.gameObject.GetComponentInChildren<RespawnPoint>();
                SetPosition(componentInChildren.GetSpawnPosition());
            }
            else if (lastValidSub != null)
            {
                RespawnPoint componentInChildren2 = lastValidSub.gameObject.GetComponentInChildren<RespawnPoint>();
                SetPosition(componentInChildren2.GetSpawnPosition());
                SetCurrentSub(lastValidSub);
            }
            else if ((bool)lastEscapePod)
            {
                lastEscapePod.RespawnPlayer();
            }
            else
            {
                EscapePod.main.RespawnPlayer();
            }
        }

        public void OnConsoleCommand_kill()
        {
            if ((bool)liveMixin)
            {
                liveMixin.Kill();
            }
        }

        public void OnConsoleCommand_takedamage(NotificationCenter.Notification n)
        {
            float num = global::UnityEngine.Random.value * 100f;
            if (n.data != null && n.data.Count > 0)
            {
                string text = (string)n.data[0];
                num = Convert.ToSingle(text);
                Debug.Log("\"" + text + "\" => " + num);
            }
            ErrorMessage.AddDebug("takedamage = " + num);
            if ((bool)liveMixin)
            {
                liveMixin.TakeDamage(num, Utils.GetRandomPosInView(20f));
            }
        }

        public void OnConsoleCommand_shotgun()
        {
            shotgunMode = !shotgunMode;
            if (shotgunMode)
            {
                Debug.Log("shotgun mode enabled!");
            }
            else
            {
                Debug.Log("shotgun mode disabled!");
            }
        }

        private void OnConsoleCommand_warp(NotificationCenter.Notification n)
        {
            if (n != null && n.data != null && n.data.Count == 3 && float.TryParse((string)n.data[0], out var result) && float.TryParse((string)n.data[1], out var result2) && float.TryParse((string)n.data[2], out var result3))
            {
                SetPosition(new Vector3(result, result2, result3));
                OnPlayerPositionCheat();
            }
        }

        private void OnConsoleCommand_warpforward(NotificationCenter.Notification n)
        {
            float result = 3f;
            if (n != null && n.data != null && n.data.Count == 1)
            {
                float.TryParse((string)n.data[0], out result);
            }
            Transform aimingTransform = camRoot.GetAimingTransform();
            SetPosition(base.transform.position + aimingTransform.forward * result);
            OnPlayerPositionCheat();
        }

        public void OnConsoleCommand_invisible(NotificationCenter.Notification n)
        {
            GameModeUtils.ToggleCheat(GameModeOption.NoAggression);
            EcoTarget component = GetComponent<EcoTarget>();
            if (component != null)
            {
                component.enabled = !GameModeUtils.IsInvisible();
            }
            ErrorMessage.AddDebug("invisible cheat is now " + GameModeUtils.IsCheatActive(GameModeOption.NoAggression));
        }

        private void OnConsoleCommand_spawnnearby()
        {
            SpawnNearby(0.5f);
        }

        private void OnConsoleCommand_interpolate()
        {
            wantInterpolate = !wantInterpolate;
            ErrorMessage.AddDebug("interpolate player movement: " + wantInterpolate);
        }

        public bool AddUsedTool(TechType toolType)
        {
            return usedTools.Add(toolType);
        }

        public bool IsToolUsed(TechType toolType)
        {
            return usedTools.Contains(toolType);
        }

        public GameObject GetSkyEnvironment()
        {
            SubRoot subRoot = GetCurrentSub();
            if (subRoot != null)
            {
                return subRoot.gameObject;
            }
            if (currentEscapePod != null)
            {
                MarmoLifepodSky component = currentEscapePod.GetComponent<MarmoLifepodSky>();
                if (component != null)
                {
                    return component.gameObject;
                }
            }
            return null;
        }

        public bool IsFrozenStats()
        {
            return freezeStatsCount > 0;
        }

        public void FreezeStats()
        {
            if (freezeStatsCount == 0)
            {
                Survival component = GetComponent<Survival>();
                if (component != null)
                {
                    component.freezeStats = true;
                }
            }
            freezeStatsCount++;
        }

        public void UnfreezeStats()
        {
            freezeStatsCount--;
            if (freezeStatsCount == 0)
            {
                Survival component = GetComponent<Survival>();
                if (component != null)
                {
                    component.freezeStats = false;
                }
            }
            else if (freezeStatsCount < 0)
            {
                Debug.LogError("UnfreezeStats when not frozen");
                freezeStatsCount = 0;
            }
        }

        public void WaitForTeleportation()
        {
            global::UWE.Utils.EnterPhysicsSyncSection();
            FreezeStats();
            InvokeRepeating("CheckTeleportationComplete", 0.2f, 0.2f);
        }

        public void CheckTeleportationComplete()
        {
            if (LargeWorldStreamer.main.IsWorldSettled())
            {
                CancelInvoke("CheckTeleportationComplete");
                UnfreezeStats();
                PrecursorTeleporter.TeleportationComplete();
                Invoke("CompleteTeleportation", 0.9f);
                global::UWE.Utils.ExitPhysicsSyncSection();
            }
        }

        public void CompleteTeleportation()
        {
            Inventory.main.quickSlots.SetIgnoreHotkeyInput(ignore: false);
            GetPDA().SetIgnorePDAInput(ignore: false);
            playerController.inputEnabled = true;
            teleportingLoopSound.Stop();
            if ((bool)currentMountedVehicle)
            {
                currentMountedVehicle.gameObject.GetComponentInChildren<Rigidbody>().isKinematic = false;
            }
            else
            {
                playerController.SetEnabled(enabled: true);
            }
        }

        public void SetHeadVisible(bool visible)
        {
            head.shadowCastingMode = (visible ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
        }

        private void CrushDamageUpdate()
        {
            if (!base.gameObject.activeInHierarchy)
            {
                return;
            }
            float depth = GetDepth();
            float num = 200f;
            if (depth > num && IsSwimming())
            {
                float num2 = (depth - num) / (2f * num);
                if ((bool)liveMixin)
                {
                    liveMixin.TakeDamage(num2 * 100f, Utils.GetRandomPosInView(), DamageType.Pressure);
                }
            }
        }

        private void FindAwakeSpawnedEnts()
        {
            surfaceFX = surfaceFXSpawn.spawnedObj.GetComponentInChildren<VFXController>();
            scubaMaskModel = scubaMaskModelSpawn.spawnedObj;
        }

        private IEnumerator Start()
        {
            FindAwakeSpawnedEnts();
            scubaMaskModel.active = false;
            depthClass.changedEvent.AddHandler(this, OnPlayerDepthClassChanged);
            isUnderwater.changedEvent.AddHandler(this, OnPlayerIsUnderwaterChanged);
            EquipmentChanged(string.Empty, null);
            Inventory.main.equipment.onEquip += EquipmentChanged;
            Inventory.main.equipment.onUnequip += EquipmentChanged;
            playerAnimator.SetBool("vr_active", GameOptions.GetVrAnimationMode());
            UpdateReinforcedSuit();
            if (isNewBorn)
            {
                infectedMixin.infectedAmount = 0.1f;
            }
            infectedMixin.UpdateInfectionShading();
            rigidBody.interpolation = (interpolate ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None);
            if (GameModeUtils.SpawnsInitialItems())
            {
                while (!uGUI.main.hud.active)
                {
                    yield return null;
                }
                yield return new WaitForSeconds(3f);
                SetupCreativeMode();
            }
            timeStartRealSeconds = Time.realtimeSinceStartup;
            InvokeRepeating("TrackTravelStats", global::UnityEngine.Random.value, 5f);
        }

        private void OnDestroy()
        {
            GameStats.UpdateTimePlayed(Time.realtimeSinceStartup - timeStartRealSeconds);
            GameStats.UpdateMaxDepth(maxDepth);
            GameStats.UpdateDistanceTraveled(distanceTraveled);
        }

        public void ApplyEscapePodSkyIfNeeded()
        {
            if (currentEscapePod != null)
            {
                SkyEnvironmentChanged.Broadcast(base.gameObject, currentEscapePod);
            }
        }

        private void SetupCreativeMode()
        {
            if (!Utils.GetContinueMode())
            {
                InitialEquipment[] array = creativeEquipment;
                for (int i = 0; i < array.Length; i++)
                {
                    InitialEquipment initialEquipment = array[i];
                    CraftData.AddToInventory(initialEquipment.techType, initialEquipment.count);
                }
                Inventory.main.quickSlots.Select(0);
            }
            KnownTech.UnlockAll(verbose: false);
        }

        private bool CheckSubValid(SubRoot sub)
        {
            bool result = false;
            if (sub != null)
            {
                bool flag = true;
                LiveMixin component = sub.GetComponent<LiveMixin>();
                if (component != null)
                {
                    flag = component.IsAlive();
                }
                result = flag && sub.GetLeakAmount() <= 0.2f;
            }
            return result;
        }

        public bool HasInventoryRoom(int width, int height)
        {
            Exosuit exosuit = currentMountedVehicle as Exosuit;
            if (exosuit != null)
            {
                return exosuit.storageContainer.container.HasRoomFor(width, height);
            }
            return Inventory.Get().HasRoomFor(width, height);
        }

        public bool HasInventoryRoom(Pickupable p)
        {
            Exosuit exosuit = currentMountedVehicle as Exosuit;
            if (exosuit != null)
            {
                return exosuit.storageContainer.container.HasRoomFor(p);
            }
            return Inventory.Get().HasRoomFor(p);
        }

        private void ValidateCurrentSub()
        {
            if (currentSub != null)
            {
                if (!currentSub.isBase && (currentSub.transform.position - base.transform.position).magnitude > 35f)
                {
                    SetCurrentSub(null);
                }
            }
            else
            {
                CancelInvoke("ValidateCurrentSub");
            }
        }

        public void ValidateEscapePod()
        {
            if (currentEscapePod != null && (currentEscapePod.transform.position - base.transform.position).sqrMagnitude > escapePodRadius * escapePodRadius)
            {
                escapePod.Update(newValue: false);
                currentEscapePod = null;
            }
        }

        public void ValidateCurrentWaterPark()
        {
            if (currentWaterPark != null)
            {
                if (!currentWaterPark.IsPointInside(base.transform.position))
                {
                    currentWaterPark = null;
                }
            }
            else
            {
                CancelInvoke("ValidateCurrentWaterPark");
            }
        }

        public void SetCurrentSub(SubRoot sub)
        {
            currentSub = sub;
            if (currentSub != null)
            {
                InvokeRepeating("ValidateCurrentSub", 0f, 1f);
            }
            else
            {
                CancelInvoke("ValidateCurrentSub");
            }
        }

        public void OnSubDamaged(SubRoot.DamageEvent ev)
        {
            MainCameraControl.main.ShakeCamera(1f);
        }

        public void SetScubaMaskActive(bool state)
        {
            if (XRSettings.enabled)
            {
                state = false;
            }
            if (state)
            {
                CancelInvoke("HideScubaMask");
                scubaMaskModel.SetActive(value: true);
            }
            else
            {
                Invoke("HideScubaMask", 1f);
            }
        }

        private void HideScubaMask()
        {
            scubaMaskModel.SetActive(value: false);
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            serializedIsUnderwater = isUnderwater.value;
            serializedDepthClass = depthClass.value;
            serializedEscapePod = escapePod.value;
            if (currentSub != null)
            {
                currentSubUID = currentSub.gameObject.GetComponent<UniqueIdentifier>().Id;
            }
            else
            {
                currentSubUID = null;
            }
            if (currentWaterPark != null)
            {
                currentWaterParkUID = currentWaterPark.gameObject.GetComponent<UniqueIdentifier>().Id;
            }
            else
            {
                currentWaterParkUID = null;
            }
            KnownTech.Serialize(out knownTech, out analyzedTech);
            timeCapsules = TimeCapsuleContentProvider.Serialize();
            journal = PDALog.Serialize();
            encyclopedia = PDAEncyclopedia.Serialize();
            scanner = PDAScanner.Serialize();
            notifications = NotificationManager.main.Serialize();
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            isNewBorn = false;
            isUnderwater.Update(serializedIsUnderwater);
            depthClass.Update(serializedDepthClass);
            escapePod.Update(serializedEscapePod);
            if (escapePod.value)
            {
                currentEscapePod = EscapePod.main;
            }
            SetDisplaySurfaceWater(displaySurfaceWater);
            bool flag = false;
            if (version < 2)
            {
                flag = true;
                List<TechType> list = new List<TechType>();
                for (int i = 0; i < knownTech.Count; i++)
                {
                    TechType techType = knownTech[i];
                    switch (techType)
                    {
                        case TechType.FragmentAnalyzer:
                            techType = TechType.FragmentAnalyzerBlueprintOld;
                            if (!list.Contains(techType))
                            {
                                list.Add(techType);
                            }
                            break;
                        case TechType.SpecimenAnalyzer:
                            techType = TechType.SpecimenAnalyzerBlueprint;
                            if (!list.Contains(techType))
                            {
                                list.Add(techType);
                            }
                            break;
                        case TechType.Workbench:
                            techType = TechType.WorkbenchBlueprint;
                            if (!list.Contains(techType))
                            {
                                list.Add(techType);
                            }
                            break;
                        default:
                            if (!list.Contains(techType))
                            {
                                list.Add(techType);
                            }
                            break;
                        case TechType.SpecialHullPlateBlueprintOld:
                            break;
                    }
                }
                knownTech = list;
            }
            if (version < 3)
            {
                flag = true;
                while (knownTech.Remove(TechType.BaseFiltrationMachine))
                {
                }
            }
            if (version < 4)
            {
                flag = true;
                while (knownTech.Remove(TechType.FragmentAnalyzerBlueprintOld))
                {
                }
            }
            if (version < 5)
            {
                flag = true;
                infectedMixin.infectedAmount = 0.1f;
            }
            TimeCapsuleContentProvider.Deserialize(timeCapsules);
            timeCapsules = null;
            KnownTech.Deserialize(knownTech, analyzedTech);
            PDALog.Deserialize(journal);
            journal = null;
            PDAEncyclopedia.Deserialize(encyclopedia);
            encyclopedia = null;
            PDAScanner.Deserialize(scanner);
            scanner = null;
            NotificationManager.main.Deserialize(notifications);
            if (flag)
            {
                version = 5;
            }
            Invoke("ApplyEscapePodSkyIfNeeded", 0.5f);
        }

        public bool IsNewBorn()
        {
            return isNewBorn;
        }

        public void Awake()
        {
            main = this;
            mainObject = base.gameObject;
            mainCollider = GetComponent<Collider>();
            DevConsole.RegisterConsoleCommand(this, "warpme");
            DevConsole.RegisterConsoleCommand(this, "kill");
            DevConsole.RegisterConsoleCommand(this, "takedamage");
            DevConsole.RegisterConsoleCommand(this, "shotgun");
            DevConsole.RegisterConsoleCommand(this, "warp");
            DevConsole.RegisterConsoleCommand(this, "warpforward");
            DevConsole.RegisterConsoleCommand(this, "invisible");
            DevConsole.RegisterConsoleCommand(this, "spawnnearby");
            DevConsole.RegisterConsoleCommand(this, "interpolate");
            if (initialSub != null)
            {
                SetCurrentSub(initialSub);
            }
            timeMonitor = new Utils.ScalarMonitor(Time.time);
            playerController = GetComponent<PlayerController>();
            base.gameObject.AddComponent<InteractionVolumeUser>();
            TimeCapsuleContentProvider.Initialize();
            PDAData.Initialize(pdaData);
        }

        public bool SpawnNearby(float spawnRadius, GameObject ignoreObject = null)
        {
            Vector3 position = Vector3.zero;
            bool flag = false;
            for (int i = 0; i < 10; i++)
            {
                float f = global::UnityEngine.Random.value * 2f * (float)System.Math.PI;
                Vector3 vector = new Vector3(base.transform.position.x + Mathf.Cos(f) * spawnRadius, base.transform.position.y, base.transform.position.z + Mathf.Sin(f) * spawnRadius);
                if (playerController.WayToPositionClear(vector, ignoreObject, ignoreLiving: true))
                {
                    position = vector;
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                base.transform.parent = null;
                SetPosition(position);
            }
            else
            {
                ErrorMessage.AddError(Language.main.Get("SpawnNearbyFailed"));
            }
            return flag;
        }

        public float GetOxygenAvailable()
        {
            return oxygenMgr.GetOxygenAvailable();
        }

        public float GetOxygenCapacity()
        {
            return oxygenMgr.GetOxygenCapacity();
        }

        public void SetPosition(Vector3 wsPos, Quaternion wsRot)
        {
            SetPosition(wsPos);
            base.transform.rotation = wsRot;
        }

        public void SetPosition(Vector3 wsPos)
        {
            base.transform.position = wsPos;
            lastPosition = wsPos;
            if (!Physics.autoSyncTransforms)
            {
                Physics.SyncTransforms();
            }
        }

        private void TrackTravelStats()
        {
            Vector3 position = base.transform.position;
            maxDepth = Mathf.Max(maxDepth, 0f - position.y);
            distanceTraveled += Vector3.Distance(position, lastPosition);
            lastPosition = position;
        }

        public SubRoot GetCurrentSub()
        {
            return currentSub;
        }

        public SubRoot GetLastSub()
        {
            return lastValidSub;
        }

        public float GetDepth()
        {
            return Ocean.main.GetDepthOf(base.gameObject);
        }

        public bool IsUnderwaterForSwimming()
        {
            return isUnderwaterForSwimming.value;
        }

        public bool IsUnderwater()
        {
            return isUnderwater.value;
        }

        public bool IsSwimming()
        {
            if (motorMode != MotorMode.Dive)
            {
                return motorMode == MotorMode.Seaglide;
            }
            return true;
        }

        public bool IsInside()
        {
            if (main.motorMode != MotorMode.Vehicle)
            {
                return IsInsideWalkable();
            }
            return true;
        }

        public bool IsInsideWalkable()
        {
            if (!(currentSub != null))
            {
                return escapePod.value;
            }
            return true;
        }

        public bool CanBreathe()
        {
            if (currentSub != null)
            {
                if (currentSub.powerRelay != null && currentSub.powerRelay.GetPowerStatus() != 0)
                {
                    return !IsUnderwater();
                }
                return false;
            }
            if (currentMountedVehicle != null)
            {
                return currentMountedVehicle.IsPowered();
            }
            return !IsUnderwater();
        }

        public bool IsInSub()
        {
            return currentSub != null;
        }

        public bool IsInBase()
        {
            if (currentSub != null)
            {
                return currentSub.GetComponent<Base>() != null;
            }
            return false;
        }

        public bool IsInSubmarine()
        {
            if (currentSub != null)
            {
                return currentSub.GetComponent<Base>() == null;
            }
            return false;
        }

        public bool CanBeAttacked()
        {
            if (!IsInsideWalkable() && !justSpawned)
            {
                return !GameModeUtils.IsInvisible();
            }
            return false;
        }

        public bool IsBleederAttached()
        {
            return armsController.IsBleederAttached();
        }

        public Mode GetMode()
        {
            return mode;
        }

        public void EnterLockedMode(Transform parent, bool teleport = false)
        {
            GameInput.ClearInput();
            if (parent != null)
            {
                base.transform.parent = parent;
                base.transform.localRotation = Quaternion.identity;
                MainCameraControl.main.ResetCamera();
                if (teleport)
                {
                    base.transform.localPosition = Vector3.zero;
                }
            }
            playerController.SetEnabled(enabled: false);
            mode = Mode.LockedPiloting;
            Inventory.main.quickSlots.DeselectImmediate();
            playerModeChanged.Trigger(mode);
            MainCameraControl.main.SetEnabled(val: true);
        }

        public bool ExitLockedMode(bool respawn = false, bool findNewPosition = true)
        {
            GameInput.ClearInput();
            bool flag = true;
            if (findNewPosition && currentMountedVehicle != null)
            {
                flag = SpawnNearby(3f, currentMountedVehicle.gameObject);
                MainCameraControl.main.LookAt(currentMountedVehicle.transform.position + currentMountedVehicle.transform.forward * 10f);
                currentMountedVehicle = null;
            }
            if (flag)
            {
                playerController.SetEnabled(enabled: true);
                mode = Mode.Normal;
                playerModeChanged.Trigger(mode);
                sitting = false;
                playerController.ForceControllerSize();
            }
            return flag;
        }

        public bool IsPilotingInLockedMode<V>() where V : Component
        {
            if (mode == Mode.LockedPiloting && base.transform.parent != null)
            {
                return (global::UnityEngine.Object)base.transform.parent.gameObject.GetComponent<V>() != (global::UnityEngine.Object)null;
            }
            return false;
        }

        public bool IsPiloting()
        {
            if (mode != Mode.Piloting)
            {
                return mode == Mode.LockedPiloting;
            }
            return true;
        }

        public PilotingChair GetPilotingChair()
        {
            return currChair;
        }

        public void EnterPilotingMode(PilotingChair chair, bool keepCinematicState = false)
        {
            GameInput.ClearInput();
            if (IsInSub())
            {
                currChair = chair;
                if (!keepCinematicState)
                {
                    cinematicModeActive = true;
                    MainCameraControl.main.lookAroundMode = true;
                }
                base.transform.parent = chair.sittingPosition.transform;
                global::UWE.Utils.ZeroTransform(base.transform);
                currentSub.GetComponent<SubControl>().Set(SubControl.Mode.DirectInput);
                mode = Mode.Piloting;
                Inventory.main.quickSlots.DeselectImmediate();
                playerModeChanged.Trigger(mode);
            }
            else
            {
                Debug.LogError("tried to enter piloting mode while not in a sub!");
            }
        }

        public void TryEject()
        {
            if (isPiloting && CanEject())
            {
                ToNormalMode(!IsInSub());
            }
        }

        public bool EnterSittingMode()
        {
            if (mode != 0)
            {
                return false;
            }
            mode = Mode.Sitting;
            return true;
        }

        public void ExitSittingMode()
        {
            if (mode == Mode.Sitting)
            {
                mode = Mode.Normal;
            }
        }

        private void UpdateRotation()
        {
            Vector3 localEulerAngles = base.transform.localEulerAngles;
            if (!cinematicModeActive && (localEulerAngles.x != 0f || localEulerAngles.z != 0f))
            {
                base.transform.localEulerAngles = global::UWE.Utils.LerpEuler(localEulerAngles, new Vector3(0f, localEulerAngles.y, 0f), Time.deltaTime * 10f);
            }
        }

        private void UpdatePosition()
        {
            if (mode == Mode.LockedPiloting && base.transform.parent != null)
            {
                base.transform.localPosition = global::UWE.Utils.LerpVector(base.transform.localPosition, Vector3.zero, 5f * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (interpolate && rigidBody.interpolation != RigidbodyInterpolation.Interpolate && !rigidBody.isKinematic)
            {
                rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
            }
            else if (!interpolate || (rigidBody.isKinematic && rigidBody.interpolation != 0))
            {
                rigidBody.interpolation = RigidbodyInterpolation.None;
            }
            bool flag = !IsUnderwater() && !groundMotor.IsGrounded() && !cinematicModeActive;
            if (flag != wasFalling)
            {
                wasFalling = flag;
                if (flag)
                {
                    timeFallingBegan = Time.time;
                }
                else
                {
                    timeFallingBegan = -1f;
                }
            }
        }

        public bool GetPlayFallingAnimation()
        {
            if (mode == Mode.Normal && timeFallingBegan != -1f)
            {
                return timeFallingBegan + 0.45f <= Time.time;
            }
            return false;
        }

        private void LateUpdate()
        {
            UpdateRotation();
            UpdatePosition();
            string biomeStr = CalculateBiome();
            UpdateBiomeRichPresence(biomeStr);
            biomeString = biomeStr;
            int num = Mathf.FloorToInt(GetDepth());
            if (cachedDepth != num)
            {
                cachedDepth = num;
                if (num == 250 || num == 1000 || num == 2000 || num == 3000 || num == 4000)
                {
                    GoalManager goalManager = GoalManager.main;
                    if (goalManager != null)
                    {
                        goalManager.OnCustomGoalEvent("DepthLevel_" + num);
                    }
                }
            }
            UpdateVrFading();
        }

        private void UpdateBiomeRichPresence(string biomeStr)
        {
            if (!string.IsNullOrEmpty(biomeStr) && !string.Equals(biomeStr, biomeString, StringComparison.Ordinal))
            {
                int num = biomeStr.IndexOf('_');
                if (num >= 0)
                {
                    biomeStr = biomeStr.Substring(0, num);
                }
                PlatformUtils.main.GetServices().SetRichPresence("PresenceExploring_biome_" + biomeStr.ToLower());
            }
        }

        private string CalculateBiome()
        {
            if (ObservatoryAmbientSound.IsPlayerInObservatory())
            {
                return "observatory";
            }
            if ((bool)GeneratorRoomAmbientSound.main && GeneratorRoomAmbientSound.main.isPlayerInside)
            {
                return "generatorRoom";
            }
            if ((bool)CrashedShipAmbientSound.main && CrashedShipAmbientSound.main.isPlayerInside)
            {
                return "crashedShip";
            }
            string biomeOverride = AtmosphereDirector.main.GetBiomeOverride();
            if (precursorOutOfWater)
            {
                if (string.IsNullOrEmpty(biomeOverride))
                {
                    return "PrecursorGun";
                }
                if (biomeOverride.StartsWith("precursor", StringComparison.OrdinalIgnoreCase))
                {
                    return biomeOverride;
                }
                if (biomeOverride.StartsWith("prison", StringComparison.OrdinalIgnoreCase))
                {
                    return biomeOverride;
                }
                return "PrecursorGun";
            }
            if (!string.IsNullOrEmpty(biomeOverride))
            {
                return biomeOverride;
            }
            if ((bool)LargeWorld.main)
            {
                return LargeWorld.main.GetBiome(base.transform.position);
            }
            return "<unknown>";
        }

        private void HideEquipmentSlot(string slot)
        {
            _ = Inventory.main.equipment;
            int i = 0;
            for (int num = equipmentModels.Length; i < num; i++)
            {
                EquipmentType equipmentType = equipmentModels[i];
                if (!(equipmentType.slot == slot))
                {
                    continue;
                }
                int j = 0;
                for (int num2 = equipmentType.equipment.Length; j < num2; j++)
                {
                    EquipmentModel equipmentModel = equipmentType.equipment[j];
                    if ((bool)equipmentModel.model)
                    {
                        equipmentModel.model.SetActive(value: false);
                    }
                }
                if ((bool)equipmentType.defaultModel)
                {
                    equipmentType.defaultModel.SetActive(value: true);
                }
            }
        }

        private void EquipmentChanged(string slot, InventoryItem item)
        {
            Equipment equipment = Inventory.main.equipment;
            int i = 0;
            for (int num = equipmentModels.Length; i < num; i++)
            {
                EquipmentType equipmentType = equipmentModels[i];
                TechType techTypeInSlot = equipment.GetTechTypeInSlot(equipmentType.slot);
                bool flag = false;
                int j = 0;
                for (int num2 = equipmentType.equipment.Length; j < num2; j++)
                {
                    EquipmentModel equipmentModel = equipmentType.equipment[j];
                    bool flag2 = equipmentModel.techType == techTypeInSlot;
                    flag = flag || flag2;
                    if ((bool)equipmentModel.model)
                    {
                        equipmentModel.model.SetActive(flag2);
                    }
                }
                if ((bool)equipmentType.defaultModel)
                {
                    equipmentType.defaultModel.SetActive(!flag);
                }
            }
            UpdateReinforcedSuit();
        }

        private void UpdateReinforcedSuit()
        {
            temperatureDamage.minDamageTemperature = 49f;
            if (HasReinforcedSuit())
            {
                temperatureDamage.minDamageTemperature += 15f;
            }
            if (HasReinforcedGloves())
            {
                temperatureDamage.minDamageTemperature += 6f;
            }
        }

        public bool HasReinforcedSuit()
        {
            return Inventory.main.equipment.GetTechTypeInSlot("Body") == TechType.ReinforcedDiveSuit;
        }

        public bool HasReinforcedGloves()
        {
            return Inventory.main.equipment.GetTechTypeInSlot("Gloves") == TechType.ReinforcedGloves;
        }

        public void SetRadiationAmount(float amount)
        {
            _radiationAmount = amount;
        }

        public void ExitPilotingMode(bool keepCinematicState = false)
        {
            GameInput.ClearInput();
            Debug.Log("ExitPilotingMode");
            base.transform.parent = null;
            if (!keepCinematicState)
            {
                MainCameraControl.main.lookAroundMode = false;
                currChair.ReleaseBy(this);
            }
            currentSub.GetComponent<SubControl>().Set(SubControl.Mode.GameObjects);
            mode = Mode.Normal;
            currChair = null;
            playerModeChanged.Trigger(mode);
        }

        public void UpdateIsUnderwater()
        {
            Vector3 position;
            Vector3 vector = (position = base.transform.position) - Vector3.up * (isUnderwaterForSwimming.value ? 0.8f : (-0.1f));
            if (escapePod.value)
            {
                isUnderwater.Update(newValue: false);
            }
            else if (currentWaterPark != null)
            {
                isUnderwater.Update(newValue: true);
            }
            else if (currentSub != null)
            {
                isUnderwater.Update(currentSub.IsUnderwater(position));
            }
            else if (GetMode() == Mode.LockedPiloting)
            {
                isUnderwater.Update(newValue: false);
            }
            else
            {
                if (!precursorOutOfWater)
                {
                    float oceanLevel = Ocean.main.GetOceanLevel();
                    isUnderwater.Update(position.y < oceanLevel);
                    isUnderwaterForSwimming.Update(vector.y < oceanLevel);
                    return;
                }
                isUnderwater.Update(newValue: false);
            }
            isUnderwaterForSwimming.Update(isUnderwater.value);
        }

        private void OnPlayerIsUnderwaterChanged(Utils.MonitoredValue<bool> isUnderwater)
        {
            if (isUnderwater.value && 0f - playerController.velocity.y > 5.5f && (bool)surfaceFX && _displaySurfaceWater)
            {
                surfaceFX.Play();
            }
        }

        private void OnPlayerDepthClassChanged(Utils.MonitoredValue<int> depthClass)
        {
            if (IsInside() || precursorOutOfWater)
            {
                return;
            }
            if (depthClass.value == 0)
            {
                if ((bool)surfaceFX && _displaySurfaceWater)
                {
                    surfaceFX.Play(0);
                    surfaceFX.Play(1);
                }
                GoalManager.main.OnCustomGoalEvent("Player_Surface");
            }
            else
            {
                GoalManager.main.OnCustomGoalEvent("Player_Underwater");
            }
        }

        public void SetDisplaySurfaceWater(bool displaySurfaceWater)
        {
            _displaySurfaceWater = displaySurfaceWater;
            MainCamera.camera.GetComponent<WaterSurfaceOnCamera>().SetVisible(displaySurfaceWater);
            MainCamera.camera.GetComponent<WaterscapeVolumeOnCamera>().SetVisible(displaySurfaceWater);
        }

        public bool CanEject()
        {
            ProfilingUtils.BeginSample("CanEject");
            try
            {
                return currentMountedVehicle == null || currentMountedVehicle.GetAllowedToEject();
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        public Vehicle GetVehicle()
        {
            return currentMountedVehicle;
        }

        public bool ToNormalMode(bool findNewPosition = true)
        {
            bool result = true;
            switch (mode)
            {
                case Mode.Piloting:
                    ExitPilotingMode();
                    break;
                case Mode.LockedPiloting:
                    result = ExitLockedMode(respawn: false, findNewPosition);
                    break;
            }
            return result;
        }

        public void EndGame()
        {
            uGUI.main.hardcoreGameOver.Show();
        }

        private IEnumerator ResetPlayerOnDeath()
        {
            yield return new WaitForSeconds(5f);
            global::UWE.Utils.EnterPhysicsSyncSection();
            playerDeathEvent.Trigger(this);
            base.gameObject.SendMessage("DisableHeadCameraController", null, SendMessageOptions.RequireReceiver);
            uGUI.main.respawning.Show();
            ToNormalMode();
            bool lostStuff = Inventory.main.LoseItems();
            if ((bool)AtmosphereDirector.main)
            {
                AtmosphereDirector.main.ResetDirector();
            }
            bool flag = false;
            if (lastValidSub != null && CheckSubValid(lastValidSub))
            {
                RespawnPoint componentInChildren = lastValidSub.gameObject.GetComponentInChildren<RespawnPoint>();
                if ((bool)componentInChildren)
                {
                    SetPosition(componentInChildren.GetSpawnPosition());
                    SetCurrentSub(lastValidSub);
                    flag = true;
                }
            }
            if (!flag)
            {
                EscapePod escapePod = lastEscapePod ?? EscapePod.main;
                if ((bool)escapePod)
                {
                    escapePod.RespawnPlayer();
                    SetCurrentSub(null);
                    currentEscapePod = escapePod;
                }
            }
            yield return new WaitForSeconds(1f);
            LargeWorldStreamer streamer = LargeWorldStreamer.main;
            while (!streamer.IsWorldSettled())
            {
                yield return CoroutineUtils.waitForNextFrame;
            }
            uGUI.main.respawning.Hide();
            if ((bool)liveMixin)
            {
                liveMixin.ResetHealth();
            }
            oxygenMgr.AddOxygen(1000f);
            timeSpawned = Time.time;
            playerRespawnEvent.Trigger(this);
            DamageFX.main.ClearHudDamage();
            SuffocationReset();
            yield return null;
            precursorOutOfWater = false;
            SetDisplaySurfaceWater(displaySurfaceWater: true);
            UnfreezeStats();
            Inventory.main.quickSlots.SetIgnoreHotkeyInput(ignore: false);
            GetPDA().SetIgnorePDAInput(ignore: false);
            playerController.inputEnabled = true;
            playerController.SetEnabled(enabled: true);
            yield return new WaitForSeconds(1f);
            global::UWE.Utils.ExitPhysicsSyncSection();
            ErrorMessage.AddWarning((!lostStuff) ? Language.main.Get("YouDied") : Language.main.Get("YouDiedLostStuff"));
        }

        public bool SetPrecursorOutOfWater(bool on)
        {
            if (!liveMixin.IsAlive())
            {
                return false;
            }
            precursorOutOfWater = on;
            return true;
        }

        public void OnKill(DamageType damageType)
        {
            base.gameObject.SendMessage("EnableHeadCameraController", null, SendMessageOptions.RequireReceiver);
            if (GetPDA().isOpen)
            {
                GetPDA().Close();
            }
            if ((bool)deathMusic)
            {
                deathMusic.StartEvent();
            }
            switch (damageType)
            {
                case DamageType.Fire:
                    playerAnimator.SetTrigger("player_death_fire");
                    break;
                case DamageType.Explosive:
                    playerAnimator.SetTrigger("player_death_explosion");
                    break;
                default:
                    playerAnimator.SetTrigger("player_death");
                    break;
            }
            if (GameModeUtils.IsPermadeath())
            {
                SaveLoadManager.main.ClearSlotAsync(SaveLoadManager.main.GetCurrentSlot());
                EndGame();
                return;
            }
            uGUI.main.overlays.Set(0, 1f);
            MainCameraControl.main.enabled = false;
            playerController.inputEnabled = false;
            Inventory.main.quickSlots.SetIgnoreHotkeyInput(ignore: true);
            GetPDA().SetIgnorePDAInput(ignore: true);
            playerController.SetEnabled(enabled: false);
            FreezeStats();
            StartCoroutine("ResetPlayerOnDeath");
        }

        public float GetSurfaceDepth()
        {
            return 0.1f;
        }

        public Ocean.DepthClass GetDepthClass()
        {
            Ocean.DepthClass result = Ocean.DepthClass.Surface;
            CrushDamage crushDamage = null;
            if ((currentSub != null && !currentSub.isBase) || mode == Mode.LockedPiloting)
            {
                crushDamage = ((!(currentSub != null)) ? base.gameObject.GetComponentInParent<CrushDamage>() : currentSub.gameObject.GetComponent<CrushDamage>());
            }
            if (crushDamage != null)
            {
                result = crushDamage.GetDepthClass();
                crushDepth = crushDamage.crushDepth;
            }
            else
            {
                crushDepth = 0f;
                float depthOf = Ocean.main.GetDepthOf(base.gameObject);
                if (depthOf > 200f)
                {
                    result = Ocean.DepthClass.Crush;
                }
                else if (depthOf > 100f)
                {
                    result = Ocean.DepthClass.Unsafe;
                }
                else if (depthOf > GetSurfaceDepth())
                {
                    result = Ocean.DepthClass.Safe;
                }
            }
            return result;
        }

        public float GetBreathPeriod()
        {
            Mode mode = this.mode;
            if (mode != 0 && (uint)(mode - 1) <= 1u)
            {
                return 3f;
            }
            if (Inventory.Get().equipment.GetCount(TechType.Rebreather) > 0)
            {
                return 3f;
            }
            return GetDepthClass() switch
            {
                Ocean.DepthClass.Crush => 1.5f, 
                Ocean.DepthClass.Unsafe => 2.25f, 
                Ocean.DepthClass.Safe => 3f, 
                _ => 99999f, 
            };
        }

        private float GetOxygenPerBreath(float breathingInterval, int depthClass)
        {
            float num = 1f;
            if (Inventory.main.equipment.GetCount(TechType.Rebreather) == 0 && mode != Mode.Piloting && mode != Mode.LockedPiloting)
            {
                switch (depthClass)
                {
                    case 2:
                        num = 1.5f;
                        break;
                    case 3:
                        num = 2f;
                        break;
                }
            }
            float result = breathingInterval * num;
            if (!GameModeUtils.RequiresOxygen())
            {
                result = 0f;
            }
            return result;
        }

        private void SetMotorMode(MotorMode newMotorMode)
        {
            if (newMotorMode != motorMode)
            {
                playerController.SetMotorMode(newMotorMode);
                motorMode = newMotorMode;
                SetScubaMaskActive(motorMode == MotorMode.Dive || motorMode == MotorMode.Seaglide);
                playerMotorModeChanged.Trigger(newMotorMode);
                if (!IsSwimming() && MainGameController.Instance.CanPerformAssetCollection())
                {
                    ProfilingUtils.BeginSample("UnloadUnusedAssets");
                    MainGameController.Instance.PerformGarbageAndAssetCollection();
                    ProfilingUtils.EndSample();
                }
                if (IsSwimming())
                {
                    ProfilingUtils.BeginSample("TriggerDiveGoal");
                    diveGoal.Trigger();
                    ProfilingUtils.EndSample();
                }
            }
        }

        private void UpdateMotorMode()
        {
            if (Inventory.main.GetPickupCount(TechType.Exosuit) > 0)
            {
                SetMotorMode(MotorMode.Mech);
            }
            else if (GetCurrentSub() == null && currentEscapePod == null)
            {
                if (mode == Mode.LockedPiloting)
                {
                    SetMotorMode(MotorMode.Vehicle);
                }
                else if (isUnderwater.value)
                {
                    bool flag = false;
                    Pickupable held = Inventory.main.GetHeld();
                    if (held != null && held.gameObject.GetComponent<Seaglide>() != null)
                    {
                        EnergyMixin component = held.gameObject.GetComponent<EnergyMixin>();
                        if (component != null && !component.IsDepleted())
                        {
                            flag = true;
                        }
                    }
                    if (flag)
                    {
                        SetMotorMode(MotorMode.Seaglide);
                    }
                    else
                    {
                        SetMotorMode(MotorMode.Dive);
                    }
                }
                else
                {
                    SetMotorMode(MotorMode.Run);
                }
            }
            else
            {
                SetMotorMode(MotorMode.Walk);
            }
        }

        private void HandleShotgunMode()
        {
            if (!main.GetRightHandDown() || !((double)timeLastShotgun + 0.3 < (double)Time.time))
            {
                return;
            }
            Vector3 position = default(Vector3);
            GameObject closestObj = null;
            global::UWE.Utils.TraceFPSTargetPosition(base.gameObject, 3000f, ref closestObj, ref position);
            if (closestObj != null)
            {
                LiveMixin liveMixin = closestObj.GetComponent<LiveMixin>();
                if (!liveMixin)
                {
                    liveMixin = Utils.FindEnabledAncestorWithComponent<LiveMixin>(closestObj);
                }
                if ((bool)liveMixin)
                {
                    liveMixin.TakeDamage(100f, position);
                }
                WorldForces.AddExplosion(position, DayNightCycle.main.timePassed, 100f, 20f);
            }
            Utils.PlayEnvSound("event:/tools/stasis_gun/fire", base.transform.position, 1f);
            timeLastShotgun = Time.time;
        }

        public void OnPlayerPositionCheat()
        {
            currentEscapePod = null;
            escapePod.Update(newValue: false);
            currentSub = null;
            currentWaterPark = null;
            precursorOutOfWater = false;
            SetDisplaySurfaceWater(displaySurfaceWater: true);
        }

        public virtual void Update()
        {
            ProfilingUtils.BeginSample("Player.Update()");
            if (shotgunMode)
            {
                HandleShotgunMode();
            }
            UpdateIsUnderwater();
            UpdateMotorMode();
            ValidateEscapePod();
            depthClass.Update((int)GetDepthClass());
            movementSpeed = playerController.velocity.magnitude / 5f;
            depthLevel = base.transform.position.y;
            timeMonitor.Update(Time.time);
            SuffocationUpdate();
            if (!CanBreathe() && !IsFrozenStats())
            {
                float breathPeriod = GetBreathPeriod();
                if (timeMonitor.DidChangeInterval(breathPeriod))
                {
                    oxygenMgr.RemoveOxygen(GetOxygenPerBreath(breathPeriod, depthClass.value));
                    tookBreathEvent.Trigger(this);
                }
            }
            SetScubaMaskActive(IsUnderwater());
            UpdateRadiationSound();
            RestoreSubIfNeeded();
            RestoreWaterParkIfNeeded();
            UpdateItemHolstering();
            infectedMixin.UpdateInfectionShading();
            ProfilingUtils.EndSample();
        }

        public void StartPlayerInfectionReveal()
        {
            timePlayerInfectionRevealed = Time.time;
        }

        public void StartPlayerInfectionCure()
        {
            timePlayerInfectionCured = Time.time;
        }

        public float GetInfectionAmount()
        {
            if (timePlayerInfectionCured > 0f && infectedMixin.infectedAmount > 0f)
            {
                float time = Mathf.Clamp01((Time.time - timePlayerInfectionCured) / 20f);
                return infectionCureCurve.Evaluate(time);
            }
            if (infectionRevealed && infectedMixin.infectedAmount >= 1f)
            {
                float time2 = Mathf.Clamp01((Time.time - timePlayerInfectionRevealed) / 8f);
                return infectionRevealCurve.Evaluate(time2);
            }
            return 0f;
        }

        public IEnumerator TriggerInfectionRevealAsync()
        {
            if (!infectionRevealed)
            {
                float num = armsController.StartHolsterTime(12f);
                if (num > 0f)
                {
                    yield return new WaitForSeconds(num);
                }
                infectionRevealed = true;
                StartPlayerInfectionReveal();
                StartHideGlovesFor(12f);
                Utils.PlayFMODAsset(infectionRevealSound, base.transform, 0f);
                playerAnimator.SetTrigger("player_infected");
            }
        }

        public void StartHideGlovesFor(float hideDuration)
        {
            HideEquipmentSlot("Gloves");
            StartCoroutine(ShowGlovesInAsync(hideDuration));
        }

        private IEnumerator ShowGlovesInAsync(float inSeconds)
        {
            yield return new WaitForSeconds(inSeconds);
            EquipmentChanged(string.Empty, null);
        }

        public bool GetCanItemBeUsed()
        {
            if (cinematicModeActive)
            {
                return false;
            }
            return !playerController.IsSprinting();
        }

        private void UpdateItemHolstering()
        {
            if (!playerController.enabled)
            {
                restoreQuickSlot = -1;
            }
            else if (playerController.IsSprinting())
            {
                if (restoreQuickSlot == -1)
                {
                    restoreQuickSlot = Inventory.main.quickSlots.GetActiveSlotID();
                    Inventory.main.quickSlots.Deselect();
                }
            }
            else if (restoreQuickSlot != -1)
            {
                Inventory.main.quickSlots.Select(restoreQuickSlot);
                restoreQuickSlot = -1;
            }
        }

        private void RestoreSubIfNeeded()
        {
            if (!string.IsNullOrEmpty(currentSubUID) && UniqueIdentifier.TryGetIdentifier(currentSubUID, out var uid))
            {
                SetCurrentSub(uid.GetComponent<SubRoot>());
                currentSubUID = null;
            }
        }

        private void RestoreWaterParkIfNeeded()
        {
            if (!string.IsNullOrEmpty(currentWaterParkUID) && UniqueIdentifier.TryGetIdentifier(currentWaterParkUID, out var uid))
            {
                currentWaterPark = uid.GetComponent<WaterPark>();
                currentWaterParkUID = null;
            }
        }

        private void UpdateRadiationSound()
        {
            float num = radiationAmount;
            if (fmodIndexIntensity < 0)
            {
                fmodIndexIntensity = radiateSound.GetParameterIndex("intensity");
            }
            if (num > 0f)
            {
                radiateSound.Play();
                radiateSound.SetParameterValue(fmodIndexIntensity, num);
            }
            else
            {
                radiateSound.Stop();
            }
        }

        public void PlayGrab()
        {
            timeGrabbed = Time.time;
        }

        public Transform GetLeftHandBone()
        {
            return leftHandBone;
        }

        public bool PlayBash()
        {
            PlayerTool heldTool = Inventory.main.GetHeldTool();
            bool num = heldTool != null && heldTool.hasBashAnimation;
            if (num)
            {
                timeBashed = Time.time;
            }
            return num;
        }

        public bool IsInClawExosuit()
        {
            Exosuit exosuit = currentMountedVehicle as Exosuit;
            if (exosuit != null)
            {
                return exosuit.HasClaw();
            }
            return false;
        }

        public bool IsFreeToInteract()
        {
            if (!cinematicModeActive)
            {
                if (mode != 0)
                {
                    return mode == Mode.Piloting;
                }
                return true;
            }
            return false;
        }

        public void OnTakeDamage(DamageInfo damageInfo)
        {
            float num = damageInfo.damage / 100f;
            MainCameraControl.main.ShakeCamera(num * 2f);
            if (num > 0f && damageInfo.type != DamageType.Radiation)
            {
                DamageFX.main.AddHudDamage(num, damageInfo.position, damageInfo);
            }
        }

        public bool GetPlayerInAnimState(string layer, string state)
        {
            if ((bool)armsController)
            {
                return armsController.IsInAnimationState(layer, state);
            }
            return false;
        }

        public bool GetInMechMode()
        {
            return motorMode == MotorMode.Mech;
        }

        public void OnSwimFastStartAnim(AnimationEvent e)
        {
            swimFastEvent.Trigger(this);
        }

        public void OnSwimFastEndAnim(AnimationEvent e)
        {
        }

        public void PlayOneShotPS(GameObject particleSystemTemplate)
        {
            Utils.PlayOneShotPS(particleSystemTemplate, fpParticleEmissionPoint.transform.position, fpParticleEmissionPoint.transform.rotation, fpParticleEmissionPoint.transform);
        }

        private void SuffocationReset()
        {
            suffocation.Set(0f, current: true, target: true);
        }

        private void SuffocationUpdate()
        {
            bool flag = !Utils.NearlyEqual(GetOxygenAvailable(), 0f);
            if (flag != suffocation.target)
            {
                if (flag)
                {
                    suffocation.Set(suffocationRecoveryTime, flag);
                }
                else
                {
                    suffocation.Set(suffocationTime, flag, SuffocationDie);
                }
            }
            suffocation.Update();
            uGUI.main.overlays.Set(0, 1f - suffocation.t);
        }

        private void SuffocationDie()
        {
            liveMixin.Kill();
        }

        public bool GetLeftHandDown()
        {
            return GameInput.GetButtonDown(GameInput.Button.LeftHand);
        }

        public bool GetLeftHandUp()
        {
            return GameInput.GetButtonUp(GameInput.Button.LeftHand);
        }

        public bool GetRightHandDown()
        {
            return GameInput.GetButtonDown(GameInput.Button.RightHand);
        }

        public bool GetRightHandUp()
        {
            return GameInput.GetButtonUp(GameInput.Button.RightHand);
        }

        public bool GetLeftHandHeld()
        {
            return GameInput.GetButtonHeld(GameInput.Button.LeftHand);
        }

        public bool GetRightHandHeld()
        {
            return GameInput.GetButtonHeld(GameInput.Button.RightHand);
        }

        public bool GetReloadUp()
        {
            return GameInput.GetButtonUp(GameInput.Button.Reload);
        }

        public bool GetReloadHeld()
        {
            return GameInput.GetButtonHeld(GameInput.Button.Reload);
        }

        public string GetBiomeString()
        {
            return biomeString ?? "";
        }

        public bool GetQuickSlotKeyDown(int slotID)
        {
            if (slotID < 0 || slotID >= quickSlotButtonsCount)
            {
                return false;
            }
            return GameInput.GetButtonDown(quickSlotButtons[slotID]);
        }

        public bool GetQuickSlotKeyHeld(int slotID)
        {
            if (slotID < 0 || slotID >= quickSlotButtonsCount)
            {
                return false;
            }
            return GameInput.GetButtonHeld(quickSlotButtons[slotID]);
        }

        public bool GetQuickSlotKeyUp(int slotID)
        {
            if (slotID < 0 || slotID >= quickSlotButtonsCount)
            {
                return false;
            }
            return GameInput.GetButtonUp(quickSlotButtons[slotID]);
        }

        private bool GetPlayMetalFootstepSounds()
        {
            if (groundMotor.GetGroundSurfaceType() != VFXSurfaceTypes.metal && !(GetCurrentSub() != null) && !escapePod.value && !(biomeString == "crashedShip"))
            {
                return precursorOutOfWater;
            }
            return true;
        }

        private void OnLand(Vector3 velocity)
        {
            if (!IsUnderwater())
            {
                if (GetPlayMetalFootstepSounds())
                {
                    Utils.PlayFMODAsset(footStepSounds.metalSound, bottom, 1f);
                }
                else
                {
                    Utils.PlayFMODAsset(footStepSounds.landSound, bottom, 1f);
                }
                float num = (0f - Mathf.Min(0f, velocity.y - -10f)) * 2.5f;
                if (num > 0f)
                {
                    liveMixin.TakeDamage(num, base.transform.position, DamageType.Collide);
                }
            }
            MainCameraControl.main.OnLand(velocity);
        }

        private void OnJump()
        {
            if (!IsUnderwater())
            {
                Transform t = MainCamera.camera.transform;
                if (GetPlayMetalFootstepSounds())
                {
                    Utils.PlayFMODAsset(footStepSounds.metalSound, t, 1f);
                }
                else
                {
                    Utils.PlayFMODAsset(footStepSounds.landSound, t, 1f);
                }
            }
            Utils.PlayFMODAsset(jumpSound, MainCamera.camera.transform, 0f);
            timeFallingBegan = Time.time - 0.45f;
            wasFalling = true;
        }

        private void OnAcidEnter()
        {
            acidLoopingSound.Play();
        }

        private void OnAcidExit()
        {
            acidLoopingSound.Stop();
        }

        bool IObstacle.CanDeconstruct(out string reason)
        {
            reason = Language.main.Get("PlayerObstacle");
            return false;
        }

        public string CompileTimeCheck()
        {
            UniqueIdentifier[] components = GetComponents<UniqueIdentifier>();
            if (components.Length != 1)
            {
                return "Expecting exactly one unique identifier";
            }
            SceneObjectIdentifier sceneObjectIdentifier = components[0] as SceneObjectIdentifier;
            if (!sceneObjectIdentifier)
            {
                return "Expecting a SceneObjectIdentifier";
            }
            if (sceneObjectIdentifier.serializeObjectTree)
            {
                return "Player must not serialize the entire object tree (Inventory would duplicate!)";
            }
            return null;
        }

        private void UpdateVrFading()
        {
            if (XRSettings.enabled)
            {
                float num = Vector3.Distance(camAnchor.transform.position, camRoot.mainCam.transform.position);
                float num2 = 0.1f;
                float num3 = 0.01f;
                float value = Mathf.Clamp01((num - num3) / (num2 - num3));
                Shader.SetGlobalFloat(ShaderPropertyID._UweVrFadeAmount, value);
            }
            else
            {
                Shader.SetGlobalFloat(ShaderPropertyID._UweVrFadeAmount, 0f);
            }
        }

        static Player()
        {
            InitialEquipment[] array = new InitialEquipment[9];
            InitialEquipment initialEquipment = new InitialEquipment
            {
                techType = TechType.Knife,
                count = 1
            };
            array[0] = initialEquipment;
            initialEquipment = new InitialEquipment
            {
                techType = TechType.Flashlight,
                count = 1
            };
            array[1] = initialEquipment;
            initialEquipment = new InitialEquipment
            {
                techType = TechType.Builder,
                count = 1
            };
            array[2] = initialEquipment;
            initialEquipment = new InitialEquipment
            {
                techType = TechType.Seaglide,
                count = 1
            };
            array[3] = initialEquipment;
            initialEquipment = new InitialEquipment
            {
                techType = TechType.PropulsionCannon,
                count = 1
            };
            array[4] = initialEquipment;
            initialEquipment = new InitialEquipment
            {
                techType = TechType.StasisRifle,
                count = 1
            };
            array[5] = initialEquipment;
            initialEquipment = new InitialEquipment
            {
                techType = TechType.Constructor,
                count = 1
            };
            array[6] = initialEquipment;
            initialEquipment = new InitialEquipment
            {
                techType = TechType.Fins,
                count = 1
            };
            array[7] = initialEquipment;
            initialEquipment = new InitialEquipment
            {
                techType = TechType.Floater,
                count = 5
            };
            array[8] = initialEquipment;
            creativeEquipment = array;
            wantInterpolate = true;
            quickSlotButtons = new GameInput.Button[5]
            {
                GameInput.Button.Slot1,
                GameInput.Button.Slot2,
                GameInput.Button.Slot3,
                GameInput.Button.Slot4,
                GameInput.Button.Slot5
            };
            quickSlotButtonsCount = quickSlotButtons.Length;
        }
    }
}
