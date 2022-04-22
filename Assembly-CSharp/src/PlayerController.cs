using UnityEngine;

namespace AssemblyCSharp
{
    public class PlayerController : MonoBehaviour, IProtoEventListener
    {
        [AssertNotNull]
        public Player player;

        public float standheight = 1.5f;

        public float swimheight = 0.5f;

        public Vector3 velocity = Vector3.zero;

        public PlayerMotor underWaterController;

        public PlayerMotor groundController;

        public PlayerMotor activeController;

        public float controllerRadius = 0.3f;

        public float defaultSwimDrag = 2.5f;

        public bool inputEnabled = true;

        private float currentControllerHeight = 1.5f;

        private float desiredControllerHeight = 1.5f;

        private bool underWater;

        private bool inVehicle;

        public Transform forwardReference
        {
            get
            {
                return MainCamera.camera.transform;
            }
            private set
            {
            }
        }

        private void Start()
        {
            underWaterController.SetEnabled(enabled: true);
            underWaterController.SetEnabled(enabled: false);
            groundController.SetEnabled(enabled: true);
            groundController.SetEnabled(enabled: false);
            groundController.SetControllerRadius(controllerRadius);
            underWaterController.SetControllerRadius(controllerRadius);
            activeController = groundController;
            activeController.SetEnabled(enabled: true);
            currentControllerHeight = standheight;
            desiredControllerHeight = standheight;
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            Invoke("HandleControllerStateAfterDeserialization", 0.1f);
        }

        public bool IsSprinting()
        {
            if (base.enabled)
            {
                return activeController.IsSprinting();
            }
            return false;
        }

        public void SetEnabled(bool enabled)
        {
            velocity = Vector3.zero;
            if (activeController != null)
            {
                activeController.SetVelocity(velocity);
            }
            if (!enabled)
            {
                underWaterController.SetEnabled(enabled: false);
                groundController.SetEnabled(enabled: false);
            }
            else if (activeController != null)
            {
                activeController.SetEnabled(enabled: true);
            }
            base.enabled = enabled;
        }

        public void ForceControllerSize()
        {
            player.UpdateIsUnderwater();
            bool flag = player.IsUnderwaterForSwimming();
            bool flag2 = player.GetVehicle();
            desiredControllerHeight = ((flag || flag2) ? swimheight : standheight);
            currentControllerHeight = desiredControllerHeight;
            groundController.SetControllerHeight(currentControllerHeight);
            underWaterController.SetControllerHeight(currentControllerHeight);
        }

        private void HandleControllerStateAfterDeserialization()
        {
            HandleUnderWaterState();
            HandleControllerState();
        }

        private void HandleControllerState()
        {
            groundController.SetEnabled(enabled: false);
            underWaterController.SetEnabled(enabled: false);
            if (!inVehicle)
            {
                if (underWater)
                {
                    activeController = (player.IsInSub() ? groundController : underWaterController);
                    desiredControllerHeight = swimheight;
                    activeController.SetControllerHeight(currentControllerHeight);
                    activeController.SetEnabled(enabled: true);
                }
                else
                {
                    activeController = groundController;
                    desiredControllerHeight = standheight;
                    activeController.SetControllerHeight(currentControllerHeight);
                    activeController.SetEnabled(enabled: true);
                }
            }
        }

        private void HandleUnderWaterState()
        {
            bool flag = player.IsUnderwaterForSwimming();
            bool flag2 = player.GetVehicle();
            if (underWater != flag || inVehicle != flag2)
            {
                underWater = flag;
                inVehicle = flag2;
                HandleControllerState();
            }
            activeController.SetUnderWater(underWater);
        }

        public void SetMotorMode(Player.MotorMode newMotorMode)
        {
            float forwardMaxSpeed = 5f;
            float backwardMaxSpeed = 5f;
            float strafeMaxSpeed = 5f;
            float underWaterGravity = 0f;
            float swimDrag = defaultSwimDrag;
            bool canSwim = true;
            switch (newMotorMode)
            {
                case Player.MotorMode.Seaglide:
                    forwardMaxSpeed = 25f;
                    backwardMaxSpeed = 5f;
                    strafeMaxSpeed = 5f;
                    swimDrag = 2.5f;
                    break;
                case Player.MotorMode.Mech:
                    forwardMaxSpeed = 4.5f;
                    backwardMaxSpeed = 4.5f;
                    strafeMaxSpeed = 4.5f;
                    underWaterGravity = 7.2f;
                    canSwim = false;
                    break;
                case Player.MotorMode.Walk:
                case Player.MotorMode.Run:
                    forwardMaxSpeed = 3.5f;
                    backwardMaxSpeed = 5f;
                    strafeMaxSpeed = 5f;
                    break;
            }
            underWaterController.forwardMaxSpeed = forwardMaxSpeed;
            underWaterController.backwardMaxSpeed = backwardMaxSpeed;
            underWaterController.strafeMaxSpeed = strafeMaxSpeed;
            underWaterController.underWaterGravity = underWaterGravity;
            underWaterController.swimDrag = swimDrag;
            underWaterController.canSwim = canSwim;
            groundController.forwardMaxSpeed = forwardMaxSpeed;
            groundController.backwardMaxSpeed = backwardMaxSpeed;
            groundController.strafeMaxSpeed = strafeMaxSpeed;
            groundController.underWaterGravity = underWaterGravity;
            groundController.canSwim = canSwim;
        }

        public bool TestHasSpace(Vector3 position)
        {
            RaycastHit hitInfo;
            return !Physics.CapsuleCast(position, position, controllerRadius + 0.01f, Vector3.up, out hitInfo, currentControllerHeight, -524289, QueryTriggerInteraction.Ignore);
        }

        public bool WayToPositionClear(Vector3 position, GameObject ignoreObj = null, bool ignoreLiving = false)
        {
            Vector3 point = base.transform.position - Vector3.up * 0.5f * currentControllerHeight;
            Vector3 point2 = base.transform.position + Vector3.up * 0.5f * currentControllerHeight;
            Vector3 value = position - base.transform.position;
            int num = global::UWE.Utils.CapsuleCastIntoSharedBuffer(point, point2, controllerRadius + 0.01f, Vector3.Normalize(value), value.magnitude, -524289, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < num; i++)
            {
                RaycastHit raycastHit = global::UWE.Utils.sharedHitBuffer[i];
                GameObject gameObject = raycastHit.collider.gameObject;
                if ((!(ignoreObj != null) || !global::UWE.Utils.IsAncestorOf(ignoreObj, gameObject)) && (!ignoreLiving || !(gameObject.GetComponentInParent<Living>() != null)))
                {
                    return false;
                }
            }
            return true;
        }

        public void UpdateController()
        {
            HandleUnderWaterState();
            float num = global::UWE.Utils.Slerp(currentControllerHeight, desiredControllerHeight, Time.deltaTime * 2f);
            float num2 = num - currentControllerHeight;
            bool flag = true;
            if (num2 > 0f)
            {
                Vector3 vector = base.transform.position + new Vector3(0f, currentControllerHeight * 0.5f, 0f);
                flag = !Physics.CapsuleCast(vector, vector, controllerRadius + 0.01f, Vector3.up, out var _, num2, -524289);
            }
            if (flag)
            {
                currentControllerHeight = num;
            }
            underWaterController.SetControllerHeight(currentControllerHeight);
            groundController.SetControllerHeight(currentControllerHeight);
            velocity = activeController.UpdateMove();
        }

        private void FixedUpdate()
        {
            UpdateController();
        }
    }
}
