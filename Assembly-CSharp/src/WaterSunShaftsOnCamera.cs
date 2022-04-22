using System;
using Gendarme;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Camera))]
    public class WaterSunShaftsOnCamera : ImageEffectWithEvents
    {
        private enum Pass
        {
            SunShafts,
            Combine
        }

        public WaterSurface surface;

        public Shader shader;

        private Material material;

        public float maxDistance = 15f;

        public float shaftsScale = 0.05f;

        public float startDistance = 5f;

        public int reduction = 2;

        public float intensity = 0.003f;

        public float traceStepSize = 0.05f;

        public int consoleReduction = 4;

        public float consoleTraceStepSize = 0.1f;

        [Tooltip("Toggle light rays")]
        public bool eventsOnly;

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        private void Awake()
        {
            material = new Material(shader);
            material.hideFlags = HideFlags.HideAndDontSave;
            GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
            GraphicsUtil.onQualityLevelChanged = (GraphicsUtil.OnQualityLevelChanged)Delegate.Combine(GraphicsUtil.onQualityLevelChanged, new GraphicsUtil.OnQualityLevelChanged(OnQualityLevelChanged));
            UpdateForQuality();
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        private void OnDestroy()
        {
            GraphicsUtil.onQualityLevelChanged = (GraphicsUtil.OnQualityLevelChanged)Delegate.Remove(GraphicsUtil.onQualityLevelChanged, new GraphicsUtil.OnQualityLevelChanged(OnQualityLevelChanged));
        }

        private void OnQualityLevelChanged()
        {
            UpdateForQuality();
        }

        private void UpdateForQuality()
        {
            base.enabled = QualitySettings.shadows != ShadowQuality.Disable;
        }

        public override bool CheckResources()
        {
            return true;
        }

        public int GetCascadeIndex()
        {
            if (QualitySettings.shadowCascades == 2)
            {
                float num = QualitySettings.shadowCascade2Split * QualitySettings.shadowDistance;
                if (maxDistance > num)
                {
                    return 1;
                }
            }
            else if (QualitySettings.shadowCascades == 4)
            {
                Vector3 vector = QualitySettings.shadowCascade4Split * QualitySettings.shadowDistance;
                if (maxDistance > vector.z)
                {
                    return 3;
                }
                if (maxDistance > vector.y)
                {
                    return 2;
                }
                if (maxDistance > vector.x)
                {
                    return 1;
                }
            }
            return 0;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            using (new OnRenderImageWrapper(this, source, destination))
            {
                if (!eventsOnly)
                {
                    int cascadeIndex = GetCascadeIndex();
                    float value = (PlatformUtils.isConsolePlatform ? consoleTraceStepSize : traceStepSize);
                    int num = (PlatformUtils.isConsolePlatform ? consoleReduction : reduction);
                    if (surface != null)
                    {
                        material.SetTexture(ShaderPropertyID._CausticsTexture, surface.GetCausticsTexture());
                    }
                    material.SetFloat(ShaderPropertyID._StartDistance, startDistance);
                    material.SetFloat(ShaderPropertyID._MaxDistance, maxDistance);
                    material.SetFloat(ShaderPropertyID._ShaftsScale, shaftsScale);
                    material.SetFloat(ShaderPropertyID._Intensity, intensity);
                    material.SetFloat(ShaderPropertyID._LightTraceStep, Mathf.Clamp(value, 0.001f, 1f));
                    Matrix4x4 cameraToWorldMatrix = MainCamera.camera.cameraToWorldMatrix;
                    material.SetMatrix(ShaderPropertyID._CameraToWorldMatrix, cameraToWorldMatrix);
                    material.SetInt(ShaderPropertyID._CascadeIndex, cascadeIndex);
                    Matrix4x4 value2 = surface.sunLight.transform.worldToLocalMatrix * cameraToWorldMatrix;
                    material.SetMatrix(ShaderPropertyID._CameraToCausticsMatrix, value2);
                    RenderTexture temporary = RenderTexture.GetTemporary(destination.width / num, destination.height / num, 0);
                    Graphics.Blit(source, temporary, material, 0);
                    material.SetTexture(ShaderPropertyID._OriginalTex, source);
                    Graphics.Blit(temporary, destination, material, 1);
                    RenderTexture.ReleaseTemporary(temporary);
                }
                else
                {
                    Graphics.Blit(source, destination);
                }
            }
        }
    }
}
