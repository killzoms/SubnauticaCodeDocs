using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public sealed class ConstructableBase : Constructable
    {
        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int protoVersion = 1;

        [ProtoMember(2)]
        public TechType faceLinkedModuleType;

        [ProtoMember(3)]
        public Vector3 faceLinkedModulePosition = Vector3.zero;

        [ProtoMember(4)]
        public Base.Face? moduleFace;

        private List<Renderer> ghostRenderers;

        private bool ghostRenderersVisible = true;

        public override void Awake()
        {
            base.Awake();
            InitGhostRenderers();
        }

        public void LinkModule(Base.Face? moduleFace)
        {
            this.moduleFace = moduleFace;
        }

        private void InitGhostRenderers()
        {
            if (ghostRenderers == null)
            {
                ghostRenderers = new List<Renderer>();
                if (model != null)
                {
                    model.GetComponentsInChildren(ghostRenderers);
                }
            }
        }

        private void SetGhostVisible(bool visible)
        {
            if (ghostRenderersVisible == visible)
            {
                return;
            }
            ghostRenderersVisible = visible;
            InitGhostRenderers();
            int i = 0;
            for (int count = ghostRenderers.Count; i < count; i++)
            {
                Renderer renderer = ghostRenderers[i];
                if (renderer != null)
                {
                    renderer.enabled = ghostRenderersVisible;
                }
            }
        }

        private void SetModuleConstructAmount(Base targetBase, float amount)
        {
            if (!moduleFace.HasValue)
            {
                return;
            }
            if (targetBase != null)
            {
                Base.Face value = moduleFace.Value;
                value.cell = targetBase.GetAnchor() + value.cell;
                IBaseModule module = targetBase.GetModule(value);
                if (module != null)
                {
                    module.constructed = amount;
                    return;
                }
                Debug.LogErrorFormat(this, "IBaseModule not found in targetBase at cell [{0}]", moduleFace.Value);
            }
            else
            {
                Debug.LogError("targetBase is null", this);
            }
        }

        private void OnGlobalEntitiesLoaded()
        {
            Base componentInChildren = model.GetComponentInChildren<Base>();
            OnGhostBasePostRebuildGeometry(componentInChildren);
            componentInChildren.onPostRebuildGeometry += OnGhostBasePostRebuildGeometry;
        }

        private void OnGhostBasePostRebuildGeometry(Base b)
        {
            ReplaceMaterials(model);
            UpdateMaterial();
        }

        public override bool UpdateGhostModel(Transform aimTransform, GameObject ghostModel, RaycastHit hit, out bool geometryChanged, ConstructableBase ghostModelParentConstructableBase)
        {
            if (ghostModel == null)
            {
                geometryChanged = false;
                return false;
            }
            bool positionFound;
            bool result = ghostModel.GetComponent<BaseGhost>().UpdatePlacement(aimTransform, placeMaxDistance, out positionFound, out geometryChanged, ghostModelParentConstructableBase);
            SetGhostVisible(!positionFound);
            return result;
        }

        public override bool Construct()
        {
            BaseGhost component = model.GetComponent<BaseGhost>();
            Base targetBase = ((component != null) ? component.TargetBase : null);
            bool result = base.Construct();
            SetModuleConstructAmount(targetBase, base.amount);
            return result;
        }

        public override bool Deconstruct()
        {
            bool result = base.Deconstruct();
            BaseGhost component = model.GetComponent<BaseGhost>();
            Base @base = ((component != null) ? component.TargetBase : null);
            SetModuleConstructAmount(@base, base.amount);
            if (@base != null && constructedAmount <= 0f)
            {
                @base.DestroyIfEmpty(component);
            }
            return result;
        }

        public override bool SetState(bool value, bool setAmount = true)
        {
            if (_constructed != value && value)
            {
                List<ConstructableBounds> list = new List<ConstructableBounds>();
                GetComponentsInChildren(includeInactive: true, list);
                List<GameObject> list2 = new List<GameObject>();
                for (int i = 0; i < list.Count; i++)
                {
                    ConstructableBounds constructableBounds = list[i];
                    OrientedBounds orientedBounds = OrientedBounds.ToWorldBounds(constructableBounds.transform, constructableBounds.bounds);
                    list2.Clear();
                    Builder.GetOverlappedObjects(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, list2);
                    int j = 0;
                    for (int count = list2.Count; j < count; j++)
                    {
                        GameObject gameObject = list2[j];
                        if (Builder.CanDestroyObject(gameObject))
                        {
                            global::UnityEngine.Object.Destroy(gameObject);
                        }
                    }
                }
                model.GetComponent<BaseGhost>().Finish();
            }
            bool result = base.SetState(value, setAmount);
            if (_constructed)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
                return result;
            }
            UpdateMaterial();
            return result;
        }

        public override void OnConstructedChanged(bool constructed)
        {
        }

        protected override bool InitializeModelCopy()
        {
            ReplaceMaterials(model);
            modelCopy = model;
            return true;
        }

        protected override void DestroyModelCopy()
        {
            modelCopy.transform.SetParent(null, worldPositionStays: true);
            base.DestroyModelCopy();
            model = null;
        }

        public override void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            base.OnProtoDeserialize(serializer);
            SetGhostVisible(visible: false);
        }
    }
}
