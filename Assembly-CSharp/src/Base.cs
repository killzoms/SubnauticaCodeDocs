using System;
using System.Collections;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [ProtoContract]
    public class Base : MonoBehaviour, IProtoEventListener
    {
        public delegate void BaseEventHandler(Base b);

        public delegate void BaseFaceEventHandler(Base b, Face face);

        public delegate void BaseResizeEventHandler(Base b, Int3 offset);

        public enum CellType : byte
        {
            Empty,
            Room,
            Foundation,
            OccupiedByOtherCell,
            Corridor,
            Observatory,
            Connector,
            Moonpool,
            MapRoom,
            MapRoomRotated,
            Count
        }

        public class CellTypeComparer : IEqualityComparer<CellType>
        {
            public bool Equals(CellType x, CellType y)
            {
                int num = (int)x;
                return num.Equals((int)y);
            }

            public int GetHashCode(CellType obj)
            {
                return (int)obj;
            }
        }

        public enum FaceType : byte
        {
            None = 0,
            Solid = 1,
            Window = 2,
            Hatch = 3,
            ObsoleteDoor = 4,
            Ladder = 5,
            Reinforcement = 6,
            BulkheadClosed = 7,
            BulkheadOpened = 8,
            Hole = 9,
            UpgradeConsole = 10,
            Planter = 11,
            FiltrationMachine = 12,
            WaterPark = 13,
            BioReactor = 14,
            NuclearReactor = 0xF,
            Count = 0x10,
            OccupiedByOtherFace = 0x80,
            OccupiedByNorthFace = 0x80,
            OccupiedByEastFace = 130,
            OccupiedBySouthFace = 129,
            OccupiedByWestFace = 131,
            OccupiedByAboveFace = 132,
            OccupiedByBelowFace = 133
        }

        public class FaceTypeComparer : IEqualityComparer<FaceType>
        {
            public bool Equals(FaceType x, FaceType y)
            {
                int num = (int)x;
                return num.Equals((int)y);
            }

            public int GetHashCode(FaceType obj)
            {
                return (int)obj;
            }
        }

        private struct PieceDef
        {
            public Transform prefab;

            public Int3 extraCells;

            public Vector3 offset;

            public Quaternion rotation;

            public PieceDef(GameObject prefab, Int3 extraCells, Quaternion rotation)
            {
                this.prefab = prefab.transform;
                this.extraCells = extraCells;
                this.rotation = rotation;
                offset = Int3.Scale(extraCells, halfCellSize);
            }
        }

        private struct CorridorFace
        {
            public Piece piece;

            public Quaternion rotation;

            public CorridorFace(Piece piece, Vector3 angles)
            {
                this.piece = piece;
                rotation = Quaternion.Euler(angles);
            }
        }

        private struct CorridorDef
        {
            public Piece piece;

            public Piece supportPiece;

            public Piece adjustableSupportPiece;

            public Quaternion rotation;

            public CorridorFace[,] faces;

            public Direction[] worldToLocal;

            public CorridorDef(Piece piece, Piece supportPiece, Piece adjustableSupportPiece)
            {
                this.piece = piece;
                this.supportPiece = supportPiece;
                this.adjustableSupportPiece = adjustableSupportPiece;
                rotation = Quaternion.identity;
                faces = new CorridorFace[6, 16];
                worldToLocal = AllDirections;
            }

            public void SetFace(Direction side, FaceType faceType, Piece piece, Vector3 angles)
            {
                faces[(int)side, (uint)faceType] = new CorridorFace(piece, angles);
            }

            public CorridorDef GetRotated(float yRotation)
            {
                CorridorDef result = this;
                result.rotation *= Quaternion.Euler(0f, yRotation, 0f);
                Quaternion inverse = result.rotation.GetInverse();
                result.worldToLocal = new Direction[6];
                Direction[] allDirections = AllDirections;
                foreach (Direction direction in allDirections)
                {
                    Vector3 normal = inverse * DirectionOffset[(int)direction].ToVector3();
                    result.worldToLocal[(int)direction] = NormalToDirection(normal);
                }
                return result;
            }
        }

        private struct RoomFace
        {
            public Int3 offset;

            public Direction direction;

            public Quaternion rotation;

            public Vector3 localOffset;

            public RoomFace(int x, int z, Direction direction, float yAngle, Vector3 localOffset = default(Vector3))
            {
                offset = new Int3(x, 0, z);
                this.direction = direction;
                rotation = Quaternion.Euler(0f, yAngle, 0f);
                this.localOffset = localOffset;
            }
        }

        private enum Piece
        {
            Invalid,
            Foundation,
            CorridorCap,
            CorridorWindow,
            CorridorHatch,
            CorridorBulkhead,
            CorridorIShapeGlass,
            CorridorLShapeGlass,
            CorridorIShape,
            CorridorLShape,
            CorridorTShape,
            CorridorXShape,
            CorridorIShapeGlassSupport,
            CorridorLShapeGlassSupport,
            CorridorIShapeSupport,
            CorridorLShapeSupport,
            CorridorTShapeSupport,
            CorridorIShapeGlassAdjustableSupport,
            CorridorLShapeGlassAdjustableSupport,
            CorridorIShapeAdjustableSupport,
            CorridorLShapeAdjustableSupport,
            CorridorTShapeAdjustableSupport,
            CorridorXShapeAdjustableSupport,
            CorridorIShapeCoverSide,
            CorridorIShapeWindowSide,
            CorridorIShapeWindowTop,
            CorridorIShapeWindowBottom,
            CorridorIShapeReinforcementSide,
            CorridorIShapeHatchSide,
            CorridorIShapeHatchTop,
            CorridorIShapeHatchBottom,
            CorridorIShapeLadderTop,
            CorridorIShapeLadderBottom,
            CorridorIShapePlanterSide,
            CorridorTShapeWindowTop,
            CorridorTShapeWindowBottom,
            CorridorTShapeHatchTop,
            CorridorTShapeHatchBottom,
            CorridorTShapeLadderTop,
            CorridorTShapeLadderBottom,
            CorridorXShapeWindowTop,
            CorridorXShapeWindowBottom,
            CorridorXShapeHatchTop,
            CorridorXShapeHatchBottom,
            CorridorXShapeLadderTop,
            CorridorXShapeLadderBottom,
            CorridorCoverIShapeBottomExtClosed,
            CorridorCoverIShapeBottomExtOpened,
            CorridorCoverIShapeBottomIntClosed,
            CorridorCoverIShapeBottomIntOpened,
            CorridorCoverIShapeTopExtClosed,
            CorridorCoverIShapeTopExtOpened,
            CorridorCoverIShapeTopIntClosed,
            CorridorCoverIShapeTopIntOpened,
            CorridorCoverTShapeBottomExtClosed,
            CorridorCoverTShapeBottomExtOpened,
            CorridorCoverTShapeBottomIntClosed,
            CorridorCoverTShapeBottomIntOpened,
            CorridorCoverTShapeTopExtClosed,
            CorridorCoverTShapeTopExtOpened,
            CorridorCoverTShapeTopIntClosed,
            CorridorCoverTShapeTopIntOpened,
            CorridorCoverXShapeBottomExtClosed,
            CorridorCoverXShapeBottomExtOpened,
            CorridorCoverXShapeBottomIntClosed,
            CorridorCoverXShapeBottomIntOpened,
            CorridorCoverXShapeTopExtClosed,
            CorridorCoverXShapeTopExtOpened,
            CorridorCoverXShapeTopIntClosed,
            CorridorCoverXShapeTopIntOpened,
            ConnectorTube,
            ConnectorTubeWindow,
            ConnectorCap,
            ConnectorLadder,
            Room,
            RoomCorridorConnector,
            RoomCoverSide,
            RoomCoverSideVariant,
            RoomExteriorBottom,
            RoomExteriorFoundationBottom,
            RoomExteriorTop,
            RoomReinforcementSide,
            RoomWindowSide,
            RoomCoverBottom,
            RoomCoverTop,
            RoomLadderBottom,
            RoomLadderTop,
            RoomAdjustableSupport,
            RoomHatch,
            RoomPlanterSide,
            RoomFiltrationMachine,
            RoomWaterParkTop,
            RoomWaterParkBottom,
            RoomWaterParkHatch,
            RoomWaterParkSide,
            RoomInteriorBottom,
            RoomInteriorTop,
            RoomInteriorBottomHole,
            RoomInteriorTopHole,
            RoomBioReactor,
            RoomNuclearReactor,
            Observatory,
            ObservatoryCoverSide,
            ObservatoryCorridorConnector,
            ObservatoryHatch,
            Moonpool,
            MoonpoolCoverSide,
            MoonpoolCoverSideShort,
            MoonpoolReinforcementSide,
            MoonpoolReinforcementSideShort,
            MoonpoolWindowSide,
            MoonpoolWindowSideShort,
            MoonpoolUpgradeConsole,
            MoonpoolUpgradeConsoleShort,
            MoonpoolAdjustableSupport,
            MoonpoolHatch,
            MoonpoolHatchShort,
            MoonpoolCorridorConnector,
            MoonpoolCorridorConnectorShort,
            MoonpoolPlanterSide,
            MoonpoolPlanterSideShort,
            MapRoom,
            MapRoomCoverSide,
            MapRoomCorridorConnector,
            MapRoomHatch,
            MapRoomWindowSide,
            MapRoomPlanterSide,
            MapRoomReinforcementSide,
            Count
        }

        private struct DockedVehicle
        {
            public Int3 cellPosition;

            public Vehicle vehicle;
        }

        public enum Direction
        {
            North,
            South,
            East,
            West,
            Above,
            Below,
            Count
        }

        [ProtoContract]
        public struct Face : IEquatable<Face>
        {
            [ProtoMember(1)]
            public Int3 cell;

            [ProtoMember(2)]
            public Direction direction;

            public Face(Int3 cell, Direction direction)
            {
                this.cell = cell;
                this.direction = direction;
            }

            public override int GetHashCode()
            {
                int num = 923;
                num = 31 * num + cell.GetHashCode();
                return 31 * num + direction.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is Face)
                {
                    return Equals((Face)obj);
                }
                return false;
            }

            public bool Equals(Face other)
            {
                if (cell == other.cell)
                {
                    return direction == other.direction;
                }
                return false;
            }

            public static bool operator ==(Face lhs, Face rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Face lhs, Face rhs)
            {
                return !lhs.Equals(rhs);
            }

            public override string ToString()
            {
                return $"Face ({cell}) {direction}";
            }
        }

        private struct FaceDef
        {
            public Face face;

            public FaceType faceType;

            public FaceDef(int x, int y, int z, Direction direction, FaceType faceType)
            {
                face = new Face(new Int3(x, y, z), direction);
                this.faceType = faceType;
            }
        }

        private struct PieceData
        {
            public Piece piece;

            public string name;

            public Int3 extraCells;

            public IAssetBundleWrapperRequest request;

            public PieceData(Piece _piece, string _name)
            {
                piece = _piece;
                name = _name;
                extraCells = Int3.zero;
                request = null;
            }

            public PieceData(Piece _piece, string _name, Int3 _extraCells)
            {
                piece = _piece;
                name = _name;
                extraCells = _extraCells;
                request = null;
            }
        }

        public const string kMainPieceGeometry = "MainPieceGeometry";

        public const string basePiecesBundleName = "basegeneratorpieces";

        private static readonly List<BaseGhost> sGhosts = new List<BaseGhost>();

        private bool waitingForWorld;

        private float nextWorldPollTime;

        private static bool initialized;

        private static Transform cellPrefab;

        private static PieceDef[] pieces;

        private static CorridorDef[] corridors;

        private static CorridorDef[] glassCorridors;

        public static readonly Vector3 cellSize = new Vector3(5f, 3.5f, 5f);

        public static readonly Vector3 halfCellSize = cellSize * 0.5f;

        public static readonly CellTypeComparer sCellTypeComparer = new CellTypeComparer();

        public static readonly Int3[] CellSize = new Int3[10]
        {
            Int3.zero,
            new Int3(3, 1, 3),
            new Int3(2, 1, 2),
            Int3.zero,
            new Int3(1),
            new Int3(1),
            new Int3(1),
            new Int3(4, 1, 3),
            new Int3(3, 1, 3),
            new Int3(3, 1, 3)
        };

        public static readonly float[] CellPowerConsumption = new float[10]
        {
            0f,
            5f / 6f,
            0f,
            0f,
            0.0833333358f,
            0f,
            0f,
            1.66666663f,
            5f / 12f,
            5f / 12f
        };

        public static readonly FaceTypeComparer sFaceTypeComparer = new FaceTypeComparer();

        public static readonly TechType[] FaceToRecipe = new TechType[16]
        {
            TechType.None,
            TechType.BaseWall,
            TechType.BaseWindow,
            TechType.BaseHatch,
            TechType.BaseDoor,
            TechType.BaseLadder,
            TechType.BaseReinforcement,
            TechType.BaseBulkhead,
            TechType.BaseBulkhead,
            TechType.None,
            TechType.BaseUpgradeConsole,
            TechType.BasePlanter,
            TechType.BaseFiltrationMachine,
            TechType.BaseWaterPark,
            TechType.BaseBioReactor,
            TechType.BaseNuclearReactor
        };

        private static readonly float[] CellHullStrength = new float[10] { 0f, -1.25f, 2f, 0f, -1f, -3f, -0.5f, -5f, -1f, -1f };

        private static readonly float[] FaceHullStrength = new float[16]
        {
            0f, 0f, -1f, -1f, 0f, 0f, 7f, 3f, 3f, 0f,
            0f, 0f, -1f, 0f, 0f, 0f
        };

        private Dictionary<Piece, Piece> exteriorToInteriorPiece = new Dictionary<Piece, Piece>
        {
            {
                Piece.CorridorCoverIShapeBottomExtClosed,
                Piece.CorridorCoverIShapeBottomIntClosed
            },
            {
                Piece.CorridorCoverIShapeBottomExtOpened,
                Piece.CorridorCoverIShapeBottomIntOpened
            },
            {
                Piece.CorridorCoverIShapeTopExtClosed,
                Piece.CorridorCoverIShapeTopIntClosed
            },
            {
                Piece.CorridorCoverIShapeTopExtOpened,
                Piece.CorridorCoverIShapeTopIntOpened
            },
            {
                Piece.CorridorCoverTShapeBottomExtClosed,
                Piece.CorridorCoverTShapeBottomIntClosed
            },
            {
                Piece.CorridorCoverTShapeBottomExtOpened,
                Piece.CorridorCoverTShapeBottomIntOpened
            },
            {
                Piece.CorridorCoverTShapeTopExtClosed,
                Piece.CorridorCoverTShapeTopIntClosed
            },
            {
                Piece.CorridorCoverTShapeTopExtOpened,
                Piece.CorridorCoverTShapeTopIntOpened
            },
            {
                Piece.CorridorCoverXShapeBottomExtClosed,
                Piece.CorridorCoverXShapeBottomIntClosed
            },
            {
                Piece.CorridorCoverXShapeBottomExtOpened,
                Piece.CorridorCoverXShapeBottomIntOpened
            },
            {
                Piece.CorridorCoverXShapeTopExtClosed,
                Piece.CorridorCoverXShapeTopIntClosed
            },
            {
                Piece.CorridorCoverXShapeTopExtOpened,
                Piece.CorridorCoverXShapeTopIntOpened
            }
        };

        private const float kCoverOffset = 3.423f;

        private static readonly Quaternion[] corridorConnectorRotation = new Quaternion[4]
        {
            Quaternion.Euler(0f, 270f, 0f),
            Quaternion.Euler(0f, 90f, 0f),
            Quaternion.Euler(0f, 0f, 0f),
            Quaternion.Euler(0f, 180f, 0f)
        };

        private static readonly RoomFace[] roomFaces = new RoomFace[22]
        {
            new RoomFace(2, 1, Direction.East, 0f),
            new RoomFace(2, 0, Direction.South, 45f),
            new RoomFace(1, 0, Direction.South, 90f),
            new RoomFace(0, 0, Direction.West, 135f),
            new RoomFace(0, 1, Direction.West, 180f),
            new RoomFace(0, 2, Direction.North, 225f),
            new RoomFace(1, 2, Direction.North, 270f),
            new RoomFace(2, 2, Direction.East, 315f),
            new RoomFace(1, 0, Direction.Below, 0f, new Vector3(0f, 0f, -3.423f)),
            new RoomFace(0, 1, Direction.Below, 90f, new Vector3(-3.423f, 0f, 0f)),
            new RoomFace(1, 1, Direction.Below, 0f),
            new RoomFace(2, 1, Direction.Below, 270f, new Vector3(3.423f, 0f, 0f)),
            new RoomFace(1, 2, Direction.Below, 180f, new Vector3(0f, 0f, 3.423f)),
            new RoomFace(1, 0, Direction.Above, 0f, new Vector3(0f, 0f, -3.423f)),
            new RoomFace(0, 1, Direction.Above, 90f, new Vector3(-3.423f, 0f, 0f)),
            new RoomFace(1, 1, Direction.Above, 0f),
            new RoomFace(2, 1, Direction.Above, 270f, new Vector3(3.423f, 0f, 0f)),
            new RoomFace(1, 2, Direction.Above, 180f, new Vector3(0f, 0f, 3.423f)),
            new RoomFace(1, 1, Direction.East, 0f),
            new RoomFace(1, 1, Direction.South, 90f),
            new RoomFace(1, 1, Direction.West, 180f),
            new RoomFace(1, 1, Direction.North, 270f)
        };

        private static readonly RoomFace[] roomWaterParkFaces = new RoomFace[4]
        {
            new RoomFace(1, 1, Direction.East, 0f),
            new RoomFace(1, 1, Direction.South, 90f),
            new RoomFace(1, 1, Direction.West, 180f),
            new RoomFace(1, 1, Direction.North, 270f)
        };

        private static readonly RoomFace[] moonpoolFaces = new RoomFace[6]
        {
            new RoomFace(1, 0, Direction.South, 270f),
            new RoomFace(2, 0, Direction.South, 270f),
            new RoomFace(1, 2, Direction.North, 90f),
            new RoomFace(2, 2, Direction.North, 90f),
            new RoomFace(3, 1, Direction.East, 180f),
            new RoomFace(0, 1, Direction.West, 0f)
        };

        private static readonly FaceType[] constructFaceTypes = new FaceType[16]
        {
            FaceType.None,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.None,
            FaceType.None,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.None,
            FaceType.None
        };

        private static readonly FaceType[] deconstructFaceTypes = new FaceType[16]
        {
            FaceType.None,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.None,
            FaceType.None,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.Solid,
            FaceType.None,
            FaceType.None
        };

        private static readonly Piece[] observatoryFacePieces = new Piece[16]
        {
            Piece.ObservatoryCorridorConnector,
            Piece.ObservatoryCoverSide,
            Piece.Invalid,
            Piece.ObservatoryHatch,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid
        };

        private static readonly Piece[] mapRoomFacePieces = new Piece[16]
        {
            Piece.MapRoomCorridorConnector,
            Piece.MapRoomCoverSide,
            Piece.MapRoomWindowSide,
            Piece.MapRoomHatch,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid,
            Piece.Invalid
        };

        private static readonly bool[,] roomLadderPlaces = new bool[3, 3]
        {
            { false, true, false },
            { true, true, true },
            { false, true, false }
        };

        private static readonly Vector3[,] roomLadderExits = new Vector3[3, 3]
        {
            {
                Vector3.zero,
                new Vector3(2f, 0.3f, 5f),
                Vector3.zero
            },
            {
                new Vector3(5f, 0.3f, 2f),
                new Vector3(5f, 0.3f, 5.3f),
                new Vector3(5f, 0.3f, 8f)
            },
            {
                Vector3.zero,
                new Vector3(8f, 0.3f, 5f),
                Vector3.zero
            }
        };

        private static readonly Vector3 corridorLadderExit = new Vector3(0f, 0.7f, 0.3f);

        private static readonly Piece[,] roomFacePieces = new Piece[6, 16]
        {
            {
                Piece.RoomCorridorConnector,
                Piece.RoomCoverSide,
                Piece.RoomWindowSide,
                Piece.RoomHatch,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomReinforcementSide,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomPlanterSide,
                Piece.RoomFiltrationMachine,
                Piece.Invalid,
                Piece.RoomBioReactor,
                Piece.RoomNuclearReactor
            },
            {
                Piece.RoomCorridorConnector,
                Piece.RoomCoverSide,
                Piece.RoomWindowSide,
                Piece.RoomHatch,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomReinforcementSide,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomPlanterSide,
                Piece.RoomFiltrationMachine,
                Piece.Invalid,
                Piece.RoomBioReactor,
                Piece.RoomNuclearReactor
            },
            {
                Piece.RoomCorridorConnector,
                Piece.RoomCoverSide,
                Piece.RoomWindowSide,
                Piece.RoomHatch,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomReinforcementSide,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomPlanterSide,
                Piece.RoomFiltrationMachine,
                Piece.Invalid,
                Piece.RoomBioReactor,
                Piece.RoomNuclearReactor
            },
            {
                Piece.RoomCorridorConnector,
                Piece.RoomCoverSide,
                Piece.RoomWindowSide,
                Piece.RoomHatch,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomReinforcementSide,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomPlanterSide,
                Piece.RoomFiltrationMachine,
                Piece.Invalid,
                Piece.RoomBioReactor,
                Piece.RoomNuclearReactor
            },
            {
                Piece.Invalid,
                Piece.RoomCoverTop,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomLadderTop,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid
            },
            {
                Piece.Invalid,
                Piece.RoomCoverBottom,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomLadderBottom,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid
            }
        };

        private static readonly Piece[,] roomFaceCentralPieces = new Piece[6, 16]
        {
            {
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomBioReactor,
                Piece.RoomNuclearReactor
            },
            {
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomBioReactor,
                Piece.RoomNuclearReactor
            },
            {
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomBioReactor,
                Piece.RoomNuclearReactor
            },
            {
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomBioReactor,
                Piece.RoomNuclearReactor
            },
            {
                Piece.Invalid,
                Piece.RoomCoverTop,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomLadderTop,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomWaterParkTop,
                Piece.Invalid,
                Piece.Invalid
            },
            {
                Piece.Invalid,
                Piece.RoomCoverBottom,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomLadderBottom,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.RoomWaterParkBottom,
                Piece.Invalid,
                Piece.Invalid
            }
        };

        private static readonly Piece[,] moonpoolFacePieces = new Piece[4, 16]
        {
            {
                Piece.MoonpoolCorridorConnector,
                Piece.MoonpoolCoverSide,
                Piece.MoonpoolWindowSide,
                Piece.MoonpoolHatch,
                Piece.Invalid,
                Piece.Invalid,
                Piece.MoonpoolReinforcementSide,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.MoonpoolUpgradeConsole,
                Piece.MoonpoolPlanterSide,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid
            },
            {
                Piece.MoonpoolCorridorConnector,
                Piece.MoonpoolCoverSide,
                Piece.MoonpoolWindowSide,
                Piece.MoonpoolHatch,
                Piece.Invalid,
                Piece.Invalid,
                Piece.MoonpoolReinforcementSide,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.MoonpoolUpgradeConsole,
                Piece.MoonpoolPlanterSide,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid
            },
            {
                Piece.MoonpoolCorridorConnectorShort,
                Piece.MoonpoolCoverSideShort,
                Piece.MoonpoolWindowSideShort,
                Piece.MoonpoolHatchShort,
                Piece.Invalid,
                Piece.Invalid,
                Piece.MoonpoolReinforcementSideShort,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.MoonpoolUpgradeConsoleShort,
                Piece.MoonpoolPlanterSideShort,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid
            },
            {
                Piece.MoonpoolCorridorConnectorShort,
                Piece.MoonpoolCoverSideShort,
                Piece.MoonpoolWindowSideShort,
                Piece.MoonpoolHatchShort,
                Piece.Invalid,
                Piece.Invalid,
                Piece.MoonpoolReinforcementSideShort,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.MoonpoolUpgradeConsoleShort,
                Piece.MoonpoolPlanterSideShort,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid,
                Piece.Invalid
            }
        };

        public static readonly Direction[] HorizontalDirections = new Direction[4]
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

        public static readonly Direction[] VerticalDirections = new Direction[2]
        {
            Direction.Above,
            Direction.Below
        };

        public static readonly Direction[] AllDirections = new Direction[6]
        {
            Direction.North,
            Direction.South,
            Direction.East,
            Direction.West,
            Direction.Above,
            Direction.Below
        };

        private static readonly Direction[] OppositeDirections = new Direction[6]
        {
            Direction.South,
            Direction.North,
            Direction.West,
            Direction.East,
            Direction.Below,
            Direction.Above
        };

        private static readonly Vector3[] DirectionNormals = new Vector3[6]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down
        };

        private static readonly Vector3[] FaceNormals = new Vector3[6]
        {
            Vector3.back,
            Vector3.forward,
            Vector3.left,
            Vector3.right,
            Vector3.down,
            Vector3.up
        };

        public static readonly Int3[] DirectionOffset = new Int3[6]
        {
            new Int3(0, 0, 1),
            new Int3(0, 0, -1),
            new Int3(1, 0, 0),
            new Int3(-1, 0, 0),
            new Int3(0, 1, 0),
            new Int3(0, -1, 0)
        };

        private static readonly Quaternion[] FaceRotation = new Quaternion[6]
        {
            Quaternion.Euler(0f, -90f, 0f),
            Quaternion.Euler(0f, 90f, 0f),
            Quaternion.Euler(0f, 0f, 0f),
            Quaternion.Euler(0f, -180f, 0f),
            Quaternion.Euler(-90f, 0f, 0f),
            Quaternion.Euler(90f, 0f, 0f)
        };

        public const byte NorthMask = 1;

        public const byte SouthMask = 2;

        public const byte EastMask = 4;

        public const byte WestMask = 8;

        public const byte AboveMask = 16;

        public const byte BelowMask = 32;

        public const byte CellUsedMask = 64;

        public const byte HorizontalMask = 15;

        private static readonly FaceDef[][] faceDefs = new FaceDef[10][]
        {
            null,
            new FaceDef[34]
            {
                new FaceDef(0, 0, 0, Direction.South, OccupiedFaceType(Direction.West)),
                new FaceDef(0, 0, 2, Direction.West, OccupiedFaceType(Direction.North)),
                new FaceDef(2, 0, 2, Direction.North, OccupiedFaceType(Direction.East)),
                new FaceDef(2, 0, 0, Direction.East, OccupiedFaceType(Direction.South)),
                new FaceDef(0, 0, 0, Direction.West, FaceType.Solid),
                new FaceDef(0, 0, 1, Direction.West, FaceType.Solid),
                new FaceDef(0, 0, 2, Direction.North, FaceType.Solid),
                new FaceDef(1, 0, 2, Direction.North, FaceType.Solid),
                new FaceDef(2, 0, 2, Direction.East, FaceType.Solid),
                new FaceDef(2, 0, 1, Direction.East, FaceType.Solid),
                new FaceDef(2, 0, 0, Direction.South, FaceType.Solid),
                new FaceDef(1, 0, 0, Direction.South, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.Above, FaceType.Solid),
                new FaceDef(1, 0, 0, Direction.Above, FaceType.Solid),
                new FaceDef(2, 0, 0, Direction.Above, FaceType.Solid),
                new FaceDef(0, 0, 1, Direction.Above, FaceType.Solid),
                new FaceDef(1, 0, 1, Direction.Above, FaceType.Solid),
                new FaceDef(2, 0, 1, Direction.Above, FaceType.Solid),
                new FaceDef(0, 0, 2, Direction.Above, FaceType.Solid),
                new FaceDef(1, 0, 2, Direction.Above, FaceType.Solid),
                new FaceDef(2, 0, 2, Direction.Above, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.Below, FaceType.Solid),
                new FaceDef(1, 0, 0, Direction.Below, FaceType.Solid),
                new FaceDef(2, 0, 0, Direction.Below, FaceType.Solid),
                new FaceDef(0, 0, 1, Direction.Below, FaceType.Solid),
                new FaceDef(1, 0, 1, Direction.Below, FaceType.Solid),
                new FaceDef(2, 0, 1, Direction.Below, FaceType.Solid),
                new FaceDef(0, 0, 2, Direction.Below, FaceType.Solid),
                new FaceDef(1, 0, 2, Direction.Below, FaceType.Solid),
                new FaceDef(2, 0, 2, Direction.Below, FaceType.Solid),
                new FaceDef(1, 0, 1, Direction.West, FaceType.None),
                new FaceDef(1, 0, 1, Direction.North, FaceType.None),
                new FaceDef(1, 0, 1, Direction.East, FaceType.None),
                new FaceDef(1, 0, 1, Direction.South, FaceType.None)
            },
            null,
            null,
            new FaceDef[6]
            {
                new FaceDef(0, 0, 0, Direction.North, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.East, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.South, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.West, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.Above, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.Below, FaceType.Solid)
            },
            new FaceDef[6]
            {
                new FaceDef(0, 0, 0, Direction.North, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.East, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.South, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.West, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.Above, FaceType.Solid),
                new FaceDef(0, 0, 0, Direction.Below, FaceType.Solid)
            },
            new FaceDef[0],
            new FaceDef[6]
            {
                new FaceDef(1, 0, 0, Direction.South, FaceType.Solid),
                new FaceDef(2, 0, 0, Direction.South, FaceType.Solid),
                new FaceDef(1, 0, 2, Direction.North, FaceType.Solid),
                new FaceDef(2, 0, 2, Direction.North, FaceType.Solid),
                new FaceDef(3, 0, 1, Direction.East, FaceType.Solid),
                new FaceDef(0, 0, 1, Direction.West, FaceType.Solid)
            },
            new FaceDef[2]
            {
                new FaceDef(0, 0, 1, Direction.West, FaceType.Solid),
                new FaceDef(2, 0, 1, Direction.East, FaceType.Solid)
            },
            new FaceDef[2]
            {
                new FaceDef(1, 0, 2, Direction.North, FaceType.Solid),
                new FaceDef(1, 0, 0, Direction.South, FaceType.Solid)
            }
        };

        [HideInInspector]
        public bool isGhost;

        [NonSerialized]
        [ProtoMember(1)]
        public Grid3Shape baseShape;

        [NonSerialized]
        [ProtoMember(2, OverwriteList = true)]
        public FaceType[] faces;

        [NonSerialized]
        [ProtoMember(3, OverwriteList = true)]
        public CellType[] cells;

        [NonSerialized]
        [ProtoMember(4, OverwriteList = true)]
        public byte[] links;

        [NonSerialized]
        [ProtoMember(5)]
        public Int3 cellOffset;

        [NonSerialized]
        [ProtoMember(6, OverwriteList = true)]
        public byte[] masks;

        [NonSerialized]
        [ProtoMember(7, OverwriteList = true)]
        public bool[] isGlass;

        [NonSerialized]
        [ProtoMember(8)]
        public Int3 anchor = Int3.zero;

        [NonSerialized]
        public byte[] flowData;

        private Transform[] cellObjects;

        private List<int> occupiedCellIndexes;

        private Bounds occupiedBounds;

        private static List<BaseDeconstructable> sDeconstructables = new List<BaseDeconstructable>();

        private static List<IBaseModule> sBaseModules = new List<IBaseModule>();

        private static List<IBaseModuleGeometry> sBaseModulesGeometry = new List<IBaseModuleGeometry>();

        public bool isReady => !waitingForWorld;

        public Grid3Shape Shape => baseShape;

        public Int3.RangeEnumerator AllCells => Int3.Range(baseShape.ToInt3());

        public List<int> OccupiedCellIndexes => occupiedCellIndexes;

        public Int3.Bounds Bounds => new Int3.Bounds(Int3.zero, baseShape.ToInt3() - 1);

        public event BaseEventHandler onPostRebuildGeometry;

        public event BaseResizeEventHandler onBaseResize;

        public event BaseFaceEventHandler onBulkheadFaceChanged;

        private static float ApplyDepthScaling(float str, float y)
        {
            if (str >= 0f)
            {
                return str;
            }
            float num = Ocean.main.GetOceanLevel() - y;
            return Mathf.Max(1f, 1f + (num - 100f) / 1000f) * str;
        }

        private bool ExteriorToInteriorPiece(Piece exterior, out Piece interior)
        {
            interior = Piece.Invalid;
            return exteriorToInteriorPiece.TryGetValue(exterior, out interior);
        }

        private CorridorDef GetCorridorDef(int index)
        {
            byte b = links[index];
            if (!isGlass[index])
            {
                return corridors[b];
            }
            return glassCorridors[b];
        }

        private static FaceType OccupiedFaceType(Direction occupyingDirection)
        {
            return FaceType.OccupiedByOtherFace | (FaceType)occupyingDirection;
        }

        public static void Initialize()
        {
            if (!initialized)
            {
                CoroutineUtils.PumpCoroutine(InitializeAsync());
            }
        }

        public static IAssetBundleWrapperCreateRequest KickoffAssetBundleLoadRequest()
        {
            IAssetBundleWrapperCreateRequest assetBundleWrapperCreateRequest = AssetBundleManager.LoadBundleAsync("basegeneratorpieces");
            assetBundleWrapperCreateRequest.SafeMoveNext();
            return assetBundleWrapperCreateRequest;
        }

        public static IEnumerator InitializeAsync()
        {
            if (!initialized)
            {
                yield return RegisterPiecesAsync();
                RegisterCorridors();
                IPrefabRequest cellLoadRequest = PrefabDatabase.GetPrefabForFilenameAsync("Base/Ghosts/BaseCell");
                yield return cellLoadRequest;
                if (cellLoadRequest.TryGetPrefab(out var prefab))
                {
                    cellPrefab = prefab.transform;
                }
                else
                {
                    Debug.LogError("Failed to load basepiece: BaseCell");
                }
                initialized = true;
            }
        }

        public static void Deinitialize()
        {
            if (initialized)
            {
                pieces = null;
                corridors = null;
                glassCorridors = null;
                cellPrefab = null;
                initialized = false;
            }
        }

        private void ReleaseArrays()
        {
            flowData = null;
            cellObjects = null;
            occupiedCellIndexes = null;
            cells = null;
            faces = null;
            links = null;
            masks = null;
            isGlass = null;
        }

        private void AllocateArrays()
        {
            if (baseShape.Size != 0)
            {
                if (flowData == null)
                {
                    flowData = new byte[baseShape.Size];
                }
                if (occupiedCellIndexes == null)
                {
                    occupiedCellIndexes = new List<int>();
                }
                if (cellObjects == null)
                {
                    cellObjects = new Transform[baseShape.Size];
                }
                if (cells == null)
                {
                    cells = new CellType[baseShape.Size];
                }
                if (faces == null)
                {
                    faces = new FaceType[baseShape.Size * 6];
                }
                if (links == null)
                {
                    links = new byte[baseShape.Size];
                }
                if (isGlass == null)
                {
                    isGlass = new bool[baseShape.Size];
                }
            }
        }

        public Int3 GetSize()
        {
            return baseShape.ToInt3();
        }

        public void SetSize(Int3 size)
        {
            if (!(GetSize() == size))
            {
                baseShape = new Grid3Shape(size);
                ReleaseArrays();
                AllocateArrays();
                if (this.onBaseResize != null)
                {
                    this.onBaseResize(this, Int3.zero);
                }
            }
        }

        private Int3 EnsureSize(Int3.Bounds region)
        {
            if (region.mins >= 0 && region.maxs < baseShape.ToInt3())
            {
                return Int3.zero;
            }
            Int3 zero = Int3.zero;
            if (region.mins.x < 0)
            {
                zero.x = -region.mins.x;
            }
            if (region.mins.y < 0)
            {
                zero.y = -region.mins.y;
            }
            if (region.mins.z < 0)
            {
                zero.z = -region.mins.z;
            }
            Int3.Bounds bounds = Int3.Bounds.Union(region, Bounds);
            Grid3Shape grid3Shape = baseShape;
            Transform[] array = cellObjects;
            CellType[] array2 = cells;
            FaceType[] array3 = faces;
            byte[] array4 = links;
            bool[] array5 = isGlass;
            baseShape = new Grid3Shape(bounds.size);
            ReleaseArrays();
            AllocateArrays();
            if (array2 != null)
            {
                foreach (Int3 allCell in AllCells)
                {
                    int index = baseShape.GetIndex(allCell);
                    int index2 = grid3Shape.GetIndex(allCell - zero);
                    if (index2 != -1)
                    {
                        cellObjects[index] = array[index2];
                        cells[index] = array2[index2];
                        links[index] = array4[index2];
                        isGlass[index] = array5[index2];
                        Direction[] allDirections = AllDirections;
                        foreach (Direction direction in allDirections)
                        {
                            SetFace(index, direction, array3[(int)(index2 * 6 + direction)]);
                        }
                    }
                }
            }
            cellOffset -= zero;
            RecalculateFlowData();
            if (this.onBaseResize != null)
            {
                this.onBaseResize(this, zero);
            }
            return zero;
        }

        public void CopyFrom(Base sourceBase, Int3.Bounds sourceRange, Int3 offset)
        {
            Int3.Bounds region = sourceRange;
            region.Move(offset);
            Int3 @int = EnsureSize(region);
            anchor += @int;
            foreach (Int3 item in region)
            {
                int index = sourceBase.baseShape.GetIndex(item - offset);
                int index2 = baseShape.GetIndex(item + @int);
                if (index == -1 || index2 == -1)
                {
                    continue;
                }
                if (sourceBase.IsCellUsed(index))
                {
                    cells[index2] = sourceBase.cells[index];
                    links[index2] = sourceBase.links[index];
                    isGlass[index2] = sourceBase.isGlass[index];
                }
                Direction[] allDirections = AllDirections;
                foreach (Direction direction in allDirections)
                {
                    if (sourceBase.IsFaceUsed(index, direction))
                    {
                        int faceIndex = GetFaceIndex(index, direction);
                        int faceIndex2 = GetFaceIndex(index2, direction);
                        faces[faceIndex2] = sourceBase.faces[faceIndex];
                    }
                }
            }
            FixCorridorLinks();
            RecalculateFlowData();
            RebuildGeometry();
            if (!isGhost)
            {
                GetComponentsInChildren(sGhosts);
                for (int j = 0; j < sGhosts.Count; j++)
                {
                    sGhosts[j].RecalculateTargetOffset();
                }
                sGhosts.Clear();
            }
        }

        private void Start()
        {
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            flowData = null;
            cellObjects = null;
        }

        private static IEnumerator SetPieceAsync(Piece piece, string prefabName)
        {
            return SetPieceAsync(piece, prefabName, Int3.zero);
        }

        private static IEnumerator SetPieceAsync(Piece piece, string prefabName, Int3 extraCells)
        {
            IAssetBundleWrapperCreateRequest bundleRequest = AssetBundleManager.LoadBundleAsync("basegeneratorpieces");
            yield return bundleRequest;
            IAssetBundleWrapper assetBundle = bundleRequest.assetBundle;
            string text = $"Assets/Prefabs/Base/GeneratorPieces/{prefabName}.prefab";
            IAssetBundleWrapperRequest pieceRequest = assetBundle.LoadAssetAsync<GameObject>(text);
            yield return pieceRequest;
            GameObject gameObject = (GameObject)pieceRequest.asset;
            if (!gameObject)
            {
                Debug.LogErrorFormat("Failed to load base piece '{0}'", prefabName);
            }
            else
            {
                gameObject.SetActive(value: false);
                pieces[(int)piece] = new PieceDef(gameObject, extraCells, Quaternion.identity);
            }
        }

        private static bool ProcessActiveLoadPiece(PieceData pieceData)
        {
            if (pieceData.request == null)
            {
                Debug.LogErrorFormat("Failed to load base piece '{0}'", pieceData.name);
            }
            else if ((bool)pieceData.request.asset)
            {
                GameObject gameObject = (GameObject)pieceData.request.asset;
                if (!gameObject)
                {
                    Debug.LogErrorFormat("Failed to load base piece '{0}'", pieceData.name);
                }
                gameObject.SetActive(value: false);
                pieces[(int)pieceData.piece] = new PieceDef(gameObject, pieceData.extraCells, Quaternion.identity);
                return true;
            }
            return false;
        }

        private static IEnumerator RegisterPiecesAsync()
        {
            if (pieces != null)
            {
                yield break;
            }
            pieces = new PieceDef[128];
            Int3 extraCells = new Int3(2, 0, 2);
            Int3 extraCells2 = new Int3(3, 0, 2);
            PieceData[] piecesToLoad = new PieceData[127]
            {
                new PieceData(Piece.Foundation, "BaseFoundationPiece", new Int3(1, 0, 1)),
                new PieceData(Piece.CorridorCap, "BaseCorridorCap"),
                new PieceData(Piece.CorridorWindow, "BaseCorridorWindow"),
                new PieceData(Piece.CorridorHatch, "BaseCorridorHatch"),
                new PieceData(Piece.CorridorBulkhead, "BaseCorridorBulkhead"),
                new PieceData(Piece.CorridorIShapeGlass, "BaseCorridorIShapeGlass"),
                new PieceData(Piece.CorridorLShapeGlass, "BaseCorridorLShapeGlass"),
                new PieceData(Piece.CorridorIShape, "BaseCorridorIShape"),
                new PieceData(Piece.CorridorLShape, "BaseCorridorLShape"),
                new PieceData(Piece.CorridorTShape, "BaseCorridorTShape"),
                new PieceData(Piece.CorridorXShape, "BaseCorridorXShape"),
                new PieceData(Piece.CorridorIShapeGlassSupport, "BaseCorridorIShapeSupport"),
                new PieceData(Piece.CorridorLShapeGlassSupport, "BaseCorridorLShapeSupport"),
                new PieceData(Piece.CorridorIShapeSupport, "BaseCorridorIShapeSupport"),
                new PieceData(Piece.CorridorLShapeSupport, "BaseCorridorLShapeSupport"),
                new PieceData(Piece.CorridorTShapeSupport, "BaseCorridorTShapeSupport"),
                new PieceData(Piece.CorridorIShapeGlassAdjustableSupport, "BaseCorridorIShapeAdjustableSupport"),
                new PieceData(Piece.CorridorLShapeGlassAdjustableSupport, "BaseCorridorLShapeAdjustableSupport"),
                new PieceData(Piece.CorridorIShapeAdjustableSupport, "BaseCorridorIShapeAdjustableSupport"),
                new PieceData(Piece.CorridorLShapeAdjustableSupport, "BaseCorridorLShapeAdjustableSupport"),
                new PieceData(Piece.CorridorTShapeAdjustableSupport, "BaseCorridorTShapeAdjustableSupport"),
                new PieceData(Piece.CorridorXShapeAdjustableSupport, "BaseCorridorXShapeAdjustableSupport"),
                new PieceData(Piece.CorridorIShapeCoverSide, "BaseCorridorIShapeCoverSide"),
                new PieceData(Piece.CorridorIShapeWindowSide, "BaseCorridorIShapeWindowSide"),
                new PieceData(Piece.CorridorIShapeWindowTop, "BaseCorridorIShapeWindowTop"),
                new PieceData(Piece.CorridorIShapeWindowBottom, "BaseCorridorIShapeWindowBottom"),
                new PieceData(Piece.CorridorIShapeReinforcementSide, "BaseCorridorIShapeReinforcementSide"),
                new PieceData(Piece.CorridorIShapeHatchSide, "BaseCorridorIShapeHatchSide"),
                new PieceData(Piece.CorridorIShapeHatchTop, "BaseCorridorIShapeHatchTop"),
                new PieceData(Piece.CorridorIShapeHatchBottom, "BaseCorridorIShapeHatchBottom"),
                new PieceData(Piece.CorridorIShapePlanterSide, "BaseCorridorIShapeInteriorPlanterSide"),
                new PieceData(Piece.CorridorIShapeLadderTop, "BaseCorridorLadderTop"),
                new PieceData(Piece.CorridorIShapeLadderBottom, "BaseCorridorLadderBottom"),
                new PieceData(Piece.CorridorTShapeWindowTop, "BaseCorridorTShapeWindowTop"),
                new PieceData(Piece.CorridorTShapeWindowBottom, "BaseCorridorTShapeWindowBottom"),
                new PieceData(Piece.CorridorTShapeHatchTop, "BaseCorridorTShapeHatchTop"),
                new PieceData(Piece.CorridorTShapeHatchBottom, "BaseCorridorTShapeHatchBottom"),
                new PieceData(Piece.CorridorTShapeLadderTop, "BaseCorridorLadderTop"),
                new PieceData(Piece.CorridorTShapeLadderBottom, "BaseCorridorLadderBottom"),
                new PieceData(Piece.CorridorXShapeWindowTop, "BaseCorridorXShapeWindowTop"),
                new PieceData(Piece.CorridorXShapeWindowBottom, "BaseCorridorXShapeWindowBottom"),
                new PieceData(Piece.CorridorXShapeHatchTop, "BaseCorridorXShapeHatchTop"),
                new PieceData(Piece.CorridorXShapeHatchBottom, "BaseCorridorXShapeHatchBottom"),
                new PieceData(Piece.CorridorXShapeLadderTop, "BaseCorridorLadderTop"),
                new PieceData(Piece.CorridorXShapeLadderBottom, "BaseCorridorLadderBottom"),
                new PieceData(Piece.CorridorCoverIShapeBottomExtClosed, "BaseCorridorCoverIShapeBottomExtClosed"),
                new PieceData(Piece.CorridorCoverIShapeBottomExtOpened, "BaseCorridorCoverIShapeBottomExtOpened"),
                new PieceData(Piece.CorridorCoverIShapeBottomIntClosed, "BaseCorridorCoverIShapeBottomIntClosed"),
                new PieceData(Piece.CorridorCoverIShapeBottomIntOpened, "BaseCorridorCoverIShapeBottomIntOpened"),
                new PieceData(Piece.CorridorCoverIShapeTopExtClosed, "BaseCorridorCoverIShapeTopExtClosed"),
                new PieceData(Piece.CorridorCoverIShapeTopExtOpened, "BaseCorridorCoverIShapeTopExtOpened"),
                new PieceData(Piece.CorridorCoverIShapeTopIntClosed, "BaseCorridorCoverIShapeTopIntClosed"),
                new PieceData(Piece.CorridorCoverIShapeTopIntOpened, "BaseCorridorCoverIShapeTopIntOpened"),
                new PieceData(Piece.CorridorCoverTShapeBottomExtClosed, "BaseCorridorCoverTShapeBottomExtClosed"),
                new PieceData(Piece.CorridorCoverTShapeBottomExtOpened, "BaseCorridorCoverTShapeBottomExtOpened"),
                new PieceData(Piece.CorridorCoverTShapeBottomIntClosed, "BaseCorridorCoverTShapeBottomIntClosed"),
                new PieceData(Piece.CorridorCoverTShapeBottomIntOpened, "BaseCorridorCoverTShapeBottomIntOpened"),
                new PieceData(Piece.CorridorCoverTShapeTopExtClosed, "BaseCorridorCoverTShapeTopExtClosed"),
                new PieceData(Piece.CorridorCoverTShapeTopExtOpened, "BaseCorridorCoverTShapeTopExtOpened"),
                new PieceData(Piece.CorridorCoverTShapeTopIntClosed, "BaseCorridorCoverTShapeTopIntClosed"),
                new PieceData(Piece.CorridorCoverTShapeTopIntOpened, "BaseCorridorCoverTShapeTopIntOpened"),
                new PieceData(Piece.CorridorCoverXShapeBottomExtClosed, "BaseCorridorCoverXShapeBottomExtClosed"),
                new PieceData(Piece.CorridorCoverXShapeBottomExtOpened, "BaseCorridorCoverXShapeBottomExtOpened"),
                new PieceData(Piece.CorridorCoverXShapeBottomIntClosed, "BaseCorridorCoverXShapeBottomIntClosed"),
                new PieceData(Piece.CorridorCoverXShapeBottomIntOpened, "BaseCorridorCoverXShapeBottomIntOpened"),
                new PieceData(Piece.CorridorCoverXShapeTopExtClosed, "BaseCorridorCoverXShapeTopExtClosed"),
                new PieceData(Piece.CorridorCoverXShapeTopExtOpened, "BaseCorridorCoverXShapeTopExtOpened"),
                new PieceData(Piece.CorridorCoverXShapeTopIntClosed, "BaseCorridorCoverXShapeTopIntClosed"),
                new PieceData(Piece.CorridorCoverXShapeTopIntOpened, "BaseCorridorCoverXShapeTopIntOpened"),
                new PieceData(Piece.ConnectorTube, "BaseConnectorTube"),
                new PieceData(Piece.ConnectorTubeWindow, "BaseConnectorTubeWindow"),
                new PieceData(Piece.ConnectorCap, "BaseConnectorCap"),
                new PieceData(Piece.ConnectorLadder, "BaseConnectorLadder"),
                new PieceData(Piece.Room, "BaseRoom", extraCells),
                new PieceData(Piece.RoomCorridorConnector, "BaseRoomCorridorConnector", extraCells),
                new PieceData(Piece.RoomCoverSide, "BaseRoomCoverSide", extraCells),
                new PieceData(Piece.RoomCoverSideVariant, "BaseRoomCoverSideVariant", extraCells),
                new PieceData(Piece.RoomExteriorBottom, "BaseRoomExteriorBottom", extraCells),
                new PieceData(Piece.RoomExteriorFoundationBottom, "BaseRoomExteriorFoundationBottom", extraCells),
                new PieceData(Piece.RoomExteriorTop, "BaseRoomExteriorTop", extraCells),
                new PieceData(Piece.RoomReinforcementSide, "BaseRoomReinforcementSide", extraCells),
                new PieceData(Piece.RoomWindowSide, "BaseRoomWindowSide", extraCells),
                new PieceData(Piece.RoomPlanterSide, "BaseRoomPlanterSide", extraCells),
                new PieceData(Piece.RoomFiltrationMachine, "BaseRoomFiltrationMachine", extraCells),
                new PieceData(Piece.RoomCoverBottom, "BaseRoomCoverBottom", extraCells),
                new PieceData(Piece.RoomCoverTop, "BaseRoomCoverTop", extraCells),
                new PieceData(Piece.RoomLadderBottom, "BaseRoomLadderBottom", extraCells),
                new PieceData(Piece.RoomLadderTop, "BaseRoomLadderTop", extraCells),
                new PieceData(Piece.RoomAdjustableSupport, "BaseRoomAdjustableSupport", extraCells),
                new PieceData(Piece.RoomHatch, "BaseRoomHatch", extraCells),
                new PieceData(Piece.RoomWaterParkTop, "BaseRoomWaterParkTop", extraCells),
                new PieceData(Piece.RoomWaterParkBottom, "BaseRoomWaterParkBottom", extraCells),
                new PieceData(Piece.RoomWaterParkHatch, "BaseWaterParkHatch", extraCells),
                new PieceData(Piece.RoomWaterParkSide, "BaseWaterParkSide", extraCells),
                new PieceData(Piece.RoomInteriorBottom, "BaseRoomInteriorBottom", extraCells),
                new PieceData(Piece.RoomInteriorTop, "BaseRoomInteriorTop", extraCells),
                new PieceData(Piece.RoomInteriorBottomHole, "BaseRoomInteriorBottomHole", extraCells),
                new PieceData(Piece.RoomInteriorTopHole, "BaseRoomInteriorTopHole", extraCells),
                new PieceData(Piece.RoomBioReactor, "BaseRoomBioReactor", extraCells),
                new PieceData(Piece.RoomNuclearReactor, "BaseRoomNuclearReactor", extraCells),
                new PieceData(Piece.Moonpool, "BaseMoonpool", extraCells2),
                new PieceData(Piece.MoonpoolCoverSide, "BaseMoonpoolCoverSide"),
                new PieceData(Piece.MoonpoolCoverSideShort, "BaseMoonpoolCoverSideShort"),
                new PieceData(Piece.MoonpoolReinforcementSide, "BaseMoonpoolReinforcementSide"),
                new PieceData(Piece.MoonpoolReinforcementSideShort, "BaseMoonpoolReinforcementSideShort"),
                new PieceData(Piece.MoonpoolWindowSide, "BaseMoonpoolWindowSide"),
                new PieceData(Piece.MoonpoolWindowSideShort, "BaseMoonpoolWindowSideShort"),
                new PieceData(Piece.MoonpoolUpgradeConsole, "BaseMoonpoolUpgradeConsole"),
                new PieceData(Piece.MoonpoolUpgradeConsoleShort, "BaseMoonpoolUpgradeConsoleShort"),
                new PieceData(Piece.MoonpoolAdjustableSupport, "BaseMoonpoolAdjustableSupport"),
                new PieceData(Piece.MoonpoolHatch, "BaseMoonpoolHatch"),
                new PieceData(Piece.MoonpoolHatchShort, "BaseMoonpoolHatchShort"),
                new PieceData(Piece.MoonpoolPlanterSide, "BaseMoonpoolPlanterSide"),
                new PieceData(Piece.MoonpoolPlanterSideShort, "BaseMoonpoolPlanterSideShort"),
                new PieceData(Piece.MoonpoolCorridorConnector, "BaseMoonpoolCorridorConnector"),
                new PieceData(Piece.MoonpoolCorridorConnectorShort, "BaseMoonpoolCorridorConnectorShort"),
                new PieceData(Piece.Observatory, "BaseObservatory"),
                new PieceData(Piece.ObservatoryCorridorConnector, "BaseObservatoryCorridorConnector"),
                new PieceData(Piece.ObservatoryCoverSide, "BaseObservatoryCoverSide"),
                new PieceData(Piece.ObservatoryHatch, "BaseObservatoryHatch"),
                new PieceData(Piece.MapRoom, "BaseMapRoom", extraCells),
                new PieceData(Piece.MapRoomCorridorConnector, "BaseMapRoomCorridorConnector", extraCells),
                new PieceData(Piece.MapRoomCoverSide, "BaseMapRoomCoverSide", extraCells),
                new PieceData(Piece.MapRoomHatch, "BaseMapRoomHatch", extraCells),
                new PieceData(Piece.MapRoomWindowSide, "BaseMapRoomWindowSide", extraCells),
                new PieceData(Piece.MapRoomPlanterSide, "BaseMapRoomPlanterSide", extraCells),
                new PieceData(Piece.MapRoomReinforcementSide, "BaseMapRoomReinforcementSide", extraCells)
            };
            IAssetBundleWrapperCreateRequest bundleRequest = AssetBundleManager.LoadBundleAsync("basegeneratorpieces");
            yield return bundleRequest;
            IAssetBundleWrapper bundle = bundleRequest.assetBundle;
            int nextPieceToLoad = 0;
            List<PieceData> activeLoads = new List<PieceData>();
            while (nextPieceToLoad < piecesToLoad.Length || activeLoads.Count > 0)
            {
                for (int num = activeLoads.Count - 1; num >= 0; num--)
                {
                    if (ProcessActiveLoadPiece(activeLoads[num]))
                    {
                        activeLoads.RemoveAt(num);
                    }
                }
                while (activeLoads.Count < 32 && nextPieceToLoad < piecesToLoad.Length)
                {
                    PieceData currentPiece = piecesToLoad[nextPieceToLoad++];
                    string text = $"Assets/Prefabs/Base/GeneratorPieces/{currentPiece.name}.prefab";
                    IAssetBundleWrapperRequest request = bundle.LoadAssetAsync<GameObject>(text);
                    yield return request;
                    currentPiece.request = request;
                    activeLoads.Add(currentPiece);
                    currentPiece = default(PieceData);
                }
                yield return null;
            }
        }

        private static void RegisterCorridors()
        {
            if (corridors == null)
            {
                CorridorDef corridorDef = new CorridorDef(Piece.CorridorIShapeGlass, Piece.CorridorIShapeGlassSupport, Piece.CorridorIShapeGlassAdjustableSupport);
                corridorDef.SetFace(Direction.North, FaceType.Solid, Piece.CorridorCap, Vector3.zero);
                corridorDef.SetFace(Direction.South, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, 180f, 0f));
                corridorDef.SetFace(Direction.North, FaceType.Hatch, Piece.CorridorHatch, Vector3.zero);
                corridorDef.SetFace(Direction.South, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, 180f, 0f));
                corridorDef.SetFace(Direction.North, FaceType.Window, Piece.CorridorWindow, Vector3.zero);
                corridorDef.SetFace(Direction.South, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, 180f, 0f));
                corridorDef.SetFace(Direction.Below, FaceType.Solid, Piece.CorridorCoverIShapeBottomExtClosed, Vector3.zero);
                corridorDef.SetFace(Direction.Below, FaceType.Hole, Piece.CorridorCoverIShapeBottomExtOpened, Vector3.zero);
                corridorDef.SetFace(Direction.Below, FaceType.Ladder, Piece.CorridorIShapeLadderBottom, Vector3.zero);
                corridorDef.SetFace(Direction.Below, FaceType.Hatch, Piece.CorridorIShapeHatchBottom, Vector3.zero);
                corridorDef.SetFace(Direction.North, FaceType.BulkheadClosed, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef.SetFace(Direction.North, FaceType.BulkheadOpened, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef.SetFace(Direction.South, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, 180f, 0f));
                corridorDef.SetFace(Direction.South, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, 180f, 0f));
                CorridorDef corridorDef2 = new CorridorDef(Piece.CorridorLShapeGlass, Piece.CorridorLShapeGlassSupport, Piece.CorridorLShapeGlassAdjustableSupport);
                corridorDef2.SetFace(Direction.East, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, 90f, 0f));
                corridorDef2.SetFace(Direction.North, FaceType.Solid, Piece.CorridorCap, Vector3.zero);
                corridorDef2.SetFace(Direction.East, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, 90f, 0f));
                corridorDef2.SetFace(Direction.North, FaceType.Hatch, Piece.CorridorHatch, Vector3.zero);
                corridorDef2.SetFace(Direction.East, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, 90f, 0f));
                corridorDef2.SetFace(Direction.North, FaceType.Window, Piece.CorridorWindow, Vector3.zero);
                corridorDef2.SetFace(Direction.East, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, 90f, 0f));
                corridorDef2.SetFace(Direction.East, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, 90f, 0f));
                corridorDef2.SetFace(Direction.North, FaceType.BulkheadClosed, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef2.SetFace(Direction.North, FaceType.BulkheadOpened, Piece.CorridorBulkhead, Vector3.zero);
                CorridorDef corridorDef3 = new CorridorDef(Piece.CorridorIShape, Piece.CorridorIShapeSupport, Piece.CorridorIShapeAdjustableSupport);
                corridorDef3.SetFace(Direction.East, FaceType.Solid, Piece.CorridorIShapeCoverSide, Vector3.zero);
                corridorDef3.SetFace(Direction.West, FaceType.Solid, Piece.CorridorIShapeCoverSide, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.North, FaceType.Solid, Piece.CorridorCap, Vector3.zero);
                corridorDef3.SetFace(Direction.South, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.Above, FaceType.Solid, Piece.CorridorCoverIShapeTopExtClosed, Vector3.zero);
                corridorDef3.SetFace(Direction.Below, FaceType.Solid, Piece.CorridorCoverIShapeBottomExtClosed, Vector3.zero);
                corridorDef3.SetFace(Direction.Above, FaceType.Hole, Piece.CorridorCoverIShapeTopExtOpened, Vector3.zero);
                corridorDef3.SetFace(Direction.Below, FaceType.Hole, Piece.CorridorCoverIShapeBottomExtOpened, Vector3.zero);
                corridorDef3.SetFace(Direction.Above, FaceType.Ladder, Piece.CorridorIShapeLadderTop, Vector3.zero);
                corridorDef3.SetFace(Direction.Below, FaceType.Ladder, Piece.CorridorIShapeLadderBottom, Vector3.zero);
                corridorDef3.SetFace(Direction.East, FaceType.Window, Piece.CorridorIShapeWindowSide, Vector3.zero);
                corridorDef3.SetFace(Direction.West, FaceType.Window, Piece.CorridorIShapeWindowSide, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.North, FaceType.Window, Piece.CorridorWindow, Vector3.zero);
                corridorDef3.SetFace(Direction.South, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.Above, FaceType.Window, Piece.CorridorIShapeWindowTop, Vector3.zero);
                corridorDef3.SetFace(Direction.North, FaceType.BulkheadClosed, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef3.SetFace(Direction.North, FaceType.BulkheadOpened, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef3.SetFace(Direction.South, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.South, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.East, FaceType.Reinforcement, Piece.CorridorIShapeReinforcementSide, Vector3.zero);
                corridorDef3.SetFace(Direction.West, FaceType.Reinforcement, Piece.CorridorIShapeReinforcementSide, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.East, FaceType.Hatch, Piece.CorridorIShapeHatchSide, Vector3.zero);
                corridorDef3.SetFace(Direction.West, FaceType.Hatch, Piece.CorridorIShapeHatchSide, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.Above, FaceType.Hatch, Piece.CorridorIShapeHatchTop, Vector3.zero);
                corridorDef3.SetFace(Direction.Below, FaceType.Hatch, Piece.CorridorIShapeHatchBottom, Vector3.zero);
                corridorDef3.SetFace(Direction.North, FaceType.Hatch, Piece.CorridorHatch, Vector3.zero);
                corridorDef3.SetFace(Direction.South, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, 180f, 0f));
                corridorDef3.SetFace(Direction.East, FaceType.Planter, Piece.CorridorIShapePlanterSide, Vector3.zero);
                corridorDef3.SetFace(Direction.West, FaceType.Planter, Piece.CorridorIShapePlanterSide, new Vector3(0f, 180f, 0f));
                CorridorDef corridorDef4 = new CorridorDef(Piece.CorridorLShape, Piece.CorridorLShapeSupport, Piece.CorridorLShapeAdjustableSupport);
                corridorDef4.SetFace(Direction.East, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, 90f, 0f));
                corridorDef4.SetFace(Direction.North, FaceType.Solid, Piece.CorridorCap, Vector3.zero);
                corridorDef4.SetFace(Direction.East, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, 90f, 0f));
                corridorDef4.SetFace(Direction.North, FaceType.Window, Piece.CorridorWindow, Vector3.zero);
                corridorDef4.SetFace(Direction.East, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, 90f, 0f));
                corridorDef4.SetFace(Direction.North, FaceType.Hatch, Piece.CorridorHatch, Vector3.zero);
                corridorDef4.SetFace(Direction.East, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, 90f, 0f));
                corridorDef4.SetFace(Direction.East, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, 90f, 0f));
                corridorDef4.SetFace(Direction.North, FaceType.BulkheadClosed, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef4.SetFace(Direction.North, FaceType.BulkheadOpened, Piece.CorridorBulkhead, Vector3.zero);
                CorridorDef corridorDef5 = new CorridorDef(Piece.CorridorTShape, Piece.CorridorTShapeSupport, Piece.CorridorTShapeAdjustableSupport);
                corridorDef5.SetFace(Direction.East, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.West, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, -90f, 0f));
                corridorDef5.SetFace(Direction.North, FaceType.Solid, Piece.CorridorCap, Vector3.zero);
                corridorDef5.SetFace(Direction.South, FaceType.Solid, Piece.CorridorIShapeCoverSide, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.Above, FaceType.Solid, Piece.CorridorCoverTShapeTopExtClosed, Vector3.zero);
                corridorDef5.SetFace(Direction.Below, FaceType.Solid, Piece.CorridorCoverTShapeBottomExtClosed, Vector3.zero);
                corridorDef5.SetFace(Direction.Above, FaceType.Hole, Piece.CorridorCoverTShapeTopExtOpened, Vector3.zero);
                corridorDef5.SetFace(Direction.Below, FaceType.Hole, Piece.CorridorCoverTShapeBottomExtOpened, Vector3.zero);
                corridorDef5.SetFace(Direction.Above, FaceType.Ladder, Piece.CorridorTShapeLadderTop, Vector3.zero);
                corridorDef5.SetFace(Direction.Below, FaceType.Ladder, Piece.CorridorTShapeLadderBottom, Vector3.zero);
                corridorDef5.SetFace(Direction.South, FaceType.Window, Piece.CorridorIShapeWindowSide, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.East, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.West, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, -90f, 0f));
                corridorDef5.SetFace(Direction.North, FaceType.Window, Piece.CorridorWindow, Vector3.zero);
                corridorDef5.SetFace(Direction.Above, FaceType.Window, Piece.CorridorTShapeWindowTop, Vector3.zero);
                corridorDef5.SetFace(Direction.East, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.East, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.West, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, -90f, 0f));
                corridorDef5.SetFace(Direction.West, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, -90f, 0f));
                corridorDef5.SetFace(Direction.North, FaceType.BulkheadClosed, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef5.SetFace(Direction.North, FaceType.BulkheadOpened, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef5.SetFace(Direction.South, FaceType.Reinforcement, Piece.CorridorIShapeReinforcementSide, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.South, FaceType.Hatch, Piece.CorridorIShapeHatchSide, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.Above, FaceType.Hatch, Piece.CorridorTShapeHatchTop, Vector3.zero);
                corridorDef5.SetFace(Direction.Below, FaceType.Hatch, Piece.CorridorTShapeHatchBottom, Vector3.zero);
                corridorDef5.SetFace(Direction.East, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, 90f, 0f));
                corridorDef5.SetFace(Direction.West, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, -90f, 0f));
                corridorDef5.SetFace(Direction.North, FaceType.Hatch, Piece.CorridorHatch, Vector3.zero);
                CorridorDef corridorDef6 = new CorridorDef(Piece.CorridorXShape, Piece.Invalid, Piece.CorridorXShapeAdjustableSupport);
                corridorDef6.SetFace(Direction.East, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, 90f, 0f));
                corridorDef6.SetFace(Direction.West, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, -90f, 0f));
                corridorDef6.SetFace(Direction.North, FaceType.Solid, Piece.CorridorCap, Vector3.zero);
                corridorDef6.SetFace(Direction.South, FaceType.Solid, Piece.CorridorCap, new Vector3(0f, 180f, 0f));
                corridorDef6.SetFace(Direction.Above, FaceType.Solid, Piece.CorridorCoverXShapeTopExtClosed, Vector3.zero);
                corridorDef6.SetFace(Direction.Below, FaceType.Solid, Piece.CorridorCoverXShapeBottomExtClosed, Vector3.zero);
                corridorDef6.SetFace(Direction.Above, FaceType.Hole, Piece.CorridorCoverXShapeTopExtOpened, Vector3.zero);
                corridorDef6.SetFace(Direction.Below, FaceType.Hole, Piece.CorridorCoverXShapeBottomExtOpened, Vector3.zero);
                corridorDef6.SetFace(Direction.Above, FaceType.Ladder, Piece.CorridorXShapeLadderTop, Vector3.zero);
                corridorDef6.SetFace(Direction.Below, FaceType.Ladder, Piece.CorridorXShapeLadderBottom, Vector3.zero);
                corridorDef6.SetFace(Direction.Above, FaceType.Hatch, Piece.CorridorXShapeHatchTop, Vector3.zero);
                corridorDef6.SetFace(Direction.Below, FaceType.Hatch, Piece.CorridorXShapeHatchBottom, Vector3.zero);
                corridorDef6.SetFace(Direction.East, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, 90f, 0f));
                corridorDef6.SetFace(Direction.West, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, -90f, 0f));
                corridorDef6.SetFace(Direction.North, FaceType.Hatch, Piece.CorridorHatch, Vector3.zero);
                corridorDef6.SetFace(Direction.South, FaceType.Hatch, Piece.CorridorHatch, new Vector3(0f, 180f, 0f));
                corridorDef6.SetFace(Direction.East, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, 90f, 0f));
                corridorDef6.SetFace(Direction.West, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, -90f, 0f));
                corridorDef6.SetFace(Direction.North, FaceType.Window, Piece.CorridorWindow, Vector3.zero);
                corridorDef6.SetFace(Direction.South, FaceType.Window, Piece.CorridorWindow, new Vector3(0f, 180f, 0f));
                corridorDef6.SetFace(Direction.Above, FaceType.Window, Piece.CorridorXShapeWindowTop, Vector3.zero);
                corridorDef6.SetFace(Direction.East, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, 90f, 0f));
                corridorDef6.SetFace(Direction.East, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, 90f, 0f));
                corridorDef6.SetFace(Direction.West, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, -90f, 0f));
                corridorDef6.SetFace(Direction.West, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, -90f, 0f));
                corridorDef6.SetFace(Direction.North, FaceType.BulkheadClosed, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef6.SetFace(Direction.North, FaceType.BulkheadOpened, Piece.CorridorBulkhead, Vector3.zero);
                corridorDef6.SetFace(Direction.South, FaceType.BulkheadClosed, Piece.CorridorBulkhead, new Vector3(0f, 180f, 0f));
                corridorDef6.SetFace(Direction.South, FaceType.BulkheadOpened, Piece.CorridorBulkhead, new Vector3(0f, 180f, 0f));
                corridors = new CorridorDef[16];
                corridors[3] = corridorDef3;
                corridors[12] = corridorDef3.GetRotated(90f);
                corridors[5] = corridorDef4;
                corridors[6] = corridorDef4.GetRotated(90f);
                corridors[10] = corridorDef4.GetRotated(180f);
                corridors[9] = corridorDef4.GetRotated(-90f);
                corridors[13] = corridorDef5;
                corridors[7] = corridorDef5.GetRotated(90f);
                corridors[14] = corridorDef5.GetRotated(180f);
                corridors[11] = corridorDef5.GetRotated(-90f);
                corridors[15] = corridorDef6;
                glassCorridors = new CorridorDef[16];
                glassCorridors[3] = corridorDef;
                glassCorridors[12] = corridorDef.GetRotated(90f);
                glassCorridors[5] = corridorDef2;
                glassCorridors[6] = corridorDef2.GetRotated(90f);
                glassCorridors[10] = corridorDef2.GetRotated(180f);
                glassCorridors[9] = corridorDef2.GetRotated(-90f);
            }
        }

        public bool IsCellEmpty(Int3 cell)
        {
            int cellIndex = GetCellIndex(cell);
            if (cellIndex != -1)
            {
                return cells[cellIndex] == CellType.Empty;
            }
            return true;
        }

        private bool IsFoundation(int index)
        {
            return cells[index] == CellType.Foundation;
        }

        public bool IsInterior(int index)
        {
            return IsInterior(cells[index]);
        }

        public bool CompareRoomCellTypes(Int3 startCell, CellType compareType, bool hasAny = false)
        {
            bool result = !hasAny;
            Int3 @int = CellSize[1];
            Int3.Bounds bounds = new Int3.Bounds(startCell, startCell + @int - 1);
            for (int i = bounds.mins.x; i <= bounds.maxs.x; i++)
            {
                for (int j = bounds.mins.z; j <= bounds.maxs.z; j++)
                {
                    CellType cell = GetCell(new Int3(i, startCell.y, j));
                    if (hasAny)
                    {
                        if (cell == compareType)
                        {
                            result = true;
                            break;
                        }
                    }
                    else if (cell != compareType)
                    {
                        result = false;
                        break;
                    }
                }
            }
            return result;
        }

        private bool IsInterior(CellType cellType)
        {
            if (cellType != CellType.Room && cellType != CellType.Corridor && cellType != CellType.Observatory && cellType != CellType.Moonpool && cellType != CellType.MapRoom)
            {
                return cellType == CellType.MapRoomRotated;
            }
            return true;
        }

        public bool HasSpaceFor(Int3 cell, Int3 size)
        {
            GetComponentsInChildren(sGhosts);
            bool result = HasSpaceFor(cell, size, sGhosts);
            sGhosts.Clear();
            return result;
        }

        private bool HasSpaceFor(Int3 cell, Int3 size, List<BaseGhost> ghosts)
        {
            Int3.RangeEnumerator rangeEnumerator = Int3.Range(cell, cell + size - 1);
            while (rangeEnumerator.MoveNext())
            {
                Int3 current = rangeEnumerator.Current;
                int index = baseShape.GetIndex(current);
                if (index != -1 && cells[index] != 0)
                {
                    return false;
                }
                if (IsCellUnderConstruction(current, ghosts))
                {
                    return false;
                }
            }
            return true;
        }

        private bool HasSpaceFor(Int3 cell, Piece piece)
        {
            if (piece == Piece.Invalid)
            {
                return false;
            }
            PieceDef pieceDef = pieces[(int)piece];
            return HasSpaceFor(cell, pieceDef.extraCells + 1);
        }

        private bool HasFoundation(Int3 point)
        {
            Int3 maxs = point - new Int3(0, 1, 0);
            foreach (Int3 item in Int3.Range(new Int3(maxs.x, 0, maxs.z), maxs))
            {
                int cellIndex = GetCellIndex(item);
                if (cellIndex == -1)
                {
                    break;
                }
                if (IsFoundation(cellIndex))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasFoundationFor(Int3 cell, Piece piece)
        {
            if (piece == Piece.Invalid)
            {
                return false;
            }
            PieceDef pieceDef = pieces[(int)piece];
            foreach (Int3 item in Int3.Range(cell, cell + pieceDef.extraCells))
            {
                if (!HasFoundation(item))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsHorizontal(Direction direction)
        {
            Direction[] horizontalDirections = HorizontalDirections;
            for (int i = 0; i < horizontalDirections.Length; i++)
            {
                if (horizontalDirections[i] == direction)
                {
                    return true;
                }
            }
            return false;
        }

        public Transform CreateCellObject(Int3 cell)
        {
            int cellIndex = GetCellIndex(cell);
            if (cellIndex == -1)
            {
                return null;
            }
            Transform transform = global::UnityEngine.Object.Instantiate(cellPrefab, GridToLocal(cell), Quaternion.identity);
            cellObjects[cellIndex] = transform;
            transform.SetParent(base.transform, worldPositionStays: false);
            transform.tag = "Generated";
            return transform;
        }

        public Transform FindFaceObject(Face face)
        {
            Transform result = null;
            Transform cellObject = GetCellObject(face.cell);
            if (cellObject != null)
            {
                cellObject.GetComponentsInChildren(includeInactive: false, sDeconstructables);
                for (int i = 0; i < sDeconstructables.Count; i++)
                {
                    BaseDeconstructable baseDeconstructable = sDeconstructables[i];
                    if (baseDeconstructable != null && baseDeconstructable.face.HasValue && baseDeconstructable.face.Value == face)
                    {
                        result = baseDeconstructable.transform;
                        break;
                    }
                }
            }
            sDeconstructables.Clear();
            return result;
        }

        public Transform GetCellObject(Int3 cell)
        {
            int cellIndex = GetCellIndex(cell);
            if (cellIndex == -1)
            {
                return null;
            }
            return cellObjects[cellIndex];
        }

        public Int3? FindCellObject(Transform cellObject)
        {
            foreach (Int3 allCell in AllCells)
            {
                if (GetCellObject(allCell) == cellObject)
                {
                    return allCell;
                }
            }
            return null;
        }

        private void BindCellObjects()
        {
            BaseCell[] componentsInChildren = GetComponentsInChildren<BaseCell>();
            foreach (BaseCell baseCell in componentsInChildren)
            {
                if (baseCell.transform.parent != base.transform)
                {
                    continue;
                }
                Int3 @int = WorldToGrid(baseCell.transform.position);
                int index = baseShape.GetIndex(@int);
                if (index == -1)
                {
                    Debug.LogError("Base contains invalid cell object at: " + @int);
                }
                else if (cellObjects[index] != null)
                {
                    if (cellObjects[index] != baseCell)
                    {
                        Debug.LogError("Cell object already bound: " + @int);
                    }
                }
                else
                {
                    cellObjects[index] = baseCell.transform;
                }
            }
        }

        private Transform SpawnPiece(Piece piece, Int3 cell, Quaternion rotation, Direction? faceDirection = null)
        {
            if (piece == Piece.Invalid)
            {
                return null;
            }
            Transform transform = GetCellObject(cell);
            if (transform == null)
            {
                transform = CreateCellObject(cell);
            }
            PieceDef pieceDef = pieces[(int)piece];
            Vector3 position = Int3.Scale(pieceDef.extraCells, halfCellSize);
            Transform transform2 = global::UnityEngine.Object.Instantiate(pieceDef.prefab, position, pieceDef.rotation * rotation);
            if (faceDirection.HasValue && piece == Piece.CorridorBulkhead)
            {
                BaseWaterTransition[] componentsInChildren = transform2.GetComponentsInChildren<BaseWaterTransition>();
                foreach (BaseWaterTransition obj in componentsInChildren)
                {
                    obj.face.cell = cell;
                    obj.face.direction = faceDirection.Value;
                }
            }
            transform2.SetParent(transform, worldPositionStays: false);
            transform2.BroadcastMessage("OnAddedToBase", this, SendMessageOptions.DontRequireReceiver);
            transform2.gameObject.SetActive(value: true);
            return transform2;
        }

        private Transform SpawnPiece(Piece piece, Int3 cell)
        {
            return SpawnPiece(piece, cell, Quaternion.identity);
        }

        public static Direction ReverseDirection(Direction direction)
        {
            return OppositeDirections[(int)direction];
        }

        private static byte PackOffset(Int3 offset)
        {
            return (byte)((uint)(((offset.x & 7) << 5) | ((offset.y & 3) << 3)) | ((uint)offset.z & 7u));
        }

        private static Int3 UnpackOffset(byte packedOffset)
        {
            return new Int3((packedOffset >> 5) & 7, (packedOffset >> 3) & 3, packedOffset & 7);
        }

        public bool IsCellValid(Int3 cell)
        {
            return baseShape.GetIndex(cell) != -1;
        }

        public Int3 NormalizeCell(Int3 cell)
        {
            int index = baseShape.GetIndex(cell);
            if (index != -1 && cells[index] == CellType.OccupiedByOtherCell)
            {
                return cell - UnpackOffset(links[index]);
            }
            return cell;
        }

        public CellType GetRawCellType(Int3 cell)
        {
            int index = baseShape.GetIndex(cell);
            if (index != -1)
            {
                return cells[index];
            }
            return CellType.Empty;
        }

        public int GetCellIndex(Int3 cell)
        {
            return baseShape.GetIndex(NormalizeCell(cell));
        }

        private Int3 GetCellPointFromIndex(int cellIndex)
        {
            return baseShape.GetPoint(cellIndex).ToInt3();
        }

        public CellType GetCell(Int3 cell)
        {
            int cellIndex = GetCellIndex(cell);
            if (cellIndex == -1)
            {
                return CellType.Empty;
            }
            return cells[cellIndex];
        }

        public CellType GetCell(int cellIndex)
        {
            return GetCell(baseShape.GetPoint(cellIndex).ToInt3());
        }

        public float GetCellPowerConsumption(Int3 cell)
        {
            return CellPowerConsumption[(uint)GetCell(cell)];
        }

        public Int3 GetAnchor()
        {
            return anchor;
        }

        private static int GetFaceIndex(int cellIndex, Direction direction)
        {
            return (int)(cellIndex * 6 + direction);
        }

        private Direction NormalizeFaceDirection(int cellIndex, Direction direction)
        {
            int faceIndex = GetFaceIndex(cellIndex, direction);
            FaceType faceType = faces[faceIndex];
            if ((faceType & FaceType.OccupiedByOtherFace) != 0)
            {
                direction = (Direction)(faceType & (FaceType)127);
            }
            return direction;
        }

        private int GetNormalizedFaceIndex(int cellIndex, Direction direction)
        {
            return GetFaceIndex(cellIndex, NormalizeFaceDirection(cellIndex, direction));
        }

        private FaceType GetFace(int index, Direction direction)
        {
            return faces[GetNormalizedFaceIndex(index, direction)];
        }

        private void SetFace(int index, Direction direction, FaceType faceType)
        {
            faces[GetNormalizedFaceIndex(index, direction)] = faceType;
        }

        private void SetFaceOccupiedBy(Face face, Direction occupyingDirection)
        {
            int index = baseShape.GetIndex(face.cell);
            faces[GetFaceIndex(index, face.direction)] = FaceType.OccupiedByOtherFace | (FaceType)occupyingDirection;
        }

        public bool GetAreCellFacesUsed(Int3 cell)
        {
            int index = baseShape.GetIndex(cell);
            switch (GetCell(cell))
            {
                case CellType.Empty:
                case CellType.Foundation:
                    return false;
                case CellType.Corridor:
                {
                    CorridorDef corridorDef = GetCorridorDef(index);
                    int num = 7;
                    Direction[] allDirections = AllDirections;
                    foreach (Direction direction2 in allDirections)
                    {
                        if (IsCellFaceUsed(index, direction2))
                        {
                            return true;
                        }
                        Direction direction3 = corridorDef.worldToLocal[(int)direction2];
                        if (corridorDef.faces[(int)direction3, num].piece == Piece.Invalid)
                        {
                            continue;
                        }
                        Int3 adjacent = GetAdjacent(cell, direction2);
                        int cellIndex = GetCellIndex(adjacent);
                        if (cellIndex != -1)
                        {
                            int faceIndex = GetFaceIndex(cellIndex, ReverseDirection(direction2));
                            if (IsBulkhead(faces[faceIndex]))
                            {
                                return true;
                            }
                        }
                    }
                    break;
                }
                default:
                {
                    Direction[] allDirections = AllDirections;
                    foreach (Direction direction in allDirections)
                    {
                        if (IsCellFaceUsed(index, direction))
                        {
                            return true;
                        }
                    }
                    break;
                }
            }
            return false;
        }

        private bool IsCellFaceUsed(int cellIndex, Direction direction)
        {
            int faceIndex = GetFaceIndex(cellIndex, direction);
            FaceType faceType = faces[faceIndex];
            if ((faceType & FaceType.OccupiedByOtherFace) != 0)
            {
                Direction direction2 = (Direction)(faceType & (FaceType)127);
                int faceIndex2 = GetFaceIndex(cellIndex, direction2);
                faceType = faces[faceIndex2];
            }
            if (faceType != 0 && faceType != FaceType.Solid)
            {
                return faceType != FaceType.Hole;
            }
            return false;
        }

        public FaceType GetFace(Face face)
        {
            int index = baseShape.GetIndex(face.cell);
            if (index == -1)
            {
                return FaceType.None;
            }
            return GetFace(index, face.direction);
        }

        public FaceType GetFaceRaw(Face face)
        {
            int index = baseShape.GetIndex(face.cell);
            if (index == -1)
            {
                return FaceType.None;
            }
            int faceIndex = GetFaceIndex(index, face.direction);
            if (faceIndex < 0 || faceIndex >= faces.Length)
            {
                return FaceType.None;
            }
            return faces[faceIndex];
        }

        private bool CanSetCorridorFace(Face face, FaceType faceType)
        {
            int cellIndex = GetCellIndex(face.cell);
            if (GetFace(cellIndex, face.direction) != constructFaceTypes[(uint)faceType])
            {
                return false;
            }
            CorridorDef corridorDef = GetCorridorDef(cellIndex);
            Direction direction = corridorDef.worldToLocal[(int)face.direction];
            if (corridorDef.faces[(int)direction, (uint)faceType].piece == Piece.Invalid)
            {
                return false;
            }
            bool flag = IsInterior(cellIndex);
            bool result = false;
            switch (faceType)
            {
                case FaceType.Window:
                case FaceType.Hatch:
                case FaceType.ObsoleteDoor:
                case FaceType.Reinforcement:
                case FaceType.BulkheadClosed:
                case FaceType.BulkheadOpened:
                case FaceType.Planter:
                    result = flag;
                    break;
            }
            return result;
        }

        private Piece GetRoomPiece(Face face, FaceType faceType)
        {
            Int3 cell = face.cell;
            Int3 @int = NormalizeCell(cell);
            if (cell - @int == new Int3(1, 0, 1))
            {
                return roomFaceCentralPieces[(int)face.direction, (uint)faceType];
            }
            return roomFacePieces[(int)face.direction, (uint)faceType];
        }

        private Piece GetMoonpoolPiece(Face face, FaceType faceType)
        {
            Int3 cell = face.cell;
            NormalizeCell(cell);
            int direction = (int)face.direction;
            if (direction >= 0 && direction < moonpoolFacePieces.GetLength(0) && (int)faceType >= 0 && (int)faceType < moonpoolFacePieces.GetLength(1))
            {
                return moonpoolFacePieces[direction, (uint)faceType];
            }
            return Piece.Invalid;
        }

        public bool CanSetWaterPark(Face faceStart, out Face faceEnd)
        {
            faceEnd = default(Face);
            faceEnd.cell = faceStart.cell;
            faceEnd.direction = ((faceStart.direction == Direction.Below) ? Direction.Above : Direction.Below);
            int cellIndex = GetCellIndex(faceStart.cell);
            if (cellIndex == -1)
            {
                return false;
            }
            CellType cellType = cells[cellIndex];
            if (cellType != 0 && cellType != CellType.Room)
            {
                return false;
            }
            if (GetRoomPiece(faceStart, FaceType.WaterPark) == Piece.Invalid)
            {
                return false;
            }
            if (GetRoomPiece(faceEnd, FaceType.WaterPark) == Piece.Invalid)
            {
                return false;
            }
            CellType cell = GetCell(faceEnd.cell);
            if (cell != 0 && cell != CellType.Room)
            {
                return false;
            }
            if (GetFace(faceStart) != FaceType.Solid)
            {
                return false;
            }
            int index = baseShape.GetIndex(faceStart.cell);
            Direction[] horizontalDirections = HorizontalDirections;
            foreach (Direction direction in horizontalDirections)
            {
                if (GetFace(index, direction) != 0)
                {
                    return false;
                }
            }
            if (GetFace(faceEnd) != FaceType.Solid)
            {
                return false;
            }
            index = baseShape.GetIndex(faceEnd.cell);
            horizontalDirections = HorizontalDirections;
            foreach (Direction direction2 in horizontalDirections)
            {
                if (GetFace(index, direction2) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanSetLadder(Face faceStart, out Face faceEnd)
        {
            faceEnd = faceStart;
            int cellIndex = GetCellIndex(faceStart.cell);
            if (cellIndex == -1)
            {
                return false;
            }
            switch (cells[cellIndex])
            {
                case CellType.Room:
                    return CanSetRoomLadder(cellIndex, faceStart, out faceEnd);
                case CellType.Corridor:
                {
                    FaceType face = GetFace(cellIndex, faceStart.direction);
                    if (face != FaceType.Solid && face != FaceType.Hole)
                    {
                        return false;
                    }
                    CorridorDef corridorDef = GetCorridorDef(cellIndex);
                    Direction direction = corridorDef.worldToLocal[(int)faceStart.direction];
                    if (corridorDef.faces[(int)direction, (uint)face].piece == Piece.Invalid)
                    {
                        return false;
                    }
                    Int3 exit = default(Int3);
                    if (GetLadderExitCell(faceStart, out exit))
                    {
                        faceEnd = new Face(exit, ReverseDirection(faceStart.direction));
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        private bool CanSetRoomLadder(int index, Face faceStart, out Face faceEnd)
        {
            faceEnd = GetAdjacentFace(faceStart);
            if (GetRoomPiece(faceStart, FaceType.Ladder) == Piece.Invalid)
            {
                return false;
            }
            if (GetCell(faceEnd.cell) != CellType.Room)
            {
                return false;
            }
            Int3 @int = NormalizeCell(faceStart.cell);
            Int3 int2 = faceStart.cell - @int;
            if (int2 < Int3.zero || int2 >= CellSize[1])
            {
                return false;
            }
            if (!roomLadderPlaces[int2.x, int2.z])
            {
                return false;
            }
            if (GetFace(faceStart) != FaceType.Solid)
            {
                return false;
            }
            if (GetFace(faceEnd) != FaceType.Solid)
            {
                return false;
            }
            return true;
        }

        public bool GetLadderExitPosition(Face face, out Vector3 position)
        {
            position = Vector3.zero;
            if (GetLadderExitCell(face, out var exit))
            {
                int cellIndex = GetCellIndex(exit);
                if (cellIndex == -1)
                {
                    Debug.LogErrorFormat(this, "Could not find cell index for ladder exit {0}", exit);
                    return false;
                }
                Int3 cell = exit;
                Vector3 vector = corridorLadderExit;
                if (cells[cellIndex] == CellType.Room)
                {
                    Int3 @int = NormalizeCell(exit);
                    Int3 int2 = exit - @int;
                    if (int2 < Int3.zero || int2 >= CellSize[1])
                    {
                        Debug.LogErrorFormat(this, "Exit offset {0} is out of room bounds", int2);
                        return false;
                    }
                    cell = @int;
                    vector = roomLadderExits[int2.x, int2.z];
                }
                Vector3 vector2 = GridToLocal(cell);
                position = base.transform.TransformPoint(vector2 + vector);
                return true;
            }
            return false;
        }

        private bool GetLadderExitCell(Face face, out Int3 exit)
        {
            return GetLadderExitCell(face.cell, face.direction, out exit);
        }

        public bool GetLadderExitCell(Int3 cell, Direction direction, out Int3 exit)
        {
            exit = Int3.zero;
            if (direction != Direction.Above && direction != Direction.Below)
            {
                return false;
            }
            int cellIndex = GetCellIndex(cell);
            if (cellIndex == -1)
            {
                return false;
            }
            CellType cellType = cells[cellIndex];
            if (!IsInterior(cellType))
            {
                return false;
            }
            do
            {
                cell += DirectionOffset[(int)direction];
                cellIndex = GetCellIndex(cell);
                if (cellIndex == -1)
                {
                    return false;
                }
                cellType = cells[cellIndex];
            }
            while (cellType == CellType.Connector);
            if (IsInterior(cellType))
            {
                CorridorDef corridorDef = GetCorridorDef(cellIndex);
                if (corridorDef.piece == Piece.CorridorLShape || cellType == CellType.Observatory || cellType == CellType.MapRoom || cellType == CellType.MapRoomRotated || (corridorDef.piece == Piece.CorridorIShapeGlass && direction == Direction.Below))
                {
                    return false;
                }
                exit = cell;
                return true;
            }
            return false;
        }

        public static bool IsBulkhead(FaceType faceType)
        {
            if (faceType != FaceType.BulkheadClosed)
            {
                return faceType == FaceType.BulkheadOpened;
            }
            return true;
        }

        public bool CanSetBulkhead(Face fromCell)
        {
            Face adjacentFace = GetAdjacentFace(fromCell);
            if (!CanSetFace(fromCell, FaceType.BulkheadClosed))
            {
                return false;
            }
            if (!CanSetFace(adjacentFace, FaceType.BulkheadClosed))
            {
                return false;
            }
            return true;
        }

        public bool CanSetConnector(Int3 cell)
        {
            if (GetCell(cell) != 0)
            {
                return false;
            }
            Int3 adjacent = GetAdjacent(cell, Direction.Above);
            CellType cell2 = GetCell(adjacent);
            Int3 adjacent2 = GetAdjacent(cell, Direction.Below);
            CellType cell3 = GetCell(adjacent2);
            if (cell2 == CellType.Empty)
            {
                if (cell3 == CellType.Empty)
                {
                    return false;
                }
                return CanConnectToCell(adjacent2, Direction.Above);
            }
            if (cell3 == CellType.Empty)
            {
                return CanConnectToCell(adjacent, Direction.Below);
            }
            if (CanConnectToCell(adjacent, Direction.Below))
            {
                return CanConnectToCell(adjacent2, Direction.Above);
            }
            return false;
        }

        private bool CanConnectToCell(Int3 cell, Direction direction)
        {
            switch (GetCell(cell))
            {
                case CellType.Connector:
                    return true;
                case CellType.Corridor:
                {
                    int cellIndex = GetCellIndex(cell);
                    if (isGlass[cellIndex] && direction == Direction.Above)
                    {
                        return false;
                    }
                    if (GetCorridorDef(cellIndex).piece == Piece.CorridorLShape)
                    {
                        return false;
                    }
                    FaceType face = GetFace(cellIndex, direction);
                    if (face != FaceType.Solid)
                    {
                        return face == FaceType.Hole;
                    }
                    return true;
                }
                default:
                    return false;
            }
        }

        private bool CanSetRoomFace(Face face, FaceType faceType)
        {
            int index = baseShape.GetIndex(face.cell);
            if (GetFace(index, face.direction) != constructFaceTypes[(uint)faceType])
            {
                return false;
            }
            bool flag = GetCell(GetAdjacent(face)) == CellType.Room;
            bool flag2 = face.direction == Direction.Above || face.direction == Direction.Below;
            bool result = false;
            switch (faceType)
            {
                case FaceType.BulkheadClosed:
                case FaceType.BulkheadOpened:
                    result = !flag2;
                    break;
                case FaceType.Ladder:
                    result = flag && flag2;
                    break;
                case FaceType.Window:
                case FaceType.Hatch:
                case FaceType.Reinforcement:
                case FaceType.Planter:
                case FaceType.FiltrationMachine:
                    result = !flag2;
                    break;
            }
            return result;
        }

        public bool CanSetModule(ref Face face, FaceType faceType)
        {
            int cellIndex = GetCellIndex(face.cell);
            if (cellIndex == -1)
            {
                return false;
            }
            if (cells[cellIndex] != CellType.Room)
            {
                return false;
            }
            if (GetRoomPiece(face, faceType) == Piece.Invalid)
            {
                return false;
            }
            Int3 @int = NormalizeCell(face.cell);
            face.cell = @int + new Int3(1, 0, 1);
            int index = baseShape.GetIndex(face.cell);
            Direction[] horizontalDirections = HorizontalDirections;
            foreach (Direction direction in horizontalDirections)
            {
                FaceType face2 = GetFace(index, direction);
                if (face2 != 0 && face2 != FaceType.Solid)
                {
                    return false;
                }
            }
            horizontalDirections = VerticalDirections;
            foreach (Direction direction2 in horizontalDirections)
            {
                if (GetFace(index, direction2) != FaceType.Solid)
                {
                    return false;
                }
            }
            return true;
        }

        public IBaseModule GetModule(Face face)
        {
            Int3 @int = face.cell - anchor;
            GetComponentsInChildren(includeInactive: true, sBaseModules);
            int i = 0;
            for (int count = sBaseModules.Count; i < count; i++)
            {
                IBaseModule baseModule = sBaseModules[i];
                Face moduleFace = baseModule.moduleFace;
                if (moduleFace.cell == @int && moduleFace.direction == face.direction)
                {
                    sBaseModules.Clear();
                    return baseModule;
                }
            }
            sBaseModules.Clear();
            return null;
        }

        public IBaseModuleGeometry GetModuleGeometry(Face face)
        {
            if (cells != null && cellObjects != null)
            {
                Int3 cell = anchor + face.cell;
                Transform cellObject = GetCellObject(cell);
                if (cellObject != null)
                {
                    cellObject.GetComponentsInChildren(includeInactive: true, sBaseModulesGeometry);
                    int i = 0;
                    for (int count = sBaseModulesGeometry.Count; i < count; i++)
                    {
                        IBaseModuleGeometry baseModuleGeometry = sBaseModulesGeometry[i];
                        if (baseModuleGeometry.geometryFace.direction == face.direction)
                        {
                            sBaseModulesGeometry.Clear();
                            return baseModuleGeometry;
                        }
                    }
                    sBaseModulesGeometry.Clear();
                }
            }
            return null;
        }

        private bool CanSetObservatoryFace(Face face, FaceType faceType)
        {
            if (faceType != FaceType.Hatch || GetFace(baseShape.GetIndex(face.cell), face.direction) != FaceType.Solid)
            {
                return false;
            }
            return true;
        }

        private bool CanSetMapRoomFace(Face face, FaceType faceType)
        {
            int index = baseShape.GetIndex(face.cell);
            if (GetFace(index, face.direction) != constructFaceTypes[(uint)faceType])
            {
                return false;
            }
            return mapRoomFacePieces[(uint)faceType] != Piece.Invalid;
        }

        private bool CanSetMoonpoolFace(Face face, FaceType faceType)
        {
            int index = baseShape.GetIndex(face.cell);
            if (GetFace(index, face.direction) != constructFaceTypes[(uint)faceType])
            {
                return false;
            }
            bool result = false;
            switch (faceType)
            {
                case FaceType.Window:
                case FaceType.Hatch:
                case FaceType.Reinforcement:
                case FaceType.BulkheadClosed:
                case FaceType.BulkheadOpened:
                case FaceType.Planter:
                    result = true;
                    break;
                case FaceType.UpgradeConsole:
                {
                    result = true;
                    int i = 0;
                    for (int num = moonpoolFaces.Length; i < num; i++)
                    {
                        RoomFace roomFace = moonpoolFaces[i];
                        Face face2 = new Face(NormalizeCell(face.cell) + roomFace.offset, roomFace.direction);
                        if (GetFaceMask(face2) && GetFace(face2) == FaceType.UpgradeConsole)
                        {
                            result = false;
                            break;
                        }
                    }
                    break;
                }
            }
            return result;
        }

        private bool CanSetWaterParkFace(Face face, FaceType faceType)
        {
            if (faceType != FaceType.Hatch || face.direction == Direction.Above || face.direction == Direction.Below)
            {
                return false;
            }
            baseShape.GetIndex(face.cell);
            return GetFace(face) == FaceType.Solid;
        }

        public bool CanSetFace(Face srcStart, FaceType faceType)
        {
            switch (GetCell(srcStart.cell))
            {
                case CellType.Corridor:
                    return CanSetCorridorFace(srcStart, faceType);
                case CellType.Room:
                {
                    Face face = new Face(srcStart.cell, Direction.Above);
                    if (GetFace(face) == FaceType.WaterPark)
                    {
                        return CanSetWaterParkFace(srcStart, faceType);
                    }
                    return CanSetRoomFace(srcStart, faceType);
                }
                case CellType.Observatory:
                    return CanSetObservatoryFace(srcStart, faceType);
                case CellType.MapRoom:
                case CellType.MapRoomRotated:
                    return CanSetMapRoomFace(srcStart, faceType);
                case CellType.Moonpool:
                    return CanSetMoonpoolFace(srcStart, faceType);
                default:
                    return false;
            }
        }

        public void SetFace(Face face, FaceType faceType)
        {
            int index = baseShape.GetIndex(face.cell);
            if (index != -1)
            {
                SetFace(index, face.direction, faceType);
            }
        }

        private void UpdateFlowData(Int3 cell)
        {
            int cellIndex = GetCellIndex(cell);
            if (cellIndex == -1)
            {
                return;
            }
            byte b = 0;
            if (IsInterior(cellIndex))
            {
                b = (byte)(b | 0x40u);
            }
            Face face = default(Face);
            face.cell = cell;
            Direction[] allDirections = AllDirections;
            foreach (Direction direction in allDirections)
            {
                int cellIndex2 = GetCellIndex(cell + DirectionOffset[(int)direction]);
                if (cellIndex2 == -1 || !IsInterior(cellIndex2))
                {
                    continue;
                }
                face.direction = direction;
                FaceType faceType = GetFace(face);
                if (faceType == FaceType.None)
                {
                    FaceType face2 = GetFace(GetAdjacentFace(face));
                    if (IsBulkhead(face2))
                    {
                        faceType = face2;
                    }
                }
                if (faceType == FaceType.None || faceType == FaceType.ObsoleteDoor || faceType == FaceType.Ladder || faceType == FaceType.BulkheadOpened)
                {
                    b = (byte)(b | (byte)(1 << (int)direction));
                }
            }
            int index = baseShape.GetIndex(cell);
            flowData[index] = b;
        }

        private void RecalculateFlowData()
        {
            foreach (Int3 allCell in AllCells)
            {
                UpdateFlowData(allCell);
            }
        }

        private void RecomputeOccupiedCells()
        {
            occupiedCellIndexes.Clear();
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != 0)
                {
                    occupiedCellIndexes.Add(i);
                }
            }
        }

        private void UpdateFlowDataForCellAndNeighbors(Int3 cell)
        {
            UpdateFlowData(cell);
            Face face = default(Face);
            face.cell = cell;
            Direction[] allDirections = AllDirections;
            for (int i = 0; i < allDirections.Length; i++)
            {
                Direction direction = (face.direction = allDirections[i]);
                Int3 adjacent = GetAdjacent(face);
                UpdateFlowData(adjacent);
            }
        }

        private void SetFaceAndUpdateFlow(Face face, FaceType faceType)
        {
            SetFace(face, faceType);
            UpdateFlowData(face.cell);
            Int3 adjacent = GetAdjacent(face);
            UpdateFlowData(adjacent);
        }

        public static Int3 GetAdjacent(Int3 cell, Direction direction)
        {
            return cell + DirectionOffset[(int)direction];
        }

        public static Int3 GetAdjacent(Face face)
        {
            return GetAdjacent(face.cell, face.direction);
        }

        public static Face GetAdjacentFace(Face face)
        {
            return new Face(GetAdjacent(face), ReverseDirection(face.direction));
        }

        private void BuildFoundationGeometry(Int3 cell)
        {
            Transform obj = SpawnPiece(Piece.Foundation, cell);
            Int3 @int = CellSize[2];
            Int3.Bounds bounds = new Int3.Bounds(cell, cell + @int - 1);
            BaseDeconstructable.MakeCellDeconstructable(obj, bounds, TechType.BaseFoundation);
            obj.tag = "MainPieceGeometry";
        }

        private bool IsCellUsed(int index)
        {
            if (masks != null)
            {
                return (masks[index] & 0x40) != 0;
            }
            return true;
        }

        public bool IsFaceUsed(int index, Direction direction)
        {
            if (masks != null)
            {
                return (masks[index] & (1 << (int)direction)) != 0;
            }
            return true;
        }

        private void BuildCorridorGeometry(Int3 cell, int index)
        {
            CorridorDef corridorDef = GetCorridorDef(index);
            Int3.Bounds bounds = new Int3.Bounds(cell, cell);
            BaseDeconstructable parent = null;
            if (IsCellUsed(index))
            {
                TechType recipe = (isGlass[index] ? TechType.BaseCorridorGlass : TechType.BaseCorridor);
                Transform obj = SpawnPiece(corridorDef.piece, cell, corridorDef.rotation);
                parent = BaseDeconstructable.MakeCellDeconstructable(obj, bounds, recipe);
                obj.tag = "MainPieceGeometry";
                if (!isGhost)
                {
                    Piece piece = corridorDef.adjustableSupportPiece;
                    Int3.Bounds bounds2 = Bounds;
                    Int3 cell2 = cell;
                    for (int num = cell.y - 1; num >= bounds2.mins.y; num--)
                    {
                        cell2.y = num;
                        CellType cell3 = GetCell(cell2);
                        if (cell3 == CellType.Foundation && num == cell.y - 1)
                        {
                            piece = corridorDef.supportPiece;
                            break;
                        }
                        if (cell3 != 0)
                        {
                            piece = Piece.Invalid;
                            break;
                        }
                    }
                    SpawnPiece(piece, cell, corridorDef.rotation);
                }
            }
            Direction[] allDirections = AllDirections;
            foreach (Direction direction in allDirections)
            {
                if (!IsFaceUsed(index, direction))
                {
                    continue;
                }
                FaceType face2 = GetFace(index, direction);
                Direction direction2 = corridorDef.worldToLocal[(int)direction];
                CorridorFace corridorFace = corridorDef.faces[(int)direction2, (uint)face2];
                Quaternion rotation = corridorDef.rotation * corridorFace.rotation;
                if (direction == Direction.Above || direction == Direction.Below)
                {
                    switch (face2)
                    {
                        case FaceType.Solid:
                        {
                            if (ExteriorToInteriorPiece(corridorFace.piece, out var interior3))
                            {
                                SpawnPiece(interior3, cell, rotation, direction);
                            }
                            break;
                        }
                        case FaceType.Hole:
                        {
                            CorridorFace corridorFace3 = corridorDef.faces[(int)direction2, 1];
                            if (ExteriorToInteriorPiece(corridorFace3.piece, out var interior2))
                            {
                                SpawnPiece(interior2, cell, rotation, direction);
                            }
                            break;
                        }
                        case FaceType.Ladder:
                            if (!isGhost)
                            {
                                CorridorFace corridorFace2 = corridorDef.faces[(int)direction2, 9];
                                SpawnPiece(corridorFace2.piece, cell, rotation);
                                if (ExteriorToInteriorPiece(corridorFace2.piece, out var interior))
                                {
                                    SpawnPiece(interior, cell, rotation, direction);
                                }
                            }
                            rotation = Quaternion.identity;
                            break;
                    }
                }
                Transform facePiece = SpawnPiece(corridorFace.piece, cell, rotation, direction);
                if (face2 == FaceType.None)
                {
                    continue;
                }
                Face face = new Face(cell, direction);
                TechType recipe2 = FaceToRecipe[(uint)face2];
                if (IsBulkhead(face2))
                {
                    BulkheadDoor componentInChildren = facePiece.GetComponentInChildren<BulkheadDoor>();
                    if (componentInChildren != null)
                    {
                        Direction bulkheadDirection = direction;
                        componentInChildren.SetState(face2 == FaceType.BulkheadOpened);
                        componentInChildren.onStateChange = (BulkheadDoor.OnStateChange)Delegate.Combine(componentInChildren.onStateChange, (BulkheadDoor.OnStateChange)delegate(bool open)
                        {
                            FaceType faceType = (open ? FaceType.BulkheadOpened : FaceType.BulkheadClosed);
                            int index2 = cellObjects.IndexOf(facePiece.parent);
                            Grid3Point point = baseShape.GetPoint(index2);
                            if (point.Valid)
                            {
                                SetFace(index2, bulkheadDirection, faceType);
                                UpdateFlowDataForCellAndNeighbors(point.ToInt3());
                                if (this.onBulkheadFaceChanged != null)
                                {
                                    this.onBulkheadFaceChanged(this, face);
                                }
                            }
                            else
                            {
                                Debug.LogError("Bulkhead door state changed but doesn't seem to be part of a base anymore");
                            }
                        });
                    }
                    else
                    {
                        Debug.LogError("Face tagged as bulkhead but piece missing BulkheadDoor component");
                    }
                }
                switch (face2)
                {
                    case FaceType.Ladder:
                    {
                        if (GetLadderExitCell(face, out var exit))
                        {
                            Int3.Bounds bounds3 = bounds.Union(exit);
                            BaseDeconstructable.MakeFaceDeconstructable(facePiece, bounds3, face, FaceType.Ladder, recipe2);
                        }
                        else
                        {
                            Debug.LogError("Face tagged as ladder but could not find exit cell");
                        }
                        break;
                    }
                    case FaceType.Solid:
                        if (!isGhost)
                        {
                            BaseExplicitFace.MakeFaceDeconstructable(facePiece, face, parent);
                        }
                        break;
                    default:
                        if (!isGhost)
                        {
                            BaseDeconstructable.MakeFaceDeconstructable(facePiece, bounds, face, face2, recipe2);
                        }
                        break;
                    case FaceType.Hole:
                        break;
                }
            }
        }

        public Direction GetObservatoryRotation(Int3 cell, out float yaw)
        {
            Direction result = Direction.East;
            yaw = 0f;
            if (IsValidObsConnection(GetAdjacent(cell, Direction.South), Direction.North))
            {
                yaw = 90f;
                result = Direction.South;
            }
            else if (IsValidObsConnection(GetAdjacent(cell, Direction.West), Direction.East))
            {
                yaw = 180f;
                result = Direction.West;
            }
            else if (IsValidObsConnection(GetAdjacent(cell, Direction.North), Direction.South))
            {
                yaw = 270f;
                result = Direction.North;
            }
            return result;
        }

        private void BuildObservatoryGeometry(Int3 cell)
        {
            _ = ref CellSize[5];
            Int3.Bounds bounds = new Int3.Bounds(cell, cell);
            float yaw = 0f;
            Direction observatoryRotation = GetObservatoryRotation(cell, out yaw);
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            if (GetCellMask(cell))
            {
                Transform obj = SpawnPiece(Piece.Observatory, cell, rotation);
                BaseDeconstructable.MakeCellDeconstructable(obj, bounds, TechType.BaseObservatory);
                obj.tag = "MainPieceGeometry";
            }
            Face face = new Face(cell, observatoryRotation);
            if (GetFaceMask(face))
            {
                FaceType face2 = GetFace(face);
                Piece piece = observatoryFacePieces[(uint)face2];
                Transform geometry = SpawnPiece(piece, cell, Quaternion.identity);
                if (face2 != FaceType.Solid)
                {
                    TechType recipe = FaceToRecipe[(uint)face2];
                    BaseDeconstructable.MakeFaceDeconstructable(geometry, bounds, face, face2, recipe);
                }
            }
        }

        private void BuildMapRoomGeometry(Int3 cell, int index, CellType cellType)
        {
            Int3 @int = CellSize[(uint)cellType];
            Int3.Bounds bounds = new Int3.Bounds(cell, cell + @int - 1);
            Transform transform = null;
            float y = ((cellType == CellType.MapRoomRotated) ? 90 : 0);
            Quaternion rotation = Quaternion.Euler(0f, y, 0f);
            if (GetCellMask(cell))
            {
                transform = SpawnPiece(Piece.MapRoom, cell, rotation);
                BaseDeconstructable.MakeCellDeconstructable(transform, bounds, TechType.BaseMapRoom);
                transform.tag = "MainPieceGeometry";
            }
            FaceDef[] array = faceDefs[(uint)cellType];
            for (int i = 0; i < array.Length; i++)
            {
                FaceDef faceDef = array[i];
                Face face = new Face(cell + faceDef.face.cell, faceDef.face.direction);
                if (GetFaceMask(face))
                {
                    FaceType face2 = GetFace(face);
                    Piece piece = mapRoomFacePieces[(uint)face2];
                    Transform geometry = SpawnPiece(piece, cell, FaceRotation[(int)faceDef.face.direction]);
                    if (face2 != FaceType.Solid)
                    {
                        TechType recipe = FaceToRecipe[(uint)face2];
                        BaseDeconstructable.MakeFaceDeconstructable(geometry, bounds, face, face2, recipe);
                    }
                }
            }
            if (isGhost)
            {
                return;
            }
            bool flag = false;
            MapRoomFunctionality[] componentsInChildren = GetComponentsInChildren<MapRoomFunctionality>();
            for (int j = 0; j < componentsInChildren.Length; j++)
            {
                if (NormalizeCell(WorldToGrid(componentsInChildren[j].transform.position)) == cell)
                {
                    Debug.Log("found existing map room functionality at cell " + cell);
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                Debug.Log("create new map room functionality, cell " + cell);
                GameObject obj = global::UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Submarine/Build/MapRoomFunctionality"));
                obj.transform.parent = base.transform;
                obj.transform.position = transform.transform.position;
                obj.transform.rotation = transform.transform.rotation;
            }
        }

        private void BuildConnectorGeometry(Int3 cell, int index)
        {
            Int3.Bounds bounds = new Int3.Bounds(cell, cell);
            bool flag = GetFace(index, BaseAddLadderGhost.ladderFaceDir) == FaceType.Ladder;
            if (IsCellUsed(index))
            {
                Piece piece = (flag ? Piece.ConnectorTubeWindow : Piece.ConnectorTube);
                Transform obj = SpawnPiece(piece, cell, Quaternion.Euler(0f, 90f, 0f));
                BaseDeconstructable.MakeCellDeconstructable(obj, bounds, TechType.BaseConnector);
                obj.tag = "MainPieceGeometry";
            }
            if (flag)
            {
                SpawnPiece(Piece.ConnectorLadder, cell, Quaternion.identity);
            }
            Direction[] verticalDirections = VerticalDirections;
            foreach (Direction direction in verticalDirections)
            {
                if (!IsFaceUsed(index, direction))
                {
                    continue;
                }
                Int3 adjacent = GetAdjacent(cell, direction);
                int cellIndex = GetCellIndex(adjacent);
                CellType cell2 = GetCell(adjacent);
                bool flag2 = false;
                switch (cell2)
                {
                    case CellType.Empty:
                    case CellType.Room:
                    case CellType.Foundation:
                    case CellType.OccupiedByOtherCell:
                        flag2 = true;
                        break;
                    case CellType.Corridor:
                        if (GetCorridorDef(cellIndex).piece == Piece.CorridorLShape || (isGlass[cellIndex] && direction == Direction.Below))
                        {
                            flag2 = true;
                        }
                        break;
                }
                if (flag2)
                {
                    Quaternion rotation = ((direction == Direction.Above) ? Quaternion.Euler(180f, 0f, 0f) : Quaternion.identity);
                    SpawnPiece(Piece.ConnectorCap, cell, rotation);
                }
            }
        }

        private void BuildRoomGeometry(Int3 cell)
        {
            Int3 @int = CellSize[1];
            Int3.Bounds bounds = new Int3.Bounds(cell, cell + @int - 1);
            BaseDeconstructable parent = null;
            bool flag = false;
            bool flag2 = false;
            bool flag3 = false;
            bool flag4 = false;
            if (GetCellMask(cell))
            {
                Transform obj = SpawnPiece(Piece.Room, cell);
                parent = BaseDeconstructable.MakeCellDeconstructable(obj, bounds, TechType.BaseRoom);
                obj.tag = "MainPieceGeometry";
                Face face = new Face(cell + new Int3(1, 0, 1), Direction.Above);
                if (GetFaceMask(face) && GetFace(face) == FaceType.WaterPark)
                {
                    flag = true;
                }
                Int3 adjacent = GetAdjacent(cell, Direction.Above);
                Int3 adjacent2 = GetAdjacent(cell, Direction.Below);
                if (!CompareRoomCellTypes(adjacent, CellType.Room) && GetFace(new Face(cell, Direction.Above)) == FaceType.Solid)
                {
                    BaseExplicitFace.MakeFaceDeconstructable(SpawnPiece(Piece.RoomExteriorTop, cell), new Face(cell, Direction.Above), parent);
                }
                else
                {
                    flag4 = true;
                    Face face2 = new Face(adjacent + new Int3(1, 0, 1), Direction.Below);
                    if (GetFaceMask(face2) && GetFace(face2) == FaceType.WaterPark)
                    {
                        flag2 = true;
                    }
                }
                if (CompareRoomCellTypes(adjacent2, CellType.Room))
                {
                    Face face3 = new Face(adjacent2 + new Int3(1, 0, 1), Direction.Above);
                    if (GetFaceMask(face3) && GetFace(face3) == FaceType.WaterPark)
                    {
                        flag3 = true;
                    }
                }
                if (flag)
                {
                    BaseExplicitFace.MakeFaceDeconstructable(SpawnPiece(Piece.RoomInteriorTopHole, cell), new Face(cell, Direction.Above), parent);
                }
                else
                {
                    BaseExplicitFace.MakeFaceDeconstructable(SpawnPiece(Piece.RoomInteriorTop, cell), new Face(cell, Direction.Above), parent);
                }
                if (flag3)
                {
                    BaseExplicitFace.MakeFaceDeconstructable(SpawnPiece(Piece.RoomInteriorBottomHole, cell), new Face(cell, Direction.Below), parent);
                }
                else
                {
                    BaseExplicitFace.MakeFaceDeconstructable(SpawnPiece(Piece.RoomInteriorBottom, cell), new Face(cell, Direction.Below), parent);
                }
                Transform geometry = null;
                if (GetFace(new Face(cell, Direction.Below)) == FaceType.Solid)
                {
                    if (CompareRoomCellTypes(adjacent2, CellType.Foundation, hasAny: true))
                    {
                        geometry = SpawnPiece(Piece.RoomExteriorFoundationBottom, cell);
                    }
                    else if (CompareRoomCellTypes(adjacent2, CellType.Empty))
                    {
                        geometry = SpawnPiece(Piece.RoomExteriorBottom, cell);
                        SpawnPiece(Piece.RoomAdjustableSupport, cell);
                    }
                }
                BaseExplicitFace.MakeFaceDeconstructable(geometry, new Face(cell, Direction.Below), parent);
            }
            for (int i = 0; i < roomFaces.Length; i++)
            {
                RoomFace roomFace = roomFaces[i];
                Face face4 = new Face(cell + roomFace.offset, roomFace.direction);
                if (!GetFaceMask(face4))
                {
                    continue;
                }
                FaceType face5 = GetFace(face4);
                if (face5 == FaceType.Solid && roomFace.direction == Direction.Below)
                {
                    Face adjacentFace = GetAdjacentFace(face4);
                    if (GetFaceMask(adjacentFace) && GetFace(adjacentFace) == FaceType.WaterPark)
                    {
                        continue;
                    }
                }
                Piece piece = GetRoomPiece(face4, face5);
                if (piece == Piece.Invalid)
                {
                    continue;
                }
                if (piece == Piece.RoomCoverSide && i % 2 == 1)
                {
                    piece = Piece.RoomCoverSideVariant;
                }
                Transform transform = SpawnPiece(piece, cell, roomFace.rotation);
                transform.localPosition += roomFace.localOffset;
                if (face5 != FaceType.Solid)
                {
                    TechType recipe = FaceToRecipe[(uint)face5];
                    BaseDeconstructable baseDeconstructable = BaseDeconstructable.MakeFaceDeconstructable(transform, bounds, face4, face5, recipe);
                    if (isGhost)
                    {
                        continue;
                    }
                    transform.GetComponentsInChildren(includeInactive: true, sBaseModulesGeometry);
                    int j = 0;
                    for (int count = sBaseModulesGeometry.Count; j < count; j++)
                    {
                        sBaseModulesGeometry[j].geometryFace = face4;
                    }
                    sBaseModulesGeometry.Clear();
                    switch (face5)
                    {
                        case FaceType.FiltrationMachine:
                            baseDeconstructable.LinkModule(new Face(face4.cell - anchor, face4.direction));
                            break;
                        case FaceType.BioReactor:
                            baseDeconstructable.LinkModule(new Face(face4.cell - anchor, face4.direction));
                            break;
                        case FaceType.NuclearReactor:
                            baseDeconstructable.LinkModule(new Face(face4.cell - anchor, face4.direction));
                            break;
                        case FaceType.WaterPark:
                        {
                            WaterParkPiece component = transform.GetComponent<WaterParkPiece>();
                            if (!(component != null))
                            {
                                break;
                            }
                            if (flag2)
                            {
                                component.HideCeiling();
                            }
                            else if (flag4)
                            {
                                component.ShowGlassCeiling();
                            }
                            else
                            {
                                component.ShowCeiling();
                            }
                            if (!flag3)
                            {
                                component.ShowFloor();
                                _ = transform.position;
                                baseDeconstructable.LinkModule(new Face(face4.cell - anchor, face4.direction));
                                WaterPark.GetWaterParkModule(this, face4.cell, spawnIfNull: true).Rebuild(this, face4.cell);
                            }
                            else
                            {
                                Transform cellObject = GetCellObject(cell - new Int3(0, 1, 0));
                                if (cellObject != null)
                                {
                                    component.lowerPiece = cellObject.GetComponentInChildren<WaterParkPiece>();
                                }
                            }
                            component.ShowBubbles();
                            break;
                        }
                    }
                }
                else if (!isGhost)
                {
                    BaseExplicitFace.MakeFaceDeconstructable(transform, face4, parent);
                }
            }
            if (!flag && !isGhost)
            {
                return;
            }
            for (int k = 0; k < roomWaterParkFaces.Length; k++)
            {
                RoomFace roomFace2 = roomWaterParkFaces[k];
                Face face6 = new Face(cell + roomFace2.offset, roomFace2.direction);
                if (!GetFaceMask(face6))
                {
                    continue;
                }
                FaceType face7 = GetFace(face6);
                Piece piece2;
                if (face7 == FaceType.Hatch)
                {
                    piece2 = Piece.RoomWaterParkHatch;
                }
                else
                {
                    if (isGhost)
                    {
                        continue;
                    }
                    piece2 = Piece.RoomWaterParkSide;
                }
                Transform transform2 = SpawnPiece(piece2, cell, roomFace2.rotation);
                transform2.localPosition += roomFace2.localOffset;
                if (face7 != FaceType.Solid)
                {
                    TechType recipe2 = FaceToRecipe[(uint)face7];
                    BaseDeconstructable.MakeFaceDeconstructable(transform2, bounds, face6, face7, recipe2);
                }
                else if (!isGhost)
                {
                    BaseExplicitFace.MakeFaceDeconstructable(transform2, face6, parent);
                }
            }
        }

        private void BuildMoonpoolGeometry(Int3 cell)
        {
            Int3 @int = CellSize[7];
            Int3.Bounds bounds = new Int3.Bounds(cell, cell + @int - 1);
            BaseDeconstructable parent = null;
            if (GetCellMask(cell))
            {
                Transform obj = SpawnPiece(Piece.Moonpool, cell);
                parent = BaseDeconstructable.MakeCellDeconstructable(obj, bounds, TechType.BaseMoonpool);
                obj.tag = "MainPieceGeometry";
            }
            for (int i = 0; i < moonpoolFaces.Length; i++)
            {
                RoomFace roomFace = moonpoolFaces[i];
                Face face = new Face(cell + roomFace.offset, roomFace.direction);
                if (!GetFaceMask(face))
                {
                    continue;
                }
                FaceType face2 = GetFace(face);
                Piece moonpoolPiece = GetMoonpoolPiece(face, face2);
                if (moonpoolPiece == Piece.Invalid)
                {
                    continue;
                }
                Transform transform = SpawnPiece(moonpoolPiece, cell, roomFace.rotation);
                transform.localPosition = Int3.Scale(roomFace.offset, cellSize) + roomFace.localOffset;
                if (face2 != FaceType.Solid)
                {
                    TechType recipe = FaceToRecipe[(uint)face2];
                    BaseDeconstructable baseDeconstructable = BaseDeconstructable.MakeFaceDeconstructable(transform, bounds, face, face2, recipe);
                    if (!isGhost)
                    {
                        transform.GetComponentsInChildren(includeInactive: true, sBaseModulesGeometry);
                        int j = 0;
                        for (int count = sBaseModulesGeometry.Count; j < count; j++)
                        {
                            sBaseModulesGeometry[j].geometryFace = face;
                        }
                        sBaseModulesGeometry.Clear();
                        if (face2 == FaceType.UpgradeConsole)
                        {
                            baseDeconstructable.LinkModule(new Face(face.cell - anchor, face.direction));
                        }
                    }
                }
                else if (!isGhost)
                {
                    BaseExplicitFace.MakeFaceDeconstructable(transform, face, parent);
                }
            }
        }

        private void BuildGeometryForCell(Int3 cell)
        {
            if (GetCellOrAnyFaceMask(cell))
            {
                int index = baseShape.GetIndex(cell);
                switch (cells[index])
                {
                    case CellType.Foundation:
                        BuildFoundationGeometry(cell);
                        break;
                    case CellType.Corridor:
                        BuildCorridorGeometry(cell, index);
                        break;
                    case CellType.Connector:
                        BuildConnectorGeometry(cell, index);
                        break;
                    case CellType.Room:
                        BuildRoomGeometry(cell);
                        break;
                    case CellType.Moonpool:
                        BuildMoonpoolGeometry(cell);
                        break;
                    case CellType.Observatory:
                        BuildObservatoryGeometry(cell);
                        break;
                    case CellType.MapRoom:
                    case CellType.MapRoomRotated:
                        BuildMapRoomGeometry(cell, index, cells[index]);
                        break;
                    case CellType.OccupiedByOtherCell:
                        break;
                }
            }
        }

        private void BuildGeometry()
        {
            foreach (Int3 allCell in AllCells)
            {
                BuildGeometryForCell(allCell);
            }
        }

        public GameObject SpawnModule(GameObject prefab, Face face)
        {
            CellType cell = GetCell(face.cell);
            GameObject gameObject = null;
            switch (cell)
            {
                case CellType.Room:
                    gameObject = SpawnRoomModule(prefab, face);
                    break;
                case CellType.Moonpool:
                    gameObject = SpawnMoonpoolModule(prefab, face);
                    break;
            }
            if (gameObject != null)
            {
                IBaseModule component = gameObject.GetComponent<IBaseModule>();
                if (component != null)
                {
                    component.moduleFace = new Face(face.cell - anchor, face.direction);
                }
            }
            return gameObject;
        }

        private GameObject SpawnRoomModule(GameObject prefab, Face face)
        {
            if (GetRoomFaceLocation(face, out var position, out var rotation))
            {
                GameObject obj = global::UnityEngine.Object.Instantiate(prefab, position, rotation);
                obj.transform.SetParent(base.transform, worldPositionStays: false);
                return obj;
            }
            return null;
        }

        private GameObject SpawnMoonpoolModule(GameObject prefab, Face face)
        {
            if (GetMoonpoolFaceLocation(face, out var position, out var rotation))
            {
                GameObject obj = global::UnityEngine.Object.Instantiate(prefab, position, rotation);
                obj.transform.SetParent(base.transform, worldPositionStays: false);
                return obj;
            }
            return null;
        }

        private bool GetRoomFaceLocation(Face face, out Vector3 position, out Quaternion rotation)
        {
            int index = baseShape.GetIndex(face.cell);
            Int3 @int = NormalizeCell(face.cell);
            Direction direction = NormalizeFaceDirection(index, face.direction);
            for (int i = 0; i < roomFaces.Length; i++)
            {
                RoomFace roomFace = roomFaces[i];
                if (!(@int + roomFace.offset != face.cell) && roomFace.direction == direction)
                {
                    Vector3 vector = Int3.Scale(CellSize[1] - Int3.one, halfCellSize);
                    position = GridToLocal(@int) + vector + roomFace.localOffset;
                    rotation = roomFace.rotation;
                    return true;
                }
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private bool GetMoonpoolFaceLocation(Face face, out Vector3 position, out Quaternion rotation)
        {
            int index = baseShape.GetIndex(face.cell);
            Int3 @int = NormalizeCell(face.cell);
            Direction direction = NormalizeFaceDirection(index, face.direction);
            for (int i = 0; i < moonpoolFaces.Length; i++)
            {
                RoomFace roomFace = moonpoolFaces[i];
                if (!(@int + roomFace.offset != face.cell) && roomFace.direction == direction)
                {
                    Vector3 vector = Int3.Scale(CellSize[7] - Int3.one, halfCellSize);
                    position = GridToLocal(@int) + vector + roomFace.localOffset;
                    rotation = roomFace.rotation;
                    return true;
                }
            }
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private void Awake()
        {
            bool num = cells != null;
            Initialize();
            AllocateArrays();
            BaseGhost component = GetComponent<BaseGhost>();
            isGhost = component != null;
            if (num)
            {
                waitingForWorld = true;
                nextWorldPollTime = Time.time + 2f;
            }
            else
            {
                BuildGeometry();
            }
            RecalculateFlowData();
        }

        private void OnGlobalEntitiesLoaded()
        {
            Initialize();
            AllocateArrays();
            BindCellObjects();
            RecalculateFlowData();
            RebuildGeometry();
            DestroyIfEmpty();
        }

        private void RecomputeOccupiedBounds()
        {
            occupiedBounds = default(Bounds);
            bool flag = false;
            foreach (Int3 allCell in AllCells)
            {
                if (!IsCellEmpty(allCell))
                {
                    Vector3 vector = GridToWorld(allCell);
                    if (!flag)
                    {
                        occupiedBounds = new Bounds(vector, Vector3.zero);
                        flag = true;
                    }
                    else
                    {
                        occupiedBounds.Encapsulate(vector);
                    }
                }
            }
            if (!isGhost)
            {
                BaseRoot component = GetComponent<BaseRoot>();
                component.LOD.SetUseBoundsForDistanceChecks(occupiedBounds);
                component.LOD.UseBoundingBoxForVisibility = true;
            }
        }

        private void Update()
        {
            if (waitingForWorld && Time.time > nextWorldPollTime)
            {
                if (LargeWorldStreamer.main.IsRangeActiveAndBuilt(occupiedBounds))
                {
                    waitingForWorld = false;
                    RebuildGeometry();
                }
                else
                {
                    nextWorldPollTime = Time.time + 1f;
                }
            }
        }

        private Int3 LocalToGrid(Vector3 localPoint)
        {
            int x = Mathf.RoundToInt(localPoint.x / cellSize.x) - cellOffset.x;
            int y = Mathf.RoundToInt(localPoint.y / cellSize.y) - cellOffset.y;
            int z = Mathf.RoundToInt(localPoint.z / cellSize.z) - cellOffset.z;
            return new Int3(x, y, z);
        }

        public Int3 WorldToGrid(Vector3 point)
        {
            Vector3 localPoint = WorldToLocal(point);
            return LocalToGrid(localPoint);
        }

        public Vector3 WorldToLocal(Vector3 point)
        {
            return base.transform.InverseTransformPoint(point);
        }

        public Ray WorldToLocalRay(Ray ray)
        {
            return new Ray(base.transform.InverseTransformPoint(ray.origin), base.transform.InverseTransformDirection(ray.direction));
        }

        public static Direction NormalToDirection(Vector3 normal)
        {
            Direction result = Direction.Below;
            float num = -1f;
            Direction[] allDirections = AllDirections;
            foreach (Direction direction in allDirections)
            {
                float num2 = Vector3.Dot(DirectionNormals[(int)direction], normal);
                if (num2 > num)
                {
                    num = num2;
                    result = direction;
                }
            }
            return result;
        }

        public Vector3 GridToLocal(Int3 cell)
        {
            return Int3.Scale(cell + cellOffset, cellSize);
        }

        public bool GridToWorld(Int3 cell, Vector3 uvw, out Vector3 result)
        {
            if (!IsCellValid(cell))
            {
                result = Vector3.zero;
                return false;
            }
            Vector3 vector = Vector3.Scale(uvw - new Vector3(0.5f, 0.5f, 0.5f), cellSize);
            result = base.transform.TransformPoint(GridToLocal(cell) + vector);
            return true;
        }

        public Vector3 GridToWorld(Int3 cell)
        {
            return base.transform.TransformPoint(GridToLocal(cell));
        }

        private Vector3 GetFaceNormal(Direction direction)
        {
            return FaceNormals[(int)direction];
        }

        public global::UnityEngine.Plane GetFacePlane(Face face)
        {
            Vector3 vector = GridToLocal(face.cell);
            Vector3 faceNormal = GetFaceNormal(face.direction);
            return new global::UnityEngine.Plane(faceNormal, vector - Vector3.Scale(faceNormal, halfCellSize));
        }

        private Vector3 GetFaceLocalCenter(Face face)
        {
            Vector3 vector = GridToLocal(face.cell);
            Vector3 vector2 = Vector3.Scale(GetFaceNormal(face.direction), halfCellSize);
            return vector - vector2;
        }

        private bool GetNearestFace(Vector3 point, out Face face)
        {
            face = default(Face);
            Vector3 vector = base.transform.InverseTransformPoint(point);
            face.cell = LocalToGrid(vector);
            if (!IsCellValid(face.cell))
            {
                return false;
            }
            Vector3 vector2 = GridToLocal(face.cell);
            Vector3 vector3 = vector2 - halfCellSize;
            Vector3 vector4 = vector2 + halfCellSize;
            Vector3 vector5 = vector - vector3;
            Vector3 vector6 = vector4 - vector;
            Direction direction = Direction.West;
            float num = vector5.x;
            _ = Vector3.right;
            if (vector5.y < num)
            {
                direction = Direction.Below;
                num = vector5.y;
            }
            if (vector5.z < num)
            {
                direction = Direction.South;
                num = vector5.z;
            }
            if (vector6.x < num)
            {
                direction = Direction.East;
                num = vector6.x;
            }
            if (vector6.y < num)
            {
                direction = Direction.Above;
                num = vector6.y;
            }
            if (vector6.z < num)
            {
                direction = Direction.North;
                num = vector6.z;
            }
            face.direction = direction;
            return true;
        }

        private void BuildPillars()
        {
            if (isGhost)
            {
                return;
            }
            Int3.Bounds bounds = Bounds;
            Int3 mins = bounds.mins;
            Int3 maxs = bounds.maxs;
            Int3 cell = default(Int3);
            for (int i = mins.z; i <= maxs.z; i++)
            {
                cell.z = i;
                for (int j = mins.x; j <= maxs.x; j++)
                {
                    cell.x = j;
                    for (int k = mins.y; k <= maxs.y; k++)
                    {
                        cell.y = k;
                        if (GetCell(cell) != 0)
                        {
                            BaseFoundationPiece componentInChildren = GetCellObject(cell).GetComponentInChildren<BaseFoundationPiece>();
                            if (componentInChildren != null)
                            {
                                componentInChildren.OnGenerate();
                            }
                            break;
                        }
                    }
                }
            }
        }

        public void RebuildGeometry()
        {
            ProfilingUtils.BeginSample("Base.RebuildGeometry()");
            if (cellObjects == null)
            {
                return;
            }
            Initialize();
            List<DockedVehicle> list = new List<DockedVehicle>();
            VehicleDockingBay[] componentsInChildren = GetComponentsInChildren<VehicleDockingBay>();
            DockedVehicle item = default(DockedVehicle);
            foreach (VehicleDockingBay vehicleDockingBay in componentsInChildren)
            {
                Vehicle dockedVehicle = vehicleDockingBay.GetDockedVehicle();
                if (dockedVehicle != null)
                {
                    BaseCell componentInParent = vehicleDockingBay.GetComponentInParent<BaseCell>(includeInactive: true);
                    item.vehicle = dockedVehicle;
                    item.cellPosition = WorldToGrid(componentInParent.transform.position);
                    list.Add(item);
                    vehicleDockingBay.SetVehicleUndocked();
                }
            }
            ClearGeometry();
            BuildGeometry();
            BuildPillars();
            for (int j = 0; j < list.Count; j++)
            {
                DockedVehicle dockedVehicle2 = list[j];
                int index = baseShape.GetIndex(dockedVehicle2.cellPosition);
                Transform transform = cellObjects[index];
                VehicleDockingBay vehicleDockingBay2 = null;
                if (transform != null)
                {
                    vehicleDockingBay2 = transform.GetComponentInChildren<VehicleDockingBay>();
                }
                if (vehicleDockingBay2 != null)
                {
                    vehicleDockingBay2.DockVehicle(dockedVehicle2.vehicle);
                }
            }
            Invoke("BuildPillars", 2f);
            RecomputeOccupiedCells();
            RecomputeOccupiedBounds();
            if (this.onPostRebuildGeometry != null)
            {
                this.onPostRebuildGeometry(this);
            }
            ProfilingUtils.EndSample();
        }

        public void ClearGeometry()
        {
            if (cellObjects == null)
            {
                return;
            }
            for (int i = 0; i < cellObjects.Length; i++)
            {
                Transform transform = cellObjects[i];
                if ((bool)transform)
                {
                    for (int num = transform.childCount - 1; num >= 0; num--)
                    {
                        Transform child = transform.GetChild(num);
                        child.parent = null;
                        global::UnityEngine.Object.Destroy(child.gameObject);
                    }
                }
            }
        }

        private bool RaycastCellPlanes(Ray ray, Int3 cell, out Direction direction, out float distance)
        {
            distance = float.MaxValue;
            direction = Direction.Above;
            bool result = false;
            Vector3 vector = GridToLocal(cell);
            Direction[] allDirections = AllDirections;
            foreach (Direction direction2 in allDirections)
            {
                Vector3 vector2 = FaceNormals[(int)direction2];
                if (!(Vector3.Dot(vector2, ray.direction) >= 0f) && new global::UnityEngine.Plane(vector2, vector - Vector3.Scale(vector2, halfCellSize)).Raycast(ray, out var enter) && enter < distance)
                {
                    distance = enter;
                    direction = direction2;
                    result = true;
                }
            }
            return result;
        }

        private void DrawLocalStar(Vector3 point, Color color)
        {
            point = base.transform.TransformPoint(point);
            Debug.DrawLine(point - new Vector3(0.2f, 0f, 0f), point + new Vector3(0.2f, 0f, 0f), color);
            Debug.DrawLine(point - new Vector3(0f, 0.2f, 0f), point + new Vector3(0f, 0.2f, 0f), color);
            Debug.DrawLine(point - new Vector3(0f, 0f, 0.2f), point + new Vector3(0f, 0f, 0.2f), color);
        }

        private void DrawLocalStar(Vector3 point)
        {
            DrawLocalStar(point, Color.blue);
        }

        private void DrawFace(Face face)
        {
            Vector3 faceNormal = GetFaceNormal(face.direction);
            Vector3 vector = ((faceNormal.y == 0f) ? Vector3.up : Vector3.right);
            Vector3 a = Vector3.Cross(vector, faceNormal);
            vector = base.transform.TransformDirection(Vector3.Scale(vector, halfCellSize));
            a = base.transform.TransformDirection(Vector3.Scale(a, halfCellSize));
            Vector3 vector2 = base.transform.TransformPoint(GridToLocal(face.cell) - Vector3.Scale(faceNormal, halfCellSize));
            Debug.DrawLine(vector2 - vector - a, vector2 - vector + a);
            Debug.DrawLine(vector2 - vector + a, vector2 + vector + a);
            Debug.DrawLine(vector2 + vector + a, vector2 + vector - a);
            Debug.DrawLine(vector2 + vector - a, vector2 - vector - a);
        }

        private bool RaycastFace(Ray ray, out Face face)
        {
            Vector3 a = Int3.Scale(baseShape.ToInt3(), halfCellSize);
            Vector3 vector = (GridToLocal(new Int3(0)) + GridToLocal(baseShape.ToInt3() - 1)) * 0.5f;
            Direction[] allDirections = AllDirections;
            foreach (Direction direction in allDirections)
            {
                Vector3 vector2 = DirectionNormals[(int)direction];
                global::UnityEngine.Plane plane = new global::UnityEngine.Plane(vector2, vector + Vector3.Scale(a, vector2));
                if (Vector3.Dot(plane.normal, ray.direction) < 0f && plane.Raycast(ray, out var enter))
                {
                    ray.origin += ray.direction * (enter - 0.1f);
                }
            }
            DrawLocalStar(ray.origin);
            Vector3 vector3 = base.transform.TransformPoint(ray.origin);
            float num = 0f;
            Int3 cell = LocalToGrid(ray.origin);
            bool flag = true;
            while (true)
            {
                if (!RaycastCellPlanes(ray, cell, out var direction2, out var distance))
                {
                    face = default(Face);
                    return false;
                }
                Int3 adjacent = GetAdjacent(cell, direction2);
                num += distance;
                if (!IsCellValid(cell) && !IsCellValid(adjacent) && !flag)
                {
                    break;
                }
                if (IsCellEmpty(cell) != IsCellEmpty(adjacent) || GetFace(new Face(cell, direction2)) != 0)
                {
                    face = new Face(cell, direction2);
                    Vector3 end = base.transform.TransformPoint(ray.origin + ray.direction * num);
                    Debug.DrawLine(vector3, end, Color.red);
                    DrawFace(face);
                    return true;
                }
                cell = adjacent;
                flag = false;
            }
            Debug.DrawLine(vector3, vector3 + base.transform.TransformDirection(ray.direction) * 20f, Color.black);
            face = default(Face);
            return false;
        }

        public bool PickFace(Transform camera, out Face face)
        {
            Ray ray = WorldToLocalRay(new Ray(camera.position, camera.forward));
            return RaycastFace(ray, out face);
        }

        public Vector3 GetClosestPoint(Vector3 position)
        {
            GetClosestCell(position, out var _, out var worldPosition, out var _);
            float num = float.MaxValue;
            Vector3 result = Vector3.zero;
            int num2 = global::UWE.Utils.RaycastIntoSharedBuffer(position, Vector3.Normalize(worldPosition - position), (worldPosition - position).magnitude, -5, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < num2; i++)
            {
                RaycastHit raycastHit = global::UWE.Utils.sharedHitBuffer[i];
                if (global::UWE.Utils.IsAncestorOf(base.gameObject, raycastHit.collider.gameObject) && raycastHit.distance < num)
                {
                    num = raycastHit.distance;
                    result = raycastHit.point;
                }
            }
            return result;
        }

        public void GetClosestCell(Vector3 position, out Int3 cell, out Vector3 worldPosition, out float distance)
        {
            ProfilingUtils.BeginSample("Base.GetClosestCellFast");
            cell = Int3.zero;
            worldPosition = Vector3.zero;
            distance = float.PositiveInfinity;
            foreach (int occupiedCellIndex in occupiedCellIndexes)
            {
                if (cells[occupiedCellIndex] != CellType.OccupiedByOtherCell)
                {
                    Int3 cellPointFromIndex = GetCellPointFromIndex(occupiedCellIndex);
                    Vector3 vector = GridToWorld(cellPointFromIndex);
                    Int3 @int = CellSize[(uint)GetCell(cellPointFromIndex)];
                    Vector3 vector2 = (GridToWorld(cellPointFromIndex + @int - 1) - vector) * 0.5f + vector;
                    float sqrMagnitude = (vector2 - position).sqrMagnitude;
                    if (sqrMagnitude < distance)
                    {
                        distance = sqrMagnitude;
                        worldPosition = vector2;
                        cell = cellPointFromIndex;
                    }
                }
            }
            distance = Mathf.Sqrt(distance);
            ProfilingUtils.EndSample();
        }

        public Int3 PickCell(Transform camera, Vector3 point, Int3 size)
        {
            Ray ray = WorldToLocalRay(new Ray(camera.position, camera.forward));
            Int3 @int = size - 1;
            Vector3 vector = Int3.Scale(@int, halfCellSize);
            Int3 int2 = new Int3(@int.x / 2, @int.y / 2, @int.z / 2);
            Int3 int3 = @int - int2 * 2;
            Int3 int4;
            if (!RaycastFace(ray, out var face))
            {
                Vector3 vector2 = base.transform.InverseTransformPoint(point);
                int4 = LocalToGrid(vector2 - vector);
            }
            else
            {
                int4 = face.cell - int2;
                global::UnityEngine.Plane facePlane = GetFacePlane(face);
                if (facePlane.Raycast(ray, out var enter))
                {
                    Vector3 vector3 = ray.origin + ray.direction * enter;
                    DrawLocalStar(vector3, Color.red);
                    Vector3 vector4 = vector3 - GetFaceLocalCenter(face);
                    DrawLocalStar(GetFaceLocalCenter(face), Color.green);
                    if (int3.x > 0 && facePlane.normal.x == 0f && vector4.x < 0f)
                    {
                        int4.x--;
                    }
                    if (int3.y > 0 && facePlane.normal.y == 0f && vector4.y < 0f)
                    {
                        int4.y--;
                    }
                    if (int3.z > 0 && facePlane.normal.z == 0f && vector4.z < 0f)
                    {
                        int4.z--;
                    }
                    DrawLocalStar(GridToLocal(int4), Color.yellow);
                }
            }
            return int4;
        }

        public void ClearFace(Face face, FaceType faceType)
        {
            if (faceType == FaceType.WaterPark)
            {
                Direction[] horizontalDirections = HorizontalDirections;
                foreach (Direction direction in horizontalDirections)
                {
                    Face face2 = new Face(face.cell, direction);
                    SetFace(face2, FaceType.None);
                }
                horizontalDirections = VerticalDirections;
                foreach (Direction direction2 in horizontalDirections)
                {
                    Face face3 = new Face(face.cell, direction2);
                    SetFace(face3, FaceType.Solid);
                }
                return;
            }
            FaceType faceType2 = deconstructFaceTypes[(uint)faceType];
            SetFace(face, faceType2);
            if (faceType != FaceType.Ladder)
            {
                return;
            }
            if (!GetLadderExitCell(face, out var exit))
            {
                Debug.LogError("Could not find exit of ladder");
                return;
            }
            int index = baseShape.GetIndex(exit);
            Direction direction3 = ReverseDirection(face.direction);
            SetFace(index, direction3, faceType2);
            int num = Mathf.Min(face.cell.y, exit.y);
            int num2 = Mathf.Max(face.cell.y, exit.y);
            for (int j = num + 1; j < num2; j++)
            {
                Int3 cell = new Int3(exit.x, j, exit.z);
                SetFace(new Face(cell, BaseAddLadderGhost.ladderFaceDir), faceType2);
            }
        }

        public void ClearCell(Int3 cell)
        {
            int cellIndex = GetCellIndex(cell);
            CellType cellType = cells[cellIndex];
            if (cellType == CellType.Empty)
            {
                return;
            }
            Int3 @int = CellSize[(uint)cellType];
            Int3 maxs = cell + @int - 1;
            foreach (Int3 item in Int3.Range(cell, maxs))
            {
                int index = baseShape.GetIndex(item);
                cells[index] = CellType.Empty;
                links[index] = 0;
                isGlass[index] = false;
                Direction[] allDirections = AllDirections;
                foreach (Direction direction in allDirections)
                {
                    faces[GetFaceIndex(index, direction)] = FaceType.None;
                }
            }
            Transform transform = cellObjects[cellIndex];
            if (transform != null)
            {
                transform.SetParent(null, worldPositionStays: false);
                cellObjects[cellIndex] = null;
                global::UnityEngine.Object.Destroy(transform.gameObject);
            }
        }

        public bool IsValidObsConnection(Int3 cell, Direction direction)
        {
            CellType cell2 = GetCell(cell);
            if (cell2 == CellType.Room || cell2 == CellType.Corridor || cell2 == CellType.Moonpool)
            {
                return (GetCellConnections(cell) & (1 << (int)direction)) != 0;
            }
            return false;
        }

        public Transform SpawnCorridorConnector(Int3 cell, Direction direction, Transform parent = null, Int3 cellOffset = default(Int3))
        {
            new Face(cell, direction);
            FaceType faceType = FaceType.None;
            Piece piece = roomFacePieces[(int)direction, (uint)faceType];
            if (parent == null && !(parent = GetCellObject(cell)))
            {
                parent = CreateCellObject(cell);
            }
            PieceDef pieceDef = pieces[(int)piece];
            Vector3 position = Int3.Scale(pieceDef.extraCells, halfCellSize) + Int3.Scale(cellOffset, cellSize);
            Transform obj = global::UnityEngine.Object.Instantiate(pieceDef.prefab, position, pieceDef.rotation * corridorConnectorRotation[(int)direction]);
            obj.SetParent(parent, worldPositionStays: false);
            obj.gameObject.SetActive(value: true);
            return obj;
        }

        public int GetCellConnections(Int3 cell)
        {
            Int3 @int = NormalizeCell(cell);
            int index = baseShape.GetIndex(@int);
            if (index == -1)
            {
                return 0;
            }
            CellType cellType = cells[index];
            int result = 0;
            Int3 int2 = cell - @int;
            switch (cellType)
            {
                case CellType.Corridor:
                    result = links[index];
                    break;
                case CellType.Room:
                    if (int2.x == 0 && int2.z == 1)
                    {
                        result = 8;
                    }
                    else if (int2.x == 1 && int2.z == 2)
                    {
                        result = 1;
                    }
                    else if (int2.x == 2 && int2.z == 1)
                    {
                        result = 4;
                    }
                    else if (int2.x == 1 && int2.z == 0)
                    {
                        result = 2;
                    }
                    break;
                case CellType.MapRoom:
                    if (int2.x == 0 && int2.z == 1)
                    {
                        result = 8;
                    }
                    else if (int2.x == 2 && int2.z == 1)
                    {
                        result = 4;
                    }
                    break;
                case CellType.MapRoomRotated:
                    if (int2.x == 1 && int2.z == 2)
                    {
                        result = 1;
                    }
                    else if (int2.x == 1 && int2.z == 0)
                    {
                        result = 2;
                    }
                    break;
                case CellType.Observatory:
                    result = 4;
                    if (IsValidObsConnection(GetAdjacent(cell, Direction.South), Direction.North))
                    {
                        result = 2;
                    }
                    else if (IsValidObsConnection(GetAdjacent(cell, Direction.West), Direction.East))
                    {
                        result = 8;
                    }
                    else if (IsValidObsConnection(GetAdjacent(cell, Direction.North), Direction.South))
                    {
                        result = 1;
                    }
                    break;
                case CellType.Moonpool:
                    if ((int2.x == 1 && int2.z == 2) || (int2.x == 2 && int2.z == 2))
                    {
                        result = 1;
                    }
                    else if ((int2.x == 1 && int2.z == 0) || (int2.x == 2 && int2.z == 0))
                    {
                        result = 2;
                    }
                    else if (int2.x == 3 && int2.z == 1)
                    {
                        result = 4;
                    }
                    else if (int2.x == 0 && int2.z == 1)
                    {
                        result = 8;
                    }
                    break;
            }
            return result;
        }

        public void FixRoomFloors()
        {
            foreach (Int3 allCell in AllCells)
            {
                CellType rawCellType = GetRawCellType(allCell);
                if (rawCellType != CellType.Room)
                {
                    continue;
                }
                int cellIndex = GetCellIndex(allCell);
                Direction[] verticalDirections = VerticalDirections;
                foreach (Direction direction in verticalDirections)
                {
                    if (GetFace(cellIndex, direction) == FaceType.Hole)
                    {
                        bool flag = false;
                        Int3 adjacent = GetAdjacent(allCell, direction);
                        GetCell(adjacent);
                        if (rawCellType != CellType.Room)
                        {
                            flag = true;
                        }
                        else if (!isGhost && IsCellUnderConstruction(adjacent))
                        {
                            flag = true;
                        }
                        if (flag)
                        {
                            SetFace(cellIndex, direction, FaceType.Solid);
                        }
                    }
                }
            }
        }

        public void FixCorridorLinks()
        {
            foreach (Int3 allCell in AllCells)
            {
                int cellConnections = GetCellConnections(allCell);
                if (cellConnections == 0)
                {
                    continue;
                }
                Direction[] allDirections = AllDirections;
                foreach (Direction direction in allDirections)
                {
                    Face face = new Face(allCell, direction);
                    int num = 1 << (int)direction;
                    if ((cellConnections & num) != 0 && GetFace(face) == FaceType.None)
                    {
                        SetFace(face, FaceType.Solid);
                    }
                }
                allDirections = HorizontalDirections;
                foreach (Direction direction2 in allDirections)
                {
                    int num2 = 1 << (int)direction2;
                    if ((cellConnections & num2) == 0)
                    {
                        continue;
                    }
                    Face face2 = new Face(allCell, direction2);
                    int cellConnections2 = GetCellConnections(GetAdjacentFace(face2).cell);
                    Direction direction3 = ReverseDirection(direction2);
                    if ((cellConnections2 & (1 << (int)direction3)) != 0)
                    {
                        FaceType face3 = GetFace(face2);
                        if (!IsBulkhead(face3))
                        {
                            RemoveFaceLinkedModule(face2, face3);
                            SetFace(face2, FaceType.None);
                        }
                    }
                }
                int cellIndex = GetCellIndex(allCell);
                if (GetCell(allCell) != CellType.Corridor)
                {
                    continue;
                }
                allDirections = VerticalDirections;
                foreach (Direction direction4 in allDirections)
                {
                    switch (GetFace(cellIndex, direction4))
                    {
                        case FaceType.Ladder:
                        {
                            Int3 exit = default(Int3);
                            if (!GetLadderExitCell(allCell, direction4, out exit))
                            {
                                SetFace(cellIndex, direction4, FaceType.Solid);
                                break;
                            }
                            int cellIndex2 = GetCellIndex(exit);
                            Direction direction6 = ReverseDirection(direction4);
                            if (GetFace(cellIndex2, direction6) != FaceType.Ladder)
                            {
                                SetFace(cellIndex2, direction6, FaceType.Ladder);
                            }
                            break;
                        }
                        case FaceType.Solid:
                        {
                            Int3 adjacent = GetAdjacent(allCell, direction4);
                            Direction direction5 = ReverseDirection(direction4);
                            if (CanConnectToCell(allCell, direction4) && CanConnectToCell(adjacent, direction5))
                            {
                                SetFace(cellIndex, direction4, FaceType.Hole);
                            }
                            break;
                        }
                        case FaceType.Hole:
                        {
                            CellType cell = GetCell(GetAdjacent(allCell, direction4));
                            if (cell != CellType.Connector && cell != CellType.Corridor)
                            {
                                SetFace(cellIndex, direction4, FaceType.Solid);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void RemoveFaceLinkedModule(Face face, FaceType faceType)
        {
            if (faceType == FaceType.FiltrationMachine)
            {
                FiltrationMachine filtrationMachine = GetModule(face) as FiltrationMachine;
                if (filtrationMachine != null)
                {
                    global::UnityEngine.Object.Destroy(filtrationMachine.gameObject);
                    return;
                }
                Debug.LogErrorFormat(this, "Unable to find and remove FiltrationMachine module in face {0}", face);
            }
        }

        public bool SetConnector(Int3 cell)
        {
            ClearCell(cell);
            int cellIndex = GetCellIndex(cell);
            cells[cellIndex] = CellType.Connector;
            links[cellIndex] = 0;
            return true;
        }

        public bool SetCorridor(Int3 cell, int corridorType, bool glass = false)
        {
            ClearCell(cell);
            int cellIndex = GetCellIndex(cell);
            cells[cellIndex] = CellType.Corridor;
            links[cellIndex] = (byte)corridorType;
            isGlass[cellIndex] = glass;
            Direction[] allDirections = AllDirections;
            foreach (Direction direction in allDirections)
            {
                SetFace(cellIndex, direction, FaceType.Solid);
            }
            UpdateFlowDataForCellAndNeighbors(cell);
            return true;
        }

        public bool SetCell(Int3 cell, CellType cellType)
        {
            foreach (Int3 item in Int3.Range(CellSize[(uint)cellType]))
            {
                ClearCell(cell + item);
                int index = baseShape.GetIndex(cell + item);
                if (item == Int3.zero)
                {
                    cells[index] = cellType;
                    continue;
                }
                cells[index] = CellType.OccupiedByOtherCell;
                links[index] = PackOffset(item);
            }
            FaceDef[] array = faceDefs[(uint)cellType];
            if (array != null)
            {
                FaceDef[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    FaceDef faceDef = array2[i];
                    int faceIndex = GetFaceIndex(baseShape.GetIndex(faceDef.face.cell), faceDef.face.direction);
                    faces[faceIndex] = faceDef.faceType;
                }
            }
            return true;
        }

        public float GetHullStrength(Int3 cell)
        {
            int index = baseShape.GetIndex(cell);
            if (index == -1)
            {
                return 0f;
            }
            float y = GridToWorld(cell).y;
            float num = ApplyDepthScaling(CellHullStrength[(uint)cells[index]], y);
            if (isGlass[index])
            {
                num -= Mathf.Abs(num);
            }
            Direction[] allDirections = AllDirections;
            foreach (Direction direction in allDirections)
            {
                int faceIndex = GetFaceIndex(index, direction);
                FaceType faceType = faces[faceIndex];
                if ((faceType & FaceType.OccupiedByOtherFace) == 0)
                {
                    num += ApplyDepthScaling(FaceHullStrength[(uint)faceType], y);
                }
            }
            return num;
        }

        public bool AreCellsConnected(Int3 u, Int3 v)
        {
            int index = baseShape.GetIndex(u);
            if (index == -1)
            {
                return false;
            }
            Int3 @int = v - u;
            Direction[] allDirections = AllDirections;
            foreach (Direction direction in allDirections)
            {
                if (DirectionOffset[(int)direction] == @int)
                {
                    FaceType face = GetFace(index, direction);
                    if (face != 0 && face != FaceType.ObsoleteDoor)
                    {
                        return face == FaceType.Ladder;
                    }
                    return true;
                }
            }
            return false;
        }

        public void AllocateMasks()
        {
            if (masks == null)
            {
                masks = new byte[baseShape.Size];
            }
        }

        public void ClearMasks()
        {
            if (masks != null)
            {
                for (int i = 0; i < baseShape.Size; i++)
                {
                    masks[i] = 0;
                }
            }
        }

        public void SetCellMask(Int3 cell, bool isMasked)
        {
            if (masks == null)
            {
                return;
            }
            int index = baseShape.GetIndex(cell);
            if (index != -1)
            {
                if (isMasked)
                {
                    masks[index] |= 64;
                }
                else
                {
                    masks[index] &= 191;
                }
            }
        }

        public void SetFaceMask(Face face, bool isMasked)
        {
            if (masks == null)
            {
                return;
            }
            int index = baseShape.GetIndex(face.cell);
            if (index != -1)
            {
                Direction direction = NormalizeFaceDirection(index, face.direction);
                int num = 1 << (int)direction;
                if (isMasked)
                {
                    masks[index] |= (byte)num;
                }
                else
                {
                    masks[index] &= (byte)(~num);
                }
            }
        }

        public bool GetCellMask(Int3 cell)
        {
            if (masks == null)
            {
                return true;
            }
            int index = baseShape.GetIndex(cell);
            if (index == -1)
            {
                return false;
            }
            return (masks[index] & 0x40) != 0;
        }

        public bool GetFaceMask(Face face)
        {
            if (masks == null)
            {
                return true;
            }
            int index = baseShape.GetIndex(face.cell);
            if (index == -1)
            {
                return false;
            }
            Direction direction = NormalizeFaceDirection(index, face.direction);
            int num = 1 << (int)direction;
            return (masks[index] & num) != 0;
        }

        private bool GetCellOrAnyFaceMask(Int3 cell)
        {
            if (masks == null)
            {
                return true;
            }
            Int3 @int = NormalizeCell(cell);
            CellType cell2 = GetCell(@int);
            foreach (Int3 item in Int3.Range(CellSize[(uint)cell2]))
            {
                int index = baseShape.GetIndex(@int + item);
                if (index != -1 && masks[index] != 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsCellUnderConstruction(Int3 cell)
        {
            GetComponentsInChildren(sGhosts);
            bool result = IsCellUnderConstruction(cell, sGhosts);
            sGhosts.Clear();
            return result;
        }

        private bool IsCellUnderConstruction(Int3 cell, List<BaseGhost> ghosts)
        {
            for (int i = 0; i < ghosts.Count; i++)
            {
                BaseGhost baseGhost = ghosts[i];
                if (!(baseGhost.GhostBase == null) && baseGhost.GhostBase.IsCellValid(cell - baseGhost.TargetOffset))
                {
                    return true;
                }
            }
            return false;
        }

        public void OnPreDestroy()
        {
            VehicleDockingBay[] componentsInChildren = GetComponentsInChildren<VehicleDockingBay>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].SetVehicleUndocked();
            }
        }

        public void DestroyIfEmpty(BaseGhost ignoreGhost = null)
        {
            if (IsEmpty(ignoreGhost))
            {
                OnPreDestroy();
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
        }

        public bool IsEmpty(BaseGhost ignoreGhost = null)
        {
            foreach (Int3 allCell in AllCells)
            {
                if (!IsCellEmpty(allCell))
                {
                    return false;
                }
            }
            BaseGhost[] componentsInChildren = GetComponentsInChildren<BaseGhost>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                if (componentsInChildren[i] != ignoreGhost)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
