using UnityEngine;

namespace AssemblyCSharp
{
    public class MoveOnSurface : CreatureAction
    {
        [AssertNotNull]
        public OnSurfaceTracker onSurfaceTracker;

        [AssertNotNull]
        public WalkBehaviour walkBehaviour;

        public float updateTargetInterval = 5f;

        public float moveVelocity = 13f;

        public float moveRadius = 7f;

        private float timeNextTarget;

        private Vector3 desiredPosition = Vector3.zero;

        private bool actionActive;

        private Vector3 FindRandomPosition()
        {
            return Random.onUnitSphere * moveRadius + base.transform.position;
        }

        public override float Evaluate(Creature creature)
        {
            if (onSurfaceTracker.onSurface)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (timeNextTarget <= Time.time)
            {
                desiredPosition = FindRandomPosition();
                timeNextTarget = Time.time + updateTargetInterval + 6f * Random.value;
                walkBehaviour.WalkTo(desiredPosition, moveVelocity);
            }
        }
    }
}
