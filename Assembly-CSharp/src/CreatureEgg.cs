using System;
using System.Collections.Generic;
using Gendarme;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class CreatureEgg : MonoBehaviour, IShouldSerialize
    {
        public TechType hatchingCreature;

        private TechType eggType;

        public TechType overrideEggType;

        public float daysBeforeHatching = 1f;

        public bool explodeOnHatch = true;

        [AssertNotNull]
        public Animator animator;

        private const float defaultProgress = 0f;

        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public float progress;

        private float timeStartHatching;

        private bool insideWaterPark;

        private bool subscribed;

        private void Awake()
        {
            eggType = CraftData.GetTechType(base.gameObject);
            if (!KnownTech.Contains(eggType))
            {
                GetComponent<Pickupable>().SetTechTypeOverride(overrideEggType);
                Subscribe(state: true);
            }
        }

        private void Start()
        {
            animator.enabled = insideWaterPark;
        }

        private void OnKnownTechChanged(HashSet<TechType> tech)
        {
            if (tech.Contains(eggType))
            {
                GetComponent<Pickupable>().ResetTechTypeOverride();
                Subscribe(state: false);
            }
        }

        private void Subscribe(bool state)
        {
            if (subscribed != state)
            {
                if (state)
                {
                    KnownTech.onChanged += OnKnownTechChanged;
                }
                else
                {
                    KnownTech.onChanged -= OnKnownTechChanged;
                }
                subscribed = state;
            }
        }

        private void OnAddToWaterPark()
        {
            insideWaterPark = true;
            base.transform.localScale = 0.6f * Vector3.one;
            animator.enabled = true;
            UpdateHatchingTime();
            InvokeRepeating("UpdateProgress", 0f, 1f);
        }

        private void OnDisable()
        {
            insideWaterPark = false;
            CancelInvoke();
            animator.enabled = false;
            base.transform.localScale = Vector3.one;
        }

        private float GetHatchDuration()
        {
            float num = (NoCostConsoleCommand.main.fastHatchCheat ? 0.01f : 1f);
            return 1200f * daysBeforeHatching * num;
        }

        private void UpdateHatchingTime()
        {
            timeStartHatching = DayNightCycle.main.timePassedAsFloat - GetHatchDuration() * progress;
        }

        private void UpdateProgress()
        {
            float timePassedAsFloat = DayNightCycle.main.timePassedAsFloat;
            progress = Mathf.InverseLerp(timeStartHatching, timeStartHatching + GetHatchDuration(), timePassedAsFloat);
            SafeAnimator.SetFloat(animator, "progress", progress);
            if (progress >= 1f)
            {
                Hatch();
            }
        }

        private void Hatch()
        {
            CancelInvoke();
            WaterParkItem component = GetComponent<WaterParkItem>();
            if (component != null)
            {
                WaterPark waterPark = component.GetWaterPark();
                component.SetWaterPark(null);
                if (!KnownTech.Contains(eggType))
                {
                    KnownTech.Add(eggType);
                    ErrorMessage.AddMessage(Language.main.GetFormat("EggDiscovered", Language.main.Get(eggType.AsString())));
                }
                WaterParkCreature.Born(hatchingCreature, waterPark, base.transform.position);
            }
            if (explodeOnHatch)
            {
                ExploderObject.ExplodeGameObject(base.gameObject);
            }
            else
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        public int GetCreatureSize()
        {
            return WaterParkCreature.GetCreatureSize(hatchingCreature);
        }

        private void OnDestroy()
        {
            Subscribe(state: false);
        }

        [SuppressMessage("Gendarme.Rules.Correctness", "AvoidFloatingPointEqualityRule")]
        public bool ShouldSerialize()
        {
            if (version == 1)
            {
                return progress != 0f;
            }
            return true;
        }
    }
}
