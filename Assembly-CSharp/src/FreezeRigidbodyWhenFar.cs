using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Rigidbody))]
    public class FreezeRigidbodyWhenFar : MonoBehaviour
    {
        public float freezeDist = 48f;

        private void Awake()
        {
            if (GetComponent<Rigidbody>().isKinematic)
            {
                Debug.LogError("Are you sure you're not putting this on an RB that's meant to ALWAYS be kinematic?");
            }
        }

        private void FixedUpdate()
        {
            if (!(base.transform.position.y > freezeDist / 2f))
            {
                if ((MainCamera.camera.transform.position - base.transform.position).sqrMagnitude > freezeDist * freezeDist)
                {
                    GetComponent<Rigidbody>().isKinematic = true;
                }
                else
                {
                    GetComponent<Rigidbody>().isKinematic = false;
                }
            }
        }
    }
}
