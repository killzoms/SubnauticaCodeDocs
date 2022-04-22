using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Camera))]
    public class WaterscapeVolumeOnCamera : ImageEffectWithEvents
    {
        public WaterscapeVolume settings;

        private Camera camera;

        private bool visible = true;

        public void SetVisible(bool _visible)
        {
            visible = _visible;
        }

        public bool GetVisible()
        {
            return visible;
        }

        private void Awake()
        {
            camera = GetComponent<Camera>();
            camera.depthTextureMode |= DepthTextureMode.Depth;
        }

        private void OnPreRender()
        {
            if (GetShouldRender())
            {
                settings.PreRender(camera);
            }
        }

        private void OnPostRender()
        {
            settings.PostRender(camera);
        }

        public override bool CheckResources()
        {
            return true;
        }

        private bool GetShouldRender()
        {
            if (visible)
            {
                return ((1 << settings.gameObject.layer) & camera.cullingMask) != 0;
            }
            return false;
        }

        [ImageEffectOpaque]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            using (new OnRenderImageWrapper(this, source, destination))
            {
                if (GetShouldRender())
                {
                    bool cameraInside = Player.main != null && Player.main.IsInside();
                    if (MainCameraControl.main != null && MainCameraControl.main.GetComponent<FreecamController>().GetActive())
                    {
                        cameraInside = false;
                    }
                    settings.RenderImage(camera, cameraInside, source, destination);
                }
                else
                {
                    Graphics.Blit(source, destination);
                }
            }
        }
    }
}
