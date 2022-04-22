using System;
using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Rigidbody))]
    public class Floater : PlayerTool, IPropulsionCannonAmmo
    {
        public bool debug;

        public float buoyantForce = 8f;

        public float buoyantForceDeviation = 1f;

        public float buoyantPeriod = 60f;

        public float buoyantPeriodDeviation = 10f;

        public float gravitateDuration = 10f;

        public float gravitateAcceleration = 10f;

        [AssertNotNull]
        public PlayerDistanceTracker playerDistanceTracker;

        [AssertNotNull]
        public Animator animator;

        [AssertNotNull]
        public Transform model;

        [AssertNotNull]
        public JointHelper jointHelper;

        private float frequency;

        private float phase;

        private bool onNearOrMediumCellLevel = true;

        private float modelRotationSetTime;

        private bool lerpModelRotation;

        private Quaternion attachRotationOffset = Quaternion.Euler(-90f, 0f, 0f);

        private FixedJoint _fixedJoint;

        private Rigidbody gravitateBody;

        private float gravitateEndTime;

        public FMOD_CustomLoopingEmitter loopingHeldSound;

        public float massCoeff = 5.5f;

        private FixedJoint fixedJoint
        {
            get
            {
                return _fixedJoint;
            }
            set
            {
                if (_fixedJoint != null && _fixedJoint.connectedBody != null)
                {
                    _fixedJoint.connectedBody.SendMessage("OnFloaterDetached", this, SendMessageOptions.DontRequireReceiver);
                }
                _fixedJoint = value;
                onNearOrMediumCellLevel = true;
                if (_fixedJoint != null && _fixedJoint.connectedBody != null)
                {
                    _fixedJoint.connectedBody.gameObject.EnsureComponent<FloatersTarget>();
                    _fixedJoint.connectedBody.SendMessage("OnFloaterAttached", this, SendMessageOptions.DontRequireReceiver);
                    LargeWorldEntity component = _fixedJoint.connectedBody.GetComponent<LargeWorldEntity>();
                    onNearOrMediumCellLevel = component != null && (component.cellLevel == LargeWorldEntity.CellLevel.Near || component.cellLevel == LargeWorldEntity.CellLevel.Medium);
                }
                playerDistanceTracker.enabled = !onNearOrMediumCellLevel;
                SafeAnimator.SetBool(animator, "stuckTo", _fixedJoint != null);
            }
        }

        void IPropulsionCannonAmmo.OnGrab()
        {
            Disconnect();
        }

        void IPropulsionCannonAmmo.OnShoot()
        {
        }

        void IPropulsionCannonAmmo.OnRelease()
        {
        }

        void IPropulsionCannonAmmo.OnImpact()
        {
        }

        bool IPropulsionCannonAmmo.GetAllowedToGrab()
        {
            return true;
        }

        bool IPropulsionCannonAmmo.GetAllowedToShoot()
        {
            return true;
        }

        private Rigidbody GetAttachableRigidbody(GameObject go)
        {
            Rigidbody componentInParent = go.GetComponentInParent<Rigidbody>();
            if (GetCanConnectTo(componentInParent))
            {
                return componentInParent;
            }
            return null;
        }

        private bool GetCanConnectTo(Rigidbody rb)
        {
            if (!rb || rb.isKinematic)
            {
                return false;
            }
            if (rb.CompareTag("Player") || (bool)rb.GetComponent<Floater>() || (bool)rb.GetComponent<Exosuit>())
            {
                return false;
            }
            if (rb.GetComponent<UniqueIdentifier>() == null)
            {
                return false;
            }
            LiveMixin component = rb.GetComponent<LiveMixin>();
            if (component != null && !component.IsAlive() && component.destroyOnDeath)
            {
                return false;
            }
            return true;
        }

        public override void OnDraw(Player p)
        {
            base.OnDraw(p);
            loopingHeldSound.Play();
            GetComponent<Rigidbody>().isKinematic = true;
            GetComponent<Collider>().enabled = false;
        }

        public override void OnHolster()
        {
            base.OnHolster();
            loopingHeldSound.Stop();
            GetComponent<Rigidbody>().isKinematic = false;
            GetComponent<Collider>().enabled = true;
        }

        public override void OnToolUseAnim(GUIHand guiHand)
        {
            Vector3 dropPosition = MainCameraControl.main.transform.forward * 1.07f + MainCameraControl.main.transform.position;
            pickupable.Drop(dropPosition);
            GetComponent<Rigidbody>().AddForce(MainCameraControl.main.transform.forward * 8f, ForceMode.VelocityChange);
        }

        public override bool OnRightHandDown()
        {
            return Inventory.CanDropItemHere(pickupable, notify: true);
        }

        public void Disconnect()
        {
            if (fixedJoint != null)
            {
                JointHelper.Disconnect(fixedJoint, destroyHelper: false);
                fixedJoint = null;
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (((bool)fixedJoint && (bool)fixedJoint.connectedBody) || collider.isTrigger)
            {
                return;
            }
            Rigidbody attachableRigidbody = GetAttachableRigidbody(collider.gameObject);
            if (attachableRigidbody != null)
            {
                if (debug)
                {
                    Debug.Log("Gravitating towards " + attachableRigidbody.gameObject.name, base.gameObject);
                }
                gravitateBody = attachableRigidbody;
                gravitateEndTime = Time.time + gravitateDuration;
            }
        }

        private void OnCollisionEnter(Collision collisionInfo)
        {
            if ((bool)fixedJoint && (bool)fixedJoint.connectedBody)
            {
                return;
            }
            GameObject gameObject = collisionInfo.gameObject;
            Rigidbody attachableRigidbody = GetAttachableRigidbody(gameObject);
            if (attachableRigidbody != null)
            {
                if (debug)
                {
                    Debug.Log("Attaching to " + gameObject.name, base.gameObject);
                }
                JointHelper.ConnectFixed(jointHelper, attachableRigidbody);
                gravitateBody = null;
                Vector3 forward = collisionInfo.contacts[0].point - base.transform.position;
                Quaternion rotation = base.transform.rotation;
                base.transform.rotation = Quaternion.LookRotation(forward) * attachRotationOffset;
                model.rotation = rotation;
                modelRotationSetTime = Time.time;
                lerpModelRotation = true;
            }
            if (gameObject != null && gameObject.GetComponent<Living>() != null)
            {
                LiveMixin component = gameObject.GetComponent<LiveMixin>();
                if (component != null)
                {
                    component.TakeDamage(global::UnityEngine.Random.Range(3f, 7f), base.transform.position);
                }
            }
        }

        private void Start()
        {
            float num = buoyantPeriod + global::UnityEngine.Random.Range(-1f, 1f) * buoyantPeriodDeviation;
            frequency = 1f / num;
            phase = global::UnityEngine.Random.Range(0f, (float)Math.PI * 2f);
            if (pickupable.attached || !(fixedJoint == null) || !(GetComponent<JointHelper>() == null))
            {
                return;
            }
            Transform parent = base.gameObject.transform.parent;
            if ((bool)parent && (bool)parent.GetComponent<UniqueIdentifier>())
            {
                Rigidbody attachableRigidbody = GetAttachableRigidbody(parent.gameObject);
                if ((bool)attachableRigidbody)
                {
                    JointHelper.ConnectFixed(jointHelper, attachableRigidbody);
                }
            }
        }

        private void OnJointConnected(FixedJoint joint)
        {
            if (!GetCanConnectTo(joint.connectedBody))
            {
                JointHelper.Disconnect(joint, destroyHelper: false);
                return;
            }
            fixedJoint = joint;
            if (debug && (bool)fixedJoint)
            {
                Debug.Log("Floater got joint on event " + base.name, base.gameObject);
            }
        }

        private void FindConnectedJoint()
        {
            FixedJoint component = GetComponent<FixedJoint>();
            if (!(component == null))
            {
                if (!GetCanConnectTo(component.connectedBody))
                {
                    JointHelper.Disconnect(component, destroyHelper: false);
                }
                else
                {
                    fixedJoint = component;
                }
            }
        }

        private void Update()
        {
            if ((bool)fixedJoint && !fixedJoint.connectedBody)
            {
                Disconnect();
            }
            if (debug && (bool)fixedJoint && (bool)fixedJoint.connectedBody)
            {
                Debug.DrawLine(base.gameObject.transform.position, fixedJoint.connectedBody.gameObject.transform.position, Color.green, 0.1f);
            }
            if (lerpModelRotation)
            {
                float num = Mathf.InverseLerp(modelRotationSetTime, modelRotationSetTime + 0.5f, Time.time);
                model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.identity, num);
                if (num >= 1f)
                {
                    lerpModelRotation = false;
                }
            }
        }

        private void FixedUpdate()
        {
            if (gravitateBody != null && !gravitateBody.gameObject.activeInHierarchy)
            {
                gravitateBody = null;
            }
            if (Time.time < gravitateEndTime && (bool)gravitateBody && !fixedJoint)
            {
                Vector3 vector = Vector3.Normalize(gravitateBody.transform.position - base.gameObject.transform.position);
                GetComponent<Rigidbody>().AddForce(vector * gravitateAcceleration, ForceMode.Acceleration);
                Debug.DrawLine(base.gameObject.transform.position, gravitateBody.transform.position, Color.yellow, 0.1f);
            }
            else if (BuoyancyEnabled() && 0f - base.transform.position.y > 0f)
            {
                float num = Mathf.Sin(phase + Time.fixedTime * frequency);
                float num2 = buoyantForce + num * buoyantForceDeviation;
                if ((bool)fixedJoint && (bool)fixedJoint.connectedBody)
                {
                    float mass = fixedJoint.connectedBody.mass;
                    float num3 = Mathf.Max(1f, Mathf.Sqrt(mass) * massCoeff);
                    GetComponent<Rigidbody>().AddForce(Vector3.up * (num2 + 30f) * num3, ForceMode.Acceleration);
                }
                else
                {
                    GetComponent<Rigidbody>().AddForce(Vector3.up * num2, ForceMode.Acceleration);
                }
            }
        }

        private bool BuoyancyEnabled()
        {
            if (onNearOrMediumCellLevel)
            {
                return true;
            }
            return playerDistanceTracker.playerNearby;
        }
    }
}
