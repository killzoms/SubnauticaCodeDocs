using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Collider))]
    public class AggressiveOnDamage : MonoBehaviour, IOnTakeDamage
    {
        private Creature creature;

        public float minDamageThreshold = 10f;

        public float aggressionDamageScalar = 0.02f;

        public float friendlinessDecrement = 1f;

        public float tirednessDecrement = 1f;

        public float happinessDecrement = 1f;

        private void Awake()
        {
            creature = base.gameObject.GetComponent<Creature>();
        }

        public void OnTakeDamage(DamageInfo damageInfo)
        {
            if (damageInfo.damage >= minDamageThreshold)
            {
                creature.Aggression.Add(damageInfo.damage * aggressionDamageScalar);
                creature.Friendliness.Add(0f - friendlinessDecrement);
                creature.Tired.Add(0f - tirednessDecrement);
                creature.Happy.Add(0f - happinessDecrement);
            }
        }
    }
}
