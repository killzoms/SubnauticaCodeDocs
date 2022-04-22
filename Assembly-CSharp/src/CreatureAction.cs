using UnityEngine;

namespace AssemblyCSharp
{
    public abstract class CreatureAction : MonoBehaviour
    {
        protected Creature creature;

        public float evaluatePriority = 0.4f;

        public AnimationCurve priorityMultiplier;

        private bool initialized;

        protected SwimBehaviour swimBehaviour { get; private set; }

        public virtual void Awake()
        {
            swimBehaviour = base.gameObject.GetComponent<SwimBehaviour>();
        }

        public virtual void OnEnable()
        {
            if (initialized)
            {
                return;
            }
            if (creature == null)
            {
                creature = base.gameObject.GetComponent<Creature>();
                if (!creature)
                {
                    creature = base.gameObject.GetComponentInParent<Creature>();
                }
            }
            initialized = true;
        }

        public float GetEvaluatePriority()
        {
            return DayNightUtils.Evaluate(evaluatePriority, priorityMultiplier);
        }

        public virtual float Evaluate(Creature creature)
        {
            return GetEvaluatePriority();
        }

        public virtual void StartPerform(Creature creature)
        {
        }

        public virtual void StopPerform(Creature creature)
        {
        }

        public virtual void Perform(Creature creature, float deltaTime)
        {
        }
    }
}
