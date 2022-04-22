using UnityEngine;

namespace AssemblyCSharp
{
    public class AvoidPosition : CreatureAction
    {
        [SerializeField]
        private Vector3 position;

        [SerializeField]
        private float radius = 50f;

        [SerializeField]
        private float swimVelocity = 3f;

        [SerializeField]
        private float swimInterval = 1f;

        private float timeNextSwim;

        public override float Evaluate(Creature creature)
        {
            float sqrMagnitude = (position - base.transform.position).sqrMagnitude;
            if (creature != null && sqrMagnitude < radius * radius)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                Vector3 targetPosition = base.transform.position + (base.transform.position - position);
                base.swimBehaviour.SwimTo(targetPosition, swimVelocity);
            }
        }
    }
}
