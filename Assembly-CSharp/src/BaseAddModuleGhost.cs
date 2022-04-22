using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class BaseAddModuleGhost : BaseGhost, IProtoEventListener
    {
        [NonSerialized]
        [ProtoMember(1)]
        public Base.Face? anchoredFace;

        public Base.FaceType faceType;

        public GameObject modulePrefab;

        private Base.Direction direction;

        private List<Base.Direction> directions;

        public override void SetupGhost()
        {
            base.SetupGhost();
            UpdateSize(Int3.one);
            direction = Base.Direction.North;
            directions = new List<Base.Direction>(Base.HorizontalDirections);
            ErrorMessage.AddMessage(Language.main.GetFormat("GhostRotateInputHint", uGUI.FormatButton(GameInput.Button.CycleNext), uGUI.FormatButton(GameInput.Button.CyclePrev)));
        }

        public override bool UpdatePlacement(Transform camera, float placeMaxDistance, out bool positionFound, out bool geometryChanged, ConstructableBase ghostModelParentConstructableBase)
        {
            positionFound = false;
            geometryChanged = false;
            if (GameInput.GetButtonDown(GameInput.Button.CycleNext))
            {
                int num = directions.IndexOf(direction);
                if (num == 0)
                {
                    direction = directions[directions.Count - 1];
                }
                else
                {
                    direction = directions[num - 1];
                }
                geometryChanged = true;
            }
            else if (GameInput.GetButtonDown(GameInput.Button.CyclePrev))
            {
                int num2 = directions.IndexOf(direction);
                if (num2 == directions.Count - 1)
                {
                    direction = directions[0];
                }
                else
                {
                    direction = directions[num2 + 1];
                }
                geometryChanged = true;
            }
            Player main = Player.main;
            if (main == null || main.currentSub == null || !main.currentSub.isBase)
            {
                geometryChanged = SetupInvalid();
                return false;
            }
            targetBase = BaseGhost.FindBase(camera);
            if (targetBase == null)
            {
                geometryChanged = SetupInvalid();
                return false;
            }
            targetBase.transform.InverseTransformDirection(camera.forward);
            Base.Face face = new Base.Face(targetBase.WorldToGrid(camera.position), direction);
            if (!targetBase.CanSetModule(ref face, faceType))
            {
                geometryChanged = SetupInvalid();
                return false;
            }
            Int3 @int = targetBase.NormalizeCell(face.cell);
            Base.Face face2 = new Base.Face(face.cell - targetBase.GetAnchor(), face.direction);
            if (!anchoredFace.HasValue || anchoredFace.Value != face2)
            {
                anchoredFace = face2;
                Base.CellType cell = targetBase.GetCell(@int);
                Int3 int2 = Base.CellSize[(uint)cell];
                geometryChanged = UpdateSize(int2);
                ghostBase.CopyFrom(targetBase, new Int3.Bounds(@int, @int + int2 - 1), @int * -1);
                Int3 cell2 = face.cell - @int;
                Base.Face face3 = new Base.Face(cell2, face.direction);
                ghostBase.SetFace(face3, faceType);
                ghostBase.ClearMasks();
                ghostBase.SetFaceMask(face3, isMasked: true);
                RebuildGhostGeometry();
                geometryChanged = true;
            }
            ghostModelParentConstructableBase.transform.position = targetBase.GridToWorld(@int);
            ghostModelParentConstructableBase.transform.rotation = targetBase.transform.rotation;
            positionFound = true;
            if (targetBase.IsCellUnderConstruction(face.cell))
            {
                return false;
            }
            return true;
        }

        private bool UpdateSize(Int3 size)
        {
            if (size == ghostBase.GetSize())
            {
                return false;
            }
            ghostBase.ClearGeometry();
            ghostBase.SetSize(size);
            ghostBase.AllocateMasks();
            RebuildGhostGeometry();
            anchoredFace = null;
            return true;
        }

        private bool SetupInvalid()
        {
            if (!anchoredFace.HasValue)
            {
                return false;
            }
            Int3.RangeEnumerator allCells = ghostBase.AllCells;
            while (allCells.MoveNext())
            {
                ghostBase.ClearCell(allCells.Current);
            }
            RebuildGhostGeometry();
            anchoredFace = null;
            return true;
        }

        public override void Finish()
        {
            Base.Face value = anchoredFace.Value;
            value.cell += targetBase.GetAnchor();
            if (targetBase.SpawnModule(modulePrefab, value) != null)
            {
                base.Finish();
            }
            else
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            Vector3 point = ghostBase.GridToWorld(Int3.zero);
            targetOffset = targetBase.WorldToGrid(point);
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
        }
    }
}
