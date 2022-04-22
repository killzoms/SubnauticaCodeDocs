using UnityEngine;

namespace AssemblyCSharp
{
    public class AlignWithVelocity : MonoBehaviour
    {
        public float rotationSpeed = 4f;

        public bool rotateXZOnly;

        public void Start()
        {
        }

        public void Update()
        {
            Vector3 zero = Vector3.zero;
            zero = GetComponent<Rigidbody>().velocity;
            if (zero.sqrMagnitude > 0.04f)
            {
                if (rotateXZOnly)
                {
                    Vector3 forward = new Vector3(zero.x, 0f, zero.z);
                    base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * rotationSpeed);
                }
                else
                {
                    base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(zero), Time.deltaTime * rotationSpeed);
                }
            }
        }
    }
}
