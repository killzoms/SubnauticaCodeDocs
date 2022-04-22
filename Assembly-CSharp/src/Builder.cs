using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public static class Builder
    {
        public static readonly float additiveRotationSpeed = 90f;

        public static readonly GameInput.Button buttonRotateCW = GameInput.Button.CyclePrev;

        public static readonly GameInput.Button buttonRotateCCW = GameInput.Button.CycleNext;

        private static readonly Vector3[] checkDirections = new Vector3[4]
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right
        };

        private static readonly Color placeColorAllow = new Color(0f, 1f, 0f, 1f);

        private static readonly Color placeColorDeny = new Color(1f, 0f, 0f, 1f);

        private static readonly string ignoreTag = "DenyBuilding";

        private static bool initialized = false;

        private static BuildModeInputHandler inputHandler = new BuildModeInputHandler();

        private static Collider[] sColliders = new Collider[2];

        private static List<Collider> sCollidersList = new List<Collider>();

        public static float additiveRotation = 0f;

        private static GameObject prefab;

        private static float placeMaxDistance;

        private static float placeMinDistance;

        private static float placeDefaultDistance;

        private static TechType constructableTechType;

        private static List<SurfaceType> allowedSurfaceTypes;

        private static bool forceUpright;

        private static bool allowedInSub;

        private static bool allowedInBase;

        private static bool allowedOutside;

        private static bool allowedOnConstructables;

        private static bool rotationEnabled;

        private static Renderer[] renderers;

        private static GameObject ghostModel;

        private static Vector3 ghostModelPosition;

        private static Quaternion ghostModelRotation;

        private static Vector3 ghostModelScale;

        private static List<OrientedBounds> bounds = new List<OrientedBounds>();

        private static Bounds _aaBounds = default(Bounds);

        private static Vector3 placePosition;

        private static Quaternion placeRotation;

        private static Material ghostStructureMaterial;

        private static LayerMask placeLayerMask;

        private static GameObject placementTarget;

        private static string placeSound = "event:/tools/builder/place";

        public static Bounds aaBounds => _aaBounds;

        public static bool isPlacing => prefab != null;

        public static bool canPlace { get; private set; }

        private static void Initialize()
        {
            if (!initialized)
            {
                initialized = true;
                placeLayerMask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Trigger")));
                ghostStructureMaterial = new Material(Resources.Load<Material>("Materials/ghostmodel"));
            }
        }

        public static bool Begin(GameObject modulePrefab)
        {
            Initialize();
            if (modulePrefab == null)
            {
                Debug.LogWarning("Builder : Begin() : Module prefab is null!");
                return false;
            }
            if (modulePrefab != prefab)
            {
                End();
            }
            prefab = modulePrefab;
            Update();
            return true;
        }

        public static void End()
        {
            Initialize();
            inputHandler.canHandleInput = false;
            if (ghostModel != null)
            {
                ConstructableBase componentInParent = ghostModel.GetComponentInParent<ConstructableBase>();
                if (componentInParent != null)
                {
                    Object.Destroy(componentInParent.gameObject);
                }
                Object.Destroy(ghostModel);
            }
            prefab = null;
            ghostModel = null;
            canPlace = false;
            placementTarget = null;
            additiveRotation = 0f;
        }

        public static void Update()
        {
            Initialize();
            canPlace = false;
            if (!(prefab == null))
            {
                if (CreateGhost())
                {
                    inputHandler.canHandleInput = true;
                    InputHandlerStack.main.Push(inputHandler);
                }
                canPlace = UpdateAllowed();
                Transform transform = ghostModel.transform;
                transform.position = placePosition + placeRotation * ghostModelPosition;
                transform.rotation = placeRotation * ghostModelRotation;
                transform.localScale = ghostModelScale;
                Color color = (canPlace ? placeColorAllow : placeColorDeny);
                IBuilderGhostModel[] components = ghostModel.GetComponents<IBuilderGhostModel>();
                for (int i = 0; i < components.Length; i++)
                {
                    components[i].UpdateGhostModelColor(canPlace, ref color);
                }
                ghostStructureMaterial.SetColor(ShaderPropertyID._Tint, color);
            }
        }

        private static bool CreateGhost()
        {
            if (ghostModel != null)
            {
                return false;
            }
            Constructable component = prefab.GetComponent<Constructable>();
            constructableTechType = component.techType;
            placeMinDistance = component.placeMinDistance;
            placeMaxDistance = component.placeMaxDistance;
            placeDefaultDistance = component.placeDefaultDistance;
            allowedSurfaceTypes = component.allowedSurfaceTypes;
            forceUpright = component.forceUpright;
            allowedInSub = component.allowedInSub;
            allowedInBase = component.allowedInBase;
            allowedOutside = component.allowedOutside;
            allowedOnConstructables = component.allowedOnConstructables;
            rotationEnabled = component.rotationEnabled;
            if (rotationEnabled)
            {
                ShowRotationControlsHint();
            }
            if (prefab.GetComponent<ConstructableBase>() != null)
            {
                ghostModel = Object.Instantiate(prefab).GetComponent<ConstructableBase>().model;
                ghostModel.GetComponent<BaseGhost>().SetupGhost();
                ghostModelPosition = Vector3.zero;
                ghostModelRotation = Quaternion.identity;
                ghostModelScale = Vector3.one;
                renderers = MaterialExtensions.AssignMaterial(ghostModel, ghostStructureMaterial);
                InitBounds(ghostModel);
            }
            else
            {
                ghostModel = Object.Instantiate(component.model);
                ghostModel.SetActive(value: true);
                Transform component2 = component.GetComponent<Transform>();
                Transform component3 = component.model.GetComponent<Transform>();
                Quaternion quaternion = Quaternion.Inverse(component2.rotation);
                ghostModelPosition = quaternion * (component3.position - component2.position);
                ghostModelRotation = quaternion * component3.rotation;
                ghostModelScale = component3.lossyScale;
                Collider[] componentsInChildren = ghostModel.GetComponentsInChildren<Collider>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    Object.Destroy(componentsInChildren[i]);
                }
                renderers = MaterialExtensions.AssignMaterial(ghostModel, ghostStructureMaterial);
                SetupRenderers(ghostModel, Player.main.IsInSub());
                CreatePowerPreview(constructableTechType, ghostModel);
                InitBounds(prefab);
            }
            return true;
        }

        private static bool UpdateAllowed()
        {
            SetDefaultPlaceTransform(ref placePosition, ref placeRotation);
            bool geometryChanged = false;
            bool flag = false;
            ConstructableBase componentInParent = ghostModel.GetComponentInParent<ConstructableBase>();
            if (componentInParent != null)
            {
                Transform transform = componentInParent.transform;
                transform.position = placePosition;
                transform.rotation = placeRotation;
                flag = componentInParent.UpdateGhostModel(GetAimTransform(), ghostModel, default(RaycastHit), out geometryChanged, componentInParent);
                placePosition = transform.position;
                placeRotation = transform.rotation;
                if (geometryChanged)
                {
                    renderers = MaterialExtensions.AssignMaterial(ghostModel, ghostStructureMaterial);
                    InitBounds(ghostModel);
                }
            }
            else
            {
                flag = CheckAsSubModule();
            }
            if (flag)
            {
                List<GameObject> list = new List<GameObject>();
                GetObstacles(placePosition, placeRotation, bounds, list);
                flag = list.Count == 0;
                list.Clear();
            }
            return flag;
        }

        public static bool TryPlace()
        {
            Initialize();
            if (prefab == null || !canPlace)
            {
                return false;
            }
            Utils.PlayEnvSound(placeSound, ghostModel.transform.position);
            ConstructableBase componentInParent = ghostModel.GetComponentInParent<ConstructableBase>();
            if (componentInParent != null)
            {
                BaseGhost component = ghostModel.GetComponent<BaseGhost>();
                component.Place();
                if (component.TargetBase != null)
                {
                    componentInParent.transform.SetParent(component.TargetBase.transform, worldPositionStays: true);
                }
                componentInParent.SetState(value: false);
            }
            else
            {
                GameObject gameObject = Object.Instantiate(prefab);
                bool flag = false;
                bool flag2 = false;
                SubRoot currentSub = Player.main.GetCurrentSub();
                if (currentSub != null)
                {
                    flag = currentSub.isBase;
                    flag2 = currentSub.isCyclops;
                    gameObject.transform.parent = currentSub.GetModulesRoot();
                }
                else if (placementTarget != null && allowedOutside)
                {
                    SubRoot componentInParent2 = placementTarget.GetComponentInParent<SubRoot>();
                    if (componentInParent2 != null)
                    {
                        gameObject.transform.parent = componentInParent2.GetModulesRoot();
                    }
                }
                Transform transform = gameObject.transform;
                transform.position = placePosition;
                transform.rotation = placeRotation;
                Constructable componentInParent3 = gameObject.GetComponentInParent<Constructable>();
                componentInParent3.SetState(value: false);
                Utils.SetLayerRecursively(gameObject, LayerMask.NameToLayer(flag ? "Default" : "Interior"));
                if (ghostModel != null)
                {
                    Object.Destroy(ghostModel);
                }
                componentInParent3.SetIsInside(flag || flag2);
                SkyEnvironmentChanged.Send(gameObject, currentSub);
            }
            ghostModel = null;
            prefab = null;
            canPlace = false;
            return true;
        }

        public static void ShowRotationControlsHint()
        {
            ErrorMessage.AddError(Language.main.GetFormat("GhostRotateInputHint", uGUI.FormatButton(buttonRotateCW, allBindingSets: true, ", "), uGUI.FormatButton(buttonRotateCCW, allBindingSets: true, ", ")));
        }

        private static void InitBounds(GameObject gameObject)
        {
            CacheBounds(gameObject.transform, gameObject, bounds);
            _aaBounds.center = Vector3.zero;
            _aaBounds.extents = Vector3.zero;
            int count = bounds.Count;
            if (count > 0)
            {
                Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                for (int i = 0; i < count; i++)
                {
                    OrientedBounds orientedBounds = bounds[i];
                    OrientedBounds.MinMaxBounds(OrientedBounds.TransformMatrix(orientedBounds.position, orientedBounds.rotation), Vector3.zero, orientedBounds.extents, ref min, ref max);
                }
                _aaBounds.extents = (max - min) * 0.5f;
                _aaBounds.center = min + aaBounds.extents;
            }
        }

        public static void OnDrawGizmos()
        {
            Matrix4x4 matrix = Gizmos.matrix;
            Color color = Gizmos.color;
            Gizmos.matrix = OrientedBounds.TransformMatrix(placePosition, placeRotation);
            Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
            Gizmos.DrawCube(aaBounds.center, aaBounds.extents * 2f);
            Gizmos.matrix = matrix;
            Gizmos.color = color;
            OnDrawGizmos();
        }

        public static void CacheBounds(Transform transform, GameObject target, List<OrientedBounds> results, bool append = false)
        {
            if (!append)
            {
                results.Clear();
            }
            if (target == null)
            {
                return;
            }
            ConstructableBounds[] componentsInChildren = target.GetComponentsInChildren<ConstructableBounds>();
            foreach (ConstructableBounds obj in componentsInChildren)
            {
                OrientedBounds orientedBounds = OrientedBounds.ToWorldBounds(localBounds: obj.bounds, tr: obj.transform);
                if (transform != null)
                {
                    orientedBounds = OrientedBounds.ToLocalBounds(transform, orientedBounds);
                }
                results.Add(orientedBounds);
            }
        }

        public static bool CheckSpace(Vector3 position, Quaternion rotation, Vector3 extents, int layerMask, Collider allowedCollider)
        {
            if (extents.x <= 0f || extents.y <= 0f || extents.z <= 0f)
            {
                return true;
            }
            return Physics.OverlapBoxNonAlloc(position, extents, sColliders, rotation, layerMask, QueryTriggerInteraction.Ignore) switch
            {
                1 => sColliders[0] == allowedCollider, 
                0 => true, 
                _ => false, 
            };
        }

        public static bool CheckSpace(Vector3 position, Quaternion rotation, List<OrientedBounds> localBounds, int layerMask, Collider allowedCollider)
        {
            if (rotation.IsDistinguishedIdentity())
            {
                rotation = Quaternion.identity;
            }
            for (int i = 0; i < localBounds.Count; i++)
            {
                OrientedBounds orientedBounds = localBounds[i];
                if (orientedBounds.rotation.IsDistinguishedIdentity())
                {
                    orientedBounds.rotation = Quaternion.identity;
                }
                orientedBounds.position = position + rotation * orientedBounds.position;
                orientedBounds.rotation = rotation * orientedBounds.rotation;
                if (!CheckSpace(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, layerMask, allowedCollider))
                {
                    return false;
                }
            }
            return true;
        }

        public static void GetOverlappedColliders(Vector3 position, Quaternion rotation, Vector3 extents, List<Collider> results)
        {
            results.Clear();
            int num = global::UWE.Utils.OverlapBoxIntoSharedBuffer(position, extents, rotation, -1, QueryTriggerInteraction.Collide);
            for (int i = 0; i < num; i++)
            {
                Collider collider = global::UWE.Utils.sharedColliderBuffer[i];
                GameObject gameObject = collider.gameObject;
                if (!collider.isTrigger || gameObject.layer == LayerID.Useable)
                {
                    results.Add(collider);
                }
            }
        }

        public static void GetRootObjects(List<Collider> colliders, List<GameObject> results)
        {
            results.Clear();
            for (int i = 0; i < colliders.Count; i++)
            {
                GameObject gameObject = colliders[i].gameObject;
                GameObject gameObject2 = global::UWE.Utils.GetEntityRoot(gameObject);
                if (gameObject2 == null)
                {
                    SceneObjectIdentifier componentInParent = gameObject.GetComponentInParent<SceneObjectIdentifier>();
                    if (componentInParent != null)
                    {
                        gameObject2 = componentInParent.gameObject;
                    }
                }
                gameObject = ((gameObject2 != null) ? gameObject2 : gameObject);
                if (!results.Contains(gameObject))
                {
                    results.Add(gameObject);
                }
            }
        }

        public static void GetOverlappedObjects(Vector3 position, Quaternion rotation, Vector3 extents, List<GameObject> results)
        {
            GetOverlappedColliders(position, rotation, extents, sCollidersList);
            GetRootObjects(sCollidersList, results);
            sCollidersList.Clear();
        }

        public static void GetObstacles(Vector3 position, Quaternion rotation, List<OrientedBounds> localBounds, List<GameObject> results)
        {
            results.Clear();
            if (rotation.IsDistinguishedIdentity())
            {
                rotation = Quaternion.identity;
            }
            List<GameObject> list = new List<GameObject>();
            for (int i = 0; i < localBounds.Count; i++)
            {
                OrientedBounds orientedBounds = localBounds[i];
                if (orientedBounds.rotation.IsDistinguishedIdentity())
                {
                    orientedBounds.rotation = Quaternion.identity;
                }
                orientedBounds.position = position + rotation * orientedBounds.position;
                orientedBounds.rotation = rotation * orientedBounds.rotation;
                GetOverlappedColliders(orientedBounds.position, orientedBounds.rotation, orientedBounds.extents, sCollidersList);
                GetRootObjects(sCollidersList, list);
                for (int num = list.Count - 1; num >= 0; num--)
                {
                    if (!IsObstacle(list[num]))
                    {
                        list.RemoveAt(num);
                    }
                }
                for (int j = 0; j < sCollidersList.Count; j++)
                {
                    Collider collider = sCollidersList[j];
                    if (IsObstacle(collider))
                    {
                        GameObject gameObject = collider.gameObject;
                        if (!list.Contains(gameObject))
                        {
                            list.Add(gameObject);
                        }
                    }
                }
                sCollidersList.Clear();
                for (int k = 0; k < list.Count; k++)
                {
                    GameObject item = list[k];
                    if (!results.Contains(item))
                    {
                        results.Add(item);
                    }
                }
            }
        }

        public static bool CanDestroyObject(GameObject go)
        {
            if (go.GetComponentInParent<Player>() != null)
            {
                return false;
            }
            LargeWorldEntity component = go.GetComponent<LargeWorldEntity>();
            if (component != null && component.cellLevel >= LargeWorldEntity.CellLevel.Global)
            {
                return false;
            }
            if (go.GetComponentInParent<SubRoot>() != null)
            {
                return false;
            }
            if (go.GetComponentInParent<Constructable>() != null)
            {
                return false;
            }
            if (go.GetComponent<IObstacle>() != null)
            {
                return false;
            }
            Pickupable component2 = go.GetComponent<Pickupable>();
            if (component2 != null && component2.attached)
            {
                return false;
            }
            if (go.GetComponent<PlaceTool>() != null)
            {
                return false;
            }
            return true;
        }

        public static bool IsObstacle(Collider collider)
        {
            if (collider != null && collider.gameObject.layer == LayerID.TerrainCollider)
            {
                return true;
            }
            return false;
        }

        public static bool IsObstacle(GameObject go)
        {
            if (go.GetComponent<IObstacle>() != null)
            {
                return true;
            }
            return false;
        }

        public static Transform GetAimTransform()
        {
            return MainCamera.camera.transform;
        }

        public static GameObject GetGhostModel()
        {
            return ghostModel;
        }

        private static bool CheckAsSubModule()
        {
            if (!Constructable.CheckFlags(allowedInBase, allowedInSub, allowedOutside))
            {
                return false;
            }
            Transform aimTransform = GetAimTransform();
            placementTarget = null;
            if (!Physics.Raycast(aimTransform.position, aimTransform.forward, out var hitInfo, placeMaxDistance, placeLayerMask.value, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            placementTarget = hitInfo.collider.gameObject;
            SetPlaceOnSurface(hitInfo, ref placePosition, ref placeRotation);
            if (!CheckTag(hitInfo.collider))
            {
                return false;
            }
            if (!CheckSurfaceType(GetSurfaceType(hitInfo.normal)))
            {
                return false;
            }
            if (!CheckDistance(hitInfo.point, placeMinDistance))
            {
                return false;
            }
            if (!allowedOnConstructables && HasComponent<Constructable>(hitInfo.collider.gameObject))
            {
                return false;
            }
            if (!Player.main.IsInSub())
            {
                GameObject entityRoot = global::UWE.Utils.GetEntityRoot(placementTarget);
                if (!entityRoot)
                {
                    entityRoot = placementTarget;
                }
                if (!ValidateOutdoor(entityRoot))
                {
                    return false;
                }
            }
            if (!CheckSpace(placePosition, placeRotation, bounds, placeLayerMask.value, hitInfo.collider))
            {
                return false;
            }
            return true;
        }

        private static SurfaceType GetSurfaceType(Vector3 hitNormal)
        {
            if ((double)hitNormal.y < -0.33)
            {
                return SurfaceType.Ceiling;
            }
            if ((double)hitNormal.y < 0.33)
            {
                return SurfaceType.Wall;
            }
            return SurfaceType.Ground;
        }

        private static bool CheckTag(Collider c)
        {
            if (c == null)
            {
                return false;
            }
            GameObject gameObject = c.gameObject;
            if (gameObject == null)
            {
                return false;
            }
            if (gameObject.CompareTag(ignoreTag))
            {
                return false;
            }
            return true;
        }

        private static bool CheckSurfaceType(SurfaceType surfaceType)
        {
            return allowedSurfaceTypes.Contains(surfaceType);
        }

        private static bool CheckDistance(Vector3 worldPosition, float minDistance)
        {
            Transform aimTransform = GetAimTransform();
            return (worldPosition - aimTransform.position).magnitude >= minDistance;
        }

        private static bool HasComponent<T>(GameObject go) where T : Component
        {
            return (Object)go.GetComponentInParent<T>() != (Object)null;
        }

        private static void SetDefaultPlaceTransform(ref Vector3 position, ref Quaternion rotation)
        {
            Transform aimTransform = GetAimTransform();
            position = aimTransform.position + aimTransform.forward * placeDefaultDistance;
            Vector3 forward;
            Vector3 up;
            if (forceUpright)
            {
                forward = -aimTransform.forward;
                forward.y = 0f;
                forward.Normalize();
                up = Vector3.up;
            }
            else
            {
                forward = -aimTransform.forward;
                up = aimTransform.up;
            }
            rotation = Quaternion.LookRotation(forward, up);
            if (rotationEnabled)
            {
                rotation = Quaternion.AngleAxis(additiveRotation, up) * rotation;
            }
        }

        private static void SetPlaceOnSurface(RaycastHit hit, ref Vector3 position, ref Quaternion rotation)
        {
            Transform aimTransform = GetAimTransform();
            Vector3 vector = Vector3.forward;
            Vector3 vector2 = Vector3.up;
            if (forceUpright)
            {
                vector = -aimTransform.forward;
                vector.y = 0f;
                vector.Normalize();
                vector2 = Vector3.up;
            }
            else
            {
                switch (GetSurfaceType(hit.normal))
                {
                    case SurfaceType.Wall:
                        vector = hit.normal;
                        vector2 = Vector3.up;
                        break;
                    case SurfaceType.Ceiling:
                        vector = hit.normal;
                        vector2 = -aimTransform.forward;
                        vector2.y -= Vector3.Dot(vector2, vector);
                        vector2.Normalize();
                        break;
                    case SurfaceType.Ground:
                        vector2 = hit.normal;
                        vector = -aimTransform.forward;
                        vector.y -= Vector3.Dot(vector, vector2);
                        vector.Normalize();
                        break;
                }
            }
            position = hit.point;
            rotation = Quaternion.LookRotation(vector, vector2);
            if (rotationEnabled)
            {
                rotation = Quaternion.AngleAxis(additiveRotation, vector2) * rotation;
            }
        }

        private static void SetupRenderers(GameObject gameObject, bool interior)
        {
            int newLayer = ((!interior) ? LayerMask.NameToLayer("Default") : LayerMask.NameToLayer("Viewmodel"));
            Utils.SetLayerRecursively(gameObject, newLayer);
        }

        public static bool ValidateOutdoor(GameObject hitObject)
        {
            Rigidbody component = hitObject.GetComponent<Rigidbody>();
            if ((bool)component && !component.isKinematic)
            {
                return false;
            }
            SubRoot component2 = hitObject.GetComponent<SubRoot>();
            Base component3 = hitObject.GetComponent<Base>();
            if (component2 != null && component3 == null)
            {
                return false;
            }
            if (hitObject.GetComponent<Pickupable>() != null)
            {
                return false;
            }
            LiveMixin component4 = hitObject.GetComponent<LiveMixin>();
            if (component4 != null && component4.destroyOnDeath)
            {
                return false;
            }
            return true;
        }

        private static void CreatePowerPreview(TechType constructableTechType, GameObject ghostModel)
        {
            GameObject gameObject = null;
            string poweredPrefabName = CraftData.GetPoweredPrefabName(constructableTechType);
            if (poweredPrefabName != "")
            {
                gameObject = PrefabDatabase.GetPrefabForFilename(poweredPrefabName);
            }
            if (gameObject != null)
            {
                PowerRelay component = gameObject.GetComponent<PowerRelay>();
                if (component.powerFX != null && component.powerFX.attachPoint != null)
                {
                    PowerFX powerFX = ghostModel.AddComponent<PowerFX>();
                    GameObject gameObject2 = new GameObject();
                    gameObject2.transform.parent = ghostModel.transform;
                    gameObject2.transform.localPosition = component.powerFX.attachPoint.localPosition;
                    powerFX.attachPoint = gameObject2.transform;
                }
                PowerRelay powerRelay = ghostModel.AddComponent<PowerRelay>();
                powerRelay.maxOutboundDistance = component.maxOutboundDistance;
                powerRelay.dontConnectToRelays = component.dontConnectToRelays;
                if (component.internalPowerSource != null)
                {
                    PowerSource powerSource = ghostModel.AddComponent<PowerSource>();
                    powerSource.maxPower = 0f;
                    powerRelay.internalPowerSource = powerSource;
                }
            }
        }
    }
}
