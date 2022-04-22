using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class WaterParkCreature : WaterParkItem, IProtoEventListener
    {
        private WaterParkCreatureParameters parameters;

        private bool isMature;

        private double matureTime;

        private float scaleInside = 0.6f;

        private SwimBehaviour swimBehaviour;

        private float swimMinVelocity = 0.5f;

        private float swimMaxVelocity = 1f;

        private Vector3 swimTarget;

        private float swimInterval = 2f;

        private float timeNextSwim;

        private double breedInterval;

        private bool _canBreed = true;

        private bool locomotionParametersOverrode;

        private float locomotionDriftFactor;

        private float locomotionForwardRotationSpeed;

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public float age = -1f;

        [NonSerialized]
        [ProtoMember(3)]
        public float timeNextBreed = -1f;

        private static readonly Type[] behavioursToDisableInside = new Type[5]
        {
            typeof(FMOD_CustomLoopingEmitter),
            typeof(MeleeAttack),
            typeof(WorldForces),
            typeof(HeroPeeperHealingTrigger),
            typeof(ItemPrefabData)
        };

        private List<Behaviour> disabledBehaviours;

        private float outsideMoveMaxSpeed;

        private bool isInside;

        public static readonly Dictionary<TechType, WaterParkCreatureParameters> waterParkCreatureParameters = new Dictionary<TechType, WaterParkCreatureParameters>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.Spadefish,
                new WaterParkCreatureParameters(0.1f, 0.5f, 0.8f, 1f)
            },
            {
                TechType.Jumper,
                new WaterParkCreatureParameters(0.1f, 0.4f, 0.8f, 1f, isPickupableOutside: false)
            },
            {
                TechType.RabbitRay,
                new WaterParkCreatureParameters(0.1f, 0.4f, 0.8f, 1f, isPickupableOutside: false)
            },
            {
                TechType.Mesmer,
                new WaterParkCreatureParameters(0.1f, 0.4f, 0.8f, 1f, isPickupableOutside: false)
            },
            {
                TechType.Crash,
                new WaterParkCreatureParameters(0.1f, 0.4f, 0.8f, 1f, isPickupableOutside: false)
            },
            {
                TechType.Jellyray,
                new WaterParkCreatureParameters(0.08f, 0.2f, 0.5f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.Stalker,
                new WaterParkCreatureParameters(0.08f, 0.2f, 0.5f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.LavaLizard,
                new WaterParkCreatureParameters(0.08f, 0.2f, 0.5f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.BoneShark,
                new WaterParkCreatureParameters(0.03f, 0.12f, 0.4f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.Crabsnake,
                new WaterParkCreatureParameters(0.03f, 0.1f, 0.4f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.Gasopod,
                new WaterParkCreatureParameters(0.08f, 0.2f, 0.5f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.CrabSquid,
                new WaterParkCreatureParameters(0.02f, 0.1f, 0.4f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.Shocker,
                new WaterParkCreatureParameters(0.03f, 0.12f, 0.4f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.Sandshark,
                new WaterParkCreatureParameters(0.03f, 0.12f, 0.4f, 1.5f, isPickupableOutside: false)
            },
            {
                TechType.Reefback,
                new WaterParkCreatureParameters(0.01f, 0.03f, 0.1f, 2f, isPickupableOutside: false)
            },
            {
                TechType.Cutefish,
                new WaterParkCreatureParameters(0.4f, 0.7f, 1f, 1f, isPickupableOutside: false)
            }
        };

        public static readonly Dictionary<TechType, TechType> creatureEggs = new Dictionary<TechType, TechType>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.BoneShark,
                TechType.BonesharkEgg
            },
            {
                TechType.CrabSquid,
                TechType.CrabsquidEgg
            },
            {
                TechType.Crabsnake,
                TechType.CrabsnakeEgg
            },
            {
                TechType.Crash,
                TechType.CrashEgg
            },
            {
                TechType.Gasopod,
                TechType.GasopodEgg
            },
            {
                TechType.Jellyray,
                TechType.JellyrayEgg
            },
            {
                TechType.Jumper,
                TechType.JumperEgg
            },
            {
                TechType.LavaLizard,
                TechType.LavaLizardEgg
            },
            {
                TechType.Mesmer,
                TechType.MesmerEgg
            },
            {
                TechType.RabbitRay,
                TechType.RabbitrayEgg
            },
            {
                TechType.Reefback,
                TechType.ReefbackEgg
            },
            {
                TechType.Sandshark,
                TechType.SandsharkEgg
            },
            {
                TechType.Shocker,
                TechType.ShockerEgg
            },
            {
                TechType.Spadefish,
                TechType.SpadefishEgg
            },
            {
                TechType.Stalker,
                TechType.StalkerEgg
            }
        };

        public bool canBreed
        {
            get
            {
                return _canBreed;
            }
            set
            {
                if (value && !_canBreed && isMature)
                {
                    ResetBreedTime();
                }
                _canBreed = value;
            }
        }

        public static WaterParkCreatureParameters GetParameters(TechType creature)
        {
            return waterParkCreatureParameters.GetOrDefault(creature, WaterParkCreatureParameters.GetDefaultValue());
        }

        public static int GetCreatureSize(TechType creature)
        {
            return CraftData.GetItemSize(creature).x;
        }

        public static GameObject Born(TechType techType, WaterPark waterPark, Vector3 position)
        {
            GameObject gameObject = CraftData.InstantiateFromPrefab(techType);
            gameObject.SetActive(value: false);
            gameObject.transform.position = position;
            bool num = gameObject.GetComponent<Creature>() != null;
            WaterParkCreatureParameters waterParkCreatureParameters = null;
            if (num)
            {
                waterParkCreatureParameters = GetParameters(techType);
                gameObject.transform.localScale = waterParkCreatureParameters.initialSize * Vector3.one;
                WaterParkCreature waterParkCreature = gameObject.AddComponent<WaterParkCreature>();
                waterParkCreature.age = 0f;
                waterParkCreature.parameters = waterParkCreatureParameters;
            }
            Pickupable pickupable = ((!num || waterParkCreatureParameters.isPickupableOutside) ? gameObject.GetComponent<Pickupable>() : gameObject.EnsureComponent<Pickupable>());
            gameObject.SetActive(value: true);
            waterPark.AddItem(pickupable);
            return gameObject;
        }

        private void SetMatureTime()
        {
            isMature = false;
            matureTime = DayNightCycle.main.timePassed + (double)(parameters.growingPeriod * (1f - age));
        }

        private void InitializeParameters()
        {
            if (parameters == null)
            {
                parameters = GetParameters(CraftData.GetTechType(base.gameObject));
            }
        }

        public void ResetBreedTime()
        {
            timeNextBreed = (float)(DayNightCycle.main.timePassed + breedInterval);
        }

        public bool GetCanBreed()
        {
            if (canBreed)
            {
                return isMature;
            }
            return false;
        }

        private void Awake()
        {
            Creature component = base.gameObject.GetComponent<Creature>();
            if (component != null)
            {
                component.Friendliness.Add(1f);
                component.friend = Player.main.gameObject;
            }
            CreatureDeath component2 = base.gameObject.GetComponent<CreatureDeath>();
            if (component2 != null)
            {
                component2.respawn = false;
            }
        }

        private void Start()
        {
            swimBehaviour = base.gameObject.GetComponent<SwimBehaviour>();
            DevConsole.RegisterConsoleCommand(this, "setwpcage");
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            InitializeParameters();
        }

        private void Update()
        {
            if (!(currentWaterPark != null))
            {
                return;
            }
            if (Time.time > timeNextSwim)
            {
                swimTarget = currentWaterPark.GetRandomPointInside();
                swimBehaviour.SwimTo(swimTarget, Mathf.Lerp(swimMinVelocity, swimMaxVelocity, age));
                timeNextSwim = Time.time + swimInterval * global::UnityEngine.Random.Range(1f, 2f);
            }
            double timePassed = DayNightCycle.main.timePassed;
            if (!isMature)
            {
                float a = (float)(matureTime - (double)parameters.growingPeriod);
                age = Mathf.InverseLerp(a, (float)matureTime, (float)timePassed);
                base.transform.localScale = Mathf.Lerp(parameters.initialSize, parameters.maxSize, age) * Vector3.one;
                if (age == 1f)
                {
                    isMature = true;
                    if (canBreed)
                    {
                        breedInterval = parameters.growingPeriod * 0.5f;
                        if (timeNextBreed < 0f)
                        {
                            ResetBreedTime();
                        }
                    }
                }
            }
            if (GetCanBreed() && timePassed > (double)timeNextBreed)
            {
                ResetBreedTime();
                currentWaterPark.TryBreed(this);
            }
        }

        protected override void OnAddToWP()
        {
            InitializeParameters();
            base.gameObject.GetComponent<Creature>().enabled = false;
            SetInsideState();
            if (age < 0f)
            {
                float value = base.transform.localScale.x * scaleInside;
                age = Mathf.InverseLerp(parameters.initialSize, parameters.maxSize, value);
            }
            SetMatureTime();
            InvokeRepeating("ValidatePosition", global::UnityEngine.Random.value * 10f, 10f);
            base.OnAddToWP();
        }

        private void OnDrop()
        {
            InitializeParameters();
            if (currentWaterPark == null)
            {
                SetOutsideState();
                if (!parameters.isPickupableOutside)
                {
                    global::UnityEngine.Object.Destroy(pickupable);
                }
                base.enabled = false;
            }
        }

        protected override void OnRemoveFromWP()
        {
            Creature component = base.gameObject.GetComponent<Creature>();
            LiveMixin component2 = base.gameObject.GetComponent<LiveMixin>();
            if (component2 != null && component2.IsAlive())
            {
                component.enabled = true;
            }
            component.SetScale(parameters.outsideSize);
            timeNextBreed = -1f;
            CancelInvoke();
        }

        private void SetInsideState()
        {
            if (isInside)
            {
                return;
            }
            isInside = true;
            if (!base.gameObject.activeSelf)
            {
                base.gameObject.SetActive(value: true);
            }
            Animator animator = base.gameObject.GetComponent<Creature>().GetAnimator();
            if (animator != null)
            {
                AnimateByVelocity component = animator.GetComponent<AnimateByVelocity>();
                if (component != null)
                {
                    outsideMoveMaxSpeed = component.animationMoveMaxSpeed;
                    component.animationMoveMaxSpeed = swimMaxVelocity;
                }
            }
            Locomotion component2 = base.gameObject.GetComponent<Locomotion>();
            component2.canMoveAboveWater = true;
            locomotionDriftFactor = component2.driftFactor;
            locomotionForwardRotationSpeed = component2.forwardRotationSpeed;
            component2.driftFactor = 0.1f;
            component2.forwardRotationSpeed = 0.6f;
            locomotionParametersOverrode = true;
            disabledBehaviours = new List<Behaviour>();
            Behaviour[] componentsInChildren = GetComponentsInChildren<Behaviour>();
            foreach (Behaviour behaviour in componentsInChildren)
            {
                if (behaviour == null)
                {
                    Debug.LogWarning("Discarded missing behaviour on a WaterParkCreature gameObject", this);
                }
                else
                {
                    if (!behaviour.enabled)
                    {
                        continue;
                    }
                    Type type = behaviour.GetType();
                    for (int j = 0; j < behavioursToDisableInside.Length; j++)
                    {
                        if (type.Equals(behavioursToDisableInside[j]) || type.IsSubclassOf(behavioursToDisableInside[j]))
                        {
                            behaviour.enabled = false;
                            disabledBehaviours.Add(behaviour);
                            break;
                        }
                    }
                }
            }
        }

        private void SetOutsideState()
        {
            if (!isInside)
            {
                return;
            }
            isInside = false;
            Animator animator = base.gameObject.GetComponent<Creature>().GetAnimator();
            if (animator != null)
            {
                AnimateByVelocity component = animator.GetComponent<AnimateByVelocity>();
                if (component != null)
                {
                    component.animationMoveMaxSpeed = outsideMoveMaxSpeed;
                }
            }
            Locomotion component2 = base.gameObject.GetComponent<Locomotion>();
            component2.canMoveAboveWater = false;
            if (locomotionParametersOverrode)
            {
                component2.driftFactor = locomotionDriftFactor;
                component2.forwardRotationSpeed = locomotionForwardRotationSpeed;
                locomotionParametersOverrode = false;
            }
            if (disabledBehaviours == null)
            {
                return;
            }
            for (int i = 0; i < disabledBehaviours.Count; i++)
            {
                if (disabledBehaviours[i] != null)
                {
                    disabledBehaviours[i].enabled = true;
                }
            }
        }

        public override void ValidatePosition()
        {
            base.ValidatePosition();
            if (currentWaterPark != null)
            {
                currentWaterPark.EnsurePointIsInside(ref swimTarget);
            }
        }

        public override int GetSize()
        {
            return CraftData.GetItemSize(CraftData.GetTechType(base.gameObject)).x;
        }

        private void OnConsoleCommand_setwpcage(NotificationCenter.Notification n)
        {
            string text = (string)n.data[0];
            if (text != null && text != "")
            {
                float value = float.Parse(text);
                value = Mathf.Clamp01(value);
                ErrorMessage.AddDebug("Setting creature age to " + value + ".");
                age = value;
                if (age < 0f)
                {
                    timeNextBreed = -1f;
                }
                SetMatureTime();
            }
        }
    }
}
