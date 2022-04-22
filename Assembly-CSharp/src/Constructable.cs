using System;
using System.Collections.Generic;
using AssemblyCSharp.Story;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    [ProtoInclude(3100, typeof(ConstructableBase))]
    [ProtoInclude(3200, typeof(BasePowerDistributor))]
    [ProtoInclude(3201, typeof(BaseSpotLight))]
    [ProtoInclude(3202, typeof(BasePipeConnector))]
    public class Constructable : HandTarget, IProtoEventListener, IConstructable, IObstacle
    {
        private const int currentVersion = 3;

        private const float constructInterval = 1f;

        private static readonly List<IConstructable> sConstructables = new List<IConstructable>();

        private static List<Renderer> sRenderers = new List<Renderer>();

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 3;

        [NonSerialized]
        [ProtoMember(2)]
        public bool _constructed = true;

        [NonSerialized]
        [ProtoMember(5)]
        public float constructedAmount = 1f;

        [ProtoMember(6)]
        public TechType techType;

        [NonSerialized]
        [ProtoMember(7)]
        public bool isNew = true;

        [NonSerialized]
        [ProtoMember(8)]
        public bool isInside = true;

        [AssertNotNull]
        public GameObject model;

        public GameObject builtBoxFX;

        public bool controlModelState = true;

        [AssertNotNull(AssertNotNullAttribute.Options.AllowEmptyCollection)]
        public MonoBehaviour[] controlledBehaviours;

        public bool allowedOnWall;

        public bool allowedOnGround = true;

        public bool allowedOnCeiling;

        public bool deconstructionAllowed = true;

        public bool allowedInSub = true;

        public bool allowedInBase = true;

        public bool allowedOutside;

        public bool allowedOnConstructables;

        public bool forceUpright;

        public bool rotationEnabled;

        public float placeMaxDistance = 5f;

        public float placeMinDistance = 1.2f;

        public float placeDefaultDistance = 2f;

        public VFXSurfaceTypes surfaceType;

        public Texture _EmissiveTex;

        public Texture _NoiseTex;

        protected Transform tr;

        protected GameObject modelCopy;

        private Material ghostMaterial;

        private List<TechType> resourceMap;

        private VFXOverlayMaterial ghostOverlay;

        private Shader marmoUberShader;

        private int defaultLayer;

        private int viewModelLayer;

        public bool constructed => _constructed;

        public float amount => constructedAmount;

        public List<SurfaceType> allowedSurfaceTypes
        {
            get
            {
                List<SurfaceType> list = new List<SurfaceType>();
                if (allowedOnWall)
                {
                    list.Add(SurfaceType.Wall);
                }
                if (allowedOnGround)
                {
                    list.Add(SurfaceType.Ground);
                }
                if (allowedOnCeiling)
                {
                    list.Add(SurfaceType.Ceiling);
                }
                return list;
            }
        }

        public virtual void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public virtual void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            if (version < 3 && !_constructed)
            {
                constructedAmount = 0.5f;
            }
            InitResourceMap();
            bool flag = _constructed;
            _constructed = !flag;
            SetState(flag, setAmount: false);
        }

        public override void Awake()
        {
            base.Awake();
            tr = GetComponent<Transform>();
            defaultLayer = LayerMask.NameToLayer("Default");
            viewModelLayer = LayerMask.NameToLayer("Viewmodel");
            marmoUberShader = Shader.Find("MarmosetUBER");
        }

        protected virtual void Start()
        {
            InitResourceMap();
        }

        public bool DeconstructionAllowed(out string reason)
        {
            bool flag = true;
            reason = null;
            GetComponents(sConstructables);
            int i = 0;
            for (int count = sConstructables.Count; i < count; i++)
            {
                if (!sConstructables[i].CanDeconstruct(out reason))
                {
                    flag = false;
                    break;
                }
            }
            sConstructables.Clear();
            if (flag)
            {
                reason = null;
            }
            return flag;
        }

        public virtual bool SetState(bool value, bool setAmount = true)
        {
            if (_constructed == value)
            {
                return false;
            }
            _constructed = value;
            MonoBehaviour[] components = base.gameObject.GetComponents<MonoBehaviour>();
            int i = 0;
            for (int num = components.Length; i < num; i++)
            {
                MonoBehaviour monoBehaviour = components[i];
                if (!(monoBehaviour == null) && !(monoBehaviour == this) && !(monoBehaviour.GetType() == typeof(SubModuleHandler)))
                {
                    components[i].enabled = _constructed;
                }
            }
            if (controlledBehaviours != null)
            {
                int j = 0;
                for (int num2 = controlledBehaviours.Length; j < num2; j++)
                {
                    MonoBehaviour monoBehaviour2 = controlledBehaviours[j];
                    if (!(monoBehaviour2 == null) && !(monoBehaviour2 == this))
                    {
                        controlledBehaviours[j].enabled = _constructed;
                    }
                }
            }
            if (setAmount)
            {
                constructedAmount = (_constructed ? 1f : 0f);
            }
            if (_constructed)
            {
                DestroyModelCopy();
                NotifyConstructedChanged(constructed: true);
                SetupRenderers();
                ItemGoalTracker.OnConstruct(techType);
            }
            else
            {
                InitializeModelCopy();
                SetupRenderers();
                NotifyConstructedChanged(constructed: false);
            }
            return true;
        }

        public void SetIsInside(bool inside)
        {
            isInside = inside;
        }

        public bool IsInside()
        {
            return isInside;
        }

        public virtual bool UpdateGhostModel(Transform aimTransform, GameObject ghostModel, RaycastHit hit, out bool geometryChanged, ConstructableBase ghostModelParentConstructableBase)
        {
            geometryChanged = false;
            return CheckFlags(allowedInBase, allowedInSub, allowedOutside);
        }

        public virtual bool Construct()
        {
            if (_constructed)
            {
                return false;
            }
            int count = resourceMap.Count;
            int resourceID = GetResourceID();
            constructedAmount += Time.deltaTime / ((float)count * GetConstructInterval());
            constructedAmount = Mathf.Clamp01(constructedAmount);
            int resourceID2 = GetResourceID();
            if (resourceID2 != resourceID)
            {
                TechType destroyTechType = resourceMap[resourceID2 - 1];
                if (!Inventory.main.DestroyItem(destroyTechType) && GameModeUtils.RequiresIngredients())
                {
                    constructedAmount = (float)resourceID / (float)count;
                    return false;
                }
            }
            UpdateMaterial();
            if (constructedAmount >= 1f)
            {
                SetState(value: true);
            }
            return true;
        }

        public virtual bool Deconstruct()
        {
            if (_constructed)
            {
                return false;
            }
            int count = resourceMap.Count;
            int resourceID = GetResourceID();
            constructedAmount -= Time.deltaTime / ((float)count * GetConstructInterval());
            constructedAmount = Mathf.Clamp01(constructedAmount);
            int resourceID2 = GetResourceID();
            if (resourceID2 != resourceID && GameModeUtils.RequiresIngredients())
            {
                Pickupable component = CraftData.InstantiateFromPrefab(resourceMap[resourceID2]).GetComponent<Pickupable>();
                if (!Inventory.main.Pickup(component))
                {
                    ErrorMessage.AddError(Language.main.Get("InventoryFull"));
                    global::UnityEngine.Object.Destroy(component.gameObject);
                    constructedAmount = ((float)resourceID2 + 0.001f) / (float)count;
                    return false;
                }
            }
            UpdateMaterial();
            if (constructedAmount <= 0f)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
            return true;
        }

        private static float GetConstructInterval()
        {
            if (NoCostConsoleCommand.main.fastBuildCheat)
            {
                return 0.01f;
            }
            if (!GameModeUtils.RequiresIngredients())
            {
                return 0.2f;
            }
            return 1f;
        }

        public virtual Vector3 GetRandomConstructionPoint()
        {
            try
            {
                GetComponentsInChildren(includeInactive: true, sRenderers);
                Vector3 result = base.transform.position;
                if (sRenderers.Count == 0)
                {
                    return result;
                }
                Renderer renderer = sRenderers[global::UnityEngine.Random.Range(0, sRenderers.Count)];
                sRenderers.Clear();
                Mesh mesh = null;
                if (renderer is MeshRenderer)
                {
                    MeshFilter component = renderer.GetComponent<MeshFilter>();
                    if (component != null)
                    {
                        mesh = component.sharedMesh;
                    }
                }
                else if (renderer is SkinnedMeshRenderer)
                {
                    mesh = (renderer as SkinnedMeshRenderer).sharedMesh;
                }
                if (mesh != null)
                {
                    Bounds bounds = mesh.bounds;
                    Matrix4x4 localToWorldMatrix = renderer.transform.localToWorldMatrix;
                    Vector3 extents = bounds.extents;
                    result = localToWorldMatrix.MultiplyPoint3x4(bounds.center + new Vector3(global::UnityEngine.Random.Range(0f - extents.x, extents.x), global::UnityEngine.Random.Range(0f - extents.y, extents.y), global::UnityEngine.Random.Range(0f - extents.z, extents.z)));
                }
                return result;
            }
            finally
            {
            }
        }

        private int GetResourceID()
        {
            return Mathf.CeilToInt(constructedAmount * (float)resourceMap.Count);
        }

        protected void InitResourceMap()
        {
            if (resourceMap != null)
            {
                return;
            }
            resourceMap = new List<TechType>();
            if (techType == TechType.None)
            {
                return;
            }
            ITechData techData = CraftData.Get(techType);
            if (techData == null)
            {
                return;
            }
            for (int i = 0; i < techData.ingredientCount; i++)
            {
                IIngredient ingredient = techData.GetIngredient(i);
                for (int j = 0; j < ingredient.amount; j++)
                {
                    resourceMap.Add(ingredient.techType);
                }
            }
        }

        public virtual Dictionary<TechType, int> GetRemainingResources()
        {
            InitResourceMap();
            int resourceID = GetResourceID();
            Dictionary<TechType, int> dictionary = new Dictionary<TechType, int>(TechTypeExtensions.sTechTypeComparer);
            for (int i = resourceID; i < resourceMap.Count; i++)
            {
                TechType key = resourceMap[i];
                int value = 0;
                if (dictionary.TryGetValue(key, out value))
                {
                    dictionary[key] = value + 1;
                }
                else
                {
                    dictionary.Add(key, 1);
                }
            }
            return dictionary;
        }

        protected virtual bool InitializeModelCopy()
        {
            if (modelCopy != null)
            {
                return false;
            }
            modelCopy = global::UnityEngine.Object.Instantiate(model);
            modelCopy.transform.SetParent(base.gameObject.transform, worldPositionStays: false);
            modelCopy.SetActive(value: true);
            ReplaceMaterials(modelCopy);
            UpdateMaterial();
            return true;
        }

        protected void ReplaceMaterials(GameObject rootObject)
        {
            ghostMaterial = (Material)Resources.Load("Materials/constructingGhost");
            if (ghostMaterial != null)
            {
                ghostOverlay = base.gameObject.AddComponent<VFXOverlayMaterial>();
                ghostOverlay.ApplyOverlay(ghostMaterial, "ConstructableGhost", instantiateMaterial: false);
            }
            Renderer[] componentsInChildren = rootObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                Material[] materials = componentsInChildren[i].materials;
                foreach (Material material in materials)
                {
                    if (!(material == null) && !(material.shader == null) && material.HasProperty(ShaderPropertyID._BuildLinear))
                    {
                        material.EnableKeyword("FX_BUILDING");
                        material.SetTexture(ShaderPropertyID._EmissiveTex, _EmissiveTex);
                        material.SetFloat(ShaderPropertyID._Cutoff, 0.42f);
                        material.SetColor(ShaderPropertyID._BorderColor, new Color(0.7f, 0.7f, 1f, 1f));
                        material.SetFloat(ShaderPropertyID._Built, 0f);
                        material.SetVector(ShaderPropertyID._BuildParams, new Vector4(0.1f, 0.25f, 0.2f, -0.2f));
                        material.SetFloat(ShaderPropertyID._NoiseStr, 1.9f);
                        material.SetFloat(ShaderPropertyID._NoiseThickness, 0.48f);
                        material.SetFloat(ShaderPropertyID._BuildLinear, 0f);
                    }
                }
            }
            Shader.SetGlobalFloat(ShaderPropertyID._SubConstructProgress, 0f);
        }

        protected virtual void DestroyModelCopy()
        {
            if (ghostOverlay != null)
            {
                ghostOverlay.RemoveOverlay();
            }
            if (modelCopy != null)
            {
                modelCopy.AddComponent<BuiltEffectController>();
                modelCopy = null;
            }
            if (builtBoxFX != null)
            {
                GetComponentsInChildren(includeInactive: false, sRenderers);
                OrientedBounds.EncapsulateRenderers(base.transform.worldToLocalMatrix, sRenderers, out var center, out var extents);
                sRenderers.Clear();
                if (extents.x > 0f && extents.y > 0f && extents.z > 0f)
                {
                    Transform obj = global::UnityEngine.Object.Instantiate(builtBoxFX).transform;
                    OrientedBounds orientedBounds = OrientedBounds.ToWorldBounds(localBounds: new OrientedBounds(center, Quaternion.identity, extents), tr: base.transform);
                    obj.position = orientedBounds.position;
                    obj.rotation = orientedBounds.rotation;
                    obj.localScale = orientedBounds.size;
                }
            }
        }

        protected void UpdateMaterial()
        {
            if (modelCopy == null)
            {
                return;
            }
            Renderer[] allComponentsInChildren = modelCopy.GetAllComponentsInChildren<Renderer>();
            for (int i = 0; i < allComponentsInChildren.Length; i++)
            {
                Material[] materials = allComponentsInChildren[i].materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j].SetFloat(ShaderPropertyID._Built, constructedAmount);
                }
            }
        }

        protected void SetupRenderers()
        {
            Utils.SetLayerRecursively(newLayer: (!(GetComponentInParent<SubRoot>() != null)) ? defaultLayer : viewModelLayer, obj: base.gameObject);
        }

        protected void NotifyConstructedChanged(bool constructed)
        {
            GetComponents(sConstructables);
            int i = 0;
            for (int count = sConstructables.Count; i < count; i++)
            {
                sConstructables[i].OnConstructedChanged(constructed);
            }
            sConstructables.Clear();
        }

        public bool CanDeconstruct(out string reason)
        {
            reason = null;
            return deconstructionAllowed;
        }

        public virtual void OnConstructedChanged(bool constructed)
        {
            if (constructed && isNew)
            {
                isNew = false;
                CrafterLogic.NotifyCraftEnd(base.gameObject, techType);
            }
            if (controlModelState)
            {
                if (model != null)
                {
                    model.SetActive(constructed);
                    return;
                }
                Debug.LogErrorFormat(this, "controlModelState checkbox is set, but model is not assigned for Constructable component on '{0}'", base.gameObject);
            }
        }

        public static bool CheckFlags(bool allowedInBase, bool allowedInSub, bool allowedOutside)
        {
            SubRoot currentSub = Player.main.GetCurrentSub();
            if (currentSub != null)
            {
                if (Player.main.currentWaterPark != null)
                {
                    return false;
                }
                if (currentSub.isBase)
                {
                    if (!allowedInBase)
                    {
                        return false;
                    }
                }
                else if (!allowedInSub)
                {
                    return false;
                }
            }
            else if (!allowedOutside)
            {
                return false;
            }
            return true;
        }
    }
}
