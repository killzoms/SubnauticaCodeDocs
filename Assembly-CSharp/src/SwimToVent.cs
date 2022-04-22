using UnityEngine;

namespace AssemblyCSharp
{
    public class SwimToVent : CreatureAction
    {
        private PrecursorVentEntryTrigger target;

        [AssertNotNull]
        public Peeper peeper;

        public float swimVelocity = 4f;

        public float swimInterval = 1f;

        public float searchRange = 100f;

        public float searchInterval = 10f;

        private float timeNextSwim;

        private float timeNextSearch;

        private bool active;

        public override float Evaluate(Creature creature)
        {
            if (Time.time > timeNextSearch)
            {
                timeNextSearch = Time.time + searchInterval;
                UpdateTarget();
            }
            if (!target)
            {
                return 0f;
            }
            return GetEvaluatePriority();
        }

        private bool IsTargetValid(PrecursorVentEntryTrigger candidate)
        {
            if (Physics.Linecast(base.transform.position, candidate.transform.position, Voxeland.GetTerrainLayerMask(), QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            return true;
        }

        private void UpdateTarget()
        {
            if (active && (bool)target)
            {
                target.ReleaseExclusiveAccess(peeper);
            }
            PrecursorVentEntryTrigger nearestVentEntry = PrecursorVentEntryTrigger.GetNearestVentEntry(searchRange, peeper);
            target = (((bool)nearestVentEntry && IsTargetValid(nearestVentEntry)) ? nearestVentEntry : null);
            if (active && (bool)target)
            {
                target.AcquireExclusiveAccess(peeper);
            }
        }

        public override void StartPerform(Creature creature)
        {
            active = true;
            if ((bool)target)
            {
                target.AcquireExclusiveAccess(peeper);
            }
        }

        public override void StopPerform(Creature creature)
        {
            active = false;
            if ((bool)target)
            {
                target.ReleaseExclusiveAccess(peeper);
            }
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if ((bool)target && Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                base.swimBehaviour.SwimTo(target.transform.position, swimVelocity);
            }
        }

        private void OnDisable()
        {
            if (active && (bool)target)
            {
                target.ReleaseExclusiveAccess(peeper);
            }
        }

        public void OnReachBlockedVentEntry(PrecursorVentEntryTrigger entry)
        {
            if (active && entry == target)
            {
                target = null;
                Vector3 targetPosition = entry.transform.position + entry.transform.up * 15f;
                base.swimBehaviour.SwimTo(targetPosition, swimVelocity);
            }
        }
    }
}
