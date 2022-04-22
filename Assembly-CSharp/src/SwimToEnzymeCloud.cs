using UnityEngine;

namespace AssemblyCSharp
{
    public class SwimToEnzymeCloud : CreatureAction
    {
        [AssertNotNull]
        public Peeper peeper;

        public float swimVelocity = 4f;

        public float swimInterval = 1f;

        public float searchRange = 100f;

        public float searchInterval = 10f;

        private EnzymeCloud target;

        private float timeNextSwim;

        private float timeNextSearch;

        public override float Evaluate(Creature creature)
        {
            if (peeper.isHero)
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

        private bool IsTargetValid(EnzymeCloud candidate)
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
            EnzymeCloud nearestEnzymeCloud = EnzymeCloud.GetNearestEnzymeCloud(base.transform.position);
            target = ((nearestEnzymeCloud != null && IsTargetValid(nearestEnzymeCloud)) ? nearestEnzymeCloud : null);
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if ((bool)target && Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                base.swimBehaviour.SwimTo(target.transform.position, swimVelocity);
            }
        }
    }
}
