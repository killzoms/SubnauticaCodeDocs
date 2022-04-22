using UnityEngine;

namespace AssemblyCSharp
{
    public class AvatarInputHandler : MonoBehaviour
    {
        public static AvatarInputHandler main;

        public bool clicked;

        private void Awake()
        {
            main = this;
        }

        private void OnEnable()
        {
            if (!Application.isEditor || clicked)
            {
                global::UWE.Utils.lockCursor = true;
            }
        }

        private void OnDisable()
        {
        }

        private void Update()
        {
            FPSInputModule.current.EscapeMenu();
            if (Input.GetMouseButtonDown(0) && GUIUtility.hotControl == 0)
            {
                clicked = true;
                global::UWE.Utils.lockCursor = true;
            }
        }

        public bool IsEnabled()
        {
            if (base.gameObject.activeInHierarchy)
            {
                if (!global::UWE.Utils.lockCursor)
                {
                    return PlatformUtils.isConsolePlatform;
                }
                return true;
            }
            return false;
        }
    }
}
