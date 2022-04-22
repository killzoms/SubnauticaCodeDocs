using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Rigidbody))]
    [ProtoContract]
    [ProtoInclude(1000, typeof(BloomCreature))]
    [ProtoInclude(1200, typeof(Boomerang))]
    [ProtoInclude(1300, typeof(LavaLarva))]
    [ProtoInclude(1400, typeof(OculusFish))]
    [ProtoInclude(1500, typeof(Eyeye))]
    [ProtoInclude(1600, typeof(Garryfish))]
    [ProtoInclude(1700, typeof(GasoPod))]
    [ProtoInclude(1800, typeof(Grabcrab))]
    [ProtoInclude(1900, typeof(Grower))]
    [ProtoInclude(2000, typeof(Holefish))]
    [ProtoInclude(2100, typeof(Hoverfish))]
    [ProtoInclude(2200, typeof(Jellyray))]
    [ProtoInclude(2300, typeof(Jumper))]
    [ProtoInclude(2400, typeof(Peeper))]
    [ProtoInclude(2500, typeof(RabbitRay))]
    [ProtoInclude(2600, typeof(Reefback))]
    [ProtoInclude(2700, typeof(Reginald))]
    [ProtoInclude(2800, typeof(SandShark))]
    [ProtoInclude(2900, typeof(Spadefish))]
    [ProtoInclude(3000, typeof(Stalker))]
    [ProtoInclude(3100, typeof(Bladderfish))]
    [ProtoInclude(3200, typeof(Hoopfish))]
    [ProtoInclude(3300, typeof(Mesmer))]
    [ProtoInclude(3400, typeof(Bleeder))]
    [ProtoInclude(3500, typeof(Slime))]
    [ProtoInclude(3600, typeof(Crash))]
    [ProtoInclude(3700, typeof(BoneShark))]
    [ProtoInclude(3800, typeof(CuteFish))]
    [ProtoInclude(3900, typeof(Leviathan))]
    [ProtoInclude(4000, typeof(ReaperLeviathan))]
    [ProtoInclude(4100, typeof(CaveCrawler))]
    [ProtoInclude(4200, typeof(BirdBehaviour))]
    [ProtoInclude(4400, typeof(Biter))]
    [ProtoInclude(4500, typeof(Shocker))]
    [ProtoInclude(4600, typeof(CrabSnake))]
    [ProtoInclude(4700, typeof(SpineEel))]
    [ProtoInclude(4800, typeof(SeaTreader))]
    [ProtoInclude(4900, typeof(CrabSquid))]
    [ProtoInclude(4910, typeof(Warper))]
    [ProtoInclude(4920, typeof(LavaLizard))]
    [ProtoInclude(5000, typeof(SeaDragon))]
    [ProtoInclude(5100, typeof(GhostRay))]
    [ProtoInclude(5200, typeof(SeaEmperorBaby))]
    [ProtoInclude(5300, typeof(GhostLeviathan))]
    [ProtoInclude(5400, typeof(SeaEmperorJuvenile))]
    [ProtoInclude(5500, typeof(GhostLeviatanVoid))]
    [RequireComponent(typeof(CreatureUtils))]
    public class Creature : Living, IProtoEventListener, IOnTakeDamage, IScheduledUpdateBehaviour, IManagedBehaviour
    {
        [SerializeField]
        private Animator traitsAnimator;

        [AssertNotNull]
        public LiveMixin liveMixin;

        [AssertNotNull]
        public AnimationCurve initialCuriosity;

        [AssertNotNull]
        public AnimationCurve initialFriendliness;

        [AssertNotNull]
        public AnimationCurve initialHunger;

        [AssertNotNull]
        public CreatureTrait Curiosity;

        [AssertNotNull]
        public CreatureTrait Friendliness;

        [AssertNotNull]
        public CreatureTrait Hunger;

        [AssertNotNull]
        public CreatureTrait Aggression;

        [AssertNotNull]
        public CreatureTrait Scared;

        [AssertNotNull]
        public CreatureTrait Tired;

        [AssertNotNull]
        public CreatureTrait Happy;

        [AssertNotNull]
        public AnimationCurve activity;

        public float maxFlinchAnimationDamage = 10f;

        private float flinch;

        private float flinchFadeRate = 1f;

        public bool hasEyes = true;

        public float eyeFOV = 0.25f;

        public bool eyesOnTop;

        public float hearingSensitivity = 1f;

        public bool detectsMotion = true;

        private const int currentVersion = 3;

        [NonSerialized]
        [ProtoMember(1)]
        public Vector3 leashPosition = Vector3.zero;

        [NonSerialized]
        [ProtoMember(2)]
        public int version = 3;

        [NonSerialized]
        [ProtoMember(3)]
        public bool isInitialized;

        private readonly List<CreatureAction> actions = new List<CreatureAction>();

        private CreatureAction prevBestAction;

        private CreatureAction lastAction;

        private float timeLastActionSet;

        public float babyScaleSize = 0.5f;

        [AssertNotNull]
        public AnimationCurve sizeDistribution;

        private float Size = -1f;

        public float seaLevelOffset;

        public bool cyclopsSonarDetectable;

        public bool debug;

        public string debugActionsString;

        private long techTypeHash;

        private static readonly int animAggressive = Animator.StringToHash("aggressive");

        private static readonly int animScared = Animator.StringToHash("scared");

        private static readonly int animTired = Animator.StringToHash("tired");

        private static readonly int animHappy = Animator.StringToHash("happy");

        private static readonly int animFlinch = Animator.StringToHash("flinch");

        [NonSerialized]
        public GameObject friend;

        protected const string kSafeShallows = "safeShallows";

        public static Bounds prisonAquriumBounds = new Bounds(new Vector3(325f, -1554.33f, -455f), new Vector3(550f, 200f, 550f));

        private const string kPrisonAquariumPrefix = "Prison_Aquarium";

        private int indexLastActionChecked;

        private string newDebugActionsString = string.Empty;

        private float lastUpdateTime = -1f;

        public int scheduledUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "Creature";
        }

        public virtual void Start()
        {
            if (initialCuriosity != null && initialCuriosity.length > 0)
            {
                Curiosity.Value = initialCuriosity.Evaluate(global::UnityEngine.Random.value);
            }
            if (initialFriendliness != null && initialFriendliness.length > 0)
            {
                Friendliness.Value = initialFriendliness.Evaluate(global::UnityEngine.Random.value);
            }
            if (initialHunger != null && initialHunger.length > 0)
            {
                Hunger.Value = initialHunger.Evaluate(global::UnityEngine.Random.value);
            }
            bool flag = !isInitialized && Size < 0f;
            float magnitude = (base.transform.localScale - Vector3.one).magnitude;
            if (flag && !Utils.NearlyEqual(magnitude, 0f))
            {
                base.transform.localScale = Vector3.one;
            }
            GrowMixin component = base.gameObject.GetComponent<GrowMixin>();
            if ((bool)component)
            {
                component.growScalarChanged.AddHandler(base.gameObject, OnGrowChanged);
            }
            else if (flag && sizeDistribution != null)
            {
                float size = Mathf.Clamp01(sizeDistribution.Evaluate(global::UnityEngine.Random.value));
                SetSize(size);
            }
            TechType techType = CraftData.GetTechType(base.gameObject);
            if (techType != 0)
            {
                techTypeHash = global::UWE.Utils.SDBMHash(techType.AsString());
            }
            else if (!PlatformUtils.isConsolePlatform)
            {
                Debug.LogError("Creature: Couldn't find tech type for creature name: " + base.gameObject.name);
            }
            ScanCreatureActions();
            if (isInitialized)
            {
                InitializeAgain();
            }
            else
            {
                InitializeOnce();
                isInitialized = true;
            }
            DeferredSchedulerUtils.Schedule(this);
        }

        protected virtual void InitializeOnce()
        {
            ProcessInfection();
            leashPosition = base.transform.position;
        }

        protected virtual void InitializeAgain()
        {
            InfectedMixin component = base.gameObject.GetComponent<InfectedMixin>();
            if ((bool)component && component.IsInfected() && global::UnityEngine.Random.value < 0.2f)
            {
                component.SetInfectedAmount(1f);
            }
        }

        private void ProcessInfection()
        {
            if (!LargeWorld.main)
            {
                return;
            }
            string biome = LargeWorld.main.GetBiome(base.transform.position);
            bool num = string.Equals("safeShallows", biome, StringComparison.OrdinalIgnoreCase);
            bool flag = prisonAquriumBounds.Contains(base.transform.position);
            if (!num && !flag)
            {
                InfectedMixin component = base.gameObject.GetComponent<InfectedMixin>();
                if ((bool)component && global::UnityEngine.Random.value < 0.05f)
                {
                    component.SetInfectedAmount(1f);
                }
            }
        }

        private CreatureAction ChooseBestAction()
        {
            if (actions.Count == 0)
            {
                return null;
            }
            if ((bool)liveMixin && !liveMixin.IsAlive())
            {
                SwimBehaviour component = GetComponent<SwimBehaviour>();
                if ((bool)component)
                {
                    component.Idle();
                }
                return null;
            }
            ProfilingUtils.BeginSample("Creature.ChooseBestAction");
            float num = 0f;
            CreatureAction creatureAction = null;
            if (prevBestAction != null)
            {
                creatureAction = prevBestAction;
                ProfilingUtils.BeginSample("bestAction.Evaluate");
                num = creatureAction.Evaluate(this);
                ProfilingUtils.EndSample();
            }
            indexLastActionChecked++;
            if (indexLastActionChecked >= actions.Count)
            {
                indexLastActionChecked = 0;
            }
            CreatureAction creatureAction2 = actions[indexLastActionChecked];
            ProfilingUtils.BeginSample("current.Evaluate");
            float num2 = creatureAction2.Evaluate(this);
            ProfilingUtils.EndSample();
            if (debug)
            {
                Debug.Log(string.Concat(base.gameObject.name, ".", creatureAction2.GetType(), ".Evaluate() returned: ", num2));
            }
            if (Application.isEditor && CreatureDebugger.main.debug)
            {
                newDebugActionsString = string.Concat(creatureAction2.GetType(), ": ", num2, " ");
            }
            if (num2 > num && !Utils.NearlyEqual(num2, 0f))
            {
                if (debug)
                {
                    Debug.LogFormat("     Found {0} evaluation with higher priority {1} then {2}", creatureAction2.GetType(), num2, (creatureAction != null) ? creatureAction.GetType().ToString() : "<none>");
                }
                num = num2;
                creatureAction = creatureAction2;
            }
            if (creatureAction != null && Application.isEditor && CreatureDebugger.main.debug)
            {
                debugActionsString = newDebugActionsString + " => " + creatureAction.GetType();
            }
            ProfilingUtils.EndSample();
            return creatureAction;
        }

        public void ScanCreatureActions()
        {
            actions.Clear();
            CreatureAction[] components = base.gameObject.GetComponents<CreatureAction>();
            foreach (CreatureAction creatureAction in components)
            {
                if (creatureAction.enabled)
                {
                    actions.Add(creatureAction);
                }
            }
            indexLastActionChecked = actions.Count - 1;
        }

        public CreatureAction GetBestAction()
        {
            return prevBestAction;
        }

        private void UpdateBehaviour(float deltaTime)
        {
            _ = StopwatchProfiler.Instance;
            ProfilingUtils.BeginSample("CreatureBehavior::Update");
            ProfilingUtils.BeginSample("choose action");
            CreatureAction creatureAction = ChooseBestAction();
            if (prevBestAction != creatureAction)
            {
                if ((bool)prevBestAction)
                {
                    prevBestAction.StopPerform(this);
                }
                if ((bool)creatureAction)
                {
                    creatureAction.StartPerform(this);
                }
                prevBestAction = creatureAction;
            }
            ProfilingUtils.EndSample();
            if ((bool)creatureAction)
            {
                ProfilingUtils.BeginSample("perform action");
                creatureAction.Perform(this, deltaTime);
                ProfilingUtils.EndSample();
                ProfilingUtils.BeginSample("debug");
                if (Application.isEditor)
                {
                    lastAction = creatureAction;
                    timeLastActionSet = Time.time;
                }
                ProfilingUtils.EndSample();
            }
            float num = DayNightUtils.Evaluate(1f, activity);
            Tired.Value = Mathf.Lerp(Tired.Value, 1f - num, 0.1f * deltaTime);
            ProfilingUtils.BeginSample("update traits");
            Curiosity.UpdateTrait(deltaTime);
            Friendliness.UpdateTrait(deltaTime);
            Hunger.UpdateTrait(deltaTime);
            Aggression.UpdateTrait(deltaTime);
            Scared.UpdateTrait(deltaTime);
            Tired.UpdateTrait(deltaTime);
            Happy.UpdateTrait(deltaTime);
            flinch = Mathf.Lerp(flinch, 0f, flinchFadeRate * deltaTime);
            ProfilingUtils.EndSample();
            if ((bool)traitsAnimator && traitsAnimator.isActiveAndEnabled)
            {
                traitsAnimator.SetFloat(animAggressive, Aggression.Value);
                traitsAnimator.SetFloat(animScared, Scared.Value);
                traitsAnimator.SetFloat(animTired, Tired.Value);
                traitsAnimator.SetFloat(animHappy, Happy.Value);
                traitsAnimator.SetFloat(animFlinch, flinch);
            }
            ProfilingUtils.EndSample();
        }

        public void ScheduledUpdate()
        {
            float deltaTime = Time.time - lastUpdateTime;
            UpdateBehaviour(deltaTime);
            lastUpdateTime = Time.time;
        }

        private void OnGrowChanged(float growScalar)
        {
            SetSize(growScalar);
        }

        private void SetSize(float size)
        {
            float num = Mathf.Lerp(babyScaleSize, 1f, size);
            base.transform.localScale = new Vector3(num, num, num);
            Size = size;
        }

        public Animator GetAnimator()
        {
            return traitsAnimator;
        }

        public float GetSize()
        {
            return Size;
        }

        public void SetScale(float scale)
        {
            base.transform.localScale = scale * Vector3.one;
            Size = Mathf.InverseLerp(babyScaleSize, 1f, scale);
        }

        public float GetSpeedScalar()
        {
            float num = 1f;
            SpeedGene component = base.gameObject.GetComponent<SpeedGene>();
            if ((bool)component)
            {
                num += component.GetSpeedScalar();
                Debug.Log(base.gameObject.name + ".GetSpeedScalar() gene affecting scalar is " + num);
            }
            return num;
        }

        public virtual string GetActiveBehaviourName()
        {
            return GetType().ToString();
        }

        public CreatureAction GetLastAction()
        {
            return lastAction;
        }

        public string GetLastActionDebugString()
        {
            return debugActionsString;
        }

        public float GetTimeLastActionSet()
        {
            return timeLastActionSet;
        }

        public virtual void OnTakeDamage(DamageInfo damageInfo)
        {
            float num = damageInfo.damage;
            if (damageInfo.type == DamageType.Electrical)
            {
                num *= 35f;
            }
            flinch += num / maxFlinchAnimationDamage;
            flinch = Mathf.Clamp01(flinch);
        }

        public virtual void OnKill()
        {
            base.enabled = false;
        }

        public virtual void OnDrop()
        {
            leashPosition = base.transform.position;
        }

        public long GetTechTypeHash()
        {
            return techTypeHash;
        }

        public bool GetCanSeeObject(GameObject obj)
        {
            float distance = 0f;
            if (!hasEyes || !IsInFieldOfView(obj, out distance))
            {
                return false;
            }
            return true;
        }

        public bool IsInFieldOfView(GameObject go, out float distance)
        {
            ProfilingUtils.BeginSample("Creature.IsInFieldOfView");
            bool result = false;
            distance = 0f;
            if (go != null)
            {
                Vector3 vector = go.transform.position - base.transform.position;
                distance = vector.magnitude;
                Vector3 rhs = (eyesOnTop ? base.transform.up : base.transform.forward);
                Vector3 lhs = vector / distance;
                if ((Mathf.Approximately(eyeFOV, -1f) || Vector3.Dot(lhs, rhs) >= eyeFOV) && !Physics.Linecast(base.transform.position, go.transform.position, Voxeland.GetTerrainLayerMask()))
                {
                    result = true;
                }
            }
            ProfilingUtils.EndSample();
            return result;
        }

        public virtual void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public virtual void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            if (version < 3)
            {
                isInitialized = leashPosition != Vector3.zero && Vector3.Distance(leashPosition, base.transform.position) < 150f;
                version = 3;
            }
        }

        public virtual void OnEnable()
        {
            UpdateSchedulerUtils.Register(this);
        }

        public virtual void OnDisable()
        {
            UpdateSchedulerUtils.Deregister(this);
        }

        public virtual void OnDestroy()
        {
            UpdateSchedulerUtils.Deregister(this);
        }

        protected void AllowCreatureUpdates(bool allowed)
        {
            if (base.enabled)
            {
                if (allowed)
                {
                    UpdateSchedulerUtils.Register(this);
                }
                else
                {
                    UpdateSchedulerUtils.Deregister(this);
                }
            }
        }
    }
}
