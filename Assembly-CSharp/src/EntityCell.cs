using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class EntityCell : IPriorityQueueItem
    {
        public enum State
        {
            Uninitialized,
            Finalized,
            IsAwake,
            IsAsleep,
            QueuedForAwake,
            QueuedForSleep,
            InAwakeAsync,
            InSleepAsync,
            InAwakeAsyncToSleep,
            InSleepAsyncToAwake,
            InSerializeSerialDataAsync,
            InSerializeWaiterDataAsync
        }

        private CellManager manager;

        private LargeWorldStreamer host;

        private Int3 batchId;

        private Int3 cellId;

        private int level;

        private SerialData serialData = new SerialData();

        private SerialData legacyData = new SerialData();

        private SerialData waiterData = new SerialData();

        public GameObject liveRoot;

        private State state;

        private List<LargeWorldEntity> waiterQueue;

        private Vector3 cachedWorldPos;

        private CellProcessingStats heapStats;

        private static int queueInSerial = 0;

        private static int queueOutSerial = 0;

        private static ObjectPool<EntityCell> cellPool = ObjectPoolHelper.CreatePool<EntityCell>(64000);

        public Int3 BatchId => batchId;

        public Int3 CellId => cellId;

        public int Level => level;

        public State CurrentState => state;

        public float priority { get; private set; }

        public int numPriorityChanges { get; private set; }

        public EntityCell()
        {
        }

        public EntityCell(CellManager manager, LargeWorldStreamer host, Int3 batchId, Int3 cellId, int level)
        {
            InitData(manager, host, batchId, cellId, level);
        }

        public void InitData(CellManager manager, LargeWorldStreamer host, Int3 batchId, Int3 cellId, int level)
        {
            this.manager = manager;
            this.host = host;
            this.batchId = batchId;
            this.cellId = cellId;
            this.level = level;
            liveRoot = null;
            state = State.Uninitialized;
        }

        public Vector3 GetCenter()
        {
            return GetCenter(GetBlockBounds());
        }

        public static Vector3 GetCenter(Int3.Bounds blockBounds)
        {
            Vector3 vector = blockBounds.mins.ToVector3();
            Vector3 vector2 = (blockBounds.maxs + 1).ToVector3();
            return (vector + vector2) / 2f;
        }

        public Int3 GetSize()
        {
            return BatchCells.GetCellSize(level, host.blocksPerBatch);
        }

        public Int3.Bounds GetBlockBounds()
        {
            return BatchCells.GetBlockBounds(batchId, cellId, level, host.blocksPerBatch);
        }

        public void ClearTempSerialData()
        {
            if (state == State.IsAwake || state == State.QueuedForSleep)
            {
                serialData.Clear();
            }
        }

        public SerialData GetSerialData()
        {
            return serialData;
        }

        public IEnumerator EnsureSerialDataSerialized(ProtobufSerializer serializer)
        {
            if (state == State.IsAsleep || state == State.QueuedForAwake)
            {
                return null;
            }
            return SerializeSerialDataAsync(serializer);
        }

        public void ReadSerialDataFromStream(Stream stream, int dataLength)
        {
            serialData.ReadFromStream(stream, dataLength);
        }

        public void ReadLegacyDataFromStream(Stream stream, int dataLength)
        {
            legacyData.ReadFromStream(stream, dataLength);
        }

        public void ReadWaiterDataFromStream(Stream stream, int dataLength)
        {
            waiterData.ReadFromStream(stream, dataLength);
        }

        public void MoveSerialDataToLegacy()
        {
            SerialData serialData = legacyData;
            legacyData = this.serialData;
            this.serialData = serialData;
            this.serialData.Clear();
        }

        public SerialData GetLegacyData()
        {
            return legacyData;
        }

        public SerialData GetWaiterData()
        {
            return waiterData;
        }

        public IEnumerator EnsureWaiterDataSerialized(ProtobufSerializer serializer)
        {
            if (waiterQueue == null || waiterQueue.Count == 0)
            {
                return null;
            }
            return SerializeWaiterDataAsync(serializer);
        }

        private IEnumerator SerializeSerialDataAsync(ProtobufSerializer serializer)
        {
            State previousState = state;
            state = State.InSerializeSerialDataAsync;
            yield return SerializeAsyncImpl(serializer);
            state = previousState;
        }

        private IEnumerator SerializeWaiterDataAsync(ProtobufSerializer serializer)
        {
            if (waiterQueue == null || waiterQueue.Count == 0)
            {
                yield break;
            }
            State previousState = state;
            state = State.InSerializeWaiterDataAsync;
            using (ScratchMemoryStream stream = new ScratchMemoryStream())
            {
                for (int i = 0; i < waiterQueue.Count; i++)
                {
                    LargeWorldEntity largeWorldEntity = waiterQueue[i];
                    if ((bool)largeWorldEntity)
                    {
                        GameObject gameObject = largeWorldEntity.gameObject;
                        yield return serializer.SerializeObjectTreeAsync(stream, gameObject);
                    }
                    else
                    {
                        Debug.LogWarningFormat(liveRoot, "Skipping destroyed waiter {0} on serialize {1}", i, this);
                    }
                }
                ProfilingUtils.BeginSample("clear waiter queue");
                ClearWaiterQueue();
                ProfilingUtils.EndSample();
                waiterData.Concatenate(stream);
            }
            state = previousState;
        }

        private IEnumerator SerializeAsyncImpl(ProtobufSerializer serializer)
        {
            switch (state)
            {
                default:
                    Debug.LogWarningFormat(liveRoot, "Unexpected state {0} in SerializeAsync of cell {1}", state, this);
                    break;
                case State.InSleepAsync:
                case State.InSleepAsyncToAwake:
                case State.InSerializeSerialDataAsync:
                    break;
            }
            if (!liveRoot || liveRoot.transform.childCount == 0)
            {
                serialData.Clear();
                yield break;
            }
            using ScratchMemoryStream stream = new ScratchMemoryStream();
            ProfilingUtils.BeginSample("Cell::SerializeAync header");
            serializer.SerializeStreamHeader(stream);
            ProfilingUtils.EndSample();
            yield return serializer.SerializeObjectTreeAsync(stream, liveRoot);
            serialData.CopyFrom(stream);
        }

        public void EnsureRoot()
        {
            if (!liveRoot)
            {
                liveRoot = CreateRoot();
            }
        }

        private GameObject CreateRoot()
        {
            ProfilingUtils.BeginSample("Cell::CreateRoot");
            DebugDisplayTimer.Start();
            Vector3 cellRootPosition = GetCellRootPosition();
            ProfilingUtils.BeginSample("InstantiateEntityCellRoot");
            GameObject gameObject = global::UnityEngine.Object.Instantiate(host.cellRootPrefab, cellRootPosition, Quaternion.identity, host.cellsRoot);
            ProfilingUtils.EndSample();
            if (Application.isEditor)
            {
                gameObject.name = $"Batch {batchId.ToCsvString()} cell {cellId.ToCsvString()} L{level} root";
                gameObject.EnsureComponent<LargeWorldEntityCell>().cell = this;
            }
            gameObject.hideFlags |= HideFlags.NotEditable;
            ProfilingUtils.EndSample();
            return gameObject;
        }

        public Vector3 GetCellRootPosition()
        {
            return host.land.transform.TransformPoint(GetCenter());
        }

        public bool AddEntity(LargeWorldEntity ent)
        {
            bool result = false;
            ProfilingUtils.BeginSample("Cell::AddEntity");
            switch (state)
            {
                case State.IsAwake:
                case State.QueuedForSleep:
                    EnsureRoot();
                    ProfilingUtils.BeginSample("Set parent");
                    ent.transform.SetParent(liveRoot.transform, worldPositionStays: true);
                    ProfilingUtils.EndSample();
                    ent.OnAddToCell();
                    result = true;
                    break;
                case State.IsAsleep:
                case State.QueuedForAwake:
                case State.InAwakeAsync:
                case State.InSleepAsync:
                case State.InAwakeAsyncToSleep:
                case State.InSleepAsyncToAwake:
                case State.InSerializeSerialDataAsync:
                case State.InSerializeWaiterDataAsync:
                    ProfilingUtils.BeginSample("WaiterDeactivate");
                    ent.gameObject.SetActive(value: false);
                    ent.transform.SetParent(host.waitersRoot, worldPositionStays: true);
                    ProfilingUtils.EndSample();
                    ProfilingUtils.BeginSample("AddEntity-new Waiter Queue");
                    waiterQueue = waiterQueue ?? new List<LargeWorldEntity>();
                    waiterQueue.Add(ent);
                    ProfilingUtils.EndSample();
                    break;
                default:
                    Debug.LogWarningFormat(ent, "Unexpected state {0} in Cell.AddEntity of cell {1}", state, this);
                    break;
            }
            ProfilingUtils.EndSample();
            return result;
        }

        public void LegacyRead(BinaryReader reader, int version)
        {
            int dataLength = reader.ReadInt32();
            serialData.ReadFromStream(reader.BaseStream, dataLength);
            ProfilingUtils.BeginSample("read waiters");
            if (version == 2)
            {
                int num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                {
                    int count = reader.ReadInt32();
                    reader.ReadBytes(count);
                }
            }
            else if (version == 3)
            {
                Debug.LogError("EntityCell.LegacyRead version 3 is no longer supported");
            }
            else if (version >= 4)
            {
                reader.ReadInt32();
                int count2 = reader.ReadInt32();
                reader.ReadBytes(count2);
            }
            ProfilingUtils.EndSample();
        }

        public void RequestAbort()
        {
            switch (state)
            {
                case State.InAwakeAsync:
                case State.InSleepAsync:
                case State.InAwakeAsyncToSleep:
                case State.InSleepAsyncToAwake:
                    Debug.LogWarningFormat("Requesting abort in state {0}", state);
                    state = (liveRoot ? State.IsAwake : State.IsAsleep);
                    break;
                case State.InSerializeSerialDataAsync:
                case State.InSerializeWaiterDataAsync:
                    Debug.LogWarningFormat("Requesting abort in state {0}", state);
                    state = State.IsAwake;
                    break;
                default:
                    Debug.LogWarningFormat("Unexpected state {0} in EntityCell::RequestAbort.", state);
                    break;
            }
        }

        public void BeginLoad()
        {
            Utils.AssertEditMode();
            liveRoot = CreateRoot();
        }

        public void EndLoad()
        {
            Utils.AssertEditMode();
            if ((bool)liveRoot)
            {
                ProfilingUtils.BeginSample("cell level check");
                LargeWorldEntity[] componentsInChildren = liveRoot.GetComponentsInChildren<LargeWorldEntity>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].OnAddToCell();
                }
                ProfilingUtils.EndSample();
            }
        }

        public void LoadPassFromStream(Stream stream, ProtobufSerializer serializer)
        {
            Utils.AssertEditMode();
            ProfilingUtils.BeginSample("Cell::LoadPassFromStream");
            serializer.DeserializeObjects(stream, null, forceParent: true, liveRoot.transform, 0);
            ProfilingUtils.EndSample();
        }

        public void SavePassToStream(Stream stream, ProtobufSerializer serializer, Func<UniqueIdentifier, bool> predicate)
        {
            Utils.AssertEditMode();
            ProfilingUtils.BeginSample("Cell::SavePassToStream");
            IList<UniqueIdentifier> uids = CollectEntities(predicate);
            serializer.SerializeObjects(stream, uids, storeParent: false);
            ProfilingUtils.EndSample();
        }

        public IList<UniqueIdentifier> CollectEntities(Func<UniqueIdentifier, bool> predicate)
        {
            Utils.AssertEditMode();
            if (!liveRoot)
            {
                return new UniqueIdentifier[0];
            }
            return (from p in liveRoot.GetComponentsInChildren<UniqueIdentifier>(includeInactive: true).Where(predicate)
                where p.transform.parent == liveRoot.transform
                select p).ToList();
        }

        public bool ContainsEntity(Func<UniqueIdentifier, bool> predicate)
        {
            Utils.AssertEditMode();
            return liveRoot.GetComponentsInChildren<UniqueIdentifier>(includeInactive: true).Any(predicate);
        }

        public void DestroyTileInstanceEntities()
        {
            Utils.AssertEditMode();
            TileInstanceEntity[] componentsInChildren = liveRoot.GetComponentsInChildren<TileInstanceEntity>(includeInactive: true);
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                global::UnityEngine.Object.DestroyImmediate(componentsInChildren[i].gameObject);
            }
        }

        public void EnsureAwake(ProtobufSerializer serializer)
        {
            Utils.AssertEditMode();
            switch (state)
            {
                case State.IsAsleep:
                case State.QueuedForAwake:
                    CoroutineUtils.PumpCoroutine(AwakeAsync(serializer));
                    return;
                case State.QueuedForSleep:
                    state = State.IsAwake;
                    return;
                case State.IsAwake:
                    return;
            }
            Debug.LogWarningFormat("Unexpected state {0} in Cell.EnsureAwake of cell {1}", state, this);
        }

        public void Awake(ProtobufSerializer serializer)
        {
            Utils.AssertEditMode();
            CoroutineUtils.PumpCoroutine(AwakeAsync(serializer));
        }

        private IEnumerator AwakeAsync(ProtobufSerializer serializer)
        {
            state = State.InAwakeAsync;
            if (serialData.Length > 0)
            {
                using (MemoryStream stream2 = new MemoryStream(serialData.Data.Array, serialData.Data.Offset, serialData.Data.Length, writable: false))
                {
                    if (serializer.TryDeserializeStreamHeader(stream2))
                    {
                        CoroutineTask<GameObject> task2 = serializer.DeserializeObjectTreeAsync(stream2, forceInactiveRoot: true, 0);
                        yield return task2;
                        liveRoot = task2.GetResult();
                    }
                }
                serialData.Clear();
            }
            if (waiterData.Length > 0)
            {
                EnsureRoot();
                using (MemoryStream stream2 = new MemoryStream(waiterData.Data.Array, waiterData.Data.Offset, waiterData.Data.Length, writable: false))
                {
                    while (stream2.Position < waiterData.Length)
                    {
                        CoroutineTask<GameObject> task2 = serializer.DeserializeObjectTreeAsync(stream2, forceInactiveRoot: true, 0);
                        yield return task2;
                        GameObject result = task2.GetResult();
                        result.transform.SetParent(liveRoot.transform, worldPositionStays: true);
                        result.SetActive(value: true);
                    }
                }
                waiterData.Clear();
            }
            bool backToSleep = state == State.InAwakeAsyncToSleep;
            state = State.IsAwake;
            if ((bool)liveRoot)
            {
                ProfilingUtils.BeginSample("Cell::AwakeAsync activate root");
                liveRoot.transform.SetParent(host.cellsRoot, worldPositionStays: false);
                liveRoot.SetActive(value: true);
                ProfilingUtils.EndSample();
                if (Application.isEditor)
                {
                    liveRoot.name = $"Batch {batchId.ToCsvString()} cell {cellId.ToCsvString()} L{level} root";
                    liveRoot.EnsureComponent<LargeWorldEntityCell>().cell = this;
                    GetCellRootPosition();
                }
            }
            if (legacyData.Length > 0)
            {
                using (MemoryStream stream2 = new MemoryStream(legacyData.Data.Array, legacyData.Data.Offset, legacyData.Data.Length, writable: false))
                {
                    if (serializer.TryDeserializeStreamHeader(stream2))
                    {
                        CoroutineTask<GameObject> task2 = serializer.DeserializeObjectTreeAsync(stream2, forceInactiveRoot: true, 0);
                        yield return task2;
                        GameObject result2 = task2.GetResult();
                        Transform transform = result2.transform;
                        transform.SetParent(host.cellsRoot, worldPositionStays: false);
                        ReregisterEntities(transform, manager);
                        global::UWE.Utils.DestroyWrap(result2);
                    }
                }
                legacyData.Clear();
            }
            ProfilingUtils.BeginSample("waiterQueue");
            if (waiterQueue != null)
            {
                EnsureRoot();
                for (int i = 0; i < waiterQueue.Count; i++)
                {
                    LargeWorldEntity largeWorldEntity = waiterQueue[i];
                    if ((bool)largeWorldEntity)
                    {
                        ProfilingUtils.BeginSample("WaiterActivate");
                        largeWorldEntity.transform.SetParent(liveRoot.transform, worldPositionStays: true);
                        largeWorldEntity.gameObject.SetActive(value: true);
                        ProfilingUtils.EndSample();
                    }
                    else
                    {
                        Debug.LogWarningFormat(liveRoot, "Skipping destroyed waiter {0} on awake {1}", i, this);
                    }
                }
                waiterQueue.Clear();
                waiterQueue = null;
            }
            ProfilingUtils.EndSample();
            if (backToSleep)
            {
                manager.QueueForSleep(this);
            }
        }

        public void ReregisterEntities()
        {
            if ((bool)liveRoot)
            {
                ReregisterEntities(liveRoot.transform, manager);
            }
        }

        private static void ReregisterEntities(Transform rootTransform, CellManager manager)
        {
            for (int num = rootTransform.childCount - 1; num >= 0; num--)
            {
                LargeWorldEntity component = rootTransform.GetChild(num).GetComponent<LargeWorldEntity>();
                manager.RegisterEntity(component);
            }
        }

        public IEnumerator EnsureSleepAsync(ProtobufSerializer serializer)
        {
            switch (state)
            {
                case State.IsAwake:
                case State.QueuedForSleep:
                    return SleepAsync(serializer);
                case State.IsAsleep:
                    return null;
                case State.QueuedForAwake:
                    state = State.IsAsleep;
                    return null;
                default:
                    Debug.LogWarningFormat("Unexpected state {0} in Cell.EnsureSleep of cell {1}", state, this);
                    return null;
            }
        }

        public void Sleep(ProtobufSerializer serializer)
        {
            Utils.AssertEditMode();
            CoroutineUtils.PumpCoroutine(SleepAsync(serializer));
        }

        private IEnumerator SleepAsync(ProtobufSerializer serializer)
        {
            state = State.InSleepAsync;
            if ((bool)liveRoot)
            {
                StopwatchProfiler.GetCachedProfilerTag("Cell-Sleep1-DeactivateRoot-", liveRoot.name);
                liveRoot.SetActive(value: false);
            }
            yield return SerializeAsyncImpl(serializer);
            if ((bool)liveRoot)
            {
                ProfilingUtils.BeginSample("Cell::SleepAsync destroy root");
                global::UWE.Utils.DestroyWrap(liveRoot);
                liveRoot = null;
                ProfilingUtils.EndSample();
            }
            bool num = state == State.InSleepAsyncToAwake;
            state = State.IsAsleep;
            if (num)
            {
                manager.QueueForAwake(this);
            }
        }

        public void Initialize(SerialData serialData = null, SerialData legacySerialData = null, SerialData waiterSerialData = null)
        {
            state = State.IsAsleep;
            if (serialData != null)
            {
                this.serialData.CopyFrom(serialData);
            }
            if (legacySerialData != null)
            {
                legacyData.CopyFrom(legacySerialData);
            }
            if (waiterSerialData != null)
            {
                waiterData.CopyFrom(waiterSerialData);
            }
        }

        public void QueueForAwake(IQueue<EntityCell> queue)
        {
            switch (state)
            {
                case State.IsAsleep:
                    state = State.QueuedForAwake;
                    OnEnqueue();
                    queue.Enqueue(this);
                    break;
                case State.QueuedForSleep:
                    state = State.IsAwake;
                    break;
                case State.InAwakeAsyncToSleep:
                    state = State.InAwakeAsync;
                    break;
                case State.InSleepAsync:
                    state = State.InSleepAsyncToAwake;
                    break;
                default:
                    Debug.LogWarningFormat("Unexpected state {0} in Cell.QueueForAwake of cell {1}", state, this);
                    break;
                case State.IsAwake:
                case State.QueuedForAwake:
                case State.InAwakeAsync:
                case State.InSleepAsyncToAwake:
                    break;
            }
        }

        public void QueueForSleep(IQueue<EntityCell> queue)
        {
            switch (state)
            {
                case State.IsAwake:
                    state = State.QueuedForSleep;
                    OnEnqueue();
                    queue.Enqueue(this);
                    break;
                case State.QueuedForAwake:
                    state = State.IsAsleep;
                    break;
                case State.InSleepAsyncToAwake:
                    state = State.InSleepAsync;
                    break;
                case State.InAwakeAsync:
                    state = State.InAwakeAsyncToSleep;
                    break;
                default:
                    Debug.LogWarningFormat("Unexpected state {0} in Cell.QueueForSleep of cell {1}", state, this);
                    break;
                case State.IsAsleep:
                case State.QueuedForSleep:
                case State.InSleepAsync:
                case State.InAwakeAsyncToSleep:
                    break;
            }
        }

        public IEnumerator Proceed(ProtobufSerializer serializer)
        {
            switch (state)
            {
                case State.IsAwake:
                case State.IsAsleep:
                case State.InAwakeAsync:
                case State.InSleepAsync:
                case State.InAwakeAsyncToSleep:
                case State.InSleepAsyncToAwake:
                    return null;
                case State.Finalized:
                    return null;
                case State.Uninitialized:
                    return null;
                case State.QueuedForAwake:
                    return AwakeAsync(serializer);
                case State.QueuedForSleep:
                    return SleepAsync(serializer);
                default:
                    Debug.LogWarningFormat("Unexpected state {0} in Cell.Proceed of cell {1}", state, this);
                    return null;
            }
        }

        private bool IsInitialized()
        {
            State state = this.state;
            if ((uint)state <= 1u)
            {
                return false;
            }
            return true;
        }

        private bool IsProcessing()
        {
            State state = this.state;
            if ((uint)(state - 6) <= 5u)
            {
                return true;
            }
            return false;
        }

        public bool IsEmpty()
        {
            if ((bool)liveRoot && liveRoot.transform.childCount > 0)
            {
                return false;
            }
            if (!serialData.IsEmpty() || !legacyData.IsEmpty() || !waiterData.IsEmpty())
            {
                return false;
            }
            if (waiterQueue != null && waiterQueue.Count > 0)
            {
                return false;
            }
            return true;
        }

        public void SetRenderersEnabled(bool val)
        {
            Utils.AssertEditMode();
            if ((bool)liveRoot)
            {
                Renderer[] componentsInChildren = liveRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].enabled = val;
                }
            }
        }

        public void Cycle(ProtobufSerializer serializer)
        {
            Utils.AssertEditMode();
            if (state == State.IsAwake)
            {
                Sleep(serializer);
                Awake(serializer);
            }
        }

        private void ClearWaiterQueue()
        {
            if (waiterQueue == null)
            {
                return;
            }
            for (int i = 0; i < waiterQueue.Count; i++)
            {
                LargeWorldEntity largeWorldEntity = waiterQueue[i];
                if ((bool)largeWorldEntity)
                {
                    global::UWE.Utils.DestroyWrap(largeWorldEntity.gameObject);
                }
            }
            waiterQueue.Clear();
            waiterQueue = null;
        }

        public void Reset()
        {
            if (state == State.Finalized)
            {
                return;
            }
            if ((bool)liveRoot)
            {
                if (liveRoot.transform.childCount > 0 && manager != null && !manager.abortRequested && Application.isPlaying)
                {
                    Debug.LogWarningFormat(liveRoot, "Resetting cell with live root {0}", this);
                }
                global::UWE.Utils.DestroyWrap(liveRoot);
                liveRoot = null;
            }
            if (waiterQueue != null && waiterQueue.Count > 0)
            {
                Debug.LogWarningFormat(liveRoot, "Resetting cell with {0} waiters {1}", waiterQueue.Count, this);
            }
            ClearWaiterQueue();
            serialData.Clear();
            legacyData.Clear();
            waiterData.Clear();
            manager = null;
            host = null;
            batchId = Int3.zero;
            cellId = Int3.zero;
            level = 0;
            state = State.Finalized;
        }

        public bool HasData()
        {
            if (serialData.Length <= 0 && legacyData.Length <= 0)
            {
                return waiterData.Length > 0;
            }
            return true;
        }

        public int EstimateBytes()
        {
            return 8 + 4 + 4 + 12 + 12 + 4 + 4 + 4 + (12 + serialData.Data.Length) + (12 + legacyData.Data.Length) + (12 + waiterData.Data.Length);
        }

        public override string ToString()
        {
            return $"Cell {cellId} (level {level}, batch {batchId}, state {state})";
        }

        public static EntityCell GetFromPool(CellManager manager, LargeWorldStreamer host, Int3 batchId, Int3 cellId, int level)
        {
            EntityCell entityCell = cellPool.Get();
            entityCell.InitData(manager, host, batchId, cellId, level);
            return entityCell;
        }

        public static void ReturnToPool(EntityCell cell)
        {
            cell.Reset();
            cellPool.Return(cell);
        }

        public void OnEnqueue()
        {
            cachedWorldPos = GetCellRootPosition();
            UpdatePriority();
            numPriorityChanges = 0;
            InitializeHeapStats();
        }

        public void OnDequeue()
        {
            FinalizeHeapStats();
        }

        public float GetPriority()
        {
            return priority;
        }

        public float UpdatePriority()
        {
            Vector3 cachedCameraPosition = LargeWorldStreamer.main.cachedCameraPosition;
            Vector3 cachedCameraForward = LargeWorldStreamer.main.cachedCameraForward;
            Vector3 rhs = cachedWorldPos - cachedCameraPosition;
            float magnitude = rhs.magnitude;
            float num = Vector3.Dot(cachedCameraForward, rhs) / magnitude;
            float num2 = 2f - num;
            float num3 = num2 * num2;
            priority = magnitude * num3;
            if (state == State.QueuedForSleep)
            {
                priority = 5000f - priority;
            }
            numPriorityChanges++;
            return priority;
        }

        private void InitializeHeapStats()
        {
            if (HeapStats.main.IsRecording && state == State.QueuedForAwake)
            {
                Vector3 cachedCameraPosition = LargeWorldStreamer.main.cachedCameraPosition;
                Vector3 cachedCameraForward = LargeWorldStreamer.main.cachedCameraForward;
                Vector3 vector = cachedWorldPos - cachedCameraPosition;
                heapStats = new CellProcessingStats();
                heapStats.inId = queueInSerial++;
                heapStats.inTime = LargeWorldStreamer.main.cachedTime;
                heapStats.inAngle = Vector3.Angle(cachedCameraForward, vector);
                heapStats.inDistance = Vector3.Magnitude(vector);
                heapStats.inPriority = GetPriority();
                heapStats.inQueueLength = manager.GetQueueLength();
            }
        }

        private void FinalizeHeapStats()
        {
            if (heapStats != null)
            {
                Vector3 cachedCameraPosition = LargeWorldStreamer.main.cachedCameraPosition;
                Vector3 cachedCameraForward = LargeWorldStreamer.main.cachedCameraForward;
                Vector3 vector = cachedWorldPos - cachedCameraPosition;
                heapStats.numPriorityChanges = numPriorityChanges;
                heapStats.outId = queueOutSerial++;
                heapStats.outTime = LargeWorldStreamer.main.cachedTime;
                heapStats.outAngle = Vector3.Angle(cachedCameraForward, vector);
                heapStats.outDistance = Vector3.Magnitude(vector);
                heapStats.outPriority = UpdatePriority();
                HeapStats.main.RecordStats("CellsHeap", heapStats);
                heapStats = null;
            }
        }
    }
}
