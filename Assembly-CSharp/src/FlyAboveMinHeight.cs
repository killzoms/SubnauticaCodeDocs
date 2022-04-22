using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class FlyAboveMinHeight : CreatureAction
    {
        public float minHeight = 2f;

        public float flyVelocity = 3f;

        public float flyInterval = 1f;

        public float flyUp = 0.3f;

        private float timeNextFly;

        public override float Evaluate(Creature creature)
        {
            if (base.transform.position.y < minHeight)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            SafeAnimator.SetBool(creature.GetAnimator(), "flapping", value: true);
        }

        public override void StopPerform(Creature creature)
        {
            SafeAnimator.SetBool(creature.GetAnimator(), "flapping", value: false);
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (Time.time > timeNextFly)
            {
                timeNextFly = Time.time + flyInterval;
                Vector3 vector = new Vector3(base.transform.forward.x, 0f, base.transform.forward.z);
                vector = vector.normalized;
                vector.y = flyUp;
                Vector3 targetPosition = base.transform.position + vector * 10f;
                base.swimBehaviour.SwimTo(targetPosition, flyVelocity);
            }
        }
    }
}
