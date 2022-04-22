using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class SubControl : MonoBehaviour
    {
        public enum Mode
        {
            Autopilot,
            GameObjects,
            DirectInput
        }

        public Event<Mode> modeChangedEvent = new Event<Mode>();

        public float BaseTurningTorque = 4f;

        public float TurningTorquePerEngine = 2f;

        public float BaseVerticalAccel = 50f;

        public float AccelPerBallast = 20f;

        public float BaseForwardAccel = 50f;

        public float AccelPerEngine = 25f;

        public bool appliedThrottle;

        private Vector3 throttle;

        private float lastTimeThrottled;

        public float accelScale = 1f;

        public float turnScale = 1f;

        private float spawnTime;

        private SubRoot sub;

        public int useThrottleIndex = 2;

        public int wasUsingThrottleIndex;

        [AssertNotNull]
        public EngineRpmSFXManager engineRPMManager;

        public PowerRelay powerRelay;

        [AssertNotNull]
        public CyclopsMotorMode cyclopsMotorMode;

        [AssertNotNull]
        public FMODAsset engineStartSound;

        private VehicleAccelerationModifier[] accelerationModifiers;

        private ISubTurnHandler[] turnHandlers;

        private ISubThrottleHandler[] throttleHandlers;

        private float steeringReponsiveness = 2f;

        private float steeringWheelYaw;

        private float steeringWheelPitch;

        private bool canAccel = true;

        public Animator mainAnimator;

        [AssertNotNull]
        public BehaviourLOD LOD;

        public Mode controlMode { get; private set; }

        private void Start()
        {
            Set(Mode.GameObjects);
            sub = GetComponent<SubRoot>();
            GetComponent<Rigidbody>().isKinematic = false;
            spawnTime = Time.time;
            powerRelay = GetComponent<PowerRelay>();
            accelerationModifiers = base.gameObject.GetComponentsInChildren<VehicleAccelerationModifier>();
            turnHandlers = base.gameObject.GetComponentsInChildren<ISubTurnHandler>();
            throttleHandlers = base.gameObject.GetComponentsInChildren<ISubThrottleHandler>();
        }

        public void NewSpeed(int newSpeedIndex)
        {
            useThrottleIndex = newSpeedIndex;
            wasUsingThrottleIndex = useThrottleIndex;
        }

        public void NewEngineMode(bool engineOn)
        {
            canAccel = engineOn;
        }

        public void Set(Mode newMode)
        {
            Mode mode = controlMode;
            controlMode = newMode;
            if (mode != newMode)
            {
                if (mode == Mode.Autopilot && sub != null)
                {
                    new global::UnityEngine.Plane(sub.subAxis.right * -1f, sub.GetWorldCenterOfMass());
                    new global::UnityEngine.Plane(sub.subAxis.right, sub.GetWorldCenterOfMass());
                }
                modeChangedEvent.Trigger(newMode);
            }
        }

        public void SetGameObjectsMode()
        {
            Set(Mode.GameObjects);
        }

        public void SetAutoMode()
        {
            Set(Mode.Autopilot);
        }

        public bool IsAutoMode()
        {
            return controlMode == Mode.Autopilot;
        }

        public bool IsManualMode()
        {
            return controlMode == Mode.GameObjects;
        }

        private void Update()
        {
            if (!LOD.IsFull())
            {
                return;
            }
            appliedThrottle = false;
            if (controlMode == Mode.DirectInput)
            {
                throttle = GameInput.GetMoveDirection();
                if (canAccel && (double)throttle.magnitude > 0.0001)
                {
                    float amountConsumed = 0f;
                    float amount = throttle.magnitude * cyclopsMotorMode.GetPowerConsumption() * Time.deltaTime / sub.GetPowerRating();
                    if (!GameModeUtils.RequiresPower() || powerRelay.ConsumeEnergy(amount, out amountConsumed))
                    {
                        lastTimeThrottled = Time.time;
                        appliedThrottle = true;
                    }
                }
                if (appliedThrottle && canAccel)
                {
                    float topClamp = 0.33f;
                    if (useThrottleIndex == 1)
                    {
                        topClamp = 0.66f;
                    }
                    if (useThrottleIndex == 2)
                    {
                        topClamp = 1f;
                    }
                    engineRPMManager.AccelerateInput(topClamp);
                    for (int i = 0; i < throttleHandlers.Length; i++)
                    {
                        throttleHandlers[i].OnSubAppliedThrottle();
                    }
                    if (lastTimeThrottled < Time.time - 5f)
                    {
                        Utils.PlayFMODAsset(engineStartSound, MainCamera.camera.transform);
                    }
                }
                if (AvatarInputHandler.main.IsEnabled())
                {
                    if (GameInput.GetButtonDown(GameInput.Button.RightHand))
                    {
                        base.transform.parent.BroadcastMessage("ToggleFloodlights", null, SendMessageOptions.DontRequireReceiver);
                    }
                    if (GameInput.GetButtonDown(GameInput.Button.Exit))
                    {
                        Player.main.TryEject();
                    }
                }
            }
            if (!appliedThrottle)
            {
                throttle = new Vector3(0f, 0f, 0f);
            }
        }

        private void FixedUpdate()
        {
            if (!LOD.IsFull() || powerRelay.GetPowerStatus() == PowerSystem.Status.Offline)
            {
                return;
            }
            for (int i = 0; i < accelerationModifiers.Length; i++)
            {
                accelerationModifiers[i].ModifyAcceleration(ref throttle);
            }
            if (!(Ocean.main.GetDepthOf(base.gameObject) > 0f))
            {
                return;
            }
            float b = 0f;
            float b2 = 0f;
            if ((double)Mathf.Abs(throttle.x) > 0.0001)
            {
                float baseTurningTorque = BaseTurningTorque;
                if (canAccel)
                {
                    GetComponent<Rigidbody>().AddTorque(sub.subAxis.up * baseTurningTorque * turnScale * throttle.x, ForceMode.Acceleration);
                }
                ShipSide useShipSide;
                if (throttle.x > 0f)
                {
                    useShipSide = ShipSide.Port;
                    b = 90f;
                }
                else
                {
                    useShipSide = ShipSide.Starboard;
                    b = -90f;
                }
                if (throttle.x < -0.1f || throttle.x > 0.1f)
                {
                    for (int j = 0; j < turnHandlers.Length; j++)
                    {
                        turnHandlers[j].OnSubTurn(useShipSide);
                    }
                }
            }
            if ((double)Mathf.Abs(throttle.y) > 0.0001)
            {
                float baseVerticalAccel = BaseVerticalAccel;
                baseVerticalAccel += (float)base.gameObject.GetComponentsInChildren<BallastWeight>().Length * AccelPerBallast;
                if (canAccel)
                {
                    GetComponent<Rigidbody>().AddForce(Vector3.up * baseVerticalAccel * accelScale * throttle.y, ForceMode.Acceleration);
                }
                b2 = ((!(throttle.y > 0f)) ? (-90f) : 90f);
            }
            if ((double)Mathf.Abs(throttle.z) > 0.0001)
            {
                float baseForwardAccel = BaseForwardAccel;
                if (canAccel)
                {
                    GetComponent<Rigidbody>().AddForce(sub.subAxis.forward * baseForwardAccel * accelScale * throttle.z, ForceMode.Acceleration);
                }
            }
            steeringWheelYaw = Mathf.Lerp(steeringWheelYaw, b, Time.deltaTime * steeringReponsiveness);
            steeringWheelPitch = Mathf.Lerp(steeringWheelPitch, b2, Time.deltaTime * steeringReponsiveness);
            if ((bool)mainAnimator)
            {
                mainAnimator.SetFloat("view_yaw", steeringWheelYaw);
                mainAnimator.SetFloat("view_pitch", steeringWheelPitch);
                Player.main.playerAnimator.SetFloat("cyclops_yaw", steeringWheelYaw);
                Player.main.playerAnimator.SetFloat("cyclops_pitch", steeringWheelPitch);
            }
        }
    }
}
