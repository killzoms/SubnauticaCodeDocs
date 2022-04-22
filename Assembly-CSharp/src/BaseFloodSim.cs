using System;
using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Base))]
    [ProtoContract]
    public class BaseFloodSim : MonoBehaviour, IProtoEventListener
    {
        private enum CompartmentStatus
        {
            NoLeaks,
            LeakyNotFull,
            LeakyFull,
            WasLeaky,
            Unused
        }

        private struct CellWaterPlane
        {
            public Int3 cell;

            public BaseWaterPlaneManager waterPlaneManager;
        }

        private const float globalWaterLevel = 0f;

        private const float flowRate = 10f;

        private const float bulkheadFlowRate = 1f;

        private const float maxCompression = 0.1f;

        private const float drainRate = 0.0166666675f;

        public float leakSpeedPerHole = 0.1f;

        public VoiceNotification hullRestoredNotification;

        private ushort[] cellFloodGroup;

        private float[] floodGroupWaterLevel;

        private List<float> floodGroupArea;

        private const ushort noFloodGroup = ushort.MaxValue;

        private ushort[] cellCompartment;

        private List<CompartmentStatus> compartmentStatus;

        private ushort noCompartment = ushort.MaxValue;

        private BehaviourLOD LOD;

        private float[] cellWaterLevel;

        private float[] delta;

        private int minLeakersToUpdatePerFrame = 5;

        private int numLeakersToUpdatePerFrame = 5;

        private int leakerIndex;

        private float timeBetweenLowLODUpdates = 1f;

        private float nextUpdateTime;

        private float lastUpdateTime;

        private bool wasLODFull;

        private int curWaterPlaneY = int.MinValue;

        private int waterPlaneMinY = int.MaxValue;

        private int waterPlaneMaxY = int.MinValue;

        private List<BaseWaterTransition> waterTransitions;

        private List<CellWaterPlane> waterPlanes;

        [AssertNotNull]
        [SerializeField]
        private Base baseComp;

        private Grid3Shape shape;

        [NonSerialized]
        [ProtoMember(1, OverwriteList = true)]
        public float[] flatValueGrid;

        private Leakable[] leakers;

        private bool pendingValuesRestore;

        private bool leakObjectRefreshPending;

        private int GetOldIndex(int x, int y, int z)
        {
            _ = shape;
            int y2 = shape.y;
            int z2 = shape.z;
            return x * (y2 * z2) + y * z2 + z;
        }

        public void OnProtoSerialize(ProtobufSerializer serializer)
        {
            flatValueGrid = new float[shape.Size];
            foreach (Int3 item in Int3.Range(shape.ToInt3()))
            {
                int index = shape.GetIndex(item.x, item.y, item.z);
                int oldIndex = GetOldIndex(item.x, item.y, item.z);
                flatValueGrid[oldIndex] = cellWaterLevel[index];
            }
        }

        public void OnProtoDeserialize(ProtobufSerializer serializer)
        {
            pendingValuesRestore = true;
        }

        private void Awake()
        {
            waterTransitions = new List<BaseWaterTransition>();
            waterPlanes = new List<CellWaterPlane>();
            baseComp.onPostRebuildGeometry += OnPostRebuildGeometry;
            baseComp.onBaseResize += OnBaseResize;
            baseComp.onBulkheadFaceChanged += OnBulkheadFaceChanged;
            BaseRoot component = baseComp.GetComponent<BaseRoot>();
            LOD = component.LOD;
        }

        private void OnPostRebuildGeometry(Base b)
        {
            leakObjectRefreshPending = true;
            ResizeToBase(Int3.zero);
            BuildFloodGroups();
            BuildCompartments();
            SetupWaterRendering();
        }

        private void OnBaseResize(Base b, Int3 offset)
        {
            ResizeToBase(offset);
        }

        private void OnBulkheadFaceChanged(Base b, Base.Face face)
        {
            int num = cellCompartment[shape.GetIndex(face.cell)];
            Int3 adjacent = Base.GetAdjacent(face);
            switch (baseComp.GetFace(face))
            {
                case Base.FaceType.BulkheadOpened:
                {
                    int num3 = cellCompartment[shape.GetIndex(adjacent)];
                    if (num != num3)
                    {
                        AssignCompartments(adjacent, num, num3);
                        if (compartmentStatus[num3] == CompartmentStatus.LeakyFull || compartmentStatus[num3] == CompartmentStatus.LeakyNotFull)
                        {
                            compartmentStatus[num] = compartmentStatus[num3];
                        }
                        compartmentStatus[num3] = CompartmentStatus.Unused;
                    }
                    break;
                }
                case Base.FaceType.BulkheadClosed:
                {
                    int num2 = CreateOrReuseCompartment();
                    compartmentStatus[num2] = compartmentStatus[num];
                    AssignCompartments(adjacent, num2, num);
                    break;
                }
            }
        }

        private int CreateOrReuseCompartment()
        {
            for (int i = 0; i < compartmentStatus.Count; i++)
            {
                if (compartmentStatus[i] == CompartmentStatus.Unused)
                {
                    return i;
                }
            }
            compartmentStatus.Add(CompartmentStatus.Unused);
            return compartmentStatus.Count - 1;
        }

        private void ResizeToBase(Int3 offset)
        {
            Grid3Shape grid3Shape = baseComp.Shape;
            if (cellWaterLevel != null && offset == Int3.zero && shape == grid3Shape)
            {
                return;
            }
            float[] array = cellWaterLevel;
            cellWaterLevel = new float[grid3Shape.Size];
            delta = new float[grid3Shape.Size];
            cellFloodGroup = new ushort[grid3Shape.Size];
            cellCompartment = new ushort[grid3Shape.Size];
            if (array != null)
            {
                Int3 @int = Int3.Min(grid3Shape.ToInt3(), shape.ToInt3() + offset);
                foreach (Int3 item in new Int3.RangeEnumerator(offset, @int - 1))
                {
                    Int3 point = item - offset;
                    float num = array[shape.GetIndex(point)];
                    cellWaterLevel[grid3Shape.GetIndex(item)] = num;
                }
            }
            shape = grid3Shape;
        }

        private void RestoreSerializedWaterLevels()
        {
            if (!pendingValuesRestore || flatValueGrid == null)
            {
                return;
            }
            foreach (Int3 item in Int3.Range(shape.ToInt3()))
            {
                int index = shape.GetIndex(item.x, item.y, item.z);
                int oldIndex = GetOldIndex(item.x, item.y, item.z);
                cellWaterLevel[index] = flatValueGrid[oldIndex];
            }
            pendingValuesRestore = false;
            flatValueGrid = null;
        }

        private void Start()
        {
            DevConsole.RegisterConsoleCommand(this, "baseflood");
            DevConsole.RegisterConsoleCommand(this, "dbbf");
        }

        private void OnConsoleCommand_baseflood(NotificationCenter.Notification n)
        {
            float num = 0.2f;
            if (n.data.Count > 0)
            {
                num = float.Parse((string)n.data[0]);
            }
            int i = 0;
            for (int size = shape.Size; i < size; i++)
            {
                if ((baseComp.flowData[i] & 0x40) > 0)
                {
                    cellWaterLevel[i] = num;
                }
            }
        }

        private void OnConsoleCommand_dbbf()
        {
            float num = 0f;
            int num2 = 0;
            int i = 0;
            for (int size = shape.Size; i < size; i++)
            {
                float num3 = cellWaterLevel[i];
                num += num3;
                if (num3 > 0f)
                {
                    Int3 pointAsInt = shape.GetPointAsInt3(i);
                    baseComp.GridToWorld(pointAsInt, new Vector3(0.5f, 0f, 0.5f), out var result);
                    baseComp.GridToWorld(pointAsInt, new Vector3(0.5f, 1f, 0.5f), out var result2);
                    Debug.DrawLine(result, Vector3.Lerp(result, result2, num3), Color.red, 5f);
                }
                if ((baseComp.flowData[i] & 0x40) > 0)
                {
                    num2++;
                }
            }
            Debug.Log("total water amt: " + num + " in " + num2 + " cells");
        }

        private void UpdateWaterPlanesByHeight()
        {
            ProfilingUtils.BeginSample("UpdateWaterRendering-PlanesByHeight");
            if (curWaterPlaneY < waterPlaneMinY || curWaterPlaneY > waterPlaneMaxY)
            {
                curWaterPlaneY = waterPlaneMinY;
            }
            int count = waterPlanes.Count;
            for (int i = 0; i < count; i++)
            {
                CellWaterPlane cellWaterPlane = waterPlanes[i];
                if (cellWaterPlane.cell.y == curWaterPlaneY)
                {
                    float visualWaterLevel = GetVisualWaterLevel(cellWaterPlane.cell);
                    cellWaterPlane.waterPlaneManager.leakAmount = visualWaterLevel;
                }
            }
            curWaterPlaneY++;
            ProfilingUtils.EndSample();
        }

        private void UpdateWaterRenderingTransitions()
        {
            ProfilingUtils.BeginSample("UpdateWaterRendering-Transitions");
            for (int i = 0; i < waterTransitions.Count; i++)
            {
                BaseWaterTransition baseWaterTransition = waterTransitions[i];
                Int3 cell = baseWaterTransition.face.cell;
                Int3 adjacent = Base.GetAdjacent(baseWaterTransition.face);
                float visualWaterLevel = GetVisualWaterLevel(cell);
                float visualWaterLevel2 = GetVisualWaterLevel(adjacent);
                bool flowing = (baseComp.flowData[shape.GetIndex(cell)] & (1 << (int)baseWaterTransition.face.direction)) != 0;
                baseWaterTransition.SetWaterLevels(visualWaterLevel, visualWaterLevel2, flowing);
            }
            ProfilingUtils.EndSample();
        }

        private float GetVisualWaterLevel(Int3 cell)
        {
            if (cellFloodGroup != null)
            {
                ushort num = cellFloodGroup[shape.GetIndex(cell)];
                if (num != ushort.MaxValue)
                {
                    return floodGroupWaterLevel[num];
                }
            }
            return 0f;
        }

        private bool IsInitialized()
        {
            return compartmentStatus != null;
        }

        private void TryRefreshLeakers()
        {
            if (!leakObjectRefreshPending)
            {
                return;
            }
            leakers = base.gameObject.GetComponentsInChildren<Leakable>(includeInactive: true);
            Leakable[] array = leakers;
            foreach (Leakable leakable in array)
            {
                if (leakable != null)
                {
                    leakable.RefreshLeakPoints();
                }
            }
            numLeakersToUpdatePerFrame = Mathf.Max(minLeakersToUpdatePerFrame, leakers.Length / 10);
            leakObjectRefreshPending = false;
        }

        private void UpdateLeakers()
        {
            ProfilingUtils.BeginSample("UpdateLeakers");
            int num = leakers.Length;
            if (num > 0)
            {
                int num2 = Mathf.Min(num, numLeakersToUpdatePerFrame);
                for (int i = leakerIndex; i < leakerIndex + num2; i++)
                {
                    int num3 = i % num;
                    Leakable leakable = leakers[num3];
                    if ((bool)leakable)
                    {
                        leakable.UpdateLeakPoints();
                    }
                }
                leakerIndex = (leakerIndex + num2) % num;
            }
            ProfilingUtils.EndSample();
        }

        private void Update()
        {
            try
            {
                RestoreSerializedWaterLevels();
                if (!IsInitialized())
                {
                    return;
                }
                TryRefreshLeakers();
                UpdateLeakers();
                float deltaTime = Time.deltaTime;
                bool flag = true;
                _ = waterPlanes.Count;
                if (LOD.IsFull())
                {
                    wasLODFull = true;
                }
                else
                {
                    if (wasLODFull)
                    {
                        nextUpdateTime = Time.time + global::UnityEngine.Random.value * timeBetweenLowLODUpdates;
                        wasLODFull = false;
                    }
                    if (Time.time < nextUpdateTime)
                    {
                        flag = false;
                    }
                    else
                    {
                        nextUpdateTime += timeBetweenLowLODUpdates;
                        deltaTime = Time.time - lastUpdateTime;
                    }
                }
                if (flag)
                {
                    UpdateCompartmentsStatus();
                    AddLeaks(deltaTime);
                    ApplyDraining(deltaTime);
                    Step(deltaTime);
                    UpdateFloodGroupWaterLevels();
                    if (!LOD.IsMinimal())
                    {
                        UpdateWaterPlanesByHeight();
                        UpdateWaterRenderingTransitions();
                    }
                    lastUpdateTime = Time.time;
                }
            }
            finally
            {
            }
        }

        private void UpdateCompartmentsStatus()
        {
            ProfilingUtils.BeginSample("UpdateCompartmentsStatus");
            for (int i = 0; i < this.compartmentStatus.Count; i++)
            {
                if (this.compartmentStatus[i] == CompartmentStatus.LeakyFull || this.compartmentStatus[i] == CompartmentStatus.LeakyNotFull)
                {
                    this.compartmentStatus[i] = CompartmentStatus.WasLeaky;
                }
                else
                {
                    this.compartmentStatus[i] = CompartmentStatus.Unused;
                }
            }
            Leakable[] array = leakers;
            foreach (Leakable leakable in array)
            {
                if (leakable != null && leakable.GetLeakCount() > 0)
                {
                    Int3 point = baseComp.WorldToGrid(leakable.transform.position);
                    int num = cellCompartment[shape.GetIndex(point)];
                    if (num != noCompartment)
                    {
                        this.compartmentStatus[num] = CompartmentStatus.LeakyFull;
                    }
                }
            }
            foreach (int occupiedCellIndex in baseComp.OccupiedCellIndexes)
            {
                int num2 = cellCompartment[occupiedCellIndex];
                if (num2 != noCompartment)
                {
                    CompartmentStatus compartmentStatus = this.compartmentStatus[num2];
                    if (compartmentStatus == CompartmentStatus.Unused)
                    {
                        this.compartmentStatus[num2] = CompartmentStatus.NoLeaks;
                    }
                    else if (cellWaterLevel[occupiedCellIndex] < 1f && compartmentStatus == CompartmentStatus.LeakyFull)
                    {
                        this.compartmentStatus[num2] = CompartmentStatus.LeakyNotFull;
                    }
                }
            }
            for (int k = 0; k < this.compartmentStatus.Count; k++)
            {
                if (this.compartmentStatus[k] == CompartmentStatus.WasLeaky)
                {
                    this.compartmentStatus[k] = CompartmentStatus.NoLeaks;
                    hullRestoredNotification.Play();
                }
            }
            ProfilingUtils.EndSample();
        }

        private float OutsidePressure(Int3 cell)
        {
            float num = 0f - baseComp.GridToWorld(cell).y;
            if (num > 0f)
            {
                return 1f + (num / Base.cellSize.y - 0.5f) * 0.1f;
            }
            return -1f;
        }

        private void AddLeaks(float deltaTime)
        {
            if (leakObjectRefreshPending)
            {
                return;
            }
            ProfilingUtils.BeginSample("AddLeaks");
            Leakable[] array = leakers;
            foreach (Leakable leakable in array)
            {
                if (!(leakable != null))
                {
                    continue;
                }
                int leakCount = leakable.GetLeakCount();
                if (leakCount <= 0)
                {
                    continue;
                }
                Int3 @int = baseComp.WorldToGrid(leakable.transform.position);
                int index = shape.GetIndex(@int);
                int num = cellCompartment[index];
                if (num != noCompartment && compartmentStatus[num] != CompartmentStatus.LeakyFull)
                {
                    float num2 = OutsidePressure(@int);
                    float num3 = (float)leakCount * leakSpeedPerHole * deltaTime;
                    if (num2 < cellWaterLevel[index])
                    {
                        num3 = 0f - num3;
                    }
                    cellWaterLevel[index] += num3;
                }
            }
            ProfilingUtils.EndSample();
        }

        private void ApplyDraining(float deltaTime)
        {
            ProfilingUtils.BeginSample("ApplyDraining");
            float num = 0.0166666675f * deltaTime;
            foreach (int occupiedCellIndex in baseComp.OccupiedCellIndexes)
            {
                int num2 = cellCompartment[occupiedCellIndex];
                if (num2 != noCompartment && compartmentStatus[num2] == CompartmentStatus.NoLeaks)
                {
                    float num3 = cellWaterLevel[occupiedCellIndex];
                    cellWaterLevel[occupiedCellIndex] = Mathf.Max(num3 - num, 0f);
                }
            }
            ProfilingUtils.EndSample();
        }

        private void Step(float deltaTime)
        {
            ProfilingUtils.BeginSample("Step");
            Array.Clear(delta, 0, delta.Length);
            float max = 10f * deltaTime;
            byte[] flowData = baseComp.flowData;
            int num = 1;
            int x = shape.x;
            int num2 = shape.x * shape.y;
            List<int> occupiedCellIndexes = baseComp.OccupiedCellIndexes;
            foreach (int item in occupiedCellIndexes)
            {
                byte b = flowData[item];
                shape.GetPointAsInt3(item);
                if ((b & 0x40) == 0)
                {
                    continue;
                }
                float num3 = cellWaterLevel[item];
                if (num3 > 0f)
                {
                    float b2 = 10f * deltaTime;
                    float num4 = 0f;
                    float num5 = 0f;
                    float num6 = 0f;
                    float num7 = 0f;
                    float num8 = float.PositiveInfinity;
                    float num9 = 0f;
                    if (((uint)b & (true ? 1u : 0u)) != 0)
                    {
                        num4 = (num3 - cellWaterLevel[item + num2]) * 0.5f;
                        if (num4 > 0f)
                        {
                            num4 = Mathf.Min(num4, b2);
                            num8 = Mathf.Min(num8, num4);
                            num9 += num4;
                        }
                    }
                    if ((b & 2u) != 0)
                    {
                        num5 = (num3 - cellWaterLevel[item - num2]) * 0.5f;
                        if (num5 > 0f)
                        {
                            num5 = Mathf.Min(num5, b2);
                            num8 = Mathf.Min(num8, num5);
                            num9 += num5;
                        }
                    }
                    if ((b & 4u) != 0)
                    {
                        num6 = (num3 - cellWaterLevel[item + num]) * 0.5f;
                        if (num6 > 0f)
                        {
                            num6 = Mathf.Min(num6, b2);
                            num8 = Mathf.Min(num8, num6);
                            num9 += num6;
                        }
                    }
                    if ((b & 8u) != 0)
                    {
                        num7 = (num3 - cellWaterLevel[item - num]) * 0.5f;
                        if (num7 > 0f)
                        {
                            num7 = Mathf.Min(num7, b2);
                            num8 = Mathf.Min(num8, num7);
                            num9 += num7;
                        }
                    }
                    if (num9 > 0f)
                    {
                        float num10 = num8;
                        if (num4 > 0f)
                        {
                            num4 = num4 / num9 * num10;
                            delta[item + num2] += num4;
                        }
                        if (num5 > 0f)
                        {
                            num5 = num5 / num9 * num10;
                            delta[item - num2] += num5;
                        }
                        if (num6 > 0f)
                        {
                            num6 = num6 / num9 * num10;
                            delta[item + num] += num6;
                        }
                        if (num7 > 0f)
                        {
                            num7 = num7 / num9 * num10;
                            delta[item - num] += num7;
                        }
                        delta[item] -= num10;
                        num3 -= num10;
                    }
                }
                if ((b & 0x20u) != 0)
                {
                    float num11 = cellWaterLevel[item - x];
                    float num12 = num3 + num11;
                    float value;
                    if (num12 <= 1f)
                    {
                        value = num3;
                    }
                    else if (num12 <= 2.1f)
                    {
                        float num13 = (num12 - 1f) / 1.1f;
                        value = num3 - num13;
                    }
                    else
                    {
                        float num14 = (num3 + num11 - 0.1f) * 0.5f;
                        value = num3 - num14;
                    }
                    value = Mathf.Clamp(value, 0f, max);
                    delta[item] -= value;
                    delta[item - x] += value;
                    num3 -= value;
                }
                if ((b & 0x10u) != 0)
                {
                    float num15 = cellWaterLevel[item + x];
                    float num16 = num3 + num15;
                    float value2;
                    if (num16 <= 1f)
                    {
                        value2 = 0f;
                    }
                    else if (num16 <= 2.1f)
                    {
                        float num17 = (1f + 0.1f * num16) / 1.1f;
                        value2 = num3 - num17;
                    }
                    else
                    {
                        float num18 = (num3 + num15 + 0.1f) * 0.5f;
                        value2 = num3 - num18;
                    }
                    value2 = Mathf.Clamp(value2, 0f, max);
                    delta[item] -= value2;
                    delta[item + x] += value2;
                }
            }
            foreach (int item2 in occupiedCellIndexes)
            {
                cellWaterLevel[item2] += delta[item2];
            }
            ProfilingUtils.EndSample();
        }

        private void CreateWaterPlane(Int3 cell)
        {
            if (!baseComp.GridToWorld(cell, global::UWE.Utils.half3, out var _))
            {
                return;
            }
            Transform cellObject = baseComp.GetCellObject(cell);
            if (!(cellObject != null))
            {
                return;
            }
            BaseWaterPlaneManager componentInChildren = cellObject.gameObject.GetComponentInChildren<BaseWaterPlaneManager>(includeInactive: true);
            if (componentInChildren != null)
            {
                componentInChildren.SetHost(cellObject);
                CellWaterPlane item = default(CellWaterPlane);
                item.cell = cell;
                item.waterPlaneManager = componentInChildren;
                waterPlanes.Add(item);
                if (cell.y < waterPlaneMinY)
                {
                    waterPlaneMinY = cell.y;
                }
                if (cell.y > waterPlaneMaxY)
                {
                    waterPlaneMaxY = cell.y;
                }
            }
        }

        public bool IsUnderwater(Vector3 wsPos)
        {
            Int3 cell = baseComp.WorldToGrid(wsPos);
            if (baseComp.IsCellValid(cell))
            {
                baseComp.GridToWorld(cell, new Vector3(0.5f, 0f, 0.5f), out var result);
                baseComp.GridToWorld(cell, new Vector3(0.5f, 1f, 0.5f), out var result2);
                float visualWaterLevel = GetVisualWaterLevel(cell);
                return wsPos.y < Mathf.Lerp(result.y, result2.y, visualWaterLevel);
            }
            return false;
        }

        private void SetupWaterRendering()
        {
            waterTransitions.Clear();
            waterPlanes.Clear();
            waterPlaneMinY = int.MaxValue;
            waterPlaneMaxY = int.MinValue;
            Int3.RangeEnumerator allCells = baseComp.AllCells;
            while (allCells.MoveNext())
            {
                Int3 current = allCells.Current;
                int index = shape.GetIndex(current);
                if ((baseComp.flowData[index] & 0x40u) != 0)
                {
                    Transform cellObject = baseComp.GetCellObject(current);
                    if (cellObject != null)
                    {
                        CreateWaterPlane(current);
                        BaseWaterTransition[] componentsInChildren = cellObject.GetComponentsInChildren<BaseWaterTransition>();
                        waterTransitions.AddRange(componentsInChildren);
                    }
                }
            }
        }

        private void BuildFloodGroups()
        {
            int i = 0;
            for (int size = shape.Size; i < size; i++)
            {
                cellFloodGroup[i] = ushort.MaxValue;
            }
            floodGroupArea = new List<float>();
            Int3.RangeEnumerator allCells = baseComp.AllCells;
            while (allCells.MoveNext())
            {
                Int3 current = allCells.Current;
                float num = AssignFloodGroups(floodGroupArea.Count, current);
                if (num > 0f)
                {
                    floodGroupArea.Add(num);
                }
            }
            floodGroupWaterLevel = new float[floodGroupArea.Count];
        }

        private void BuildCompartments()
        {
            int i = 0;
            for (int size = shape.Size; i < size; i++)
            {
                cellCompartment[i] = noCompartment;
            }
            compartmentStatus = new List<CompartmentStatus>();
            Int3.RangeEnumerator allCells = baseComp.AllCells;
            while (allCells.MoveNext())
            {
                Int3 current = allCells.Current;
                if (AssignCompartments(current, compartmentStatus.Count, noCompartment))
                {
                    compartmentStatus.Add(CompartmentStatus.NoLeaks);
                }
            }
        }

        private void UpdateFloodGroupWaterLevels()
        {
            ProfilingUtils.BeginSample("UpdateFloodGroupWaterLevels");
            Array.Clear(floodGroupWaterLevel, 0, floodGroupWaterLevel.Length);
            foreach (int occupiedCellIndex in baseComp.OccupiedCellIndexes)
            {
                ushort num = cellFloodGroup[occupiedCellIndex];
                if (num != ushort.MaxValue)
                {
                    float num2 = cellWaterLevel[occupiedCellIndex];
                    floodGroupWaterLevel[num] += num2;
                }
            }
            for (int i = 0; i < floodGroupWaterLevel.Length; i++)
            {
                floodGroupWaterLevel[i] /= floodGroupArea[i];
            }
            ProfilingUtils.EndSample();
        }

        private float AssignFloodGroups(int floodGroupIndex, Int3 cell)
        {
            int cellIndex = baseComp.GetCellIndex(cell);
            int index = shape.GetIndex(cell);
            if (cellIndex == -1 || cellFloodGroup[index] != ushort.MaxValue)
            {
                return 0f;
            }
            if (!baseComp.IsInterior(cellIndex))
            {
                return 0f;
            }
            cellFloodGroup[index] = (ushort)floodGroupIndex;
            float num = 1f;
            Base.Face face = default(Base.Face);
            face.cell = cell;
            Base.Direction[] horizontalDirections = Base.HorizontalDirections;
            for (int i = 0; i < horizontalDirections.Length; i++)
            {
                Base.Direction direction = (face.direction = horizontalDirections[i]);
                if (baseComp.GetFace(face) == Base.FaceType.None)
                {
                    Base.Face adjacentFace = Base.GetAdjacentFace(face);
                    if (baseComp.GetFace(adjacentFace) == Base.FaceType.None)
                    {
                        Int3 adjacent = Base.GetAdjacent(cell, direction);
                        num += AssignFloodGroups(floodGroupIndex, adjacent);
                    }
                }
            }
            return num;
        }

        private bool AssignCompartments(Int3 cell, int newCompartmentIndex, int oldCompartmentIndex)
        {
            if (baseComp.GetCellIndex(cell) == -1)
            {
                return false;
            }
            int index = shape.GetIndex(cell);
            if (cellCompartment[index] != oldCompartmentIndex)
            {
                return false;
            }
            byte b = baseComp.flowData[index];
            if ((b & 0x40) == 0)
            {
                return false;
            }
            cellCompartment[index] = (ushort)newCompartmentIndex;
            if (((uint)b & (true ? 1u : 0u)) != 0)
            {
                AssignCompartments(Base.GetAdjacent(cell, Base.Direction.North), newCompartmentIndex, oldCompartmentIndex);
            }
            if ((b & 2u) != 0)
            {
                AssignCompartments(Base.GetAdjacent(cell, Base.Direction.South), newCompartmentIndex, oldCompartmentIndex);
            }
            if ((b & 4u) != 0)
            {
                AssignCompartments(Base.GetAdjacent(cell, Base.Direction.East), newCompartmentIndex, oldCompartmentIndex);
            }
            if ((b & 8u) != 0)
            {
                AssignCompartments(Base.GetAdjacent(cell, Base.Direction.West), newCompartmentIndex, oldCompartmentIndex);
            }
            if ((b & 0x10u) != 0)
            {
                AssignCompartments(Base.GetAdjacent(cell, Base.Direction.Above), newCompartmentIndex, oldCompartmentIndex);
            }
            if ((b & 0x20u) != 0)
            {
                AssignCompartments(Base.GetAdjacent(cell, Base.Direction.Below), newCompartmentIndex, oldCompartmentIndex);
            }
            return true;
        }

        public bool tIsLeaking()
        {
            if (compartmentStatus != null)
            {
                for (int i = 0; i < compartmentStatus.Count; i++)
                {
                    if (compartmentStatus[i] == CompartmentStatus.WasLeaky || compartmentStatus[i] == CompartmentStatus.LeakyNotFull || compartmentStatus[i] == CompartmentStatus.LeakyFull)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
