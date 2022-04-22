using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class FlyAwayWhenScared : CreatureAction
    {
        public float flyVelocity = 5f;

        public float flyInterval = 2f;

        public float flyUp = 0.3f;

        public float flyFromSource = 1.3f;

        [AssertNotNull]
        public LastScarePosition lastScarePosition;

        private float timeNextFly;

        public override float Evaluate(Creature creature)
        {
            float num = ((lastScarePosition.lastScarePosition.y > 0f) ? creature.Scared.Value : 0f);
            return GetEvaluatePriority() * num;
        }

        public override void StartPerform(Creature creature)
        {
            timeNextFly = Time.time;
            SafeAnimator.SetBool(creature.GetAnimator(), "flapping", value: true);
        }

        public override void StopPerform(Creature creature)
        {
            SafeAnimator.SetBool(creature.GetAnimator(), "flapping", value: false);
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (Time.time >= timeNextFly)
            {
                timeNextFly = Time.time + flyInterval;
                Vector3 vector = lastScarePosition.lastScarePosition;
                Vector3 normalized = (base.transform.position - vector).normalized;
                Vector3 insideUnitSphere = Random.insideUnitSphere;
                insideUnitSphere += normalized * flyFromSource;
                insideUnitSphere.y = flyUp;
                Vector3 targetPosition = base.transform.position + insideUnitSphere * 10f;
                base.swimBehaviour.SwimTo(targetPosition, flyVelocity);
            }
        }
    }
}
