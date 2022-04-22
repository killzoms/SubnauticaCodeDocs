using UnityEngine;

namespace AssemblyCSharp
{
    [AddComponentMenu("Camera-Control/Mouse Look")]
    public class MouseLook : MonoBehaviour
    {
        public enum RotationAxes
        {
            MouseXAndY,
            MouseX,
            MouseY
        }

        public RotationAxes axes;

        public float sensitivityX = 15f;

        public float sensitivityY = 15f;

        public float minimumX = -360f;

        public float maximumX = 360f;

        public float minimumY = -60f;

        public float maximumY = 60f;

        public bool mouseLookEnabled = true;

        public bool invertY;

        private float rotationY;

        public void LayoutEscapeMenuGUI()
        {
            if (axes == RotationAxes.MouseY)
            {
                invertY = GUILayout.Toggle(invertY, "Invert Mouse Y");
            }
        }

        private void Update()
        {
            bool num = mouseLookEnabled && AvatarInputHandler.main.IsEnabled();
            float num2 = (num ? Input.GetAxisRaw("Mouse X") : 0f);
            float num3 = (num ? Input.GetAxisRaw("Mouse Y") : 0f);
            if (invertY)
            {
                num3 *= -1f;
            }
            if (axes == RotationAxes.MouseXAndY)
            {
                float y = base.transform.localEulerAngles.y + num2 * sensitivityX;
                rotationY += num3 * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                base.transform.localEulerAngles = new Vector3(0f - rotationY, y, 0f);
            }
            else if (axes == RotationAxes.MouseX)
            {
                base.transform.Rotate(0f, num2 * sensitivityX, 0f);
            }
            else
            {
                rotationY += num3 * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                base.transform.localEulerAngles = new Vector3(0f - rotationY, base.transform.localEulerAngles.y, 0f);
            }
        }

        public void SetEnabled(bool val)
        {
            mouseLookEnabled = val;
        }

        private void Start()
        {
            if ((bool)GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().freezeRotation = true;
            }
        }
    }
}
