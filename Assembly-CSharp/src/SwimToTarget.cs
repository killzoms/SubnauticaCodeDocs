using UnityEngine;

namespace AssemblyCSharp
{
    public class SwimToTarget : CreatureAction
    {
        public float swimVelocity = 4f;

        public float swimInterval = 1f;

        private float timeNextSwim;

        private float targetTime = -1f;

        public Transform target { get; private set; }

        public void SetTarget(Transform swimTarget, float swimTime = -1f)
        {
            target = swimTarget;
            targetTime = ((swimTime < 0f) ? (-1f) : (Time.time + swimTime));
        }

        public override float Evaluate(Creature creature)
        {
            if (!target)
            {
                return 0f;
            }
            return GetEvaluatePriority();
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if ((bool)target && Time.time > timeNextSwim)
            {
                float velocity = swimVelocity;
                if (targetTime > 0f)
                {
                    float num = targetTime - Time.time;
                    float num2 = Vector3.Distance(base.transform.position, target.position);
                    float num3 = 2f * swimVelocity;
                    velocity = ((num > 0f) ? Mathf.Clamp(num2 / num, 1f, num3) : num3);
                }
                timeNextSwim = Time.time + swimInterval;
                base.swimBehaviour.SwimTo(target.transform.position, velocity);
            }
        }
    }
}
