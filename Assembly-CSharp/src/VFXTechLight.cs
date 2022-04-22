using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXTechLight : MonoBehaviour
    {
        public Light[] lights;

        public Renderer coneRenderer;

        public Renderer[] modelRenderers;

        public bool flicker;

        public bool placedByPlayer;

        public float minLightIntensity;

        public float maxLightIntensity = 0.7f;

        public float modelPow = 1f;

        public float minTime;

        public float maxTime = 0.03f;

        private float waittime;

        private float lightIntensity = 1f;

        private Color[] initialModelColors;

        private Color initialConeColor;

        private bool lightActive = true;

        private void UpdateLightFlickering()
        {
            if (waittime < 0f)
            {
                waittime = Random.Range(minTime, maxTime);
                float value = Random.value;
                lightIntensity = Mathf.SmoothStep(minLightIntensity, maxLightIntensity, value);
                float num = lightIntensity / maxLightIntensity;
                float num2 = Mathf.Pow(num, 1f / modelPow);
                for (int i = 0; i < lights.Length; i++)
                {
                    lights[i].intensity = lightIntensity;
                }
                if (coneRenderer != null)
                {
                    coneRenderer.material.SetColor(ShaderPropertyID._Color, initialConeColor * num);
                }
                for (int j = 0; j < modelRenderers.Length; j++)
                {
                    if (modelRenderers[j] != null)
                    {
                        modelRenderers[j].material.SetColor(ShaderPropertyID._GlowColor, initialModelColors[j] * num2);
                    }
                }
            }
            waittime -= Time.deltaTime;
        }

        private void Awake()
        {
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
            if (!(GetComponent<Constructable>() != null))
            {
                return;
            }
            for (int j = 0; j < modelRenderers.Length; j++)
            {
                if (modelRenderers[j] != null)
                {
                    modelRenderers[j].material.SetColor(ShaderPropertyID._GlowColor, Color.black);
                }
            }
        }

        private void Start()
        {
            if (placedByPlayer)
            {
                lightActive = false;
                SetPhysicalState(lightActive);
            }
            else
            {
                lightActive = true;
                SetPhysicalState(lightActive);
            }
        }

        private void Update()
        {
            if (lights != null && lights.Length != 0 && flicker && lightActive)
            {
                UpdateLightFlickering();
            }
        }

        private void SetPhysicalState(bool isOn)
        {
            if (coneRenderer != null)
            {
                coneRenderer.enabled = isOn;
            }
            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null)
                {
                    Color value = (isOn ? initialModelColors[i] : Color.black);
                    modelRenderers[i].material.SetColor(ShaderPropertyID._GlowColor, value);
                }
            }
        }

        public void SetLightOnOff(bool isOn)
        {
            if (lightActive != isOn)
            {
                lightActive = isOn;
                SetPhysicalState(lightActive);
            }
        }
    }
}
