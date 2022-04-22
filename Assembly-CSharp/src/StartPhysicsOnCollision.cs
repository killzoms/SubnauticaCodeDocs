using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Rigidbody))]
    public class StartPhysicsOnCollision : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Rigidbody>().isKinematic = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            GetComponent<Rigidbody>().isKinematic = false;
        }
    }
}
