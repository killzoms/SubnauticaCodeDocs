using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class MapRoomCamera : MonoBehaviour, IProtoEventListener
    {
        public delegate void OnMapRoomCameraChanged();

        private const int currentVersion = 2;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 2;

        [NonSerialized]
        [ProtoMember(2)]
        public int cameraNumber;

        [AssertNotNull]
        public Rigidbody rigidBody;

        [AssertNotNull]
        public Camera camera;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter engineSound;

        [AssertNotNull]
        public GameObject screenEffectModel;

        [AssertNotNull]
        public Gradient gradientInner;

        [AssertNotNull]
        public Gradient gradientOuter;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter droneIdle;

        [AssertNotNull]
        public EnergyMixin energyMixin;

        [AssertNotNull]
        public LiveMixin liveMixin;

        [AssertNotNull]
        public Pickupable pickupAble;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter chargingSound;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter connectingSound;

        [AssertNotNull]
        public FMODAsset connectedSound;

        [AssertNotNull]
        public GameObject lightsParent;

        [AssertNotNull]
        public WorldForces worldForces;

        [AssertNotNull]
        public PingInstance pingInstance;

        private Player controllingPlayer;

        private MapRoomScreen screen;

        private Vector3 wishDir = Vector3.zero;

        private float controllStartTime;

        private GameObject inputStackDummy;

        private MapRoomCameraDocking dockingPoint;

        private bool readyForControl;

        private bool justStartedControl;

        private static bool renderedFirstTexture = false;

        private const float acceleration = 20f;

        private const float sidewaysTorque = 45f;

        private const float stabilizeForce = 6f;

        private const float controllTimeDelay = 0.25f;

        public static List<MapRoomCamera> cameras = new List<MapRoomCamera>();

        public static int lastCameraNum = 1;

        public static event OnMapRoomCameraChanged onMapRoomCameraChanged;

        private void Start()
        {
            if (version == 1)
            {
                version = 2;
                if (energyMixin != null)
                {
                    energyMixin.SpawnDefault(1f);
                }
            }
            cameras.Add(this);
            if (cameraNumber == 0)
            {
                cameraNumber = cameras.Count;
            }
            if (!renderedFirstTexture)
            {
                Invoke("RenderToTexture", 0.5f);
                renderedFirstTexture = true;
            }
            inputStackDummy = new GameObject("inputStackDummy");
            inputStackDummy.transform.parent = base.transform;
            inputStackDummy.SetActive(value: false);
            camera.enabled = false;
            SetDocked(dockingPoint);
            screenEffectModel.GetComponent<Renderer>().materials[0].SetColor(ShaderPropertyID._Color, gradientInner.Evaluate(0f));
            screenEffectModel.GetComponent<Renderer>().materials[1].SetColor(ShaderPropertyID._Color, gradientOuter.Evaluate(0f));
            pickupAble.pickedUpEvent.AddHandler(base.gameObject, OnPickedUp);
            lightsParent.SetActive(value: false);
            Constructable component = GetComponent<Constructable>();
            if ((bool)component)
            {
                global::UnityEngine.Object.Destroy(component);
            }
            UpdatePingLabel();
        }

        public bool IsReady()
        {
            if (readyForControl)
            {
                return !justStartedControl;
            }
            return false;
        }

        public int GetCameraNumber()
        {
            return cameraNumber;
        }

        public bool CanBeControlled(MapRoomScreen byScreen = null)
        {
            if ((energyMixin.charge > 0f || !GameModeUtils.RequiresPower()) && liveMixin.IsAlive() && !pickupAble.attached)
            {
                return base.isActiveAndEnabled;
            }
            return false;
        }

        public float GetScreenDistance(MapRoomScreen fromScreen = null)
        {
            fromScreen = ((fromScreen != null) ? fromScreen : screen);
            if (!(fromScreen != null))
            {
                return 0f;
            }
            return (fromScreen.transform.position - base.transform.position).magnitude;
        }

        public float GetDepth()
        {
            return Mathf.Abs(Mathf.Min(0f, base.transform.position.y));
        }

        private void RenderToTexture()
        {
            camera.enabled = true;
            camera.Render();
            camera.enabled = false;
        }

        public void OnKill()
        {
            if ((bool)controllingPlayer)
            {
                FreeCamera();
            }
        }

        private void OnDisable()
        {
            if ((bool)controllingPlayer)
            {
                FreeCamera();
            }
        }

        private void OnDestroy()
        {
            if ((bool)controllingPlayer)
            {
                FreeCamera();
            }
            cameras.Remove(this);
        }

        public void ControlCamera(Player player, MapRoomScreen screen)
        {
            controllStartTime = Time.time;
            controllingPlayer = player;
            player.EnterLockedMode(null);
            rigidBody.velocity = Vector3.zero;
            MainCameraControl.main.enabled = false;
            this.screen = screen;
            InputHandlerStack.main.Push(inputStackDummy);
            RenderToTexture();
            uGUI_CameraDrone.main.SetCamera(this);
            uGUI_CameraDrone.main.SetScreen(screen);
            screenEffectModel.SetActive(value: true);
            droneIdle.Play();
            readyForControl = false;
            connectingSound.Play();
            Player.main.SetHeadVisible(visible: true);
            lightsParent.SetActive(value: true);
            justStartedControl = true;
            VRUtil.Recenter();
            lastCameraNum = cameraNumber;
            if (MapRoomCamera.onMapRoomCameraChanged != null)
            {
                MapRoomCamera.onMapRoomCameraChanged();
            }
        }

        public void FreeCamera(bool resetPlayerPosition = true)
        {
            InputHandlerStack.main.Pop(inputStackDummy);
            controllingPlayer.ExitLockedMode(respawn: false, findNewPosition: false);
            controllingPlayer = null;
            if (resetPlayerPosition)
            {
                SNCameraRoot.main.transform.localPosition = Vector3.zero;
                SNCameraRoot.main.transform.localRotation = Quaternion.identity;
            }
            rigidBody.velocity = Vector3.zero;
            MainCameraControl.main.enabled = true;
            screen.OnCameraFree(this);
            screen = null;
            RenderToTexture();
            uGUI_CameraDrone.main.SetCamera(null);
            uGUI_CameraDrone.main.SetScreen(null);
            engineSound.Stop();
            screenEffectModel.SetActive(value: false);
            droneIdle.Stop();
            connectingSound.Stop();
            Player.main.SetHeadVisible(visible: false);
            lightsParent.SetActive(value: false);
        }

        private bool IsControlled()
        {
            if (controllingPlayer != null)
            {
                return controllStartTime + 0.25f <= Time.time;
            }
            return false;
        }

        public void SetDocked(MapRoomCameraDocking dockingPoint)
        {
            this.dockingPoint = dockingPoint;
            if (!pickupAble.attached)
            {
                rigidBody.isKinematic = dockingPoint != null;
            }
        }

        public void OnShinyPickUp(GameObject byObject)
        {
            if ((bool)dockingPoint)
            {
                dockingPoint.UndockCamera();
            }
        }

        public void OnPickedUp(Pickupable p)
        {
            if ((bool)dockingPoint)
            {
                dockingPoint.UndockCamera();
            }
        }

        private void UpdateEnergyRecharge()
        {
            bool flag = false;
            float charge = energyMixin.charge;
            float capacity = energyMixin.capacity;
            if (dockingPoint != null && charge < capacity)
            {
                float amount = Mathf.Min(capacity - charge, capacity * 0.1f);
                PowerRelay componentInParent = dockingPoint.GetComponentInParent<PowerRelay>();
                if (componentInParent == null)
                {
                    Debug.LogError("camera drone is docked but can't access PowerRelay component");
                }
                float amountConsumed = 0f;
                componentInParent.ConsumeEnergy(amount, out amountConsumed);
                if (!GameModeUtils.RequiresPower() || amountConsumed > 0f)
                {
                    energyMixin.AddEnergy(amountConsumed);
                    flag = true;
                }
            }
            if (flag)
            {
                chargingSound.Play();
            }
            else
            {
                chargingSound.Stop();
            }
        }

        private void Update()
        {
            ProfilingUtils.BeginSample("MapRoomCamera.Update()");
            UpdateEnergyRecharge();
            if (IsControlled() && inputStackDummy.activeInHierarchy)
            {
                if (!IsReady() && LargeWorldStreamer.main.IsWorldSettled())
                {
                    readyForControl = true;
                    connectingSound.Stop();
                    Utils.PlayFMODAsset(connectedSound, base.transform);
                }
                if (CanBeControlled() && readyForControl)
                {
                    Vector2 lookDelta = GameInput.GetLookDelta();
                    rigidBody.AddTorque(base.transform.up * lookDelta.x * 45f * 0.0015f, ForceMode.VelocityChange);
                    rigidBody.AddTorque(base.transform.right * (0f - lookDelta.y) * 45f * 0.0015f, ForceMode.VelocityChange);
                    wishDir = GameInput.GetMoveDirection();
                    wishDir.Normalize();
                    if (dockingPoint != null && wishDir != Vector3.zero)
                    {
                        dockingPoint.UndockCamera();
                    }
                }
                else
                {
                    wishDir = Vector3.zero;
                }
                if (Input.GetKeyUp(KeyCode.Escape) || GameInput.GetButtonUp(GameInput.Button.Exit))
                {
                    FreeCamera();
                }
                else if (GameInput.GetButtonDown(GameInput.Button.CycleNext))
                {
                    screen.CycleCamera();
                }
                else if (GameInput.GetButtonDown(GameInput.Button.CyclePrev))
                {
                    screen.CycleCamera(-1);
                }
                if (Player.main != null && Player.main.liveMixin != null && !Player.main.liveMixin.IsAlive())
                {
                    FreeCamera();
                }
                float magnitude = GetComponent<Rigidbody>().velocity.magnitude;
                float time = Mathf.Clamp(base.transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity).z / 15f, 0f, 1f);
                if (magnitude > 2f)
                {
                    engineSound.Play();
                    energyMixin.ConsumeEnergy(Time.deltaTime * 0.06666f);
                }
                else
                {
                    engineSound.Stop();
                }
                screenEffectModel.GetComponent<Renderer>().materials[0].SetColor(ShaderPropertyID._Color, gradientInner.Evaluate(time));
                screenEffectModel.GetComponent<Renderer>().materials[1].SetColor(ShaderPropertyID._Color, gradientOuter.Evaluate(time));
            }
            ProfilingUtils.EndSample();
        }

        private void LateUpdate()
        {
            if ((bool)controllingPlayer)
            {
                if (justStartedControl)
                {
                    justStartedControl = false;
                    return;
                }
                SNCameraRoot.main.transform.position = base.transform.position;
                SNCameraRoot.main.transform.rotation = base.transform.rotation;
            }
        }

        private void StabilizeRoll()
        {
            float num = Mathf.Abs(base.transform.eulerAngles.z - 180f);
            if (num <= 178f)
            {
                float num2 = Mathf.Clamp(1f - num / 180f, 0f, 0.5f) * 6f;
                GetComponent<Rigidbody>().AddTorque(base.transform.forward * num2 * Time.deltaTime * Mathf.Sign(base.transform.eulerAngles.z - 180f), ForceMode.VelocityChange);
            }
        }

        private void FixedUpdate()
        {
            if (IsControlled() && base.transform.position.y < worldForces.waterDepth)
            {
                rigidBody.AddForce(base.transform.rotation * (20f * wishDir), ForceMode.Acceleration);
                StabilizeRoll();
            }
        }

        public static void GetCamerasInRange(Vector3 position, float range, ICollection<MapRoomCamera> outlist)
        {
            float num = range * range;
            for (int i = 0; i < cameras.Count; i++)
            {
                MapRoomCamera mapRoomCamera = cameras[i];
                if ((mapRoomCamera.transform.position - position).sqrMagnitude <= num)
                {
                    outlist.Add(mapRoomCamera);
                }
            }
        }

        private void UpdatePingLabel()
        {
            if (pingInstance != null)
            {
                pingInstance.SetLabel(Language.main.GetFormat("MapRoomCameraInfo", GetCameraNumber()));
            }
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            UpdatePingLabel();
        }
    }
}
