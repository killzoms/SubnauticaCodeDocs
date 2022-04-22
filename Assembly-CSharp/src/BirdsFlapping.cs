using UnityEngine;

namespace AssemblyCSharp
{
    public class BirdsFlapping : CreatureAction
    {
        public float flyVelocity = 3f;

        public float flyInterval = 1f;

        public float flyUp = 0.3f;

        public float flappingDuration = 3f;

        public float flappingInterval = 5f;

        public Animator animator;

        private float timeNextFly;

        private bool flapping;

        private float timeFlappingStart;

        private float timeLastFlapping;

        public override float Evaluate(Creature creature)
        {
            if (Time.time > timeLastFlapping + flappingInterval)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            timeFlappingStart = Time.time;
            SafeAnimator.SetBool(animator, "flapping", value: true);
        }

        public override void StopPerform(Creature creature)
        {
            SafeAnimator.SetBool(animator, "flapping", value: false);
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
                if (Time.time > timeFlappingStart + flappingDuration)
                {
                    timeLastFlapping = Time.time;
                }
            }
        }
    }
}
