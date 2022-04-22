using UnityEngine;

namespace AssemblyCSharp
{
    public class BlockSeamoth : MonoBehaviour
    {
        public float pushVelocity = 2f;

        private Rigidbody seamothRigidbody;

        private void FixedUpdate()
        {
            if ((bool)seamothRigidbody)
            {
                Rigidbody rigidbody = seamothRigidbody;
                Vector3 velocity = rigidbody.velocity;
                Vector3 forward = base.transform.forward;
                float num = Vector3.Dot(velocity, -forward);
                if (!(num <= 0f))
                {
                    rigidbody.AddForce(forward * num, ForceMode.VelocityChange);
                    rigidbody.AddForce(forward * pushVelocity, ForceMode.VelocityChange);
                    seamothRigidbody = null;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.isTrigger)
            {
                SeaMoth componentInParent = other.GetComponentInParent<SeaMoth>();
                if ((bool)componentInParent)
                {
                    seamothRigidbody = componentInParent.GetComponent<Rigidbody>();
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(base.transform.position, base.transform.forward * 10f);
        }
    }
}
