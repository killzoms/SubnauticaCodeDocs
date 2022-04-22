using UnityEngine;

namespace AssemblyCSharp
{
    public class CyclopsExternalCams : MonoBehaviour, IInputHandler
    {
        public LiveMixin liveMixin;

        public Transform[] externalCamPositions = new Transform[3];

        public Light cameraLight;

        public CyclopsLightingPanel lightingPanel;

        private bool usingCamera;

        private int cameraIndex;

        private static int lightState = 1;

        private void Start()
        {
            cameraLight.enabled = false;
        }

        private void ChangeCamera(int iterate)
        {
            int num = externalCamPositions.Length;
            cameraIndex += iterate;
            if (cameraIndex > num - 1)
            {
                cameraIndex = 0;
            }
            else if (cameraIndex < 0)
            {
                cameraIndex = num - 1;
            }
            int num2 = 0;
            Transform[] array = externalCamPositions;
            for (int i = 0; i < array.Length; i++)
            {
                _ = array[i];
                if (num2 == cameraIndex)
                {
                    externalCamPositions[num2].SendMessage("ActivateCamera", cameraLight, SendMessageOptions.RequireReceiver);
                }
                else
                {
                    externalCamPositions[num2].SendMessage("DeactivateCamera", null, SendMessageOptions.RequireReceiver);
                }
                num2++;
            }
            uGUI_CameraCyclops.main.SetCamera(cameraIndex);
            lightState = 1;
            SetLight();
        }

        public bool GetUsingCameras()
        {
            return usingCamera;
        }

        public void EnterCameraView()
        {
            usingCamera = true;
            InputHandlerStack.main.Push(this);
            _ = Player.main;
            MainCameraControl.main.enabled = false;
            Player.main.SetHeadVisible(visible: true);
            VRUtil.Recenter();
            cameraLight.enabled = true;
            ChangeCamera(0);
            if ((bool)lightingPanel)
            {
                lightingPanel.TempTurnOffFloodlights();
            }
        }

        private void ExitCamera()
        {
            usingCamera = false;
            _ = Player.main;
            SNCameraRoot.main.transform.localPosition = Vector3.zero;
            SNCameraRoot.main.transform.localRotation = Quaternion.identity;
            MainCameraControl.main.enabled = true;
            Player.main.SetHeadVisible(visible: false);
            uGUI_CameraCyclops.main.SetCamera(-1);
            cameraLight.enabled = false;
            if ((bool)lightingPanel)
            {
                lightingPanel.RestoreFloodlightsFromTempState();
            }
        }

        public bool HandleInput()
        {
            if (!usingCamera)
            {
                return false;
            }
            if (!liveMixin.IsAlive())
            {
                ExitCamera();
                return false;
            }
            if (GameInput.GetButtonUp(GameInput.Button.Exit) || GameInput.GetButtonUp(GameInput.Button.RightHand) || Input.GetKeyUp(KeyCode.Escape))
            {
                ExitCamera();
                return false;
            }
            if (GameInput.GetButtonDown(GameInput.Button.CycleNext))
            {
                ChangeCamera(1);
            }
            else if (GameInput.GetButtonDown(GameInput.Button.CyclePrev))
            {
                ChangeCamera(-1);
            }
            if (GameInput.GetButtonDown(GameInput.Button.LeftHand))
            {
                IterateLightState();
                SetLight();
            }
            return true;
        }

        public bool HandleLateInput()
        {
            return true;
        }

        public void OnFocusChanged(InputFocusMode mode)
        {
            switch (mode)
            {
            }
        }

        private void SetLight()
        {
            switch (lightState)
            {
                case 0:
                    cameraLight.enabled = false;
                    break;
                case 1:
                    cameraLight.enabled = true;
                    cameraLight.color = Color.white;
                    break;
                case 2:
                    cameraLight.enabled = true;
                    cameraLight.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    break;
            }
        }

        private static void IterateLightState()
        {
            lightState++;
            if (lightState == 3)
            {
                lightState = 0;
            }
        }

        private void LateUpdate()
        {
            if (usingCamera)
            {
                Transform transform = externalCamPositions[cameraIndex];
                if (transform == null)
                {
                    usingCamera = false;
                    return;
                }
                SNCameraRoot.main.transform.position = transform.position;
                SNCameraRoot.main.transform.rotation = transform.rotation;
            }
        }
    }
}
