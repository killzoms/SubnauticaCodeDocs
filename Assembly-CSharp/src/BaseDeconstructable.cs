using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class BaseDeconstructable : MonoBehaviour
    {
        private static List<IObstacle> sObstacles = new List<IObstacle>();

        private static List<GameObject> sObstacleGameObjects = new List<GameObject>();

        private static List<OrientedBounds> sBounds = new List<OrientedBounds>();

        private static StringBuilder sb = new StringBuilder();

        [NonSerialized]
        [ProtoMember(1)]
        public Int3.Bounds bounds;

        [NonSerialized]
        [ProtoMember(2)]
        public Base.Face? face;

        [NonSerialized]
        [ProtoMember(3)]
        public Base.FaceType faceType;

        private TechType recipe;

        private Base.Face? moduleFace;

        private Base deconstructedBase;

        public string Name => recipe.AsString();

        public static BaseDeconstructable MakeCellDeconstructable(Transform geometry, Int3.Bounds bounds, TechType recipe)
        {
            BaseDeconstructable baseDeconstructable = geometry.gameObject.AddComponent<BaseDeconstructable>();
            baseDeconstructable.recipe = recipe;
            baseDeconstructable.bounds = bounds;
            baseDeconstructable.face = null;
            baseDeconstructable.faceType = Base.FaceType.None;
            return baseDeconstructable;
        }

        public static BaseDeconstructable MakeFaceDeconstructable(Transform geometry, Int3.Bounds bounds, Base.Face face, Base.FaceType faceType, TechType recipe)
        {
            BaseDeconstructable baseDeconstructable = geometry.gameObject.EnsureComponent<BaseDeconstructable>();
            baseDeconstructable.recipe = recipe;
            baseDeconstructable.bounds = bounds;
            baseDeconstructable.face = face;
            baseDeconstructable.faceType = faceType;
            return baseDeconstructable;
        }

        private bool IsBulkheadConnected()
        {
            if (deconstructedBase == null)
            {
                return false;
            }
            foreach (Int3 bound in bounds)
            {
                int cellConnections = deconstructedBase.GetCellConnections(bound);
                if (cellConnections == 0)
                {
                    continue;
                }
                Base.Direction[] allDirections = Base.AllDirections;
                foreach (Base.Direction direction in allDirections)
                {
                    int num = 1 << (int)direction;
                    if ((cellConnections & num) != 0)
                    {
                        Base.Face adjacentFace = Base.GetAdjacentFace(new Base.Face(bound, direction));
                        if (Base.IsBulkhead(deconstructedBase.GetFace(adjacentFace)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void Awake()
        {
            deconstructedBase = GetComponentInParent<Base>();
        }

        public bool DeconstructionAllowed(out string reason)
        {
            reason = null;
            _ = Player.main;
            if (!base.enabled || recipe == TechType.None)
            {
                return false;
            }
            if (deconstructedBase == null)
            {
                return false;
            }
            foreach (Int3 bound in this.bounds)
            {
                if (deconstructedBase.IsCellUnderConstruction(bound))
                {
                    reason = Language.main.Get("DeconstructGhostError");
                    return false;
                }
            }
            if (!this.face.HasValue)
            {
                foreach (Int3 bound2 in this.bounds)
                {
                    if (deconstructedBase.GetAreCellFacesUsed(bound2))
                    {
                        reason = Language.main.Get("DeconstructAttachedError");
                        return false;
                    }
                }
                if (recipe == TechType.BaseFoundation && this.bounds.maxs.y < deconstructedBase.Bounds.maxs.y)
                {
                    Int3.Bounds bounds = this.bounds;
                    bounds.mins.y = bounds.maxs.y + 1;
                    bounds.maxs.y = bounds.maxs.y + 1;
                    foreach (Int3 item in bounds)
                    {
                        if (!deconstructedBase.IsCellEmpty(item))
                        {
                            reason = Language.main.Get("DeconstructAttachedError");
                            return false;
                        }
                    }
                }
                if (IsBulkheadConnected())
                {
                    reason = Language.main.Get("DeconstructAttachedError");
                    return false;
                }
            }
            sb.Length = 0;
            sb.Append(Language.main.Get("DeconstructFailed"));
            int reasonCounter = 5;
            GetComponentsInChildren(includeInactive: true, sObstacles);
            bool reasonDefined;
            bool num = CheckObstacles(sObstacles, ref reasonCounter, out reasonDefined, sb);
            sObstacles.Clear();
            if (!num)
            {
                reason = sb.ToString();
                return false;
            }
            Builder.CacheBounds(base.transform, base.gameObject, sBounds);
            if (!this.face.HasValue)
            {
                foreach (Int3 bound3 in this.bounds)
                {
                    int cellConnections = deconstructedBase.GetCellConnections(bound3);
                    if (cellConnections == 0)
                    {
                        continue;
                    }
                    Base.Direction[] allDirections = Base.AllDirections;
                    foreach (Base.Direction direction in allDirections)
                    {
                        int num2 = 1 << (int)direction;
                        if ((cellConnections & num2) == 0)
                        {
                            continue;
                        }
                        Base.Face face = new Base.Face(bound3, direction);
                        Base.Face adjacentFace = Base.GetAdjacentFace(face);
                        int cellConnections2 = deconstructedBase.GetCellConnections(adjacentFace.cell);
                        Base.Direction direction2 = Base.ReverseDirection(direction);
                        if ((cellConnections2 & (1 << (int)direction2)) != 0)
                        {
                            Transform transform = deconstructedBase.FindFaceObject(face);
                            if (transform != null)
                            {
                                Builder.CacheBounds(base.transform, transform.gameObject, sBounds, append: true);
                            }
                            Transform transform2 = deconstructedBase.FindFaceObject(adjacentFace);
                            if (transform2 != null)
                            {
                                Builder.CacheBounds(base.transform, transform2.gameObject, sBounds, append: true);
                            }
                        }
                    }
                }
            }
            Builder.GetObstacles(base.transform.position, base.transform.rotation, sBounds, sObstacleGameObjects);
            if (sObstacleGameObjects.Count > 0)
            {
                for (int j = 0; j < sObstacleGameObjects.Count; j++)
                {
                    GameObject gameObject = sObstacleGameObjects[j];
                    gameObject.GetComponentsInChildren(includeInactive: true, sObstacles);
                    CheckObstacles(sObstacles, ref reasonCounter, out reasonDefined, sb);
                    if (reasonCounter <= 0)
                    {
                        break;
                    }
                    if (!reasonDefined)
                    {
                        TechType techType = CraftData.GetTechType(gameObject);
                        string arg = Language.main.Get(techType);
                        sb.Append(Language.main.GetFormat("DeconstructObstacle", arg));
                    }
                }
                sObstacleGameObjects.Clear();
                sObstacles.Clear();
                reason = sb.ToString();
                sb.Length = 0;
                return false;
            }
            reason = null;
            return true;
        }

        private bool CheckObstacles(List<IObstacle> obstacles, ref int reasonCounter, out bool reasonDefined, StringBuilder sb)
        {
            reasonDefined = false;
            bool result = true;
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].CanDeconstruct(out var reason))
                {
                    continue;
                }
                result = false;
                if (!string.IsNullOrEmpty(reason))
                {
                    reasonDefined = true;
                    sb.Append(reason);
                    reasonCounter--;
                    if (reasonCounter <= 0)
                    {
                        break;
                    }
                }
            }
            return result;
        }

        public void Deconstruct()
        {
            Base componentInParent = GetComponentInParent<Base>();
            if (componentInParent == null)
            {
                Debug.LogError("BaseDeconstructable without a Base");
                return;
            }
            GameObject prefabForFilename = PrefabDatabase.GetPrefabForFilename("Base/Ghosts/BaseDeconstructable");
            Vector3 position = componentInParent.GridToWorld(bounds.mins);
            GameObject obj = global::UnityEngine.Object.Instantiate(prefabForFilename, position, componentInParent.transform.rotation);
            ConstructableBase component = obj.GetComponent<ConstructableBase>();
            BaseGhost component2 = component.model.GetComponent<BaseGhost>();
            component2.Deconstruct(componentInParent, bounds, face, faceType);
            obj.transform.position = componentInParent.GridToWorld(component2.TargetOffset);
            component.techType = recipe;
            component.SetState(value: false, setAmount: false);
            if (face.HasValue)
            {
                componentInParent.ClearFace(face.Value, faceType);
            }
            else
            {
                componentInParent.ClearCell(bounds.mins);
            }
            component.LinkModule(moduleFace);
            if (componentInParent.IsEmpty())
            {
                componentInParent.OnPreDestroy();
                global::UnityEngine.Object.Destroy(componentInParent.gameObject);
                component2.ClearTargetBase();
                if ((bool)LargeWorld.main)
                {
                    LargeWorld.main.streamer.cellManager.RegisterEntity(component.gameObject);
                }
            }
            else
            {
                component.transform.parent = componentInParent.transform;
                componentInParent.FixRoomFloors();
                componentInParent.FixCorridorLinks();
                componentInParent.RebuildGeometry();
            }
        }

        public void LinkModule(Base.Face? moduleFace)
        {
            this.moduleFace = moduleFace;
        }
    }
}
