using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXSpotlight : MonoBehaviour
    {
        public Renderer coneRenderer;

        public Renderer[] modelRenderers;

        private float lightIntensity = 1f;

        private Color[] initialModelColors;

        private Color initialConeColor;

        private bool initialized;

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }
            initialModelColors = new Color[modelRenderers.Length];
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null)
                {
                    initialModelColors[i] = modelRenderers[i].material.GetColor(ShaderPropertyID._GlowColor);
                }
            }
            if (coneRenderer != null)
            {
                initialConeColor = coneRenderer.sharedMaterial.GetColor(ShaderPropertyID._Color);
            }
            initialized = true;
        }

        public void SetLightActive(bool active)
        {
            Initialize();
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null)
                {
                    Color value = (active ? initialModelColors[i] : Color.black);
                    modelRenderers[i].material.SetColor(ShaderPropertyID._GlowColor, value);
                }
            }
        }
    }
}
