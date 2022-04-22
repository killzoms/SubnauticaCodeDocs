using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_GraphicRaycaster : GraphicRaycaster
    {
        public bool guiCameraSpace;

        public override Camera eventCamera
        {
            get
            {
                if (SNCameraRoot.main != null)
                {
                    if (!guiCameraSpace)
                    {
                        return SNCameraRoot.main.mainCamera;
                    }
                    return SNCameraRoot.main.guiCamera;
                }
                return MainCamera.camera;
            }
        }
    }
}
