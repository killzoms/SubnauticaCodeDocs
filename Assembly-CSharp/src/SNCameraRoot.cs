using System.Text;
using Gendarme;
using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
    public class SNCameraRoot : MonoBehaviour, ICompileTimeCheckable
    {
        public static SNCameraRoot main;

        public Camera mainCamera;

        public Camera guiCamera;

        private const float interpupillaryDistanceThreshold = 1E-05f;

        private float stereoSeparation = float.MinValue;

        private Matrix4x4 matrixLeftEye = Matrix4x4.identity;

        private Matrix4x4 matrixRightEye = Matrix4x4.identity;

        private float overrideFieldOfView;

        public Camera mainCam => mainCamera;

        public Camera guiCam => guiCamera;

        public Transform GetForwardTransform()
        {
            return base.transform;
        }

        public Transform GetAimingTransform()
        {
            return mainCamera.transform;
        }

        public void SonarPing()
        {
            mainCamera.GetComponent<SonarScreenFX>().Ping();
        }

        private void Awake()
        {
            main = this;
            guiCamera.nearClipPlane = mainCamera.nearClipPlane;
            guiCamera.farClipPlane = mainCamera.farClipPlane;
            guiCamera.fieldOfView = mainCamera.fieldOfView;
        }

        private void Start()
        {
            Transform obj = guiCamera.transform;
            obj.SetParent(null, worldPositionStays: false);
            obj.localPosition = new Vector3(0f, 0f, 0f);
            obj.localRotation = Quaternion.identity;
            DevConsole.RegisterConsoleCommand(this, "fov");
            DevConsole.RegisterConsoleCommand(this, "farplane");
            DevConsole.RegisterConsoleCommand(this, "nearplane");
            SyncFieldOfView();
        }

        private void Update()
        {
            UpdateVR();
        }

        private void OnDestroy()
        {
            main = null;
        }

        private void UpdateVR()
        {
            if (XRSettings.enabled)
            {
                float num = mainCamera.stereoSeparation;
                if (!(Mathf.Abs(stereoSeparation - num) < 1E-05f))
                {
                    stereoSeparation = num;
                    matrixLeftEye.m03 = stereoSeparation * 0.5f;
                    matrixLeftEye.m22 = -1f;
                    matrixRightEye.m03 = (0f - stereoSeparation) * 0.5f;
                    matrixRightEye.m22 = -1f;
                    guiCamera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, matrixLeftEye);
                    guiCamera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, matrixRightEye);
                }
            }
        }

        public void SetFov(float fov)
        {
            overrideFieldOfView = fov;
            SyncFieldOfView();
        }

        public void SyncFieldOfView()
        {
            float fieldOfView = MiscSettings.fieldOfView;
            if (overrideFieldOfView != 0f)
            {
                fieldOfView = overrideFieldOfView;
            }
            mainCamera.fieldOfView = fieldOfView;
            guiCamera.fieldOfView = fieldOfView;
        }

        public void SetFarPlaneDistance(float dist)
        {
            mainCamera.farClipPlane = dist;
            guiCamera.farClipPlane = dist;
        }

        public void OnConsoleCommand_farplane(NotificationCenter.Notification n)
        {
            float farPlaneDistance = float.Parse((string)n.data[0]);
            SetFarPlaneDistance(farPlaneDistance);
        }

        public void SetNearPlaneDistance(float dist)
        {
            mainCamera.nearClipPlane = dist;
            guiCamera.nearClipPlane = dist;
        }

        public void OnConsoleCommand_nearplane(NotificationCenter.Notification n)
        {
            float nearPlaneDistance = float.Parse((string)n.data[0]);
            SetNearPlaneDistance(nearPlaneDistance);
        }

        public string CompileTimeCheck()
        {
            if (mainCamera == null || guiCamera == null)
            {
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder();
            int cullingMask = mainCamera.cullingMask;
            int cullingMask2 = guiCamera.cullingMask;
            for (int i = 0; i < 32; i++)
            {
                bool num = (cullingMask & (1 << i)) != 0;
                bool flag = (cullingMask2 & (1 << i)) != 0;
                if (num && flag)
                {
                    string arg = LayerMask.LayerToName(i);
                    stringBuilder.AppendFormat("Both Main and GUI Cameras are setup to render the {0} (\"{1}\") layer. That means all objects with this Layer will be rendered twice. \n", i, arg);
                }
            }
            if (stringBuilder.Length <= 0)
            {
                return null;
            }
            return stringBuilder.ToString();
        }
    }
}
