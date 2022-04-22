using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class MoveTowardsTarget : CreatureAction
    {
        public EcoTargetType targetType = EcoTargetType.Coral;

        public float scanInterval = 5f;

        public float moveSpeed = 5f;

        public float swimInterval = 1f;

        public bool fleeInstead;

        public float requiredAggression;

        public float minDistanceToTarget;

        public float chanceToLoseTarget = 0.1f;

        private float timeNextScan;

        private float timeNextSwim;

        private IEcoTarget currentTarget;

        private EcoRegion.TargetFilter isTargetValidFilter;

        private void Start()
        {
            InvokeRepeating("LoseTarget", Random.value, 1f);
            if (isTargetValidFilter == null)
            {
                isTargetValidFilter = IsValidTarget;
            }
        }

        private bool IsValidTarget(IEcoTarget target)
        {
            Vector3 direction = target.GetPosition() - base.transform.position;
            float num = direction.magnitude - 0.5f;
            if (num > 0f && Physics.Raycast(base.transform.position, direction, num, Voxeland.GetTerrainLayerMask()))
            {
                return false;
            }
            return true;
        }

        private void UpdateCurrentTarget()
        {
            ProfilingUtils.BeginSample("UpdateCurrentTarget");
            if (EcoRegionManager.main != null && (Mathf.Approximately(requiredAggression, 0f) || creature.Aggression.Value >= requiredAggression))
            {
                IEcoTarget ecoTarget = EcoRegionManager.main.FindNearestTarget(targetType, base.transform.position, isTargetValidFilter);
                if (ecoTarget != null)
                {
                    currentTarget = ecoTarget;
                }
                else
                {
                    currentTarget = null;
                }
            }
            ProfilingUtils.EndSample();
        }

        public override float Evaluate(Creature creature)
        {
            if (timeNextScan < Time.time)
            {
                UpdateCurrentTarget();
                timeNextScan = Time.time + scanInterval;
            }
            if (currentTarget != null && !currentTarget.Equals(null) && (currentTarget.GetPosition() - base.transform.position).sqrMagnitude >= minDistanceToTarget * minDistanceToTarget)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            ProfilingUtils.BeginSample("CreatureAction::MoveTowardsTarget");
            if (currentTarget != null && !currentTarget.Equals(null) && Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                Vector3 targetPosition = currentTarget.GetPosition();
                if (fleeInstead)
                {
                    Vector3 vector = currentTarget.GetPosition() - base.transform.position;
                    targetPosition = base.transform.position - vector;
                }
                base.swimBehaviour.SwimTo(targetPosition, moveSpeed);
            }
            ProfilingUtils.EndSample();
        }

        private void LoseTarget()
        {
            if (base.gameObject.activeInHierarchy && currentTarget != null && Random.value < chanceToLoseTarget)
            {
                currentTarget = null;
            }
        }
    }
}
