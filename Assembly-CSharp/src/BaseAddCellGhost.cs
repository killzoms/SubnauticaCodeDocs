using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class BaseAddCellGhost : BaseGhost
    {
        public Base.CellType cellType;

        public float maxHeightFromTerrain = 10f;

        public float minHeightFromTerrain = 2f;

        private static readonly List<KeyValuePair<Base.Face, Base.FaceType>> overrideFaces = new List<KeyValuePair<Base.Face, Base.FaceType>>();

        protected override Base.CellType GetCellType()
        {
            return cellType;
        }

        public override void SetupGhost()
        {
            base.SetupGhost();
            Int3 size = Base.CellSize[(uint)cellType];
            ghostBase.SetSize(size);
            ghostBase.SetCell(Int3.zero, cellType);
            RebuildGhostGeometry();
        }

        public override bool UpdatePlacement(Transform camera, float placeMaxDistance, out bool positionFound, out bool geometryChanged, ConstructableBase ghostModelParentConstructableBase)
        {
            positionFound = false;
            geometryChanged = false;
            overrideFaces.Clear();
            float placeDefaultDistance = ghostModelParentConstructableBase.placeDefaultDistance;
            Int3 @int = Base.CellSize[(uint)cellType];
            Vector3 direction = Vector3.Scale((@int - 1).ToVector3(), Base.halfCellSize);
            Vector3 position = camera.position;
            Vector3 forward = camera.forward;
            if (cellType == Base.CellType.Moonpool)
            {
                targetBase = BaseGhost.FindBase(camera, 30f);
            }
            else
            {
                targetBase = BaseGhost.FindBase(camera);
            }
            bool flag;
            if (targetBase != null)
            {
                positionFound = true;
                flag = true;
                Vector3 vector = position + forward * placeDefaultDistance;
                Vector3 vector2 = targetBase.transform.TransformDirection(direction);
                Int3 int2 = targetBase.WorldToGrid(vector - vector2);
                Int3 maxs = int2 + @int - 1;
                foreach (Int3 item in new Int3.Bounds(int2, maxs))
                {
                    if (targetBase.GetCell(item) != 0 || targetBase.IsCellUnderConstruction(item))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    if (cellType == Base.CellType.Foundation)
                    {
                        Int3.Bounds bounds = targetBase.Bounds;
                        int y = bounds.mins.y;
                        int y2 = bounds.maxs.y;
                        foreach (Int3 item2 in new Int3.Bounds(int2, new Int3(maxs.x, int2.y, maxs.z)))
                        {
                            Int3 current2 = item2;
                            for (int num = int2.y - 1; num >= y; num--)
                            {
                                current2.y = num;
                                if (targetBase.IsCellUnderConstruction(current2) || targetBase.GetCell(current2) != 0)
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                break;
                            }
                            for (int i = maxs.y + 1; i <= y2; i++)
                            {
                                current2.y = i;
                                Base.CellType cell = targetBase.GetCell(current2);
                                if (targetBase.IsCellUnderConstruction(current2) || cell == Base.CellType.Foundation || cell == Base.CellType.Moonpool)
                                {
                                    flag = false;
                                    break;
                                }
                                if (i == maxs.y + 1 && (cell == Base.CellType.Observatory || cell == Base.CellType.MapRoom || cell == Base.CellType.MapRoomRotated))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                break;
                            }
                        }
                    }
                    else if (cellType == Base.CellType.Moonpool)
                    {
                        foreach (Int3 item3 in new Int3.Bounds(Int3.zero, @int - 1))
                        {
                            Base.CellType cell2 = targetBase.GetCell(Base.GetAdjacent(int2 + item3, Base.Direction.Above));
                            Base.CellType cell3 = targetBase.GetCell(Base.GetAdjacent(int2 + item3, Base.Direction.Below));
                            flag = flag && cell2 == Base.CellType.Empty && cell3 == Base.CellType.Empty;
                        }
                    }
                    else if (cellType == Base.CellType.Room)
                    {
                        connectionMask = 15;
                        Int3 adjacent = Base.GetAdjacent(int2, Base.Direction.Below);
                        bool flag2 = targetBase.GetRawCellType(adjacent) == Base.CellType.Room;
                        bool flag3 = targetBase.CompareRoomCellTypes(adjacent, Base.CellType.Empty);
                        bool flag4 = targetBase.CompareRoomCellTypes(adjacent, Base.CellType.Foundation, hasAny: true);
                        flag = flag && (flag2 || flag3 || flag4);
                        Int3 adjacent2 = Base.GetAdjacent(int2, Base.Direction.Above);
                        bool flag5 = targetBase.GetRawCellType(adjacent2) == Base.CellType.Room;
                        bool flag6 = targetBase.CompareRoomCellTypes(adjacent2, Base.CellType.Empty);
                        flag = flag && (flag5 || flag6);
                        if (flag)
                        {
                            if (flag5)
                            {
                                overrideFaces.Add(new KeyValuePair<Base.Face, Base.FaceType>(new Base.Face(Int3.zero, Base.Direction.Above), Base.FaceType.Hole));
                            }
                            if (flag2)
                            {
                                overrideFaces.Add(new KeyValuePair<Base.Face, Base.FaceType>(new Base.Face(Int3.zero, Base.Direction.Below), Base.FaceType.Hole));
                            }
                        }
                    }
                    else if (cellType == Base.CellType.Observatory)
                    {
                        flag &= targetBase.GetCell(Base.GetAdjacent(int2, Base.Direction.Below)) == Base.CellType.Empty && targetBase.GetCell(Base.GetAdjacent(int2, Base.Direction.Above)) == Base.CellType.Empty;
                        if (flag)
                        {
                            bool num2 = targetBase.IsValidObsConnection(Base.GetAdjacent(int2, Base.Direction.North), Base.Direction.South);
                            bool flag7 = targetBase.IsValidObsConnection(Base.GetAdjacent(int2, Base.Direction.East), Base.Direction.West);
                            bool flag8 = targetBase.IsValidObsConnection(Base.GetAdjacent(int2, Base.Direction.South), Base.Direction.North);
                            bool flag9 = targetBase.IsValidObsConnection(Base.GetAdjacent(int2, Base.Direction.West), Base.Direction.East);
                            flag = num2 || flag7 || flag8 || flag9;
                            if (num2)
                            {
                                connectionMask = 1;
                            }
                            else if (flag7)
                            {
                                connectionMask = 4;
                            }
                            else if (flag8)
                            {
                                connectionMask = 2;
                            }
                            else if (flag9)
                            {
                                connectionMask = 8;
                            }
                        }
                    }
                }
                if (targetOffset != int2)
                {
                    targetOffset = int2;
                    ghostBase.SetCell(Int3.zero, cellType);
                    for (int j = 0; j < overrideFaces.Count; j++)
                    {
                        KeyValuePair<Base.Face, Base.FaceType> keyValuePair = overrideFaces[j];
                        ghostBase.SetFace(keyValuePair.Key, keyValuePair.Value);
                    }
                    overrideFaces.Clear();
                    RebuildGhostGeometry();
                    geometryChanged = true;
                }
                ghostModelParentConstructableBase.transform.position = targetBase.GridToWorld(int2);
                ghostModelParentConstructableBase.transform.rotation = targetBase.transform.rotation;
            }
            else
            {
                Vector3 offsetWorld = ghostModelParentConstructableBase.transform.TransformDirection(direction);
                flag = PlaceWithBoundsCast(position, forward, placeDefaultDistance, offsetWorld, minHeightFromTerrain, maxHeightFromTerrain, out var center);
                ghostModelParentConstructableBase.transform.position = center;
                if (flag)
                {
                    targetOffset = Int3.zero;
                }
            }
            return flag;
        }

        public bool CompareRoomCellTypes(Base targetBase, Int3 startCell, Base.CellType compareType)
        {
            bool result = true;
            Int3 @int = Base.CellSize[1];
            Int3.Bounds bounds = new Int3.Bounds(startCell, startCell + @int - 1);
            for (int i = bounds.mins.x; i <= bounds.maxs.x; i++)
            {
                for (int j = bounds.mins.z; j <= bounds.maxs.z; j++)
                {
                    if (targetBase.GetCell(new Int3(i, startCell.y, j)) != compareType)
                    {
                        result = false;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
