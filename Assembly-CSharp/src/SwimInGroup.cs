using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class SwimInGroup : CreatureAction
    {
        public float swimVelocity = 4f;

        public float swimInterval = 2f;

        public float forgetDuration = 8f;

        public float positionScalar = 0.5f;

        private Vector3 swimToPosition;

        private float timeLastFishAverage = -1f;

        private TechType myTechType;

        private EcoTargetType targetType;

        private float timeNextSwim;

        private EcoRegion.TargetFilter isTargetValidFilter;

        private void Start()
        {
            swimToPosition = base.transform.position;
            myTechType = CraftData.GetTechType(base.gameObject);
            targetType = BehaviourData.GetEcoTargetType(myTechType);
            isTargetValidFilter = IsValidTarget;
            InvokeRepeating("Fish_AverageGroup", Random.value, 2f);
        }

        private bool IsValidTarget(IEcoTarget target)
        {
            GameObject gameObject = target.GetGameObject();
            if (gameObject == null || gameObject == base.gameObject)
            {
                return false;
            }
            return CraftData.GetTechType(gameObject) == myTechType;
        }

        private void Fish_AverageGroup()
        {
            ProfilingUtils.BeginSample("SwimInGroup::Fish_AverageGroup");
            IEcoTarget ecoTarget = EcoRegionManager.main.FindNearestTarget(targetType, base.transform.position, isTargetValidFilter);
            if (ecoTarget != null)
            {
                swimToPosition += (ecoTarget.GetPosition() - swimToPosition) * positionScalar;
                timeLastFishAverage = Time.time;
            }
            ProfilingUtils.EndSample();
        }

        public override float Evaluate(Creature creature)
        {
            if (timeLastFishAverage + 1f > Time.time)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            creature.Happy.Add(1f);
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            ProfilingUtils.BeginSample("SwimInGroup::Perform");
            _ = Time.time;
            if (Time.time > timeNextSwim && timeLastFishAverage > 0f && Time.time < timeLastFishAverage + forgetDuration)
            {
                timeNextSwim = Time.time + swimInterval;
                base.swimBehaviour.SwimTo(swimToPosition, swimVelocity);
            }
            ProfilingUtils.EndSample();
        }
    }
}
