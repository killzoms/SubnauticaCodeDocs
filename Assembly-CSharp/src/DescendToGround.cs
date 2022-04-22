using UnityEngine;

namespace AssemblyCSharp
{
    public class DescendToGround : CreatureAction
    {
        private float lastExitTime;

        private float startTime;

        [AssertNotNull]
        public OnGroundTracker onGroundTracker;

        [AssertNotNull]
        public ConstantForce descendForce;

        public float forceValue = 10f;

        public float actionInterval = 5f;

        public float maxDuration = 5f;

        public override float Evaluate(Creature creature)
        {
            if (Time.time > lastExitTime + actionInterval && (startTime == 0f || startTime + maxDuration > Time.time))
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            startTime = Time.time;
            descendForce.enabled = true;
            descendForce.force = new Vector3(0f, 0f - forceValue, 0f);
        }

        public override void StopPerform(Creature creature)
        {
            descendForce.enabled = false;
            lastExitTime = Time.time;
            startTime = 0f;
        }
    }
}
