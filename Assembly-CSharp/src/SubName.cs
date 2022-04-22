using System;
using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class SubName : MonoBehaviour
    {
        [Serializable]
        public class ColorData
        {
            public Vector3 defaultHSB;

            [HideInInspector]
            public Vector3 HSB;

            public RenderData[] renderers;
        }

        [Serializable]
        public struct RenderData
        {
            public CanvasRenderer canvasRenderer;

            public Renderer renderer;

            public int materialIndex;

            public PropertyData[] colorProperties;
        }

        [Serializable]
        public struct PropertyData
        {
            public bool isSpecular;

            public string name;

            public PropertyData(bool isSpecular, string name)
            {
                this.isSpecular = isSpecular;
                this.name = name;
            }
        }

        public delegate void OnColorsDeserialize(Vector3[] serializedHSB);

        public delegate void OnNameDeserialize(string name);

        private readonly Color specularDark = new Color(0.39f, 0.39f, 0.39f, 1f);

        private readonly Color specularLight = new Color(1f, 1f, 1f, 1f);

        public Text hullName;

        public ColorData[] rendererInfo;

        public PingInstance pingInstance;

        public OnColorsDeserialize onColorsDeserialize;

        public OnNameDeserialize onNameDeserialize;

        private int colorsInitialized;

        private void Awake()
        {
            int i = colorsInitialized;
            for (int num = rendererInfo.Length; i < num; i++)
            {
                Vector3 defaultHSB = rendererInfo[i].defaultHSB;
                SetColor(i, defaultHSB, uGUI_ColorPicker.HSBToColor(defaultHSB));
            }
        }

        private float GetLuminance(Color color)
        {
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }

        public string GetName()
        {
            return hullName.text;
        }

        public Vector3[] GetColors()
        {
            int num = rendererInfo.Length;
            Vector3[] array = new Vector3[num];
            for (int i = 0; i < num; i++)
            {
                array[i] = rendererInfo[i].HSB;
            }
            return array;
        }

        public Vector3 GetColor(int i)
        {
            if (i < rendererInfo.Length)
            {
                return rendererInfo[i].HSB;
            }
            return Vector3.zero;
        }

        public void SetName(string text)
        {
            hullName.text = text;
            if ((bool)pingInstance)
            {
                pingInstance.SetLabel(text);
            }
        }

        [SuppressMessage("Subnautica.Rules", "AvoidShaderSetByStringRule")]
        public void SetColor(int index, Vector3 hsb, Color color)
        {
            if (index >= rendererInfo.Length)
            {
                return;
            }
            ColorData obj = rendererInfo[index];
            obj.HSB = hsb;
            RenderData[] renderers = obj.renderers;
            int i = 0;
            for (int num = renderers.Length; i < num; i++)
            {
                RenderData renderData = renderers[i];
                if (renderData.renderer != null)
                {
                    Material material = renderData.renderer.materials[renderData.materialIndex];
                    if (material != null)
                    {
                        PropertyData[] colorProperties = renderData.colorProperties;
                        int j = 0;
                        for (int num2 = colorProperties.Length; j < num2; j++)
                        {
                            PropertyData propertyData = colorProperties[j];
                            string value = propertyData.name;
                            if (!string.IsNullOrEmpty(value))
                            {
                                if (propertyData.isSpecular)
                                {
                                    float luminance = GetLuminance(color);
                                    Color b = Color.Lerp(specularDark, specularLight, luminance);
                                    Color value2 = Color.Lerp(color, b, 1f - 0.5f * luminance);
                                    material.SetColor(value, value2);
                                }
                                else
                                {
                                    material.SetColor(value, color);
                                }
                            }
                        }
                    }
                }
                if (renderData.canvasRenderer != null)
                {
                    Graphic[] components = renderData.canvasRenderer.GetComponents<Graphic>();
                    int k = 0;
                    for (int num3 = components.Length; k < num3; k++)
                    {
                        components[k].color = color;
                    }
                }
            }
        }

        public void DeserializeName(string name)
        {
            SetName(name);
            if (onNameDeserialize != null)
            {
                onNameDeserialize(name);
            }
        }

        public void DeserializeColors(Vector3[] serializedHSB)
        {
            int num = (colorsInitialized = Mathf.Min(serializedHSB.Length, rendererInfo.Length));
            for (int i = 0; i < num; i++)
            {
                Vector3 hsb = serializedHSB[i];
                Color color = uGUI_ColorPicker.HSBToColor(hsb);
                SetColor(i, hsb, color);
            }
            if (onColorsDeserialize != null)
            {
                onColorsDeserialize(serializedHSB);
            }
        }
    }
}
