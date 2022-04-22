using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXPushBodiesFromCenter : MonoBehaviour
    {
        public float force = 10f;

        private void Start()
        {
            Vector3 position = base.transform.position;
            Rigidbody[] componentsInChildren = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody obj in componentsInChildren)
            {
                obj.transform.parent = null;
                Vector3 vector = Vector3.Normalize(obj.transform.position - position);
                obj.AddForce(force * vector, ForceMode.Impulse);
            }
        }
    }
}
