using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    [ProtoInclude(601, typeof(BaseAddBulkheadGhost))]
    [ProtoInclude(602, typeof(BaseAddCellGhost))]
    [ProtoInclude(603, typeof(BaseAddConnectorGhost))]
    [ProtoInclude(604, typeof(BaseAddCorridorGhost))]
    [ProtoInclude(605, typeof(BaseAddFaceGhost))]
    [ProtoInclude(606, typeof(BaseAddLadderGhost))]
    [ProtoInclude(607, typeof(BaseAddWaterPark))]
    [ProtoInclude(608, typeof(BaseAddMapRoomGhost))]
    [ProtoInclude(609, typeof(BaseAddModuleGhost))]
    public class BaseGhost : MonoBehaviour
    {
        private static List<Collider> sColliders = new List<Collider>();

        private static List<MonoBehaviour> sMonoBehaviours = new List<MonoBehaviour>();

        [NonSerialized]
        [ProtoMember(1)]
        public Int3 targetOffset;

        protected Base ghostBase;

        protected Base targetBase;

        protected int connectionMask;

        private static Int3[] roomOffset = new Int3[4]
        {
            new Int3(1, 0, 3),
            new Int3(1, 0, -1),
            new Int3(3, 0, 1),
            new Int3(-1, 0, 1)
        };

        public Base TargetBase => targetBase;

        protected static LayerMask placeLayerMask => ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Trigger")));

        public Base GhostBase => ghostBase;

        public Int3 TargetOffset => targetOffset;

        protected virtual Base.CellType GetCellType()
        {
            return ghostBase.GetCell(Int3.zero);
        }

        public void Start()
        {
            DisableGhostModelScripts();
            RecalculateBounds();
        }

        public void RecalculateTargetOffset()
        {
            Vector3 point = ghostBase.GridToWorld(Int3.zero);
            targetOffset = targetBase.WorldToGrid(point);
        }

        public virtual void SetupGhost()
        {
        }

        public virtual bool UpdatePlacement(Transform camera, float placeMaxDistance, out bool positionFound, out bool geometryChanged, ConstructableBase ghostModelParentConstructableBase)
        {
            positionFound = false;
            geometryChanged = false;
            return false;
        }

        public void Place()
        {
            OnPlace();
            ghostBase.RebuildGeometry();
            DisableGhostModelScripts();
            RecalculateBounds();
            ConstructableBase componentInParent = GetComponentInParent<ConstructableBase>();
            if (targetBase != null)
            {
                componentInParent.transform.parent = targetBase.transform;
            }
            else if ((bool)LargeWorld.main)
            {
                LargeWorldEntity largeWorldEntity = componentInParent.gameObject.AddComponent<LargeWorldEntity>();
                largeWorldEntity.cellLevel = LargeWorldEntity.CellLevel.Global;
                LargeWorld.main.streamer.cellManager.RegisterEntity(largeWorldEntity);
            }
        }

        public bool PlaceWithBoundsCast(Vector3 origin, Vector3 forward, float placeDefaultDistance, Vector3 offsetWorld, float minHeight, float maxHeight, out Vector3 center)
        {
            bool result = false;
            Vector3 vector = origin + forward * placeDefaultDistance;
            Bounds aaBounds = Builder.aaBounds;
            Quaternion orientation = Quaternion.LookRotation(new Vector3(forward.x, 0f, forward.z).normalized, Vector3.up);
            Vector3 extents = aaBounds.extents;
            int layerMask = 1 << LayerID.TerrainCollider;
            center = origin - offsetWorld;
            Vector3 vector2 = forward;
            float maxDistance = placeDefaultDistance;
            if (Physics.BoxCast(center, extents, vector2, out var hitInfo, orientation, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                float distance = hitInfo.distance;
                Vector3 vector3 = center + vector2 * distance;
                center = vector3 + Vector3.up * minHeight;
                result = true;
            }
            else
            {
                center = vector - offsetWorld;
                vector2 = Vector3.down;
                maxDistance = maxHeight;
                if (Physics.BoxCast(center, extents, vector2, out hitInfo, orientation, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
                {
                    float distance2 = hitInfo.distance;
                    Vector3 vector4 = center + vector2 * distance2;
                    if (distance2 < minHeight)
                    {
                        center = vector4 + Vector3.up * minHeight;
                    }
                    result = true;
                }
            }
            return result;
        }

        public virtual void Finish()
        {
            ConstructableBase componentInParent = GetComponentInParent<ConstructableBase>();
            if (targetBase == null)
            {
                if (!PrefabDatabase.TryGetPrefabForFilename("WorldEntities/Structures/Base", out var prefab))
                {
                    Debug.LogErrorFormat(this, "Failed to load Base prefab in BaseGhost.Finish()");
                    return;
                }
                GameObject gameObject = global::UnityEngine.Object.Instantiate(prefab, componentInParent.transform.position, componentInParent.transform.rotation);
                if ((bool)LargeWorld.main)
                {
                    LargeWorld.main.streamer.cellManager.RegisterEntity(gameObject);
                }
                targetBase = gameObject.GetComponent<Base>();
            }
            targetOffset = targetBase.WorldToGrid(base.transform.position);
            targetBase.CopyFrom(ghostBase, ghostBase.Bounds, targetOffset);
        }

        protected virtual void OnPlace()
        {
        }

        private void RecalculateBounds()
        {
            _ = GetComponentInParent<ConstructableBase>() == null;
        }

        public void ClearTargetBase()
        {
            targetBase = null;
            targetOffset = Int3.zero;
        }

        private void RecalculateConnectionMask()
        {
            connectionMask = 0;
            switch (GetCellType())
            {
                case Base.CellType.Room:
                    connectionMask = 15;
                    break;
                case Base.CellType.Observatory:
                {
                    bool num = targetBase.IsValidObsConnection(Base.GetAdjacent(targetOffset, Base.Direction.North), Base.Direction.South);
                    bool flag = targetBase.IsValidObsConnection(Base.GetAdjacent(targetOffset, Base.Direction.East), Base.Direction.West);
                    bool flag2 = targetBase.IsValidObsConnection(Base.GetAdjacent(targetOffset, Base.Direction.South), Base.Direction.North);
                    bool flag3 = targetBase.IsValidObsConnection(Base.GetAdjacent(targetOffset, Base.Direction.West), Base.Direction.East);
                    if (num)
                    {
                        connectionMask = 1;
                    }
                    else if (flag)
                    {
                        connectionMask = 4;
                    }
                    else if (flag2)
                    {
                        connectionMask = 2;
                    }
                    else if (flag3)
                    {
                        connectionMask = 8;
                    }
                    break;
                }
                case Base.CellType.MapRoom:
                    connectionMask = 12;
                    break;
                case Base.CellType.MapRoomRotated:
                    connectionMask = 3;
                    break;
            }
        }

        public void Deconstruct(Base targetBase, Int3.Bounds bounds, Base.Face? face, Base.FaceType faceType)
        {
            if (targetBase.GetCell(bounds.mins) == Base.CellType.Empty)
            {
                Debug.LogError("Deconstructing empty cell");
            }
            this.targetBase = targetBase;
            if (face.HasValue)
            {
                Base.Face face2 = face.Value;
                Base.Face face3 = default(Base.Face);
                Int3 exit;
                if (faceType != Base.FaceType.Ladder)
                {
                    face3 = new Base.Face(face2.cell - bounds.mins, face2.direction);
                    ghostBase.SetSize(bounds.size);
                    ghostBase.AllocateMasks();
                    if (faceType != Base.FaceType.WaterPark)
                    {
                        ghostBase.SetFaceMask(face3, isMasked: true);
                        ghostBase.SetFaceMask(Base.GetAdjacentFace(face3), isMasked: true);
                    }
                    else
                    {
                        Base.Direction[] allDirections = Base.AllDirections;
                        foreach (Base.Direction direction in allDirections)
                        {
                            Base.Face face4 = new Base.Face(face2.cell - bounds.mins, direction);
                            ghostBase.SetFaceMask(face4, isMasked: true);
                            ghostBase.SetFaceMask(Base.GetAdjacentFace(face4), isMasked: true);
                        }
                    }
                }
                else if (targetBase.GetLadderExitCell(face2.cell, face2.direction, out exit))
                {
                    Base.Face face5 = new Base.Face(exit, Base.ReverseDirection(face2.direction));
                    bounds = bounds.Union(exit);
                    if (face2.direction == Base.Direction.Below)
                    {
                        Base.Face face6 = face2;
                        face2 = face5;
                        face5 = face6;
                    }
                    face3 = new Base.Face(face2.cell - bounds.mins, face2.direction);
                    Base.Face face7 = new Base.Face(face5.cell - bounds.mins, face5.direction);
                    ghostBase.SetSize(bounds.size);
                    ghostBase.AllocateMasks();
                    ghostBase.SetFaceMask(face3, isMasked: true);
                    ghostBase.SetFaceMask(face7, isMasked: true);
                    for (int j = 1; j < face7.cell.y; j++)
                    {
                        Int3 cell = face7.cell;
                        cell.y = j;
                        Base.Face face8 = new Base.Face(cell, BaseAddLadderGhost.ladderFaceDir);
                        ghostBase.SetFaceMask(face8, isMasked: true);
                    }
                }
                else
                {
                    Debug.LogError("Could not find ladder exit");
                }
            }
            targetOffset = bounds.mins;
            ghostBase.CopyFrom(targetBase, bounds, targetOffset * -1);
            DisableGhostModelScripts();
            RecalculateBounds();
        }

        protected void AttachCorridorConnectors(bool disableColliders = true)
        {
            if (GetCellType() != Base.CellType.Room && GetCellType() != Base.CellType.Corridor && GetCellType() != Base.CellType.Observatory && GetCellType() != Base.CellType.Moonpool && GetCellType() != Base.CellType.MapRoom && GetCellType() != Base.CellType.MapRoomRotated)
            {
                return;
            }
            Base.Direction[] horizontalDirections = Base.HorizontalDirections;
            foreach (Base.Direction direction in horizontalDirections)
            {
                bool flag = true;
                if (((GetCellType() != Base.CellType.Corridor) ? ((1 << (int)direction) & connectionMask) : (ghostBase.GetCellConnections(Int3.zero) & (1 << (int)direction))) == 0)
                {
                    continue;
                }
                Int3 cell = targetOffset;
                if (GetCellType() == Base.CellType.Room || GetCellType() == Base.CellType.MapRoom || GetCellType() == Base.CellType.MapRoomRotated)
                {
                    cell += roomOffset[(int)direction];
                }
                else
                {
                    cell += Base.DirectionOffset[(int)direction];
                }
                targetBase.GetCell(cell);
                int cellConnections = targetBase.GetCellConnections(cell);
                int num = 1 << (int)Base.ReverseDirection(direction);
                if ((cellConnections & num) == 0)
                {
                    continue;
                }
                Transform transform = null;
                if (!(transform = ghostBase.GetCellObject(Int3.zero)))
                {
                    transform = ghostBase.CreateCellObject(Int3.zero);
                }
                if (GetCellType() == Base.CellType.Room || GetCellType() == Base.CellType.MapRoom || GetCellType() == Base.CellType.MapRoomRotated)
                {
                    Transform target = ghostBase.SpawnCorridorConnector(Int3.zero, direction, transform, Int3.zero);
                    if (disableColliders)
                    {
                        DisableColliders(target);
                    }
                }
                if (targetBase.GetCell(cell) == Base.CellType.Room || targetBase.GetCell(cell) == Base.CellType.MapRoom || targetBase.GetCell(cell) == Base.CellType.MapRoomRotated)
                {
                    Int3 @int = targetBase.NormalizeCell(cell) - targetOffset;
                    Transform target2 = ghostBase.SpawnCorridorConnector(@int, Base.ReverseDirection(direction), transform, @int);
                    if (disableColliders)
                    {
                        DisableColliders(target2);
                    }
                }
            }
        }

        protected void RebuildGhostGeometry()
        {
            ghostBase.RebuildGeometry();
            DisableColliders(base.transform);
            DisableGhostModelScripts();
            RecalculateBounds();
        }

        private static void DisableColliders(Transform target)
        {
            target.GetComponentsInChildren(includeInactive: false, sColliders);
            for (int i = 0; i < sColliders.Count; i++)
            {
                sColliders[i].enabled = false;
            }
            sColliders.Clear();
        }

        private void DisableGhostModelScripts()
        {
            GetComponentsInChildren(includeInactive: false, sMonoBehaviours);
            for (int i = 0; i < sMonoBehaviours.Count; i++)
            {
                MonoBehaviour monoBehaviour = sMonoBehaviours[i];
                if (monoBehaviour.gameObject != base.gameObject)
                {
                    monoBehaviour.enabled = false;
                }
            }
            sMonoBehaviours.Clear();
        }

        protected static Base FindBase(Transform camera, float searchDistance = 20f)
        {
            if (Physics.SphereCast(new Ray(camera.position, camera.forward), 0.5f, out var hitInfo, searchDistance, placeLayerMask.value))
            {
                Base componentInParent = hitInfo.collider.GetComponentInParent<Base>();
                if (componentInParent != null && componentInParent.GetComponent<BaseGhost>() == null)
                {
                    return componentInParent;
                }
            }
            int num = global::UWE.Utils.OverlapSphereIntoSharedBuffer(camera.position + camera.forward * searchDistance * 0.5f, searchDistance * 0.5f, placeLayerMask.value);
            for (int i = 0; i < num; i++)
            {
                Base componentInParent2 = global::UWE.Utils.sharedColliderBuffer[i].GetComponentInParent<Base>();
                if (componentInParent2 != null && componentInParent2.GetComponent<BaseGhost>() == null)
                {
                    return componentInParent2;
                }
            }
            return null;
        }

        private void Awake()
        {
            targetBase = base.transform.parent.GetComponentInParent<Base>();
            ghostBase = GetComponent<Base>();
            ghostBase.onPostRebuildGeometry += OnPostRebuildGeometry;
            ghostBase.isGhost = true;
        }

        private void OnDestroy()
        {
            if ((bool)ghostBase)
            {
                ghostBase.onPostRebuildGeometry -= OnPostRebuildGeometry;
            }
        }

        private void OnPostRebuildGeometry(Base b)
        {
            if ((bool)targetBase && ghostBase.GetCellMask(Int3.zero))
            {
                RecalculateConnectionMask();
                AttachCorridorConnectors(disableColliders: false);
            }
        }
    }
}
