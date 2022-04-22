using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AssemblyCSharp
{
    public class VFXOverlayMaterial : MonoBehaviour
    {
        private struct TrackedRenderer
        {
            public Renderer renderer;

            public int numSubMeshes;

            public bool visible;
        }

        public bool debug;

        private List<TrackedRenderer> trackedRenderers = new List<TrackedRenderer>();

        private bool registeredToRender;

        private float duration = -1f;

        private float lerpValue;

        private bool destroyMaterial;

        private Color initColor;

        private Color startColor;

        private Color targetColor;

        public Material material;

        private CommandBuffer buf;

        private Camera mainCamera;

        private bool destroyOnFadeComplete;

        private void Update()
        {
            RefreshCommandBuffer();
            if (material != null && duration > 0f)
            {
                lerpValue += Time.deltaTime / duration;
                if (lerpValue > 1f)
                {
                    Object.Destroy(this);
                }
                else
                {
                    material.color = Color.Lerp(startColor, targetColor, lerpValue);
                }
            }
        }

        private void SetRenderers(Renderer[] rends)
        {
            trackedRenderers = new List<TrackedRenderer>();
            TrackedRenderer item = default(TrackedRenderer);
            foreach (Renderer renderer in rends)
            {
                if (renderer.GetComponent<WaterClipProxy>() != null || renderer.GetComponent<VFXIgnoreOverlayMaterial>() != null)
                {
                    continue;
                }
                item.renderer = renderer;
                item.visible = false;
                if (renderer.GetType() == typeof(SkinnedMeshRenderer))
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)renderer;
                    if (skinnedMeshRenderer.sharedMesh != null)
                    {
                        item.numSubMeshes = skinnedMeshRenderer.sharedMesh.subMeshCount;
                        trackedRenderers.Add(item);
                    }
                }
                else
                {
                    MeshFilter component = renderer.GetComponent<MeshFilter>();
                    if (component != null && component.sharedMesh != null)
                    {
                        item.numSubMeshes = component.sharedMesh.subMeshCount;
                        trackedRenderers.Add(item);
                    }
                }
            }
        }

        public void OnDestroy()
        {
            if (destroyMaterial)
            {
                Object.Destroy(material);
            }
            UnregisterFromRender();
        }

        public void OnDisable()
        {
            trackedRenderers.Clear();
            UnregisterFromRender();
        }

        private void RegisterToRender()
        {
            if (!registeredToRender)
            {
                mainCamera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, buf);
                registeredToRender = true;
            }
        }

        private void UnregisterFromRender()
        {
            if (registeredToRender)
            {
                if ((bool)mainCamera)
                {
                    mainCamera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, buf);
                }
                registeredToRender = false;
            }
        }

        private bool VisibleRenderersChanged()
        {
            bool result = false;
            for (int i = 0; i < trackedRenderers.Count; i++)
            {
                TrackedRenderer value = trackedRenderers[i];
                if (value.renderer == null)
                {
                    trackedRenderers.RemoveFast(i);
                    result = true;
                    i--;
                    continue;
                }
                bool flag = value.renderer.gameObject.activeInHierarchy && value.renderer.isVisible;
                if (flag != value.visible)
                {
                    result = true;
                    value.visible = flag;
                    trackedRenderers[i] = value;
                }
            }
            return result;
        }

        protected void RefreshCommandBuffer()
        {
            if (buf == null || !VisibleRenderersChanged())
            {
                return;
            }
            buf.Clear();
            for (int i = 0; i < trackedRenderers.Count; i++)
            {
                TrackedRenderer trackedRenderer = trackedRenderers[i];
                if (trackedRenderer.visible)
                {
                    Renderer renderer = trackedRenderer.renderer;
                    int numSubMeshes = trackedRenderer.numSubMeshes;
                    if (debug)
                    {
                        Debug.Log(renderer);
                    }
                    for (int j = 0; j < numSubMeshes; j++)
                    {
                        buf.DrawRenderer(renderer, material, j, -1);
                    }
                }
            }
        }

        public void ApplyAndForgetOverlay(Material mat, string commandBufferName, Color lerpToColor, float lifeTime)
        {
            duration = lifeTime;
            startColor = mat.color;
            targetColor = lerpToColor;
            ApplyOverlay(mat, commandBufferName, instantiateMaterial: true);
        }

        public void ApplyOverlay(Material mat, string commandBufferName, bool instantiateMaterial, Renderer[] rends = null)
        {
            if (rends == null)
            {
                rends = GetComponentsInChildren<Renderer>();
            }
            SetRenderers(rends);
            initColor = mat.color;
            _ = MainCamera.camera;
            if (instantiateMaterial)
            {
                material = new Material(mat);
                destroyMaterial = true;
            }
            else
            {
                material = mat;
            }
            buf = new CommandBuffer();
            buf.name = commandBufferName;
            mainCamera = SNCameraRoot.main.mainCam;
            RegisterToRender();
        }

        public void RemoveOverlay()
        {
            Object.Destroy(this);
        }
    }
}
