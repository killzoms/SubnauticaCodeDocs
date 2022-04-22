using UnityEngine;

namespace AssemblyCSharp
{
    public class GrapplingHook : MonoBehaviour
    {
        public Rigidbody rb;

        public Collider collision;

        public FMODAsset hitSound;

        public VFXController fxControl;

        private FixedJoint fixedJoint;

        private bool staticAttached;

        private const float grappleRetrieveSpeed = 10f;

        public bool attached
        {
            get
            {
                if (!(fixedJoint != null))
                {
                    return staticAttached;
                }
                return true;
            }
        }

        public bool flying
        {
            get
            {
                if (base.transform.parent == null)
                {
                    return !rb.isKinematic;
                }
                return false;
            }
        }

        public bool resting => base.transform.localPosition == Vector3.zero;

        public Rigidbody GetTargetRigidbody(GameObject go)
        {
            GameObject gameObject = global::UWE.Utils.GetEntityRoot(go);
            if (gameObject == null)
            {
                gameObject = go;
            }
            return gameObject.GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collisionInfo)
        {
            Exosuit componentInParent = collisionInfo.gameObject.GetComponentInParent<Exosuit>();
            GrapplingHook component = collisionInfo.gameObject.GetComponent<GrapplingHook>();
            if (staticAttached || ((bool)fixedJoint && (bool)fixedJoint.connectedBody) || componentInParent != null || component != null)
            {
                return;
            }
            Rigidbody targetRigidbody = GetTargetRigidbody(collisionInfo.gameObject);
            rb.velocity = Vector3.zero;
            if (targetRigidbody != null && JointHelper.ConnectFixed(base.gameObject, targetRigidbody))
            {
                staticAttached = false;
            }
            else
            {
                staticAttached = true;
                rb.isKinematic = true;
            }
            Utils.PlayFMODAsset(hitSound, base.transform, 5f);
            Vector3 upwards = default(Vector3);
            int num = 0;
            for (int i = 0; i < collisionInfo.contacts.Length; i++)
            {
                ContactPoint contactPoint = collisionInfo.contacts[i];
                if (num == 0)
                {
                    upwards = contactPoint.normal;
                }
                else
                {
                    upwards += contactPoint.normal;
                }
                num++;
            }
            if (num > 0)
            {
                upwards /= (float)num;
                Vector3 eulerAngles = Quaternion.LookRotation(base.transform.forward, upwards).eulerAngles;
                eulerAngles.z -= 90f;
                base.transform.eulerAngles = eulerAngles;
            }
            VFXSurface component2 = collisionInfo.gameObject.GetComponent<VFXSurface>();
            VFXSurfaceTypeManager.main.Play(component2, VFXEventTypes.impact, base.transform.position, base.transform.rotation, null);
        }

        private void OnJointConnected(FixedJoint joint)
        {
            fixedJoint = joint;
        }

        private void FixedUpdate()
        {
            if ((bool)fixedJoint && !fixedJoint.connectedBody)
            {
                JointHelper.Disconnect(fixedJoint, destroyHelper: true);
                fixedJoint = null;
                Debug.Log("disconnect, connected body lost");
            }
        }

        private void Update()
        {
            if (base.transform.parent != null && !resting)
            {
                base.transform.localPosition = global::UWE.Utils.LerpVector(base.transform.localPosition, Vector3.zero, Time.deltaTime * 10f);
                base.transform.localRotation = Quaternion.Slerp(base.transform.localRotation, Quaternion.identity, Time.deltaTime * 20f);
            }
        }

        public void Release()
        {
            if ((bool)fixedJoint)
            {
                JointHelper.Disconnect(fixedJoint, destroyHelper: true);
                fixedJoint = null;
            }
            fxControl.StopAndDestroy(0, 1.5f);
            staticAttached = false;
        }

        public void SetFlying(bool isFlying)
        {
            rb.isKinematic = !isFlying;
            collision.enabled = isFlying;
            fxControl.Play(0);
        }
    }
}
