using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class BaseAddCorridorGhost : BaseGhost
    {
        public enum Corridor
        {
            I,
            L,
            T,
            X
        }

        public bool isGlass;

        public Corridor corridor;

        public float maxHeightFromTerrain = 10f;

        public float minHeightFromTerrain = 2f;

        private int corridorType;

        private int rotation;

        private static bool[,] Shapes = new bool[4, 4]
        {
            { true, false, true, false },
            { true, true, false, false },
            { true, true, false, true },
            { true, true, true, true }
        };

        protected override Base.CellType GetCellType()
        {
            return Base.CellType.Corridor;
        }

        public override void SetupGhost()
        {
            base.SetupGhost();
            ghostBase.SetSize(new Int3(1));
            rotation = 0;
            corridorType = CalculateCorridorType();
            ghostBase.SetCorridor(Int3.zero, corridorType, isGlass);
            RebuildGhostGeometry();
            if (corridor != Corridor.X)
            {
                Builder.ShowRotationControlsHint();
            }
        }

        public override bool UpdatePlacement(Transform camera, float placeMaxDistance, out bool positionFound, out bool geometryChanged, ConstructableBase ghostModelParentConstructableBase)
        {
            positionFound = false;
            geometryChanged = false;
            UpdateRotation(ref geometryChanged);
            float placeDefaultDistance = ghostModelParentConstructableBase.placeDefaultDistance;
            Vector3 position = camera.position;
            Vector3 forward = camera.forward;
            targetBase = BaseGhost.FindBase(camera);
            bool flag;
            if (targetBase != null)
            {
                positionFound = true;
                flag = true;
                Vector3 point = position + forward * placeDefaultDistance;
                Int3 size = new Int3(1);
                Int3 @int = targetBase.WorldToGrid(point);
                if (targetBase.GetCell(@int) != 0 || targetBase.IsCellUnderConstruction(@int))
                {
                    @int = targetBase.PickCell(camera, point, size);
                }
                if (targetBase.GetCell(@int) != 0 && targetBase.WorldToGrid(camera.position) == @int && targetBase.PickFace(camera, out var face))
                {
                    @int = Base.GetAdjacent(face);
                }
                int y = targetBase.Bounds.mins.y;
                Int3 cell = @int;
                if (!CheckCorridorConnection(@int))
                {
                    for (int num = @int.y - 1; num >= y; num--)
                    {
                        cell.y = num;
                        if (targetBase.IsCellUnderConstruction(cell))
                        {
                            flag = false;
                            break;
                        }
                        if (targetBase.GetCell(cell) != 0)
                        {
                            if (num < @int.y - 1)
                            {
                                flag = false;
                            }
                            break;
                        }
                    }
                }
                if (!targetBase.HasSpaceFor(@int, size))
                {
                    flag = false;
                }
                Base.CellType cell2 = targetBase.GetCell(Base.GetAdjacent(@int, Base.Direction.Above));
                Base.CellType cell3 = targetBase.GetCell(Base.GetAdjacent(@int, Base.Direction.Below));
                if (cell2 == Base.CellType.Room || cell2 == Base.CellType.Observatory || cell2 == Base.CellType.Moonpool || cell2 == Base.CellType.MapRoom || cell2 == Base.CellType.MapRoomRotated || cell3 == Base.CellType.Room || cell3 == Base.CellType.Observatory || cell3 == Base.CellType.Moonpool || cell3 == Base.CellType.MapRoom || cell3 == Base.CellType.MapRoomRotated)
                {
                    flag = false;
                }
                if (targetOffset != @int)
                {
                    targetOffset = @int;
                    RebuildGhostGeometry();
                    geometryChanged = true;
                }
                ghostModelParentConstructableBase.transform.position = targetBase.GridToWorld(@int);
                ghostModelParentConstructableBase.transform.rotation = targetBase.transform.rotation;
            }
            else
            {
                flag = PlaceWithBoundsCast(position, forward, placeDefaultDistance, Vector3.zero, minHeightFromTerrain, maxHeightFromTerrain, out var center);
                if (flag)
                {
                    ghostModelParentConstructableBase.transform.position = center;
                    targetOffset = Int3.zero;
                }
            }
            return flag;
        }

        private bool CheckCorridorConnection(Int3 cell)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Shapes[(int)corridor, i])
                {
                    Base.Direction direction = Base.HorizontalDirections[(i + rotation) % 4];
                    Int3 adjacent = Base.GetAdjacent(cell, direction);
                    Base.Direction direction2 = Base.ReverseDirection(direction);
                    if ((targetBase.GetCellConnections(adjacent) & (1 << (int)direction2)) != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private int CalculateCorridorType()
        {
            int num = 0;
            for (int i = 0; i < 4; i++)
            {
                if (Shapes[(int)corridor, i])
                {
                    num |= 1 << (int)Base.HorizontalDirections[(i + rotation) % 4];
                }
            }
            return num;
        }

        private void UpdateRotation(ref bool geometryChanged)
        {
            if (GameInput.GetButtonDown(Builder.buttonRotateCW))
            {
                rotation = (rotation + 4 - 1) % 4;
            }
            else
            {
                if (!GameInput.GetButtonDown(Builder.buttonRotateCCW))
                {
                    return;
                }
                rotation = (rotation + 1) % 4;
            }
            corridorType = CalculateCorridorType();
            ghostBase.SetCorridor(Int3.zero, corridorType, isGlass);
            RebuildGhostGeometry();
            geometryChanged = true;
        }
    }
}
