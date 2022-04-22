using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AssemblyCSharp
{
    public static class MaterialExtensions
    {
        public const string keywordAlphaPremultiply = "ALPHA_PREMULTIPLY";

        private static List<Material> materials = new List<Material>();

        private static int GetMaterialCount(Renderer renderer)
        {
            renderer.GetSharedMaterials(materials);
            int count = materials.Count;
            materials.Clear();
            return count;
        }

        public static Renderer[] AssignMaterial(GameObject go, Material material)
        {
            Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                int materialCount = GetMaterialCount(componentsInChildren[i]);
                Material[] array = new Material[materialCount];
                for (int j = 0; j < materialCount; j++)
                {
                    array[j] = material;
                }
                componentsInChildren[i].materials = array;
            }
            return componentsInChildren;
        }

        public static void SetColor(Renderer[] renderers, int propertyID, Color color)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].GetMaterials(materials);
                    for (int j = 0; j < materials.Count; j++)
                    {
                        materials[j].SetColor(propertyID, color);
                    }
                }
            }
            materials.Clear();
        }

        public static void SetFloat(Renderer[] renderers, int propertyID, float value)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].GetMaterials(materials);
                    for (int j = 0; j < materials.Count; j++)
                    {
                        materials[j].SetFloat(propertyID, value);
                    }
                }
            }
            materials.Clear();
        }

        public static void SetTexture(Renderer[] renderers, int propertyID, Texture texture)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].GetMaterials(materials);
                    for (int j = 0; j < materials.Count; j++)
                    {
                        materials[j].SetTexture(propertyID, texture);
                    }
                }
            }
            materials.Clear();
        }

        public static void SetBlending(Material material, Blending blending, bool alphaPremultiply)
        {
            if (!(material == null))
            {
                switch (blending)
                {
                    case Blending.Additive:
                        SetBlendMode(material, BlendMode.One, BlendMode.One, alphaPremultiply);
                        break;
                    case Blending.Multiplicative:
                        SetBlendMode(material, BlendMode.DstColor, BlendMode.OneMinusSrcAlpha, alphaPremultiply);
                        break;
                    default:
                        SetBlendMode(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha, alphaPremultiply);
                        break;
                }
            }
        }

        public static void SetBlendMode(Material material, BlendMode srcFactor, BlendMode dstFactor, bool alphaPremultiply)
        {
            if (!(material == null))
            {
                material.SetInt(ShaderPropertyID._SrcFactor, (int)srcFactor);
                material.SetInt(ShaderPropertyID._DstFactor, (int)dstFactor);
                SetKeyword(material, "ALPHA_PREMULTIPLY", alphaPremultiply);
            }
        }

        public static void SetKeyword(Material material, string keyword, bool state)
        {
            if (!(material == null) && material.IsKeywordEnabled(keyword) != state)
            {
                if (state)
                {
                    material.EnableKeyword(keyword);
                }
                else
                {
                    material.DisableKeyword(keyword);
                }
            }
        }
    }
}
