using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Camera))]
    public class WaterSurfaceOnCamera : MonoBehaviour
    {
        public WaterSurface waterSurface;

        private bool visible = true;

        private Camera camera;

        public float cullingDepth = 200f;

        private bool enableDepthCulling;

        private bool depthCulled;

        private bool didRenderThisFrame;

        private void Awake()
        {
            camera = GetComponent<Camera>();
        }

        public void SetVisible(bool _visible)
        {
            visible = _visible;
        }

        public bool GetVisible()
        {
            return visible;
        }

        private void OnPreRender()
        {
            waterSurface.PreRender(camera);
        }

        private void OnPreCull()
        {
            didRenderThisFrame = false;
            if (visible && ((1 << waterSurface.gameObject.layer) & camera.cullingMask) != 0)
            {
                float num = waterSurface.transform.position.y + waterSurface.waterOffset;
                depthCulled = enableDepthCulling && num - camera.transform.position.y >= cullingDepth;
                if (!depthCulled)
                {
                    didRenderThisFrame = waterSurface.RenderWaterSurface(camera);
                }
            }
        }

        private void OnPostRender()
        {
            waterSurface.DoUpdate(didRenderThisFrame);
        }
    }
}
