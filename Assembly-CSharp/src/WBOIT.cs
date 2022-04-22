using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace AssemblyCSharp
{
    [ExecuteInEditMode]
    public class WBOIT : PostEffectsBase
    {
        [AssertNotNull]
        [SerializeField]
        private Camera camera;

        [AssertNotNull]
        [SerializeField]
        private Camera guiCamera;

        private RenderBuffer[] colorBuffers;

        private RenderTexture wboitTexture0;

        private RenderTexture wboitTexture1;

        private RenderTexture wboitTexture2;

        public Shader compositeShader;

        private Material compositeMaterial;

        public float temperatureScalar;

        public Texture2D temperatureRefractTex;

        private bool temperatureRefractEnabled;

        private int texAPropertyID = -1;

        private int texBPropertyID = -1;

        private int weightTogglePropertyID = -1;

        private int weightSharpnessPropertyID = -1;

        private int temperatureTexPropertyID = -1;

        private int temperaturePropertyID = -1;

        public bool useDepthWeighting = true;

        public float depthWeightingSharpness = 0.1f;

        private float nextTemperatureUpdate;

        public bool debug;

        public override bool CheckResources()
        {
            return true;
        }

        private void Awake()
        {
            compositeMaterial = new Material(compositeShader);
            InitProperties();
        }

        private void OnDestroy()
        {
            DestroyRenderTargets();
        }

        public Texture GetTextureA()
        {
            return wboitTexture1;
        }

        public Texture GetTextureB()
        {
            return wboitTexture2;
        }

        private void CreateRenderTargets()
        {
            wboitTexture0 = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            wboitTexture0.name = "WBOIT Tex0";
            wboitTexture1 = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            wboitTexture1.name = "WBOIT TexA";
            wboitTexture2 = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            wboitTexture2.name = "WBOIT TexB";
            compositeMaterial.SetTexture(texAPropertyID, wboitTexture1);
            compositeMaterial.SetTexture(texBPropertyID, wboitTexture2);
            colorBuffers = new RenderBuffer[3] { wboitTexture0.colorBuffer, wboitTexture1.colorBuffer, wboitTexture2.colorBuffer };
        }

        private void DestroyRenderTargets()
        {
            if ((bool)camera)
            {
                camera.SetTargetBuffers(new RenderBuffer[1], default(RenderBuffer));
            }
            if ((bool)guiCamera)
            {
                guiCamera.SetTargetBuffers(new RenderBuffer[1], default(RenderBuffer));
            }
            colorBuffers = null;
            if (wboitTexture0 != null)
            {
                wboitTexture0.Release();
                wboitTexture0 = null;
            }
            if (wboitTexture1 != null)
            {
                wboitTexture1.Release();
                wboitTexture1 = null;
            }
            if (wboitTexture2 != null)
            {
                wboitTexture2.Release();
                wboitTexture2 = null;
            }
        }

        private void UpdateGlobalShaderParameters()
        {
            Shader.SetGlobalFloat(weightTogglePropertyID, useDepthWeighting ? 1f : 0f);
            Shader.SetGlobalFloat(weightSharpnessPropertyID, depthWeightingSharpness);
        }

        private void UpdateMaterialShaderParameters()
        {
            if (!debug && Time.time > nextTemperatureUpdate)
            {
                float temperature = GetTemperature();
                temperatureScalar = Mathf.Clamp01((temperature - 40f) / 30f);
                nextTemperatureUpdate = Time.time + Random.value;
            }
            if (temperatureScalar > 0f && temperatureRefractTex != null)
            {
                if (!temperatureRefractEnabled)
                {
                    compositeMaterial.EnableKeyword("FX_TEMPERATURE_REFRACT");
                    temperatureRefractEnabled = true;
                }
                compositeMaterial.SetTexture(temperatureTexPropertyID, temperatureRefractTex);
                compositeMaterial.SetFloat(temperaturePropertyID, temperatureScalar);
            }
            else if (temperatureRefractEnabled)
            {
                compositeMaterial.DisableKeyword("FX_TEMPERATURE_REFRACT");
                temperatureRefractEnabled = false;
            }
        }

        private void InitProperties()
        {
            texAPropertyID = Shader.PropertyToID("_WBOIT_texA");
            texBPropertyID = Shader.PropertyToID("_WBOIT_texB");
            weightTogglePropertyID = Shader.PropertyToID("_WBOIT_WeightToggle");
            weightSharpnessPropertyID = Shader.PropertyToID("_WBOIT_WeightSharpness");
            temperatureTexPropertyID = Shader.PropertyToID("_TemperatureTex");
            temperaturePropertyID = Shader.PropertyToID("_TemperatureScalar");
        }

        private float GetTemperature()
        {
            WaterTemperatureSimulation main = WaterTemperatureSimulation.main;
            if (!(main != null))
            {
                return 0f;
            }
            return main.GetTemperature(Utils.GetLocalPlayerPos());
        }

        private void OnPreRender()
        {
            if (wboitTexture1 != null && (camera.pixelWidth != wboitTexture1.width || camera.pixelHeight != wboitTexture1.height))
            {
                DestroyRenderTargets();
            }
            if (wboitTexture1 == null || colorBuffers == null)
            {
                CreateRenderTargets();
            }
            RenderBuffer depthBuffer = wboitTexture1.depthBuffer;
            camera.SetTargetBuffers(colorBuffers, depthBuffer);
            guiCamera.SetTargetBuffers(colorBuffers, depthBuffer);
            Graphics.SetRenderTarget(wboitTexture1);
            GL.Clear(clearDepth: false, clearColor: true, new Color(0f, 0f, 0f, 1f));
            Graphics.SetRenderTarget(wboitTexture2);
            GL.Clear(clearDepth: false, clearColor: true, new Color(0f, 0f, 0f, 1f));
            UpdateGlobalShaderParameters();
            UpdateMaterialShaderParameters();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            Graphics.Blit(src, dst, compositeMaterial);
        }
    }
}
