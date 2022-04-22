using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class DealDamageOnImpact : MonoBehaviour
    {
        public bool damageTerrain = true;

        public float speedMinimumForSelfDamage = 1.6f;

        public float speedMinimumForDamage = 0.8f;

        public bool affectsEcosystem;

        public float minimumMassForDamage = 0.5f;

        public bool mirroredSelfDamage = true;

        public float mirroredSelfDamageFraction = 0.5f;

        public float capMirrorDamage = -1f;

        public float minDamageInterval;

        private float timeLastDamage;

        private float timeLastDamagedSelf;

        private Vector3 prevPosition;

        private Vector3 prevVelocity;

        private HashSet<GameObject> exceptions = new HashSet<GameObject>();

        public void AddException(GameObject target)
        {
            exceptions.Add(target);
        }

        public void RemoveException(GameObject target)
        {
            exceptions.Remove(target);
        }

        private void Start()
        {
            prevPosition = base.transform.position;
        }

        private void FixedUpdate()
        {
            prevVelocity = GetComponent<Rigidbody>().velocity;
        }

        private LiveMixin GetLiveMixin(GameObject go)
        {
            LiveMixin liveMixin = go.GetComponent<LiveMixin>();
            if (!liveMixin)
            {
                liveMixin = Utils.FindAncestorWithComponent<LiveMixin>(go);
            }
            return liveMixin;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!base.enabled || collision.contacts.Length == 0 || exceptions.Contains(collision.gameObject))
            {
                return;
            }
            float num = Mathf.Max(0f, Vector3.Dot(-collision.contacts[0].normal, prevVelocity));
            if (!(num > speedMinimumForDamage))
            {
                return;
            }
            LiveMixin liveMixin = GetLiveMixin(collision.gameObject);
            Vector3 position = ((collision.contacts.Length == 0) ? collision.transform.position : collision.contacts[0].point);
            float num2 = Mathf.Clamp(1f + GetComponent<Rigidbody>().mass * 0.001f, 0f, 10f) * 3f;
            float num3 = num * num2;
            if (liveMixin != null && Time.time > timeLastDamage + minDamageInterval)
            {
                liveMixin.TakeDamage(num3, position, DamageType.Collide, base.gameObject);
                timeLastDamage = Time.time;
            }
            Rigidbody rigidbody = Utils.FindAncestorWithComponent<Rigidbody>(collision.gameObject);
            if (!mirroredSelfDamage || !(num >= speedMinimumForSelfDamage))
            {
                return;
            }
            LiveMixin liveMixin2 = GetLiveMixin(base.gameObject);
            if ((bool)liveMixin2 && Time.time > timeLastDamagedSelf + 1f && (rigidbody == null || rigidbody.mass > minimumMassForDamage))
            {
                float num4 = num3 * mirroredSelfDamageFraction;
                if (capMirrorDamage != -1f)
                {
                    num4 = Mathf.Min(capMirrorDamage, num4);
                }
                liveMixin2.TakeDamage(num4, position, DamageType.Collide, base.gameObject);
                timeLastDamagedSelf = Time.time;
            }
        }
    }
}
