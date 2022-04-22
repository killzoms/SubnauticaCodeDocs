using UnityEngine;

namespace AssemblyCSharp
{
    public class WarpOut : CreatureAction, IOnTakeDamage
    {
        [AssertNotNull]
        public Warper warper;

        [AssertNotNull]
        public LastTarget lastTarget;

        public float damageThreshold = 10f;

        public float maxSwimTime = 30f;

        public float maxSearchTime = 10f;

        private float accumulatedDamage;

        private float spawnedTime;

        private void Start()
        {
            spawnedTime = Time.time;
        }

        public override float Evaluate(Creature creature)
        {
            if (accumulatedDamage >= damageThreshold || Time.time > spawnedTime + maxSwimTime || Time.time > Mathf.Max(spawnedTime, lastTarget.targetTime) + maxSearchTime)
            {
                return evaluatePriority;
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            warper.WarpOut();
        }

        public void OnTakeDamage(DamageInfo damageInfo)
        {
            float num = damageInfo.damage;
            if (damageInfo.type == DamageType.Electrical)
            {
                num *= 35f;
            }
            accumulatedDamage += num;
        }
    }
}
