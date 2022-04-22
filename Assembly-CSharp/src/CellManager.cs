using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gendarme;
using ProtoBuf;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class CellManager : ClipMapManager.IClipMapEventHandler
    {
        private sealed class WaitWhileProcessingOperation : IAsyncOperation
        {
            private readonly CellManager manager;

            public bool isDone => !manager.IsProcessing();

            public WaitWhileProcessingOperation(CellManager manager)
            {
                this.manager = manager;
            }
        }

        public class UpdateCellManagementCoroutine : StateMachineBase<CellManager>
        {
            private ProtobufSerializer serializer;

            public void Initialize(ProtobufSerializer serializer)
            {
                this.serializer = serializer;
            }

            public override bool MoveNext()
            {
                if (host.cellManagementQueue.Count > 0 && !host.IsFrozen())
                {
                    EntityCell entityCell = host.cellManagementQueue.Dequeue();
                    entityCell.OnDequeue();
                    _ = host.processingCell;
                    host.processingCell = entityCell;
                    current = entityCell.Proceed(serializer);
                    return true;
                }
                host.processingCell = null;
                current = null;
                return false;
            }

            public override void Reset()
            {
                serializer = null;
            }
        }

        [ProtoContract]
        public class CellsFileHeader
        {
            [ProtoMember(1)]
            public int version;

            [ProtoMember(2)]
            public int numCells;

            public override string ToString()
            {
                return $"(version={version}, numCells={numCells})";
            }
        }

        [ProtoContract]
        public class CellHeader
        {
            [ProtoMember(1)]
            public Int3 cellId;

            [ProtoMember(2)]
            public int level;

            public override string ToString()
            {
                return $"(cellId={cellId}, level={level})";
            }
        }

        [ProtoContract]
        public class CellHeaderEx
        {
            [ProtoMember(1)]
            public Int3 cellId;

            [ProtoMember(2)]
            public int level;

            [ProtoMember(3)]
            public int dataLength;

            [ProtoMember(4)]
            public int legacyDataLength;

            [ProtoMember(5)]
            public int waiterDataLength;

            public override string ToString()
            {
                return $"(cellId={cellId}, level={level}, dataLength={dataLength}, legacyDataLength={legacyDataLength}, waiterDataLength={waiterDataLength})";
            }
        }

        public const string BatchCellsFolder = "BatchCells";

        public const string CacheFolder = "CellsCache";

        public const string GeneratedFolder = "BatchCells/Generated";

        private const int BatchCellsVersion = 9;

        private const string LootSlotSuffix = "loot-slots";

        private const string CreatureSlotSuffix = "creature-slots";

        private const string GeneratedSlotSuffix = "slots";

        private const string LootSuffix = "loot";

        private const string CreatureSuffix = "creatures";

        private const string OtherEntitySuffix = "other";

        [NonSerialized]
        private readonly LargeWorldStreamer streamer;

        private readonly Dictionary<Int3, BatchCells> batch2cells = new Dictionary<Int3, BatchCells>(Int3.equalityComparer);

        public readonly IQueue<EntityCell> cellManagementQueue = new DynamicPriorityQueue<EntityCell>();

        private EntityCell processingCell;

        private AsyncAwaiter processingAwaiter;

        [NonSerialized]
        private int freezeCount;

        [NonSerialized]
        public readonly LargeWorldEntitySpawner spawner;

        private static readonly StateMachinePool<UpdateCellManagementCoroutine, CellManager> updateCellManagementCoroutines = new StateMachinePool<UpdateCellManagementCoroutine, CellManager>();

        public bool abortRequested { get; private set; }

        public CellManager(LargeWorldStreamer streamer, LargeWorldEntitySpawner spawner)
        {
            this.streamer = streamer;
            this.spawner = spawner;
            processingAwaiter = new AsyncAwaiter(new WaitWhileProcessingOperation(this));
        }

        public void EntStats()
        {
            Timer.Begin("entstats");
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            foreach (KeyValuePair<Int3, BatchCells> batch2cell in batch2cells)
            {
                foreach (EntityCell item in batch2cell.Value.All())
                {
                    if (!(item.liveRoot != null))
                    {
                        continue;
                    }
                    foreach (Transform item2 in item.liveRoot.transform)
                    {
                        string name = item2.gameObject.name;
                        int orDefault = dictionary.GetOrDefault(name, 0);
                        dictionary[name] = orDefault + 1;
                    }
                }
            }
            int num = 0;
            foreach (KeyValuePair<string, int> item3 in dictionary)
            {
                Debug.LogFormat("{0} --> {1}", item3.Key, item3.Value);
                num += item3.Value;
            }
            Debug.LogFormat("Total ents: {0}", num);
            Timer.End();
        }

        public bool IsProcessing()
        {
            return processingCell != null;
        }

        public bool IsIdle()
        {
            if (!IsProcessing())
            {
                return cellManagementQueue.Count == 0;
            }
            return false;
        }

        public int GetQueueLength()
        {
            return cellManagementQueue.Count;
        }

        public bool IsFrozen()
        {
            if (freezeCount <= 0)
            {
                return !Application.isPlaying;
            }
            return true;
        }

        public int GetFreezeCount()
        {
            return freezeCount;
        }

        public IEnumerator IncreaseFreezeCount()
        {
            freezeCount++;
            return processingAwaiter;
        }

        public void DecreaseFreezeCount()
        {
            freezeCount--;
        }

        public void RequestAbort()
        {
            abortRequested = true;
            freezeCount++;
            if (processingCell != null)
            {
                processingCell.RequestAbort();
                processingCell = null;
            }
        }

        public IEnumerator UpdateCellManagement(ProtobufSerializer serializer)
        {
            PooledStateMachine<UpdateCellManagementCoroutine> pooledStateMachine = updateCellManagementCoroutines.Get(this);
            pooledStateMachine.stateMachine.Initialize(serializer);
            return pooledStateMachine;
        }

        public static string GetBatchCellsPath(string prefix, Int3 index)
        {
            return global::Platform.IO.Path.Combine(global::Platform.IO.Path.Combine(prefix, "BatchCells"), $"batch-cells-{index.x}-{index.y}-{index.z}.bin");
        }

        public static string GetSplitBatchCellsPath(string prefix, string directory, Int3 index, string suffix)
        {
            return global::Platform.IO.Path.Combine(global::Platform.IO.Path.Combine(prefix, directory), $"batch-cells-{index.x}-{index.y}-{index.z}-{suffix}.bin");
        }

        public static string GetCacheBatchCellsPath(string prefix, Int3 index)
        {
            return global::Platform.IO.Path.Combine(global::Platform.IO.Path.Combine(prefix, "CellsCache"), $"baked-batch-cells-{index.x}-{index.y}-{index.z}.bin");
        }

        public void UnregisterEntity(GameObject go)
        {
            LargeWorldEntity component = go.GetComponent<LargeWorldEntity>();
            if (!component)
            {
                Debug.LogWarningFormat(go, "UnregisterEntity called on a non-streamed entity {0}", go.GetFullHierarchyPath());
            }
            else
            {
                UnregisterEntity(component);
            }
        }

        public void UnregisterEntity(LargeWorldEntity ent)
        {
            UnregisterCellEntity(ent, checkParent: true);
        }

        public void UnregisterCellEntity(LargeWorldEntity ent, bool checkParent)
        {
            ent.enabled = false;
            if (Application.isEditor && checkParent)
            {
                _ = (bool)ent.transform.parent;
            }
        }

        public void RegisterEntity(GameObject ent)
        {
            if (!ent)
            {
                Debug.LogErrorFormat(ent, "RegisterEntity called on a destroyed entity '{0}'. Ignoring.", ent);
            }
            else
            {
                LargeWorldEntity lwe = ent.EnsureComponent<LargeWorldEntity>();
                RegisterEntity(lwe);
            }
        }

        public bool RegisterEntity(LargeWorldEntity lwe)
        {
            switch (lwe.cellLevel)
            {
                case LargeWorldEntity.CellLevel.Global:
                    UnregisterCellEntity(lwe, checkParent: false);
                    RegisterGlobalEntity(lwe.gameObject);
                    return true;
                case LargeWorldEntity.CellLevel.Batch:
                    UnregisterCellEntity(lwe, checkParent: false);
                    return RegisterBatchEntity(lwe.gameObject);
                default:
                    return RegisterCellEntity(lwe);
            }
        }

        private bool RegisterCellEntity(LargeWorldEntity ent)
        {
            if (streamer.debugDisableAllEnts)
            {
                return false;
            }
            ProfilingUtils.BeginSample("CellManager::RegisterEntity");
            _ = Application.isPlaying;
            ent.enabled = true;
            Vector3 position = ent.transform.position;
            Int3 block = streamer.GetBlock(position);
            Int3 key = block / streamer.blocksPerBatch;
            Int3 @int = block % streamer.blocksPerBatch;
            int cellLevel = (int)ent.cellLevel;
            bool result = false;
            if (batch2cells.TryGetValue(key, out var value))
            {
                Int3 cellSize = BatchCells.GetCellSize(cellLevel, streamer.blocksPerBatch);
                Int3 cellId = @int / cellSize;
                EntityCell entityCell = value.EnsureCell(cellId, cellLevel);
                if (!Application.isPlaying)
                {
                    using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
                    entityCell.EnsureAwake(pooledObject);
                }
                result = entityCell.AddEntity(ent);
            }
            ProfilingUtils.EndSample();
            return result;
        }

        private bool RegisterBatchEntity(GameObject ent)
        {
            Int3 containingBatch = streamer.GetContainingBatch(ent.transform.position);
            if (streamer.batch2root.TryGetValue(containingBatch, out var value))
            {
                ent.transform.parent = value.transform;
                return true;
            }
            Debug.LogErrorFormat(ent, "Trying to register batch entity '{0}' to batch '{1}' which is not loaded.", ent.name, containingBatch);
            return false;
        }

        private void RegisterGlobalEntity(GameObject ent)
        {
            ent.transform.parent = streamer.globalRoot.transform;
        }

        public bool IsManagedEntity(UniqueIdentifier ent)
        {
            Utils.AssertEditMode();
            if (!ent || ent is SceneObjectIdentifier || ent is ChildObjectIdentifier)
            {
                return false;
            }
            if (ent.GetComponent<LargeWorldEntityCell>() != null)
            {
                return false;
            }
            if (ent.GetComponent<LargeWorldBatchRoot>() != null)
            {
                return false;
            }
            if (ent.GetComponentInParent<AtmosphereVolume>() != null && ent.GetComponent<AtmosphereVolume>() == null)
            {
                return false;
            }
            if (ent.name == "Additional Settings" && ent.transform.parent != null && (bool)ent.transform.parent.GetComponent<LargeWorldBatchRoot>())
            {
                return false;
            }
            return true;
        }

        public bool IsCellManagedEntity(UniqueIdentifier ent)
        {
            Light[] componentsInChildren = ent.GetComponentsInChildren<Light>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                if (componentsInChildren[i].type == LightType.Directional)
                {
                    return false;
                }
            }
            if (ent.GetComponent<TileInstance>() != null)
            {
                return false;
            }
            if (ent.GetComponent<AtmosphereVolume>() != null)
            {
                return false;
            }
            LargeWorldEntity component = ent.GetComponent<LargeWorldEntity>();
            if ((bool)component && !component.IsCellManaged())
            {
                return false;
            }
            return true;
        }

        public void OnEntityMoved(UniqueIdentifier ent)
        {
            if (!Application.isPlaying)
            {
                if (!IsManagedEntity(ent))
                {
                    return;
                }
                if (IsCellManagedEntity(ent))
                {
                    RegisterEntity(ent.gameObject);
                    return;
                }
                RegisterBatchEntity(ent.gameObject);
                TileInstance component = ent.GetComponent<TileInstance>();
                if (!(component != null))
                {
                    return;
                }
                streamer.octCompiler.OnRangeChanged(component.blockBounds.Expanded(1));
                foreach (Int3 item in component.blockBounds.Expanded(1) / streamer.blocksPerTree)
                {
                    streamer.rootsToRecompile.Add(item);
                }
            }
            else
            {
                RegisterEntity(ent.gameObject);
            }
        }

        private void LoadLegacyCellsFromStream(Int3 batchId, Stream stream, BatchCells cells)
        {
            using BinaryReader binaryReader = new BinaryReader(stream);
            int num = binaryReader.ReadInt32();
            int num2 = binaryReader.ReadInt32();
            for (int i = 0; i < num2; i++)
            {
                Int3 legacyCellId = binaryReader.ReadInt3();
                int level = 0;
                if (num >= 5)
                {
                    level = binaryReader.ReadInt32();
                }
                Int3 cellId = BatchCells.GetCellId(legacyCellId, level, num);
                EntityCell entityCell = cells.Add(cellId, level);
                entityCell.Initialize();
                entityCell.LegacyRead(binaryReader, num);
                if (!Application.isPlaying && (streamer.world.loadingWindow || streamer.world.editingWindow) && streamer.world.batchWindow.Contains(batchId))
                {
                    using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
                    entityCell.Awake(pooledObject);
                }
            }
        }

        public void LoadSplitBatchCells(BatchCells cells, bool slotsOnly = false)
        {
            Utils.AssertEditMode();
            LoadSplitPass(cells, "BatchCells", "loot-slots");
            LoadSplitPass(cells, "BatchCells", "creature-slots");
            LoadSplitPass(cells, "BatchCells/Generated", "slots");
            if (!slotsOnly)
            {
                LoadSplitPass(cells, "BatchCells", "loot");
                LoadSplitPass(cells, "BatchCells", "creatures");
                LoadSplitPass(cells, "BatchCells", "other");
            }
            foreach (EntityCell item in cells.All())
            {
                item.EndLoad();
            }
        }

        private void LoadSplitPass(BatchCells cells, string prefix, string suffix)
        {
            Utils.AssertEditMode();
            string splitBatchCellsPath = GetSplitBatchCellsPath(streamer.pathPrefix, prefix, cells.batch, suffix);
            if (!FileUtils.FileExists(splitBatchCellsPath))
            {
                return;
            }
            using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
            ProtobufSerializer value = pooledObject.Value;
            using Stream stream = FileUtils.ReadFile(splitBatchCellsPath);
            CellsFileHeader cellsFileHeader = new CellsFileHeader();
            value.Deserialize(stream, cellsFileHeader, verbose: false);
            CellHeader cellHeader = new CellHeader();
            for (int i = 0; i < cellsFileHeader.numCells; i++)
            {
                value.Deserialize(stream, cellHeader, verbose: false);
                if (cellsFileHeader.version < 9 && cellHeader.level > 1)
                {
                    LoadLegacyPassFromStream(value, stream, cells, cellHeader.cellId, cellHeader.level, cellsFileHeader.version);
                    continue;
                }
                EntityCell entityCell = cells.Get(cellHeader.cellId, cellHeader.level);
                if (entityCell == null)
                {
                    entityCell = cells.Add(cellHeader.cellId, cellHeader.level);
                    entityCell.Initialize();
                    entityCell.Awake(value);
                    entityCell.BeginLoad();
                }
                entityCell.LoadPassFromStream(stream, value);
            }
        }

        private void LoadLegacyPassFromStream(ProtobufSerializer serializer, Stream stream, BatchCells cells, Int3 cellId, int level, int version)
        {
            Utils.AssertEditMode();
            EntityCell fromPool = EntityCell.GetFromPool(this, streamer, cells.batch, cellId, level);
            fromPool.Initialize();
            fromPool.Awake(serializer);
            fromPool.BeginLoad();
            Vector3 center = EntityCell.GetCenter(BatchCells.GetBlockBounds(cellSize: BatchCells.GetCellSize(level, streamer.blocksPerBatch, version), batchId: cells.batch, cellId: cellId, blocksPerBatch: streamer.blocksPerBatch));
            fromPool.liveRoot.transform.position = streamer.land.transform.TransformPoint(center);
            fromPool.LoadPassFromStream(stream, serializer);
            fromPool.ReregisterEntities();
            EntityCell.ReturnToPool(fromPool);
        }

        public void LoadEntitySlots(Int3 batchIndex)
        {
            Utils.AssertEditMode();
            try
            {
                if (!batch2cells.TryGetValue(batchIndex, out var value))
                {
                    value = BatchCells.GetFromPool(this, streamer, batchIndex);
                    batch2cells.Add(batchIndex, value);
                    Debug.LogFormat("Added batch cells {0} for LoadEntitySlots.", batchIndex);
                }
                LoadSplitBatchCells(value, slotsOnly: true);
            }
            catch
            {
                Debug.LogErrorFormat("Exception while loading entity slots for batch {0} - rethrowing.", batchIndex);
                throw;
            }
        }

        public void SaveEntitySlots(Int3 batchIndex)
        {
            Utils.AssertEditMode();
            if (batch2cells.TryGetValue(batchIndex, out var value))
            {
                SaveSplitBatchCells(value, slotsOnly: true);
            }
        }

        private void SaveCacheBatchCells(BatchCells cells, string targetPathPrefix, bool skipEmpty)
        {
            List<EntityCell> cells2 = cells.All().ToList();
            CoroutineUtils.PumpCoroutine(SaveCacheBatchCellsPhase1(cells2));
            SaveCacheBatchCellsPhase2Threaded(cells.batch, cells2, targetPathPrefix, skipEmpty);
        }

        private IEnumerator SaveCacheBatchCellsPhase1(ICollection<EntityCell> cells)
        {
            using PooledObject<ProtobufSerializer> serializerProxy = ProtobufSerializerPool.GetProxy();
            foreach (EntityCell cell in cells)
            {
                yield return cell.EnsureSerialDataSerialized(serializerProxy);
                yield return cell.EnsureWaiterDataSerialized(serializerProxy);
            }
        }

        private void SaveCacheBatchCellsPhase2Threaded(Int3 batchId, ICollection<EntityCell> cells, string targetPathPrefix, bool skipEmpty)
        {
            _ = StopwatchProfiler.Instance;
            string path = global::Platform.IO.Path.Combine(targetPathPrefix, "CellsCache");
            try
            {
                global::Platform.IO.Directory.CreateDirectory(path);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, streamer);
                return;
            }
            finally
            {
            }
            string cacheBatchCellsPath = GetCacheBatchCellsPath(targetPathPrefix, batchId);
            int count = cells.Count;
            if (skipEmpty && count == 0 && !global::Platform.IO.File.Exists(cacheBatchCellsPath))
            {
                return;
            }
            using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
            using Stream stream = FileUtils.CreateFile(cacheBatchCellsPath);
            CellsFileHeader cellsFileHeader = new CellsFileHeader();
            cellsFileHeader.version = 9;
            cellsFileHeader.numCells = count;
            pooledObject.Value.Serialize(stream, cellsFileHeader);
            CellHeaderEx cellHeaderEx = new CellHeaderEx();
            foreach (EntityCell cell in cells)
            {
                SerialData serialData = cell.GetSerialData();
                SerialData legacyData = cell.GetLegacyData();
                SerialData waiterData = cell.GetWaiterData();
                cellHeaderEx.cellId = cell.CellId;
                cellHeaderEx.level = cell.Level;
                cellHeaderEx.dataLength = serialData.Length;
                cellHeaderEx.legacyDataLength = legacyData.Length;
                cellHeaderEx.waiterDataLength = waiterData.Length;
                pooledObject.Value.Serialize(stream, cellHeaderEx);
                stream.Write(serialData.Data.Array, serialData.Data.Offset, serialData.Length);
                stream.Write(legacyData.Data.Array, legacyData.Data.Offset, legacyData.Length);
                stream.Write(waiterData.Data.Array, waiterData.Data.Offset, waiterData.Length);
                cell.ClearTempSerialData();
            }
        }

        public void SaveSplitBatchCells(BatchCells cells, bool slotsOnly = false)
        {
            Utils.AssertEditMode();
            SaveSplitPass(cells, "BatchCells", "loot-slots", IsLootSlot);
            SaveSplitPass(cells, "BatchCells", "creature-slots", IsCreatureSlot);
            SaveSplitPass(cells, "BatchCells/Generated", "slots", IsGeneratedSlot);
            if (!slotsOnly)
            {
                SaveSplitPass(cells, "BatchCells", "loot", IsLoot);
                SaveSplitPass(cells, "BatchCells", "creatures", IsCreature);
                SaveSplitPass(cells, "BatchCells", "other", IsOtherEntity);
            }
        }

        private void SaveSplitPass(BatchCells cells, string prefix, string suffix, Func<UniqueIdentifier, bool> predicate)
        {
            Utils.AssertEditMode();
            global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(streamer.pathPrefix, prefix));
            string splitBatchCellsPath = GetSplitBatchCellsPath(streamer.pathPrefix, prefix, cells.batch, suffix);
            int num = cells.All().Count((EntityCell c) => c.ContainsEntity(predicate));
            if (num == 0 && !global::Platform.IO.File.Exists(splitBatchCellsPath))
            {
                return;
            }
            using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
            using Stream stream = FileUtils.CreateFile(splitBatchCellsPath);
            CellsFileHeader cellsFileHeader = new CellsFileHeader();
            cellsFileHeader.version = 9;
            cellsFileHeader.numCells = num;
            pooledObject.Value.Serialize(stream, cellsFileHeader);
            CellHeader cellHeader = new CellHeader();
            foreach (EntityCell item in from c in cells.All()
                     where c.ContainsEntity(predicate)
                     select c)
            {
                cellHeader.cellId = item.CellId;
                cellHeader.level = item.Level;
                pooledObject.Value.Serialize(stream, cellHeader);
                item.SavePassToStream(stream, pooledObject, predicate);
            }
        }

        private void BakeAndSaveCacheBatchCells(BatchCells cells)
        {
            Utils.AssertEditMode();
            BatchCells fromPool = BatchCells.GetFromPool(this, streamer, cells.batch);
            using (PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy())
            {
                foreach (EntityCell item in cells.All())
                {
                    CoroutineUtils.PumpCoroutine(item.EnsureSerialDataSerialized(pooledObject));
                    CoroutineUtils.PumpCoroutine(item.EnsureWaiterDataSerialized(pooledObject));
                    SerialData serialData = item.GetSerialData();
                    SerialData legacyData = item.GetLegacyData();
                    SerialData waiterData = item.GetWaiterData();
                    EntityCell entityCell = fromPool.Add(item.CellId, item.Level);
                    entityCell.Initialize(serialData, legacyData, waiterData);
                    entityCell.Awake(pooledObject);
                }
            }
            Int3 cellSize = BatchCells.GetCellSize(3, streamer.blocksPerBatch);
            IEnumerable<EntitySlot> enumerable = from p in (from p in fromPool.All()
                    where p.liveRoot != null
                    select p).SelectMany((EntityCell p) => p.CollectEntities(IsSlot))
                select p.GetComponent<EntitySlot>();
            int num = 0;
            Dictionary<Int3, List<EntitySlot>> dictionary = new Dictionary<Int3, List<EntitySlot>>(Int3.equalityComparer);
            foreach (EntitySlot item2 in enumerable)
            {
                Vector3 position = item2.transform.position;
                Int3 block = streamer.GetBlock(position);
                _ = block / streamer.blocksPerBatch;
                Int3 key = block % streamer.blocksPerBatch / cellSize;
                dictionary.GetOrAddNew(key).Add(item2);
                num++;
            }
            GameObject original = Resources.Load<GameObject>("WorldEntities/Slots/EntitySlotsPlaceholder");
            int num2 = 0;
            using (PooledObject<ProtobufSerializer> pooledObject2 = ProtobufSerializerPool.GetProxy())
            {
                foreach (KeyValuePair<Int3, List<EntitySlot>> item3 in dictionary)
                {
                    Int3 key2 = item3.Key;
                    List<EntitySlot> value = item3.Value;
                    EntityCell entityCell2 = fromPool.EnsureCell(key2, 3);
                    entityCell2.EnsureAwake(pooledObject2);
                    entityCell2.EnsureRoot();
                    GameObject go = global::UnityEngine.Object.Instantiate(original, Vector3.zero, Quaternion.identity);
                    go.transform.SetParent(entityCell2.liveRoot.transform, worldPositionStays: false);
                    go.GetComponent<EntitySlotsPlaceholder>().slotsData = value.Select((EntitySlot p) => EntitySlotData.Create(go.transform, p)).ToArray();
                    num2++;
                    foreach (EntitySlot item4 in value)
                    {
                        global::UnityEngine.Object.DestroyImmediate(item4.gameObject);
                    }
                }
            }
            int num3 = ((num2 > 0) ? (num / num2) : 0);
            Debug.LogFormat("Average slots fill rate {0} ({1} slots / {2} placeholders)", num3, num, num2);
            SaveCacheBatchCells(fromPool, streamer.pathPrefix, skipEmpty: true);
            BatchCells.ReturnToPool(fromPool);
        }

        private void LoadLegacyBatchCells(BatchCells cells)
        {
            string batchCellsPath = GetBatchCellsPath(streamer.pathPrefix, cells.batch);
            string batchCellsPath2 = GetBatchCellsPath(streamer.fallbackPrefix, cells.batch);
            if (FileUtils.FileExists(batchCellsPath))
            {
                LoadLegacyCellsFromStream(cells.batch, FileUtils.ReadFile(batchCellsPath), cells);
            }
            else if (FileUtils.FileExists(batchCellsPath2))
            {
                LoadLegacyCellsFromStream(cells.batch, FileUtils.ReadFile(batchCellsPath2), cells);
            }
        }

        public bool TryLoadCacheBatchCells(BatchCells cells)
        {
            string cacheBatchCellsPath = GetCacheBatchCellsPath(streamer.tmpPathPrefix, cells.batch);
            string cacheBatchCellsPath2 = GetCacheBatchCellsPath(streamer.pathPrefix, cells.batch);
            string cacheBatchCellsPath3 = GetCacheBatchCellsPath(streamer.fallbackPrefix, cells.batch);
            using (Stream stream = global::UWE.Utils.TryOpenEither(cacheBatchCellsPath, cacheBatchCellsPath2, cacheBatchCellsPath3))
            {
                if (stream == null)
                {
                    return false;
                }
                LoadCacheBatchCellsFromStream(cells, stream);
            }
            return true;
        }

        public static void LoadCacheBatchCellsFromStream(BatchCells cells, Stream stream)
        {
            using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
            CellsFileHeader cellsFileHeader = new CellsFileHeader();
            pooledObject.Value.Deserialize(stream, cellsFileHeader, verbose: false);
            CellHeaderEx cellHeaderEx = new CellHeaderEx();
            for (int i = 0; i < cellsFileHeader.numCells; i++)
            {
                pooledObject.Value.Deserialize(stream, cellHeaderEx, verbose: false);
                Int3 cellId = BatchCells.GetCellId(cellHeaderEx.cellId, cellHeaderEx.level, cellsFileHeader.version);
                EntityCell entityCell = cells.Add(cellId, cellHeaderEx.level);
                entityCell.Initialize();
                entityCell.ReadSerialDataFromStream(stream, cellHeaderEx.dataLength);
                if (cellsFileHeader.version < 9)
                {
                    if (cellHeaderEx.level > 1)
                    {
                        entityCell.MoveSerialDataToLegacy();
                    }
                }
                else
                {
                    entityCell.ReadLegacyDataFromStream(stream, cellHeaderEx.legacyDataLength);
                    entityCell.ReadWaiterDataFromStream(stream, cellHeaderEx.waiterDataLength);
                }
            }
        }

        public BatchCells InitializeBatchCells(Int3 index)
        {
            ProfilingUtils.BeginSample("InitializeBatchCells");
            if (batch2cells.ContainsKey(index))
            {
                Debug.LogWarningFormat("BatchCells {0} already loaded. Reloading...", index);
                UnloadBatchCells(index);
            }
            BatchCells fromPool = BatchCells.GetFromPool(this, streamer, index);
            batch2cells[index] = fromPool;
            ProfilingUtils.EndSample();
            return fromPool;
        }

        public void LoadBatchCellsThreaded(BatchCells cells, bool editMode)
        {
            ProfilingUtils.BeginSample("LoadBatchCells");
            Int3 batch = cells.batch;
            if (!editMode)
            {
                if (!TryLoadCacheBatchCells(cells) && !streamer.debugBakedBatchCells)
                {
                    LoadLegacyBatchCells(cells);
                }
            }
            else if (streamer.debugBakedBatchCells)
            {
                TryLoadCacheBatchCells(cells);
                using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
                foreach (EntityCell item in cells.All())
                {
                    item.Awake(pooledObject);
                }
            }
            else if (FileUtils.FileExists(GetBatchCellsPath(streamer.pathPrefix, batch)))
            {
                LoadLegacyBatchCells(cells);
            }
            else
            {
                LoadSplitBatchCells(cells);
            }
            ProfilingUtils.EndSample();
            if (streamer.debugBatchCells)
            {
                Debug.LogFormat("loaded cells for batch {0}", batch);
            }
        }

        public bool IsProcessingBatchCells(Int3 index)
        {
            if (processingCell != null)
            {
                return processingCell.BatchId == index;
            }
            return false;
        }

        public IEnumerator SaveBatchCellsTmpAsync(Int3 index)
        {
            if (!batch2cells.TryGetValue(index, out var cells))
            {
                yield break;
            }
            cells.RemoveEmpty();
            List<EntityCell> allCells = cells.All().ToList();
            using (PooledObject<ProtobufSerializer> serializerProxy = ProtobufSerializerPool.GetProxy())
            {
                foreach (EntityCell item in allCells)
                {
                    yield return item.EnsureSleepAsync(serializerProxy);
                }
            }
            yield return SaveCacheBatchCellsPhase1(allCells);
            yield return WorkerTask.Launch(delegate
            {
                SaveCacheBatchCellsPhase2Threaded(cells.batch, allCells, streamer.tmpPathPrefix, skipEmpty: false);
            });
        }

        public void SaveBatchCells(Int3 index, bool cacheOnly = false, bool skipCleanUp = false)
        {
            Utils.AssertEditMode();
            if (!batch2cells.TryGetValue(index, out var value))
            {
                return;
            }
            value.RemoveEmpty();
            if (!cacheOnly)
            {
                SaveSplitBatchCells(value);
            }
            Bounds wsBounds = global::UWE.Utils.MinMaxBounds(streamer.GetBatchMins(index), streamer.GetBatchMaxs(index));
            BakeTileInstanceEntities(index - 1, index + 1, wsBounds);
            BakeAndSaveCacheBatchCells(value);
            if (!skipCleanUp)
            {
                foreach (EntityCell item in value.All())
                {
                    item.DestroyTileInstanceEntities();
                }
            }
            string batchCellsPath = GetBatchCellsPath(streamer.pathPrefix, value.batch);
            if (global::Platform.IO.File.Exists(batchCellsPath))
            {
                global::Platform.IO.File.Delete(batchCellsPath);
            }
        }

        public void UnloadBatchCells(Int3 index)
        {
            if (batch2cells.TryGetValue(index, out var value))
            {
                BatchCells.ReturnToPool(value);
                batch2cells.Remove(index);
            }
        }

        public void ShowEntities(Int3.Bounds blockRange)
        {
        }

        public void HideEntities(Int3.Bounds blockRange)
        {
        }

        public void ShowEntities(Int3.Bounds blockRange, int level)
        {
            if (level > 3 || !streamer.IsReady() || streamer.debugDisableAllEnts)
            {
                return;
            }
            Int3.Bounds coarseBounds = Int3.Bounds.OuterCoarserBounds(blockRange, streamer.blocksPerBatch);
            if (streamer.debugBatchCells)
            {
                Int3.Bounds.FinerBounds(coarseBounds, streamer.blocksPerBatch);
            }
            Int3.RangeEnumerator enumerator = coarseBounds.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int3 current = enumerator.Current;
                if (batch2cells.TryGetValue(current, out var value))
                {
                    Int3 @int = current * streamer.blocksPerBatch;
                    Int3.Bounds bsRange = (blockRange - @int).Clamp(Int3.zero, streamer.blocksPerBatch - 1);
                    value.QueueForAwake(bsRange, level, cellManagementQueue);
                }
            }
        }

        public void HideEntities(Int3.Bounds blockRange, int level)
        {
            if (level > 3)
            {
                return;
            }
            Int3.Bounds coarseBounds = Int3.Bounds.OuterCoarserBounds(blockRange, streamer.blocksPerBatch);
            if (streamer.debugBatchCells)
            {
                Int3.Bounds.FinerBounds(coarseBounds, streamer.blocksPerBatch);
            }
            Int3.RangeEnumerator enumerator = coarseBounds.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int3 current = enumerator.Current;
                if (batch2cells.TryGetValue(current, out var value))
                {
                    Int3 @int = current * streamer.blocksPerBatch;
                    Int3.Bounds bsRange = (blockRange - @int).Clamp(Int3.zero, streamer.blocksPerBatch - 1);
                    value.QueueForSleep(bsRange, level, cellManagementQueue);
                }
            }
        }

        public void QueueForAwake(EntityCell cell)
        {
            cell.QueueForAwake(cellManagementQueue);
        }

        public void QueueForSleep(EntityCell cell)
        {
            cell.QueueForSleep(cellManagementQueue);
        }

        public void ResetEntityDistributions()
        {
            if (spawner != null)
            {
                spawner.ResetSpawner();
            }
        }

        public Int3 GetGlobalCell(Vector3 wsPos, int cellLevel)
        {
            Int3 block = streamer.GetBlock(wsPos);
            Int3 cellSize = BatchCells.GetCellSize(cellLevel, streamer.blocksPerBatch);
            return block / cellSize;
        }

        public EntitySlot.Filler GetPrefabForSlot(IEntitySlot slot)
        {
            if (spawner != null && !streamer.debugDisableSlotEnts)
            {
                return spawner.GetPrefabForSlot(slot);
            }
            return default(EntitySlot.Filler);
        }

        private void BakeEntityCell(EntityCell c)
        {
            Utils.AssertEditMode();
            if (!streamer.debugDisableAllEnts)
            {
                ProfilingUtils.BeginSample("BakeEntityCell");
                if (!streamer.debugDisableInstanceEnts)
                {
                    ProfilingUtils.BeginSample("Instance Ents");
                    Int3.Bounds blockBounds = c.GetBlockBounds();
                    Int3 minBatch = blockBounds.mins / streamer.blocksPerBatch;
                    Int3 maxBatch = blockBounds.maxs / streamer.blocksPerBatch;
                    Vector3 mins = streamer.land.transform.TransformPoint(blockBounds.mins.ToVector3());
                    Vector3 maxs = streamer.land.transform.TransformPoint((blockBounds.maxs + 1).ToVector3());
                    Bounds wsBounds = global::UWE.Utils.MinMaxBounds(mins, maxs);
                    minBatch -= 1;
                    maxBatch += 1;
                    BakeTileInstanceEntities(minBatch, maxBatch, wsBounds);
                    ProfilingUtils.EndSample();
                }
                ProfilingUtils.EndSample();
            }
        }

        private void BakeTileInstanceEntities(Int3 minBatch, Int3 maxBatch, Bounds wsBounds)
        {
            Utils.AssertEditMode();
            int num = 0;
            ProfilingUtils.BeginSample("iteration");
            foreach (Int3 item in Int3.Range(minBatch, maxBatch))
            {
                if (streamer.batch2insts.TryGetValue(item, out var value))
                {
                    for (int i = 0; i < value.Count; i++)
                    {
                        ProfilingUtils.BeginSample("ti spawn ents");
                        value[i].SpawnPlayModeEntities(wsBounds);
                        num++;
                        ProfilingUtils.EndSample();
                    }
                }
            }
            ProfilingUtils.EndSample();
        }

        public IEnumerable<EntityCell> LoadedCells()
        {
            Utils.AssertEditMode();
            foreach (Int3 item in streamer.LoadedBatches())
            {
                if (!batch2cells.TryGetValue(item, out var value))
                {
                    continue;
                }
                foreach (EntityCell item2 in value.All())
                {
                    yield return item2;
                }
            }
        }

        public void SaveAllBatchCells()
        {
            Debug.LogFormat("Saving {0} batches to {1}", batch2cells.Count, streamer.tmpPathPrefix);
            foreach (KeyValuePair<Int3, BatchCells> batch2cell in batch2cells)
            {
                SaveCacheBatchCells(batch2cell.Value, streamer.tmpPathPrefix, skipEmpty: true);
            }
        }

        public void HideAllEntityRenderers()
        {
            Utils.AssertEditMode();
            foreach (EntityCell item in LoadedCells())
            {
                item.SetRenderersEnabled(val: false);
            }
            foreach (TileInstance item2 in streamer.LoadedInstances())
            {
                item2.SetPreviewRenderersEnabled(val: false);
            }
        }

        public void ShowAllEntityRenderers()
        {
            Utils.AssertEditMode();
            foreach (EntityCell item in LoadedCells())
            {
                item.SetRenderersEnabled(val: true);
            }
            foreach (TileInstance item2 in streamer.LoadedInstances())
            {
                item2.SetPreviewRenderersEnabled(val: true);
            }
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public void CollectMemoryUsageStats()
        {
            List<BatchCells> list = new List<BatchCells>();
            List<int> list2 = new List<int>();
            List<int> list3 = new List<int>();
            int num = 0;
            foreach (KeyValuePair<Int3, BatchCells> batch2cell in batch2cells)
            {
                int num2 = batch2cell.Value.EstimateBytes();
                int item = batch2cell.Value.NumCellsWithData();
                list.Add(batch2cell.Value);
                list2.Add(num2);
                list3.Add(item);
                num += num2;
            }
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("BatchCellsID,MemoryUsage(bytes),CellsWithData");
            for (int i = 0; i < list.Count; i++)
            {
                stringBuilder.AppendFormat("{0},{1},{2}\n", list[i].batch.ToCsvString(), list2[i], list3[i]);
            }
            stringBuilder.AppendFormat(",{0},", num);
            Debug.Log(stringBuilder.ToString());
        }

        public int EstimateBytes()
        {
            Timer.Begin("CellManager::EstimateBytes");
            ProfilingUtils.BeginSample("CellManager::EstimateBytes");
            int num = 8;
            num += 256;
            num += 70;
            foreach (KeyValuePair<Int3, BatchCells> batch2cell in batch2cells)
            {
                num += 20;
                num += 4 + batch2cell.Value.EstimateBytes();
            }
            ProfilingUtils.EndSample();
            Timer.End();
            return num;
        }

        public static bool IsSlotsFile(string fname)
        {
            if (!fname.Contains("loot-slots") && !fname.Contains("creature-slots"))
            {
                return fname.Contains("slots");
            }
            return true;
        }

        private static bool TryGetSlot(UniqueIdentifier uid, out EntitySlot slot)
        {
            if (IsSpecialIdentifier(uid))
            {
                slot = null;
                return false;
            }
            slot = uid.GetComponent<EntitySlot>();
            return slot != null;
        }

        private static bool IsSlot(UniqueIdentifier uid)
        {
            EntitySlot slot;
            return TryGetSlot(uid, out slot);
        }

        private static bool IsLootSlot(UniqueIdentifier uid)
        {
            if (!TryGetSlot(uid, out var slot))
            {
                return false;
            }
            if (slot.autoGenerated)
            {
                return false;
            }
            return !slot.IsTypeAllowed(EntitySlot.Type.Creature);
        }

        private static bool IsCreatureSlot(UniqueIdentifier uid)
        {
            if (!TryGetSlot(uid, out var slot))
            {
                return false;
            }
            if (slot.autoGenerated)
            {
                return false;
            }
            return slot.IsTypeAllowed(EntitySlot.Type.Creature);
        }

        private static bool IsGeneratedSlot(UniqueIdentifier uid)
        {
            if (!TryGetSlot(uid, out var slot))
            {
                return false;
            }
            return slot.autoGenerated;
        }

        private static bool IsLoot(UniqueIdentifier uid)
        {
            if (IsSpecialIdentifier(uid))
            {
                return false;
            }
            EntityTag component = uid.GetComponent<EntityTag>();
            if (!component)
            {
                return false;
            }
            return component.slotType != EntitySlot.Type.Creature;
        }

        private static bool IsCreature(UniqueIdentifier uid)
        {
            if (IsSpecialIdentifier(uid))
            {
                return false;
            }
            EntityTag component = uid.GetComponent<EntityTag>();
            if (!component)
            {
                return false;
            }
            return component.slotType == EntitySlot.Type.Creature;
        }

        private static bool IsOtherEntity(UniqueIdentifier uid)
        {
            if (IsSpecialIdentifier(uid))
            {
                return false;
            }
            if ((bool)uid.GetComponent<LargeWorldEntityCell>())
            {
                return false;
            }
            if ((bool)uid.GetComponent<EntitySlot>())
            {
                return false;
            }
            if ((bool)uid.GetComponent<EntityTag>())
            {
                return false;
            }
            return true;
        }

        private static bool IsSpecialIdentifier(UniqueIdentifier uid)
        {
            if (uid is SceneObjectIdentifier)
            {
                return true;
            }
            if (uid is ChildObjectIdentifier)
            {
                return true;
            }
            if (uid is TemporaryObjectIdentifier)
            {
                return true;
            }
            return false;
        }
    }
}
