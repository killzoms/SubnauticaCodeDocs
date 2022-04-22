using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class BaseAddMapRoomGhost : BaseGhost
    {
        private static Base.CellType rotated = Base.CellType.MapRoomRotated;

        private static Base.CellType unrotated = Base.CellType.MapRoom;

        private static byte rotatedMask = 3;

        private static byte unrotatedMask = 12;

        private Base.CellType cellType = unrotated;

        public float maxHeightFromTerrain = 10f;

        public float minHeightFromTerrain = 2f;

        protected override Base.CellType GetCellType()
        {
            return cellType;
        }

        public override void SetupGhost()
        {
            base.SetupGhost();
            connectionMask = ((cellType == rotated) ? rotatedMask : unrotatedMask);
            Int3 size = Base.CellSize[8];
            ghostBase.SetSize(size);
            ghostBase.SetCell(Int3.zero, cellType);
            RebuildGhostGeometry();
            Builder.ShowRotationControlsHint();
        }

        public override bool UpdatePlacement(Transform camera, float placeMaxDistance, out bool positionFound, out bool geometryChanged, ConstructableBase ghostModelParentConstructableBase)
        {
            positionFound = false;
            geometryChanged = false;
            UpdateRotation(ref geometryChanged);
            float placeDefaultDistance = ghostModelParentConstructableBase.placeDefaultDistance;
            Int3 @int = Base.CellSize[8];
            Vector3 direction = Vector3.Scale((@int - 1).ToVector3(), Base.halfCellSize);
            Vector3 position = camera.position;
            Vector3 forward = camera.forward;
            targetBase = BaseGhost.FindBase(camera);
            bool flag;
            if (targetBase != null)
            {
                positionFound = true;
                flag = true;
                Vector3 vector = position + forward * placeDefaultDistance;
                Vector3 vector2 = targetBase.transform.TransformDirection(direction);
                Int3 int2 = targetBase.WorldToGrid(vector - vector2);
                Int3 maxs = int2 + @int - 1;
                Int3.Bounds bounds = new Int3.Bounds(int2, maxs);
                foreach (Int3 item in bounds)
                {
                    if (targetBase.GetCell(item) != 0 || targetBase.IsCellUnderConstruction(item))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    foreach (Int3 item2 in bounds)
                    {
                        Base.CellType cell = targetBase.GetCell(Base.GetAdjacent(item2, Base.Direction.Above));
                        Base.CellType cell2 = targetBase.GetCell(Base.GetAdjacent(item2, Base.Direction.Below));
                        flag = flag && cell == Base.CellType.Empty && cell2 == Base.CellType.Empty;
                    }
                }
                if (targetOffset != int2)
                {
                    targetOffset = int2;
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

        private void UpdateRotation(ref bool geometryChanged)
        {
            if (GameInput.GetButtonDown(Builder.buttonRotateCW) || GameInput.GetButtonDown(Builder.buttonRotateCCW))
            {
                if (cellType == unrotated)
                {
                    cellType = rotated;
                    connectionMask = rotatedMask;
                }
                else
                {
                    cellType = unrotated;
                    connectionMask = unrotatedMask;
                }
                ghostBase.SetCell(Int3.zero, cellType);
                RebuildGhostGeometry();
                geometryChanged = true;
            }
        }
    }
}
