using UnityEngine;
using UWE;

namespace AssemblyCSharp.UWE
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class UWEAntialiasing : ImageEffectWithEvents
    {
        private static bool antialiasingEnabled = true;

        public AAMode mode = AAMode.FXAA3Console;

        public bool showGeneratedNormals;

        public float offsetScale = 0.2f;

        public float blurRadius = 18f;

        public float edgeThresholdMin = 0.05f;

        public float edgeThreshold = 0.2f;

        public float edgeSharpness = 4f;

        public bool dlaaSharp;

        public Shader ssaaShader;

        private Material ssaa;

        public Shader dlaaShader;

        private Material dlaa;

        public Shader nfaaShader;

        private Material nfaa;

        public Shader shaderFXAAPreset2;

        private Material materialFXAAPreset2;

        public Shader shaderFXAAPreset3;

        private Material materialFXAAPreset3;

        public Shader shaderFXAAII;

        private Material materialFXAAII;

        public Shader shaderFXAAIII;

        private Material materialFXAAIII;

        public static void SetEnabled(bool enabled)
        {
            antialiasingEnabled = enabled;
            UWEAntialiasing[] array = Object.FindObjectsOfType<UWEAntialiasing>();
            for (int i = 0; i < array.Length; i++)
            {
                array[i].enabled = antialiasingEnabled;
            }
        }

        public static bool GetEnabled()
        {
            return antialiasingEnabled;
        }

        private void Awake()
        {
            base.enabled = antialiasingEnabled;
        }

        public Material CurrentAAMaterial()
        {
            Material material = null;
            return mode switch
            {
                AAMode.FXAA3Console => materialFXAAIII, 
                AAMode.FXAA2 => materialFXAAII, 
                AAMode.FXAA1PresetA => materialFXAAPreset2, 
                AAMode.FXAA1PresetB => materialFXAAPreset3, 
                AAMode.NFAA => nfaa, 
                AAMode.SSAA => ssaa, 
                AAMode.DLAA => dlaa, 
                _ => null, 
            };
        }

        public override bool CheckResources()
        {
            CheckSupport(needDepth: false);
            materialFXAAPreset2 = CreateMaterial(shaderFXAAPreset2, materialFXAAPreset2);
            materialFXAAPreset3 = CreateMaterial(shaderFXAAPreset3, materialFXAAPreset3);
            materialFXAAII = CreateMaterial(shaderFXAAII, materialFXAAII);
            materialFXAAIII = CreateMaterial(shaderFXAAIII, materialFXAAIII);
            nfaa = CreateMaterial(nfaaShader, nfaa);
            ssaa = CreateMaterial(ssaaShader, ssaa);
            dlaa = CreateMaterial(dlaaShader, dlaa);
            if (!ssaaShader.isSupported)
            {
                NotSupported();
                ReportAutoDisable();
            }
            return isSupported;
        }

        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            using (new OnRenderImageWrapper(this, source, destination))
            {
                if (!CheckResources())
                {
                    Graphics.Blit(source, destination);
                }
                else if (mode == AAMode.FXAA3Console && materialFXAAIII != null)
                {
                    materialFXAAIII.SetFloat(ShaderPropertyID._EdgeThresholdMin, edgeThresholdMin);
                    materialFXAAIII.SetFloat(ShaderPropertyID._EdgeThreshold, edgeThreshold);
                    materialFXAAIII.SetFloat(ShaderPropertyID._EdgeSharpness, edgeSharpness);
                    Graphics.Blit(source, destination, materialFXAAIII);
                }
                else if (mode == AAMode.FXAA1PresetB && materialFXAAPreset3 != null)
                {
                    Graphics.Blit(source, destination, materialFXAAPreset3);
                }
                else if (mode == AAMode.FXAA1PresetA && materialFXAAPreset2 != null)
                {
                    source.anisoLevel = 4;
                    Graphics.Blit(source, destination, materialFXAAPreset2);
                    source.anisoLevel = 0;
                }
                else if (mode == AAMode.FXAA2 && materialFXAAII != null)
                {
                    Graphics.Blit(source, destination, materialFXAAII);
                }
                else if (mode == AAMode.SSAA && ssaa != null)
                {
                    Graphics.Blit(source, destination, ssaa);
                }
                else if (mode == AAMode.DLAA && dlaa != null)
                {
                    source.anisoLevel = 0;
                    RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height);
                    Graphics.Blit(source, temporary, dlaa, 0);
                    Graphics.Blit(temporary, destination, dlaa, (!dlaaSharp) ? 1 : 2);
                    RenderTexture.ReleaseTemporary(temporary);
                }
                else if (mode == AAMode.NFAA && nfaa != null)
                {
                    source.anisoLevel = 0;
                    nfaa.SetFloat(ShaderPropertyID._OffsetScale, offsetScale);
                    nfaa.SetFloat(ShaderPropertyID._BlurRadius, blurRadius);
                    Graphics.Blit(source, destination, nfaa, showGeneratedNormals ? 1 : 0);
                }
                else
                {
                    Graphics.Blit(source, destination);
                }
            }
        }
    }
}
