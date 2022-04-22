using UnityEngine;

namespace AssemblyCSharp
{
    public class SwimToHeroPeeper : CreatureAction
    {
        [AssertNotNull]
        public InfectedMixin infectedMixin;

        public float swimVelocity = 4f;

        public float swimInterval = 1f;

        public float searchRange = 100f;

        public float searchInterval = 10f;

        public bool isPredator;

        private HeroPeeperHealingTrigger target;

        private float timeNextSwim;

        private float timeNextSearch;

        public override float Evaluate(Creature creature)
        {
            if (infectedMixin.GetInfectedAmount() < 0.15f)
            {
                return 0f;
            }
            if (Time.time > timeNextSearch)
            {
                timeNextSearch = Time.time + searchInterval;
                UpdateTarget();
            }
            if (target == null)
            {
                return 0f;
            }
            return GetEvaluatePriority();
        }

        public override void StartPerform(Creature creature)
        {
            if (isPredator)
            {
                SafeAnimator.SetBool(creature.GetAnimator(), "attacking", value: true);
            }
        }

        public override void StopPerform(Creature creature)
        {
            if (isPredator)
            {
                SafeAnimator.SetBool(creature.GetAnimator(), "attacking", value: false);
            }
        }

        private bool IsTargetValid(HeroPeeperHealingTrigger candidate)
        {
            if (Vector3.Distance(base.transform.position, candidate.transform.position) > searchRange)
            {
                return false;
            }
            if (Physics.Linecast(base.transform.position, candidate.transform.position, Voxeland.GetTerrainLayerMask(), QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            return true;
        }

        private void UpdateTarget()
        {
            HeroPeeperHealingTrigger nearestHeroPeeper = HeroPeeperHealingTrigger.GetNearestHeroPeeper(base.transform.position);
            target = ((nearestHeroPeeper != null && IsTargetValid(nearestHeroPeeper)) ? nearestHeroPeeper : null);
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if ((bool)target && Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                if (isPredator)
                {
                    Vector3 position = target.root.transform.position;
                    Vector3 normalized = (position - base.transform.position).normalized;
                    base.swimBehaviour.Attack(position, normalized, swimVelocity);
                    creature.Aggression.Add(1f);
                }
                else
                {
                    base.swimBehaviour.SwimTo(target.transform.position, swimVelocity);
                }
            }
        }
    }
}
