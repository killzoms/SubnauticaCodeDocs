using System;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Performance", "AvoidLargeStructureRule")]
    public struct GraphicsPreset
    {
        public int detail;

        public WaterSurface.Quality waterQuality;

        public int aaMode;

        public int aaQuality;

        public int aoQuality;

        public int ssrQuality;

        public bool bloom;

        public bool bloomLensDirt;

        public bool dof;

        public int motionBlurQuality;

        public bool dithering;

        private static GraphicsPreset[] presets;

        private static GraphicsPreset[] consolePresets;

        public static GraphicsPreset[] GetPresets()
        {
            if (PlatformUtils.isConsolePlatform)
            {
                return consolePresets;
            }
            return presets;
        }

        public void Apply()
        {
            GraphicsUtil.SetQualityLevel(detail);
            WaterSurface.SetQuality(waterQuality);
            UwePostProcessingManager.SetAaMode(aaMode);
            UwePostProcessingManager.SetAaQuality(aaQuality);
            UwePostProcessingManager.SetAoQuality(aoQuality);
            UwePostProcessingManager.SetSsrQuality(ssrQuality);
            UwePostProcessingManager.ToggleBloom(bloom);
            UwePostProcessingManager.ToggleBloomLensDirt(bloomLensDirt);
            UwePostProcessingManager.ToggleDof(dof);
            UwePostProcessingManager.SetMotionBlurQuality(motionBlurQuality);
            UwePostProcessingManager.ToggleDithering(dithering);
        }

        public static int GetPresetIndexForCurrentOptions()
        {
            GraphicsPreset value = default(GraphicsPreset);
            value.detail = QualitySettings.GetQualityLevel();
            value.waterQuality = WaterSurface.GetQuality();
            value.aaMode = UwePostProcessingManager.GetAaMode();
            value.aaQuality = UwePostProcessingManager.GetAaQuality();
            value.aoQuality = UwePostProcessingManager.GetAoQuality();
            value.ssrQuality = UwePostProcessingManager.GetSsrQuality();
            value.bloom = UwePostProcessingManager.GetBloomEnabled();
            value.bloomLensDirt = UwePostProcessingManager.GetBloomLensDirtEnabled();
            value.dof = UwePostProcessingManager.GetDofEnabled();
            value.motionBlurQuality = UwePostProcessingManager.GetMotionBlurQuality();
            value.dithering = UwePostProcessingManager.GetDitheringEnabled();
            return Array.IndexOf(GetPresets(), value);
        }

        static GraphicsPreset()
        {
            GraphicsPreset[] array = new GraphicsPreset[3];
            GraphicsPreset graphicsPreset = new GraphicsPreset
            {
                detail = 0,
                waterQuality = WaterSurface.RestrictQualityForSystem(WaterSurface.Quality.High),
                aaMode = 0,
                aaQuality = 1,
                ssrQuality = 0,
                aoQuality = 0,
                bloom = true,
                bloomLensDirt = false,
                motionBlurQuality = 0,
                dof = false,
                dithering = true
            };
            array[0] = graphicsPreset;
            graphicsPreset = new GraphicsPreset
            {
                detail = 1,
                waterQuality = WaterSurface.RestrictQualityForSystem(WaterSurface.Quality.High),
                aaMode = 0,
                aaQuality = 2,
                ssrQuality = 0,
                aoQuality = 1,
                bloom = true,
                bloomLensDirt = false,
                motionBlurQuality = 1,
                dof = true,
                dithering = true
            };
            array[1] = graphicsPreset;
            graphicsPreset = new GraphicsPreset
            {
                detail = 2,
                waterQuality = WaterSurface.RestrictQualityForSystem(WaterSurface.Quality.High),
                aaMode = 0,
                aaQuality = 3,
                ssrQuality = 2,
                aoQuality = 2,
                bloom = true,
                bloomLensDirt = true,
                motionBlurQuality = 2,
                dof = true,
                dithering = true
            };
            array[2] = graphicsPreset;
            presets = array;
            GraphicsPreset[] array2 = new GraphicsPreset[2];
            graphicsPreset = new GraphicsPreset
            {
                detail = 0,
                waterQuality = WaterSurface.RestrictQualityForSystem(WaterSurface.Quality.High),
                aaMode = 0,
                aaQuality = 1,
                ssrQuality = 0,
                aoQuality = 1,
                bloom = true,
                bloomLensDirt = true,
                motionBlurQuality = 0,
                dof = true,
                dithering = false
            };
            array2[0] = graphicsPreset;
            graphicsPreset = new GraphicsPreset
            {
                detail = 1,
                waterQuality = WaterSurface.RestrictQualityForSystem(WaterSurface.Quality.High),
                aaMode = 0,
                aaQuality = 1,
                ssrQuality = 0,
                aoQuality = 1,
                bloom = true,
                bloomLensDirt = true,
                motionBlurQuality = 0,
                dof = true,
                dithering = false
            };
            array2[1] = graphicsPreset;
            consolePresets = array2;
        }
    }
}
