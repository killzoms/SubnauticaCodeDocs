using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public sealed class CrafterGhostModel : MonoBehaviour
    {
        private static List<Renderer> sGhostRenderers = new List<Renderer>();

        private const string dontRenderShaderName = "DontRender";

        public Transform itemSpawnPoint;

        public Texture _EmissiveTex;

        public Texture _NoiseTex;

        private GameObject ghostModel;

        private VFXFabricating boundsToVFX;

        private List<Material> ghostMaterials = new List<Material>();

        private bool ghostModelWasInactive;

        public void UpdateModel(TechType techType)
        {
            for (int num = ghostMaterials.Count - 1; num >= 0; num--)
            {
                Material material = ghostMaterials[num];
                if (material != null)
                {
                    Object.Destroy(material);
                }
            }
            ghostMaterials.Clear();
            boundsToVFX = null;
            if (ghostModel != null)
            {
                Object.Destroy(ghostModel);
                ghostModel = null;
            }
            if (techType == TechType.None)
            {
                return;
            }
            ghostModel = GetGhostModel(techType);
            if (!(ghostModel != null))
            {
                return;
            }
            GenericLoot.SetLayer(ghostModel);
            SkyApplier skyApplier = ghostModel.AddComponent<SkyApplier>();
            skyApplier.anchorSky = Skies.BaseInterior;
            ghostModel.SetActive(value: true);
            ghostModel.transform.parent = itemSpawnPoint;
            global::UWE.Utils.SetIsKinematic(ghostModel, isKinematic: true);
            global::UWE.Utils.ZeroTransform(ghostModel.transform);
            boundsToVFX = ghostModel.GetComponent<VFXFabricating>();
            if (boundsToVFX != null)
            {
                boundsToVFX.enabled = true;
                ghostModel.transform.localPosition = boundsToVFX.posOffset;
                ghostModel.transform.localEulerAngles = boundsToVFX.eulerOffset;
                ghostModel.transform.localScale *= boundsToVFX.scaleFactor;
            }
            ghostModel.GetComponentsInChildren(includeInactive: true, sGhostRenderers);
            skyApplier.renderers = sGhostRenderers.ToArray();
            for (int i = 0; i < sGhostRenderers.Count; i++)
            {
                Material[] materials = sGhostRenderers[i].materials;
                foreach (Material material2 in materials)
                {
                    if (material2 != null)
                    {
                        ghostMaterials.Add(material2);
                        if (material2.shader != null && material2.shader.name != "DontRender")
                        {
                            material2.EnableKeyword("FX_BUILDING");
                            material2.SetTexture(ShaderPropertyID._EmissiveTex, _EmissiveTex);
                            material2.SetFloat(ShaderPropertyID._Cutoff, 0.4f);
                            material2.SetColor(ShaderPropertyID._BorderColor, new Color(0.7f, 0.7f, 1f, 1f));
                            material2.SetFloat(ShaderPropertyID._Built, 0f);
                            material2.SetFloat(ShaderPropertyID._Cutoff, 0.42f);
                            material2.SetVector(ShaderPropertyID._BuildParams, new Vector4(2f, 0.7f, 3f, -0.25f));
                            material2.SetFloat(ShaderPropertyID._NoiseStr, 0.25f);
                            material2.SetFloat(ShaderPropertyID._NoiseThickness, 0.49f);
                            material2.SetFloat(ShaderPropertyID._BuildLinear, 1f);
                            material2.SetFloat(ShaderPropertyID._MyCullVariable, 0f);
                        }
                    }
                }
            }
            sGhostRenderers.Clear();
            Shader.SetGlobalFloat(ShaderPropertyID._SubConstructProgress, 0f);
        }

        public void UpdateProgress(float progress)
        {
            if (ghostModel == null)
            {
                return;
            }
            float value = Mathf.Clamp01(progress);
            float value2 = 0f;
            float value3 = 0f;
            if (boundsToVFX != null)
            {
                value2 = boundsToVFX.minY;
                value3 = boundsToVFX.maxY;
            }
            for (int i = 0; i < ghostMaterials.Count; i++)
            {
                Material material = ghostMaterials[i];
                if (material.shader.name != "DontRender")
                {
                    material.SetFloat(ShaderPropertyID._Built, value);
                    material.SetFloat(ShaderPropertyID._minYpos, value2);
                    material.SetFloat(ShaderPropertyID._maxYpos, value3);
                }
            }
        }

        private static GameObject GetGhostModel(TechType techType)
        {
            GameObject result = null;
            bool flag = false;
            GameObject gameObject = CraftData.GetPrefabForTechType(techType, verbose: false);
            if (gameObject == null)
            {
                flag = true;
            }
            else
            {
                Pickupable component = gameObject.GetComponent<Pickupable>();
                if (component != null && component.cubeOnPickup)
                {
                    flag = true;
                }
            }
            if (flag)
            {
                gameObject = Utils.genericLootPrefab;
            }
            Constructable component2 = gameObject.GetComponent<Constructable>();
            if (component2 != null && component2.model != null)
            {
                result = Object.Instantiate(component2.model);
            }
            else
            {
                VFXFabricating componentInChildren = gameObject.GetComponentInChildren<VFXFabricating>(includeInactive: true);
                if (componentInChildren != null)
                {
                    result = Object.Instantiate(componentInChildren.gameObject);
                }
            }
            return result;
        }
    }
}
