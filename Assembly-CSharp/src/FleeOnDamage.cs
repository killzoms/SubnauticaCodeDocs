using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class FleeOnDamage : CreatureAction, IOnTakeDamage
    {
        private Vector3 moveTo;

        private float timeToFlee;

        private float accumulatedDamage;

        public float damageThreshold = 10f;

        public float fleeDuration = 2f;

        public float minFleeDistance = 5f;

        public bool breakLeash = true;

        public float swimVelocity = 10f;

        public float swimInterval = 1f;

        private float timeNextSwim;

        public override float Evaluate(Creature creature)
        {
            if (Time.time < timeToFlee)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            creature.Scared.Add(1f);
            creature.Tired.Add(-1f);
            creature.Happy.Add(-1f);
        }

        public override void StopPerform(Creature creature)
        {
            accumulatedDamage = 0f;
            if (breakLeash)
            {
                creature.leashPosition = base.transform.position;
            }
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            ProfilingUtils.BeginSample("CreatureAction::Perform (FleeOnDamage)");
            if (Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                base.swimBehaviour.SwimTo(moveTo, swimVelocity);
            }
            ProfilingUtils.EndSample();
        }

        public void OnTakeDamage(DamageInfo damageInfo)
        {
            float num = damageInfo.damage;
            if (damageInfo.type == DamageType.Electrical)
            {
                num *= 35f;
            }
            accumulatedDamage += num;
            if (accumulatedDamage > damageThreshold)
            {
                Vector3 vector = Vector3.Normalize(base.transform.position - damageInfo.position) * (minFleeDistance + damageInfo.damage / 30f);
                moveTo = new Vector3(vector.x, Mathf.Min(vector.y, Ocean.main.GetOceanLevel()), vector.z);
                timeToFlee = Time.time + fleeDuration;
            }
        }
    }
}
