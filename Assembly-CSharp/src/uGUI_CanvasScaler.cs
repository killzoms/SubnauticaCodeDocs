using System;
using UnityEngine;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(RectTransform))]
    public class uGUI_CanvasScaler : MonoBehaviour
    {
        public enum Mode
        {
            Static,
            Inversed,
            Parented,
            World
        }

        private const float positionEpsilon = 0.0001f;

        private const float rotationThreshold = 0.9961947f;

        private const float logBase = 2f;

        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        public Mode mode;

        public Mode vrMode;

        public float distance = 1f;

        private RectTransform _rectTransform;

        private Canvas _canvas;

        private float _scaleX = -1f;

        private float _scaleY = -1f;

        private float _width = -1f;

        private float _height = -1f;

        private bool isDirty = true;

        private float prevScaleFactor = 1f;

        private Transform _anchor;

        private Vector3 _spawnPosition;

        private Quaternion _spawnRotation;

        private Vector3 lastPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        private Quaternion lastRotation = new Quaternion(float.MinValue, float.MinValue, float.MinValue, float.MinValue);

        private RectTransform rectTransform
        {
            get
            {
                if (!(_rectTransform != null))
                {
                    return _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }

        private Canvas canvas
        {
            get
            {
                if (!(_canvas != null))
                {
                    return _canvas = GetComponent<Canvas>();
                }
                return _canvas;
            }
        }

        private Mode currentMode
        {
            get
            {
                if (!XRSettings.enabled)
                {
                    return mode;
                }
                return vrMode;
            }
        }

        private void OnEnable()
        {
            ManagedUpdate.Queue queue = ManagedUpdate.Queue.LateUpdateAfterInput;
            switch (currentMode)
            {
                case Mode.Inversed:
                case Mode.Parented:
                    queue = ManagedUpdate.Queue.Canvas;
                    break;
            }
            ManagedUpdate.Subscribe(queue, OnUpdate);
            ManagedCanvasUpdate.AddUICameraChangeListener(OnUICameraChange);
            SetAnchor();
        }

        private void OnDisable()
        {
            ManagedUpdate.Unsubscribe(OnUpdate);
            ManagedCanvasUpdate.RemoveUICameraChangeListener(OnUICameraChange);
            isDirty = true;
            SetScaleFactor(1f);
        }

        private void OnUpdate()
        {
            if (!isDirty)
            {
                return;
            }
            Camera camera = MainCamera.camera;
            if (camera != null)
            {
                UpdateTransform(camera);
                UpdateFrustum(camera);
                switch (currentMode)
                {
                    case Mode.Static:
                        isDirty = false;
                        break;
                    case Mode.World:
                        isDirty = false;
                        break;
                    default:
                        isDirty = false;
                        break;
                    case Mode.Inversed:
                    case Mode.Parented:
                        break;
                }
            }
        }

        private void OnUICameraChange(Camera camera)
        {
            switch (currentMode)
            {
                case Mode.Static:
                    isDirty = true;
                    break;
                case Mode.Inversed:
                case Mode.Parented:
                case Mode.World:
                    break;
            }
        }

        private void UpdateTransform(Camera cam)
        {
            Transform component = cam.GetComponent<Transform>();
            Mode mode = currentMode;
            Vector3 vector = Vector3.zero;
            Quaternion quaternion = Quaternion.identity;
            switch (mode)
            {
                case Mode.Static:
                {
                    Camera uICamera = ManagedCanvasUpdate.GetUICamera();
                    if (uICamera != null)
                    {
                        Transform transform2 = uICamera.transform;
                        vector = transform2.position + transform2.forward * distance;
                        quaternion = transform2.rotation;
                    }
                    else
                    {
                        vector = new Vector3(0f, 0f, distance);
                        quaternion = Quaternion.identity;
                    }
                    break;
                }
                case Mode.World:
                {
                    Transform transform = ((cam.transform.parent != null) ? cam.transform.parent : cam.transform);
                    if (transform != null)
                    {
                        vector = transform.position + transform.forward * distance;
                        quaternion = transform.rotation;
                    }
                    else
                    {
                        vector = new Vector3(0f, 0f, distance);
                        quaternion = Quaternion.identity;
                    }
                    break;
                }
                case Mode.Inversed:
                {
                    Vector3 vector3;
                    Quaternion quaternion3;
                    if (_anchor != null)
                    {
                        vector3 = _anchor.position;
                        quaternion3 = _anchor.rotation;
                    }
                    else
                    {
                        vector3 = _spawnPosition;
                        quaternion3 = _spawnRotation;
                    }
                    Vector3 vector4 = vector3 - component.position;
                    Quaternion quaternion4 = Quaternion.Inverse(component.rotation);
                    vector = quaternion4 * vector4;
                    quaternion = quaternion4 * quaternion3;
                    break;
                }
                case Mode.Parented:
                {
                    Vector3 vector2 = _spawnPosition - component.localPosition;
                    Quaternion quaternion2 = Quaternion.Inverse(component.localRotation);
                    vector = quaternion2 * vector2;
                    quaternion = quaternion2 * _spawnRotation;
                    break;
                }
            }
            bool flag = true;
            if (!XRSettings.enabled || (mode != Mode.Inversed && mode != Mode.Parented))
            {
                float sqrMagnitude = (vector - lastPosition).sqrMagnitude;
                float num = Quaternion.Dot(quaternion, lastRotation);
                flag = sqrMagnitude > 9.999999E-09f || num < 0.9961947f;
            }
            if (flag)
            {
                lastPosition = vector;
                lastRotation = quaternion;
                rectTransform.SetPositionAndRotation(vector, quaternion);
            }
        }

        private void UpdateFrustum(Camera cam)
        {
            if (currentMode != Mode.Inversed || !(_anchor != null))
            {
                Vector2 screenSize = GraphicsUtil.GetScreenSize();
                float num = screenSize.x / screenSize.y;
                float num2 = distance * Mathf.Tan(cam.fieldOfView * 0.5f * ((float)Math.PI / 180f));
                float num3 = num2 * 2f * num;
                if (XRSettings.enabled)
                {
                    float num4 = 0.1f;
                    num3 *= 1f + num4;
                    num2 *= 1f + num4;
                }
                float num5 = num3 / screenSize.x;
                float num6 = num2 / screenSize.y * 2f;
                float a = screenSize.x / referenceResolution.x;
                float b = screenSize.y / referenceResolution.y;
                float num7 = Mathf.Min(a, b);
                float num8 = 1f / num7;
                float num9 = screenSize.x * num8;
                float num10 = screenSize.y * num8;
                float num11 = num5 * num7;
                float num12 = num6 * num7;
                if (_width != num9)
                {
                    _width = num9;
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
                }
                if (_height != num10)
                {
                    _height = num10;
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _height);
                }
                if (_scaleX != num11 || _scaleY != num12)
                {
                    _scaleX = num11;
                    _scaleY = num12;
                    rectTransform.localScale = new Vector3(_scaleX, _scaleY, _scaleX);
                }
                SetScaleFactor(num7);
            }
        }

        private void SetScaleFactor(float scaleFactor)
        {
            if (prevScaleFactor != scaleFactor)
            {
                prevScaleFactor = scaleFactor;
                canvas.scaleFactor = scaleFactor;
            }
        }

        public void SetAnchor(Transform anchor)
        {
            _anchor = anchor;
        }

        public void SetAnchor()
        {
            Camera camera = MainCamera.camera;
            if (!(camera == null))
            {
                Transform transform = camera.transform;
                if (currentMode == Mode.Parented)
                {
                    _spawnPosition = transform.localPosition + transform.localRotation * Vector3.forward * distance;
                    _spawnRotation = transform.localRotation;
                }
                else
                {
                    _spawnPosition = transform.position + transform.forward * distance;
                    _spawnRotation = transform.rotation;
                }
            }
        }
    }
}
