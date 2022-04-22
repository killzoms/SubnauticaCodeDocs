using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using AssemblyCSharp.WorldStreaming;
using Gendarme;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(LargeWorld))]
    public class LargeWorldStreamer : MonoBehaviour, VoxelandEventHandler
    {
        [Serializable]
        public sealed class Settings
        {
            public int maxFrameMs = 10;

            public int maxSubFrameMs = 10;

            public int maxInsideFrameMs = 10;

            public int maxLoadingFrameMs = 100;

            public int maxFrameMsToPreventNewEntities = 1000000;

            public bool overrideDebugSkipEntities;

            public float budgetMBs = 200f;

            public bool warmupAllShaders;

            public int batchLoadRings = 1;

            public BatchOctreesStreamer.Settings octreesSettings;

            public BatchOctreesStreamer.Settings lowDetailOctreesSettings;

            public bool disableFarColliders;

            public int GetMaxFrameMs(Player player)
            {
                if (uGUI.isLoading)
                {
                    return maxLoadingFrameMs;
                }
                if ((bool)player)
                {
                    if (player.IsPiloting())
                    {
                        return maxSubFrameMs;
                    }
                    if (player.IsInsideWalkable())
                    {
                        return maxInsideFrameMs;
                    }
                }
                return maxFrameMs;
            }

            public override string ToString()
            {
                return $"max frame ms {maxFrameMs}, skip ents {overrideDebugSkipEntities}, budget {budgetMBs}";
            }
        }

        public struct CompactOctreeSaveItem
        {
            public Int3 index;

            public CompactOctree octree;

            public CompactOctreeSaveItem(Int3 _index, CompactOctree _octree)
            {
                index = _index;
                octree = _octree;
            }
        }

        private sealed class BatchObjectFileSaveTask : IDisposable
        {
            [SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule")]
            public MemoryStream intermediateSaveBuffer = new MemoryStream();

            private string filePath;

            [SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule")]
            private Stream outputfileStream;

            public bool isDoneCreatingFile { get; private set; }

            public bool isDoneSavingFile { get; private set; }

            public BatchObjectFileSaveTask(string inFilePath)
            {
                isDoneCreatingFile = false;
                isDoneSavingFile = false;
                filePath = inFilePath;
            }

            public void BeginCreateFile()
            {
                outputfileStream = FileUtils.CreateFile(filePath);
                isDoneCreatingFile = true;
            }

            public void BeginWriteBufferToFile()
            {
                outputfileStream.SetLength(intermediateSaveBuffer.Length);
                intermediateSaveBuffer.WriteTo(outputfileStream);
                outputfileStream.Close();
                outputfileStream = null;
                intermediateSaveBuffer.Close();
                intermediateSaveBuffer = null;
                isDoneSavingFile = true;
            }

            public void Dispose()
            {
                if (intermediateSaveBuffer != null)
                {
                    intermediateSaveBuffer.Close();
                }
                if (outputfileStream != null)
                {
                    outputfileStream.Close();
                }
            }
        }

        private sealed class BatchObjectFileSavingThread
        {
            public enum State
            {
                Running,
                Dead,
                NotRunning
            }

            private State currentState = State.NotRunning;

            private Thread internalThread;

            private readonly LocklessQueueSPMC<BatchObjectFileSaveTask> createFileTasks = new LocklessQueueSPMC<BatchObjectFileSaveTask>();

            private readonly LocklessQueueSPMC<BatchObjectFileSaveTask> saveBufferToFileTasks = new LocklessQueueSPMC<BatchObjectFileSaveTask>();

            private bool quitLoop;

            private int consoleCoreAffinityMask;

            public void CreateFileForTaskAsync(BatchObjectFileSaveTask inCreateFileTask)
            {
                if (!Application.isPlaying)
                {
                    inCreateFileTask.BeginCreateFile();
                }
                else
                {
                    createFileTasks.Push(inCreateFileTask);
                }
            }

            public void SaveIntermediateBufferToFile(BatchObjectFileSaveTask inCreateFileTask)
            {
                if (!Application.isPlaying)
                {
                    inCreateFileTask.BeginWriteBufferToFile();
                }
                else
                {
                    saveBufferToFileTasks.Push(inCreateFileTask);
                }
            }

            public void StartThread()
            {
                internalThread = new Thread(Main);
                internalThread.IsBackground = true;
                internalThread.Start();
                currentState = State.Running;
            }

            public void StopThread()
            {
                quitLoop = true;
                createFileTasks.Clear();
                saveBufferToFileTasks.Clear();
                while (currentState == State.Running)
                {
                    Thread.Sleep(1);
                }
            }

            public void Main()
            {
                global::Platform.Utils.ThreadUtils.SetThreadName("LargeWorldStreamer ObjBatchSave thread");
                global::Platform.Utils.ThreadUtils.SetThreadPriority(System.Threading.ThreadPriority.Lowest);
                global::Platform.Utils.ThreadUtils.SetThreadAffinityMask(-4);
                try
                {
                    MainLoop();
                    global::UnityEngine.Profiling.Profiler.EndThreadProfiling();
                }
                catch (Exception ex)
                {
                    global::UnityEngine.Debug.LogWarningFormat("BathObjectFileSavingThread is Dead due to exception: {0}!", ex);
                }
                finally
                {
                    currentState = State.Dead;
                }
            }

            private void MainLoop()
            {
                while (!quitLoop)
                {
                    if (createFileTasks.Pop(out var outItem))
                    {
                        outItem.BeginCreateFile();
                    }
                    if (saveBufferToFileTasks.Pop(out var outItem2))
                    {
                        outItem2.BeginWriteBufferToFile();
                    }
                    Thread.Sleep(1);
                }
            }
        }

        public class UpdateBatchStreamingCoroutine : StateMachineBase<LargeWorldStreamer>
        {
            private Int3 best;

            private Int3 worst;

            public override bool MoveNext()
            {
                ProfilingUtils.BeginSample("UpdateBatchStreaming");
                try
                {
                    switch (state)
                    {
                        case 0:
                        {
                            if (host.frozen)
                            {
                                return false;
                            }
                            ProfilingUtils.BeginSample("Compute loaded MBs");
                            host.loadedMBsOut = (float)VoxelandData.OctNode.GetPoolBytesUsed() / 1024f / 1024f;
                            ProfilingUtils.EndSample();
                            ProfilingUtils.BeginSample("Compute batch bounds");
                            Vector3 vector = new Vector3(0f, -8f, 0f);
                            Vector3 vector2 = host.cachedCameraPosition + vector;
                            Int3 containingBatch = host.GetContainingBatch(vector2);
                            Int3 containingTree = host.GetContainingTree(vector2);
                            if (host.octCompiler != null)
                            {
                                host.octCompiler.UpdateViewerOctree(containingTree);
                            }
                            ProfilingUtils.EndSample();
                            host.isIdle = false;
                            Int3.Bounds effectiveBounds = host.GetEffectiveBounds(containingBatch);
                            if (host.TryGetWorstBatch(vector2, effectiveBounds, out worst))
                            {
                                if (host.TryUnloadBatch(worst))
                                {
                                    if (host.octCompiler != null)
                                    {
                                        host.octCompiler.debugFreeze = true;
                                    }
                                    if (host.streamerV2 != null)
                                    {
                                        host.streamerV2.IncreaseFreezeCount();
                                    }
                                    current = host.cellManager.IncreaseFreezeCount();
                                    state = 1;
                                    host.debugNumBatchesUnloading++;
                                    return true;
                                }
                            }
                            else
                            {
                                if (host.TryGetBestBatch(vector2, effectiveBounds, out best))
                                {
                                    BatchCells batchCells = host.cellManager.InitializeBatchCells(best);
                                    current = host.LoadBatchThreadedAsync(batchCells, !Application.isPlaying);
                                    state = 3;
                                    host.debugNumBatchesLoading++;
                                    return true;
                                }
                                host.isIdle = true;
                            }
                            return false;
                        }
                        case 1:
                            current = host.SaveBatchTmpAsync(worst);
                            state = 2;
                            return true;
                        case 2:
                            host.UnloadBatch(worst);
                            host.cellManager.DecreaseFreezeCount();
                            if (host.streamerV2 != null)
                            {
                                host.streamerV2.DecreaseFreezeCount();
                            }
                            if (host.octCompiler != null)
                            {
                                host.octCompiler.debugFreeze = false;
                            }
                            host.debugNumBatchesUnloading--;
                            return false;
                        case 3:
                            current = host.FinalizeLoadBatchAsync(best, !Application.isPlaying);
                            state = 4;
                            host.debugNumBatchesLoading--;
                            return true;
                        case 4:
                            return false;
                        default:
                            global::UnityEngine.Debug.LogErrorFormat(host, "Unexpected state {0} in UpdateBatchStreamingCoroutine", state);
                            return false;
                    }
                }
                finally
                {
                    ProfilingUtils.EndSample();
                }
            }

            public override void Reset()
            {
                best = Int3.zero;
                worst = Int3.zero;
            }
        }

        private sealed class LoadBatchLowDetailTask : IWorkerTask, IAsyncOperation
        {
            private readonly LargeWorldStreamer streamer;

            private readonly Int3 batch;

            private bool done;

            public bool isDone => done;

            public LoadBatchLowDetailTask(LargeWorldStreamer streamer, Int3 batch)
            {
                this.streamer = streamer;
                this.batch = batch;
            }

            public void Execute()
            {
                try
                {
                    streamer.LoadBatchLowDetailThreaded(batch);
                }
                catch (Exception exception)
                {
                    global::UnityEngine.Debug.LogException(exception, streamer);
                }
                finally
                {
                    done = true;
                }
            }

            public override string ToString()
            {
                return $"LoadBatchLowDetailTask {batch}";
            }
        }

        private sealed class LoadBatchTask : IWorkerTask, IAsyncOperation
        {
            private readonly LargeWorldStreamer streamer;

            private readonly BatchCells batchCells;

            private readonly bool editMode;

            private bool done;

            public bool isDone => done;

            public LoadBatchTask(LargeWorldStreamer streamer, BatchCells batchCells, bool editMode)
            {
                this.streamer = streamer;
                this.batchCells = batchCells;
                this.editMode = editMode;
            }

            public void Execute()
            {
                try
                {
                    streamer.LoadBatchThreaded(batchCells, editMode);
                }
                catch (Exception exception)
                {
                    global::UnityEngine.Debug.LogException(exception, streamer);
                }
                finally
                {
                    done = true;
                }
            }

            public override string ToString()
            {
                return $"LoadBatchTask {batchCells.batch}";
            }
        }

        public class RasterProxy : VoxelandRasterizer
        {
            private LargeWorldStreamer streamer;

            private readonly object statsMutex = new object();

            private readonly Dictionary<int, int> downsampleStats = new Dictionary<int, int>();

            private readonly Dictionary<int, int> fullyLoadedDownsampleStats = new Dictionary<int, int>();

            private VoxelandData compiledVoxels => streamer.compiledVoxels;

            public RasterProxy(LargeWorldStreamer streamer)
            {
                this.streamer = streamer;
            }

            public void LayoutDebugGUI()
            {
                lock (statsMutex)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("Downsample Stats");
                    foreach (KeyValuePair<int, int> downsampleStat in downsampleStats)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(downsampleStat.Key.ToString());
                        GUILayout.TextArea(downsampleStat.Value.ToString());
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label("Fully Loaded Downsample Stats");
                    foreach (KeyValuePair<int, int> fullyLoadedDownsampleStat in fullyLoadedDownsampleStats)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(fullyLoadedDownsampleStat.Key.ToString());
                        GUILayout.TextArea(fullyLoadedDownsampleStat.Value.ToString());
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }

            public void Rasterize(Voxeland land, Array3<byte> typesGrid, Array3<byte> densityGrid, Int3 size, int wx, int wy, int wz, int downsamples)
            {
                if (streamer.octCompiler != null)
                {
                    streamer.octCompiler.WaitForRequests(null);
                }
                int x = wx + (size.x << downsamples) - 1;
                int y = wy + (size.y << downsamples) - 1;
                int z = wz + (size.z << downsamples) - 1;
                Int3 @int = new Int3(wx, wy, wz);
                Int3 int2 = new Int3(x, y, z);
                foreach (Int3 item in Int3.Range(Int3.Max(Int3.zero, @int / 32), Int3.Min(streamer.compactTrees.Dims() - 1, int2 / 32)))
                {
                    streamer.compactTrees.Get(item)?.RasterizeNative(0, typesGrid, densityGrid, size, wx >> downsamples, wy >> downsamples, wz >> downsamples, item.x * 32 >> downsamples, item.y * 32 >> downsamples, item.z * 32 >> downsamples, 32 >> downsamples + 1);
                }
            }

            public void CheckRangeCompiled(Int3.Bounds blockRange)
            {
            }

            public bool IsRangeUniform(Int3.Bounds blockRange)
            {
                CheckRangeCompiled(blockRange);
                foreach (Int3 item in Int3.Range(blockRange.mins / streamer.blocksPerTree, blockRange.maxs / streamer.blocksPerTree))
                {
                    if (streamer.compactTrees.CheckBounds(item) && streamer.compactTrees.Get(item) != null && !streamer.compactTrees.Get(item).IsEmpty())
                    {
                        return false;
                    }
                }
                return true;
            }

            public bool IsRangeLoaded(Int3.Bounds blockRange, int downsamples)
            {
                if (Application.isPlaying && !streamer.debugDisableFastRangeLookup)
                {
                    return streamer.octCompiler.IsRangeCompiled(blockRange.mins / compiledVoxels.biggestNode, blockRange.maxs / compiledVoxels.biggestNode);
                }
                Int3.RangeEnumerator rangeEnumerator = Int3.Range(blockRange.mins / compiledVoxels.biggestNode, blockRange.maxs / compiledVoxels.biggestNode);
                while (rangeEnumerator.MoveNext())
                {
                    Int3 current = rangeEnumerator.Current;
                    if (!streamer.debugDoNotBuildOutOfWindow)
                    {
                        Int3 p = current / streamer.treesPerBatch;
                        if (streamer.world.editingWindow && !streamer.world.batchWindow.Contains(p))
                        {
                            continue;
                        }
                    }
                    if (!streamer.octCompiler.IsRootCompiled(current, downsamples >= 4))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void OnPreBuildRange(Int3.Bounds blockRange)
            {
                if (Application.isPlaying)
                {
                    return;
                }
                ProfilingUtils.BeginSample("OnPreBuildRange");
                if (streamer.rootsToRecompile.Count > 0)
                {
                    foreach (Int3 item in streamer.rootsToRecompile)
                    {
                        streamer.octCompiler.RequestCompile(item);
                    }
                    streamer.octCompiler.WaitForRequests(null);
                    streamer.UpdateAllInstances();
                    streamer.rootsToRecompile.Clear();
                }
                ProfilingUtils.BeginSample("Request Compiles");
                foreach (Int3 item2 in Int3.Range(blockRange.mins / compiledVoxels.biggestNode, blockRange.maxs / compiledVoxels.biggestNode))
                {
                    streamer.octCompiler.RequestCompile(item2);
                }
                ProfilingUtils.EndSample();
                ProfilingUtils.BeginSample("Wait for compiles");
                streamer.octCompiler.WaitForRequests(null);
                ProfilingUtils.EndSample();
                ProfilingUtils.EndSample();
            }
        }

        public delegate float DistanceField(Vector3 wsPos);

        [AssertNotNull]
        public GameObject cellRootPrefab;

        [AssertNotNull]
        public GameObject batchRootPrefab;

        [AssertNotNull]
        public GameObject chunkLayerPrefab;

        private const int IndexVersion = 0;

        private const int HeightMapVersion = 0;

        private const int CompiledOctreesVersion = 4;

        public const string CompiledCacheFolder = "CompiledOctreesCache";

        public const string BatchObjectsCacheFolder = "BatchObjectsCache";

        public const string BatchObjectsFolder = "BatchObjects";

        public const string BatchOctreesFolder = "BatchOctrees";

        public const string BatchHeightmapFolder = "BatchHeightmap";

        [NonSerialized]
        public CellManager cellManager;

        [NonSerialized]
        public bool editMode;

        [NonSerialized]
        public bool debugSkipTerrain;

        public static LargeWorldStreamer main;

        [NonSerialized]
        public LargeWorld world;

        public float loadedMBsOut;

        public bool verbose;

        public bool debugBatchCells;

        public bool debugBakedBatchCells;

        public bool debugDisableAllEnts;

        public bool debugDisableInstanceEnts;

        public bool debugDisableProceduralEnts;

        public bool debugDisableSlotEnts;

        public bool debugKeepGroupRoots;

        public bool debugDisableLowDetailTerrain;

        public bool debugBiomeMaterials;

        public int debugBiomeDithering = 1;

        public bool debugDoNotBuildOutOfWindow;

        public bool debugDirtyRoots;

        public bool debugNoInGameCompiling;

        public bool debugDisableInstances;

        public bool debugSkipEntityLoad;

        public bool overrideDisableGrass;

        public bool debugDrawLoadedBatches;

        public bool debugDrawNonEmptyRoots;

        public Color debugDrawLoadedBatchesColor = Color.blue;

        public Color debugDrawNonEmptyRootsColor = Color.blue;

        public int debugNumBatchesLoading;

        public int debugNumBatchesUnloading;

        public bool debugDisableFastRangeLookup;

        public bool debugSkipCppFaceScan;

        private bool inited;

        private bool isIdle;

        private BatchObjectFileSavingThread batchObjSavingThread;

        public AnimationCurve warmupOctreeByteArrayLarge;

        [NonSerialized]
        public bool frozen;

        private readonly HashSet<Int3> lockedBatches = new HashSet<Int3>(Int3.equalityComparer);

        private readonly HashSet<Int3> loadedBatches = new HashSet<Int3>(Int3.equalityComparer);

        [NonSerialized]
        public GameObject transientRoot;

        [NonSerialized]
        public GameObject globalRoot;

        [AssertNotNull]
        public Transform batchesRoot;

        [AssertNotNull]
        public Transform cellsRoot;

        [AssertNotNull]
        public Transform waitersRoot;

        [NonSerialized]
        public Dictionary<Int3, LargeWorldBatchRoot> batch2root = new Dictionary<Int3, LargeWorldBatchRoot>(Int3.equalityComparer);

        public Dictionary<Int3, List<TileInstance>> batch2insts = new Dictionary<Int3, List<TileInstance>>(Int3.equalityComparer);

        [HideInInspector]
        public int maxInstanceLayer;

        [HideInInspector]
        public int minInstanceLayer;

        [NonSerialized]
        public VoxelandData data;

        [NonSerialized]
        public VoxelandData compiledVoxels;

        [NonSerialized]
        public VoxelandData bakedVoxels;

        [HideInInspector]
        public Voxeland land;

        private RasterProxy proxy;

        [NonSerialized]
        public WorldStreamer streamerV2;

        public Array3<CompactOctree> compactTrees;

        [NonSerialized]
        public LargeWorldOctreeCompiler octCompiler;

        private WorkerThread workerThread;

        public Int3 treesPerBatch;

        private Int3 nodeCount;

        public byte DNEType = 1;

        [NonSerialized]
        private Settings settings;

        private readonly List<CompactOctreeSaveItem> compactOctreeBuffer = new List<CompactOctreeSaveItem>();

        private readonly ObjectPool<CompactOctree> compactOctreePool = ObjectPoolHelper.CreatePool<CompactOctree>(20000);

        private static readonly StateMachinePool<UpdateBatchStreamingCoroutine, LargeWorldStreamer> updateBatchStreamingCoroutinePool = new StateMachinePool<UpdateBatchStreamingCoroutine, LargeWorldStreamer>();

        private static Int3 cachedRequestedIndex = Int3.negativeOne;

        private static string cachedPathIndexString = string.Empty;

        private static Dictionary<string, string> combinedOctreeCachePrefix = new Dictionary<string, string>();

        private byte[] sourceVoxelsBuffer;

        private byte[] bakedVoxelsBuffer;

        private ArrayAllocator<byte>.IAlloc batchObjectsBuffer;

        [NonSerialized]
        public HashSet<Int3> rootsToRecompile = new HashSet<Int3>(Int3.equalityComparer);

        private int hmapBufferSize = 8192;

        private byte[] hmapBufferBytes;

        private ushort[] heightLoadBuffer;

        private bool heightLoadBufferLoaded;

        private int batchLoadRings => settings.batchLoadRings;

        public float budgetMBsOut => settings.budgetMBs;

        public int lastLoadFrame { get; private set; }

        public int lastUnloadFrame { get; private set; }

        public int blocksPerTree => data.biggestNode;

        public LargeWorld.Heightmap heightmap => world.heightmap;

        public Int3 blocksPerBatch => treesPerBatch * data.biggestNode;

        public Int3 worldSize => data.GetSize();

        public Array3<float> batchSizeMBs { get; private set; }

        public Array3<bool> batchOctreesCached { get; private set; }

        public Int3 batchCount => land.data.GetNodeCount().CeilDiv(treesPerBatch);

        private bool showLowDetailTerrain
        {
            get
            {
                if (Application.isPlaying)
                {
                    return !debugDisableLowDetailTerrain;
                }
                return false;
            }
        }

        [HideInInspector]
        public string tmpPathPrefix { get; private set; }

        [HideInInspector]
        public string pathPrefix { get; private set; }

        [HideInInspector]
        public string fallbackPrefix { get; private set; }

        public Vector3 cachedCameraPosition { get; private set; }

        public Vector3 cachedCameraForward { get; private set; }

        public Vector3 cachedCameraRight { get; private set; }

        public float cachedTime { get; private set; }

        public static event EventHandler onLoadActivity;

        public bool IsReady()
        {
            return inited;
        }

        public void SetPathPrefix(string _pathPrefix, string _fallbackPrefix, bool createDirectories = true)
        {
            pathPrefix = _pathPrefix;
            fallbackPrefix = _fallbackPrefix;
            if (createDirectories)
            {
                global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(pathPrefix, "BatchHeightmap"));
                global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(pathPrefix, "BatchObjects"));
                global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(pathPrefix, "BatchOctrees"));
                global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(pathPrefix, "CompiledOctreesCache"));
                global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(pathPrefix, "BatchCells"));
                global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(pathPrefix, "CellsCache"));
            }
        }

        private void SaveVoxelandOctrees(VoxelandData srcData, Int3 batchIndex, string path)
        {
            BinaryWriter binaryWriter = new BinaryWriter(FileUtils.CreateFile(path));
            binaryWriter.Write(3);
            int num = 4;
            foreach (Int3 item in Int3.Range(treesPerBatch))
            {
                Int3 globalRid = batchIndex * treesPerBatch + item;
                if (CheckRoot(globalRid))
                {
                    int rootIndex = srcData.GetRootIndex(globalRid.x, globalRid.y, globalRid.z);
                    num += srcData.roots[rootIndex].Write(binaryWriter);
                }
            }
            binaryWriter.Close();
            if (verbose)
            {
                global::UnityEngine.Debug.Log("Wrote " + num + " bytes to " + path);
            }
        }

        private void SaveCompactOctrees(Int3 batchIndex, string path)
        {
            using BinaryWriter binaryWriter = new BinaryWriter(FileUtils.CreateFile(path));
            binaryWriter.Write(4);
            foreach (Int3 item in Int3.Range(treesPerBatch))
            {
                Int3 @int = batchIndex * treesPerBatch + item;
                if (CheckRoot(@int))
                {
                    CompactOctree compactOctree = compactTrees.Get(@int);
                    if (compactOctree == null)
                    {
                        binaryWriter.Write(Convert.ToUInt16(0));
                    }
                    else
                    {
                        compactOctree.Write(binaryWriter);
                    }
                }
            }
        }

        public bool LoadCompiledOctrees(Int3 batchId)
        {
            Utils.AssertEditMode();
            string compiledOctreesCacheFilename = GetCompiledOctreesCacheFilename(batchId);
            string compiledOctreesCachePath = GetCompiledOctreesCachePath(pathPrefix, compiledOctreesCacheFilename);
            if (!global::Platform.IO.File.Exists(compiledOctreesCachePath))
            {
                global::UnityEngine.Debug.LogErrorFormat("Failed to load batch {0}", batchId);
                return false;
            }
            using (Stream stream = FileUtils.ReadFile(compiledOctreesCachePath))
            {
                using PooledBinaryReader pooledBinaryReader = new PooledBinaryReader(stream);
                int num = pooledBinaryReader.ReadInt32();
                if (num < 4)
                {
                    global::UnityEngine.Debug.LogErrorFormat("Unsupported optoctree version {0}, batch {1}", num, batchId);
                    return false;
                }
                ReadCompiledOctrees(pooledBinaryReader, num, batchId);
                FinalizeLoadCompiledOctrees(batchId);
            }
            return true;
        }

        public IEnumerable<CompactOctreeSaveItem> ReadCompiledOctrees(PoolingBinaryReader reader, int version, Int3 batchId)
        {
            ProfilingUtils.BeginSample("ReadCompiledOctrees");
            compactOctreeBuffer.Clear();
            Int3.RangeEnumerator rangeEnumerator = Int3.Range(treesPerBatch);
            while (rangeEnumerator.MoveNext())
            {
                Int3 current = rangeEnumerator.Current;
                Int3 @int = batchId * treesPerBatch + current;
                if (CheckRoot(@int))
                {
                    CompactOctree compactOctree = compactOctreePool.Get();
                    compactOctree.Read(reader, version, batchId, current);
                    compactOctreeBuffer.Add(new CompactOctreeSaveItem(@int, compactOctree));
                }
            }
            ProfilingUtils.EndSample();
            return compactOctreeBuffer;
        }

        private void FinalizeLoadCompiledOctrees(Int3 batchId)
        {
            if (compactOctreeBuffer.Count == 0)
            {
                return;
            }
            ProfilingUtils.BeginSample("FinalizeReadCompiledOctrees");
            using (new LargeWorldOctreeCompiler.NotifyBlock(octCompiler))
            {
                List<CompactOctreeSaveItem>.Enumerator enumerator = compactOctreeBuffer.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    CompactOctree compactOctree = compactTrees.Get(enumerator.Current.index);
                    if (compactOctree != null)
                    {
                        compactOctree.NotifyUnload();
                        compactOctreePool.Return(compactOctree);
                        if (octCompiler != null)
                        {
                            octCompiler.NotifyRootUnloadedNoLock(enumerator.Current.index, keepLowDetailRoot: false);
                        }
                    }
                    compactTrees.Set(enumerator.Current.index, enumerator.Current.octree);
                    if (octCompiler != null)
                    {
                        octCompiler.NotifyRootAlreadyCompiledNoLock(enumerator.Current.index);
                    }
                }
            }
            compactOctreeBuffer.Clear();
            ProfilingUtils.EndSample();
        }

        public void SaveBatchCompiledCache(Int3 index)
        {
            SaveBatchCompiledCache(index, pathPrefix);
        }

        private void SaveBatchCompiledCache(Int3 index, string targetPathPrefix)
        {
            string path = global::Platform.IO.Path.Combine(targetPathPrefix, "CompiledOctreesCache");
            try
            {
                if (!global::Platform.IO.Directory.Exists(path))
                {
                    global::Platform.IO.Directory.CreateDirectory(path);
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                global::UnityEngine.Debug.LogException(exception, this);
                ErrorMessage.AddError(Language.main.Get("UnauthorizedAccessException"));
                return;
            }
            octCompiler.WaitForRequests(null);
            SaveCompactOctrees(index, GetCompiledOctreesCachePath(targetPathPrefix, GetCompiledOctreesCacheFilename(index)));
        }

        private IEnumerable<int> EnumRootIDs(VoxelandData src, Int3 batchId)
        {
            foreach (Int3 item in Int3.Range(treesPerBatch))
            {
                Int3 @int = batchId * treesPerBatch + item;
                int rootIndex = src.GetRootIndex(@int.x, @int.y, @int.z);
                if (rootIndex >= 0)
                {
                    yield return rootIndex;
                }
            }
        }

        private float PersistVoxelandOctrees(VoxelandData src, Int3 batchId, string path)
        {
            float num = 0f;
            bool flag = true;
            foreach (int item in EnumRootIDs(src, batchId))
            {
                if (!src.roots[item].IsEmpty())
                {
                    flag = false;
                }
                num += (float)src.roots[item].EstimateBytes() / 1024f / 1024f;
            }
            if (!flag)
            {
                SaveVoxelandOctrees(src, batchId, path);
            }
            else if (FileUtils.FileExists(path))
            {
                global::Platform.IO.File.Delete(path);
            }
            return num;
        }

        public void CollapseBatchOctrees(Int3 batchId)
        {
            foreach (int item in EnumRootIDs(data, batchId))
            {
                ProfilingUtils.BeginSample("Collapse");
                data.roots[item].Collapse();
                ProfilingUtils.EndSample();
            }
        }

        public void PruneBatchOctrees(Int3 batchId)
        {
            Array3<int> array = new Array3<int>(34);
            Int3 windowSize = array.Dims();
            foreach (int item in EnumRootIDs(data, batchId))
            {
                ProfilingUtils.BeginSample("Prune");
                PruneOctree(item, array, windowSize);
                ProfilingUtils.EndSample();
            }
        }

        private int PruneOctree(int rid, Array3<int> window, Int3 windowSize)
        {
            ProfilingUtils.BeginSample("Pre-Collapse");
            int num = data.roots[rid].Collapse();
            ProfilingUtils.EndSample();
            if (data.roots[rid].IsLeaf())
            {
                return num;
            }
            window.Clear();
            int num2 = data.biggestNode / 2;
            Int3 @int = new Int3(data.GetRootX(rid), data.GetRootY(rid), data.GetRootZ(rid));
            Int3 windowOrigin = @int * data.biggestNode - 1;
            ProfilingUtils.BeginSample("Rasterize");
            foreach (Int3 item in @int.RingBounds(1))
            {
                int rootIndex = data.GetRootIndex(item);
                if (rootIndex >= 0)
                {
                    Int3 int2 = item * data.biggestNode;
                    data.roots[rootIndex].RasterizeExists(window, windowSize, windowOrigin.x, windowOrigin.y, windowOrigin.z, int2.x, int2.y, int2.z, num2);
                }
            }
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("Prune");
            int num3 = data.roots[rid].Prune(window, windowOrigin, @int * data.biggestNode, num2);
            ProfilingUtils.EndSample();
            return num3 + num;
        }

        public void SaveBatchOctrees(Int3 batchId)
        {
            string batchBinaryFilename = GetBatchBinaryFilename(batchId);
            string batchBinaryPath = GetBatchBinaryPath(pathPrefix, batchBinaryFilename);
            float value = PersistVoxelandOctrees(data, batchId, batchBinaryPath);
            string bakedVoxelsFilename = GetBakedVoxelsFilename(batchId);
            string bakedVoxelsPath = GetBakedVoxelsPath(pathPrefix, bakedVoxelsFilename);
            if (!world.IsBatchBaked(batchId))
            {
                if (FileUtils.FileExists(bakedVoxelsPath))
                {
                    global::Platform.IO.File.Delete(bakedVoxelsPath);
                }
            }
            else
            {
                PersistVoxelandOctrees(bakedVoxels, batchId, bakedVoxelsPath);
            }
            if (editMode)
            {
                SaveBatchCompiledCache(batchId);
            }
            batchSizeMBs[batchId.x, batchId.y, batchId.z] = value;
        }

        public IEnumerator SaveBatchTmpAsync(Int3 index)
        {
            if (!Application.isPlaying)
            {
                ProfilingUtils.BeginSample("SaveBatchOctrees");
                SaveBatchCompiledCache(index, tmpPathPrefix);
                ProfilingUtils.EndSample();
            }
            yield return cellManager.SaveBatchCellsTmpAsync(index);
            yield return SaveBatchObjectsAsync(index, tmpPathPrefix);
        }

        public void EditorSaveBatch(Int3 index, bool collapseOctrees)
        {
            Utils.AssertEditMode();
            if (collapseOctrees)
            {
                ProfilingUtils.BeginSample("CollapseBatchOctrees");
                CollapseBatchOctrees(index);
                ProfilingUtils.EndSample();
            }
            ProfilingUtils.BeginSample("SaveBatchOctrees");
            SaveBatchOctrees(index);
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("SaveBatchCells");
            cellManager.SaveBatchCells(index);
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("SaveBatchObjects");
            SaveBatchObjects(index);
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("SaveBatchHeightmap");
            SaveBatchHeightmap(index.xz);
            ProfilingUtils.EndSample();
        }

        private void Awake()
        {
            main = this;
            inited = false;
            lastLoadFrame = 0;
            lastUnloadFrame = 0;
            ProfilingUtils.SetMainThreadId(Thread.CurrentThread.ManagedThreadId);
            DevConsole.RegisterConsoleCommand(this, "entstats");
            DevConsole.RegisterConsoleCommand(this, "entreset");
            DevConsole.RegisterConsoleCommand(this, "gamereset");
            DevConsole.RegisterConsoleCommand(this, "dig");
            transientRoot = new GameObject("Transient Root");
            transientRoot.transform.parent = base.transform.parent;
        }

        private void OnConsoleCommand_dig(NotificationCenter.Notification n)
        {
            float radius = 2f;
            float num = 1f;
            if (n.data.Count > 0)
            {
                radius = float.Parse((string)n.data[0]);
            }
            if (n.data.Count > 1)
            {
                num = float.Parse((string)n.data[1]);
            }
            Vector3 center = cachedCameraPosition + num * cachedCameraForward;
            PerformSphereEdit(center, radius, isAdd: false, 1);
        }

        private void OnConsoleCommand_gamereset()
        {
            StartCoroutine(ResetGameAsync());
        }

        private IEnumerator ResetGameAsync()
        {
            cellManager.ResetEntityDistributions();
            ForceUnloadAll();
            UnloadGlobalRoot();
            yield return LoadGlobalRootAsync();
            LoadSceneObjects();
        }

        private void OnConsoleCommand_entreset()
        {
            cellManager.ResetEntityDistributions();
            ForceUnloadAll();
        }

        private void OnConsoleCommand_entstats()
        {
            cellManager.EntStats();
        }

        public static string GetStreamingSettingsFileForQualityLevel(int qualityLevel)
        {
            string text = QualitySettings.names[qualityLevel];
            string arg = string.Join("_", text.ToLower().Split(global::Platform.IO.Path.GetInvalidFileNameChars()));
            return SNUtils.InsideUnmanaged($"streaming-{arg}.json");
        }

        private void LoadSettings()
        {
            string json = global::Platform.IO.File.ReadAllText(GetStreamingSettingsFileForQualityLevel(QualitySettings.GetQualityLevel()));
            settings = JsonUtility.FromJson<Settings>(json);
            global::UnityEngine.Debug.Log("Read streaming settings:\n" + settings);
        }

        public void ReloadSettings()
        {
            LoadSettings();
            if ((bool)streamerV2)
            {
                streamerV2.ReloadSettings(cellManager);
            }
        }

        public bool IsWorldSettled()
        {
            if (inited && isIdle && (bool)streamerV2 && streamerV2.IsIdle() && cellManager != null)
            {
                return cellManager.IsIdle();
            }
            return false;
        }

        public Result Initialize(WorldStreamer streamerV2, Voxeland land, string pathPrefix, string fallbackPrefix)
        {
            if (inited)
            {
                global::UnityEngine.Debug.Log("LargeWorldStreamer::Initialize - returning, already init'd frame " + Time.frameCount);
                return Result.Failure("StreamerAlreadyInitialized");
            }
            if (!CacheExists(pathPrefix) && !CacheExists(fallbackPrefix))
            {
                global::UnityEngine.Debug.LogError("streamable VL octree cache at prefix = " + pathPrefix + " DNE!");
                return Result.Failure("MissingOctreeCache");
            }
            global::UnityEngine.Debug.Log("LargeWorldStreamer::Initialize frame " + Time.frameCount);
            LoadSettings();
            tmpPathPrefix = fallbackPrefix;
            if (Application.isPlaying)
            {
                tmpPathPrefix = SaveLoadManager.GetTemporarySavePath();
                if (!debugKeepGroupRoots)
                {
                    batchesRoot = null;
                    cellsRoot = null;
                    waitersRoot = null;
                }
            }
            SetPathPrefix(fallbackPrefix, global::Platform.IO.Path.GetFullPath(fallbackPrefix), createDirectories: false);
            debugSkipTerrain |= streamerV2;
            this.streamerV2 = streamerV2;
            this.land = land;
            data = land.data;
            world = GetComponent<LargeWorld>();
            LargeWorldEntitySpawner component = GetComponent<LargeWorldEntitySpawner>();
            cellManager = new CellManager(this, component);
            maxInstanceLayer = 0;
            minInstanceLayer = 0;
            StreamReader streamReader = global::UWE.Utils.OpenEitherText(global::Platform.IO.Path.Combine(pathPrefix, "index.txt"), global::Platform.IO.Path.Combine(fallbackPrefix, "index.txt"));
            int.Parse(streamReader.ReadLine());
            Int3 @int = Int3.ParseLine(streamReader);
            nodeCount = Int3.ParseLine(streamReader);
            int newMaxNodeSize = int.Parse(streamReader.ReadLine());
            data.ClearToNothing(@int.x, @int.y, @int.z, newMaxNodeSize, !debugSkipTerrain);
            compiledVoxels = ScriptableObject.CreateInstance<VoxelandData>();
            compiledVoxels.name = "Compiled Voxel Temp Data";
            compiledVoxels.ClearToNothing(@int.x, @int.y, @int.z, newMaxNodeSize, !debugSkipTerrain);
            bakedVoxels = ScriptableObject.CreateInstance<VoxelandData>();
            bakedVoxels.name = "Height-baked Voxel Data";
            bakedVoxels.ClearToNothing(@int.x, @int.y, @int.z, newMaxNodeSize, !debugSkipTerrain);
            Int3 int2 = @int / 32;
            compactTrees = new Array3<CompactOctree>(int2.x, int2.y, int2.z);
            treesPerBatch = Int3.ParseLine(streamReader);
            batchSizeMBs = new Array3<float>(batchCount.x, batchCount.y, batchCount.z);
            batchOctreesCached = new Array3<bool>(batchCount.x, batchCount.y, batchCount.z);
            global::UnityEngine.Debug.Log(string.Concat("LargeWorldStreamer allocated space for ", batchCount, " octree batches"));
            foreach (Int3 item in Int3.Range(batchCount))
            {
                float value = float.Parse(streamReader.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture);
                batchSizeMBs.Set(item, value);
            }
            streamReader.Close();
            proxy = new RasterProxy(this);
            land.overrideRasterizer = proxy;
            if (!debugSkipCppFaceScan)
            {
                land.faceCreator = new CppVoxelandFaceScanner();
            }
            if (overrideDisableGrass || !Application.isEditor)
            {
                global::UnityEngine.Debug.Log("LargeWorldStreamer is disabling grass! Either override disable is on, or this is a standalone build");
                land.disableGrass = true;
            }
            land.eventHandler = this;
            bool flag = !AtmosphereDirector.ShadowsEnabled();
            if (Application.isPlaying && flag)
            {
                land.castShadows = false;
                land.skipHiRes = true;
                land.chunkSize = 32;
            }
            else
            {
                land.castShadows = true;
                land.skipHiRes = false;
                land.chunkSize = 16;
            }
            if (!debugSkipEntityLoad && !settings.overrideDebugSkipEntities)
            {
                ProfilingTimer.Begin("Initialize prefab database (WorldEntities)");
                if (!Application.isEditor)
                {
                    global::UnityEngine.Debug.Log("Calling LoadPrefabDatabase frame " + Time.frameCount);
                    PrefabDatabase.LoadPrefabDatabase(SNUtils.prefabDatabaseFilename);
                }
                ProfilingTimer.End();
                if (settings.warmupAllShaders)
                {
                    ProfilingTimer.Begin("Warm All Shaders");
                    Shader.WarmupAllShaders();
                    ProfilingTimer.End();
                }
            }
            else
            {
                debugDisableAllEnts = true;
            }
            if (Application.isPlaying)
            {
                ProfilingTimer.Begin("SaveLoadManager.Initialize");
                SaveLoadManager.main.InitializeNewGame();
                ProfilingTimer.End();
            }
            isIdle = false;
            ProfilingTimer.Begin("Create octree compiler");
            octCompiler = new LargeWorldOctreeCompiler(this, data, bakedVoxels, compiledVoxels, compactTrees);
            if (Application.isPlaying && !debugNoInGameCompiling)
            {
                Thread thread = new Thread(octCompiler.MainLoop);
                thread.IsBackground = true;
                thread.Start();
            }
            ProfilingTimer.End();
            ProfilingTimer.Begin("Create worker thread");
            workerThread = global::UWE.ThreadUtils.StartWorkerThread("I/O", "LargeWorldStreamerThread", System.Threading.ThreadPriority.BelowNormal, -2, 128);
            ProfilingTimer.End();
            batchObjSavingThread = new BatchObjectFileSavingThread();
            batchObjSavingThread.StartThread();
            land.data.eventHandler = octCompiler;
            bakedVoxels.eventHandler = octCompiler;
            inited = true;
            return Result.Success();
        }

        public void ForceUnloadAll()
        {
            foreach (Int3 item in new HashSet<Int3>(loadedBatches, Int3.equalityComparer))
            {
                UnloadBatch(item);
            }
        }

        public void Deinitialize()
        {
            global::UnityEngine.Debug.Log("LargeWorldStreamer::Deinitialize called, frame " + Time.frameCount);
            _ = inited;
            if (octCompiler != null)
            {
                octCompiler.RequestAbort();
                octCompiler = null;
            }
            if (workerThread != null)
            {
                workerThread.Stop();
                workerThread = null;
            }
            if (batchObjSavingThread != null)
            {
                batchObjSavingThread.StopThread();
                batchObjSavingThread = null;
            }
            if (cellManager != null)
            {
                cellManager.RequestAbort();
            }
            ForceUnloadAll();
            UnloadGlobalRoot();
            if (land != null && land.data != null)
            {
                land.overrideRasterizer = null;
            }
            inited = false;
            lastLoadFrame = -1;
            lastUnloadFrame = -1;
            data = null;
            land = null;
            if (compiledVoxels != null)
            {
                global::UnityEngine.Object.DestroyImmediate(compiledVoxels);
            }
            if (bakedVoxels != null)
            {
                global::UnityEngine.Object.DestroyImmediate(bakedVoxels);
            }
            compactTrees = null;
        }

        public bool CheckRoot(Int3 batchId, Int3 bsRootId)
        {
            Int3 globalRid = batchId * treesPerBatch + bsRootId;
            return CheckRoot(globalRid);
        }

        public bool CheckRoot(Int3 globalRid)
        {
            return CheckRoot(globalRid.x, globalRid.y, globalRid.z);
        }

        public bool CheckRoot(int rx, int ry, int rz)
        {
            if (rx < data.nodesX && ry < data.nodesY && rz < data.nodesZ && rx >= 0 && ry >= 0)
            {
                return rz >= 0;
            }
            return false;
        }

        public bool CheckBatch(Int3 batch)
        {
            if (batch >= Int3.zero)
            {
                return batch < batchCount;
            }
            return false;
        }

        public Vector3 GetBatchCenter(Int3 batchIndex)
        {
            Vector3 position = Vector3.Scale(batchIndex.ToVector3() + global::UWE.Utils.half3, blocksPerBatch.ToVector3());
            return land.transform.TransformPoint(position);
        }

        public Vector3 GetBatchMins(Int3 batchIndex)
        {
            Vector3 position = (batchIndex * blocksPerBatch).ToVector3();
            return land.transform.TransformPoint(position);
        }

        public Vector3 GetBatchMaxs(Int3 batchIndex)
        {
            Vector3 position = ((batchIndex + 1) * blocksPerBatch).ToVector3();
            return land.transform.TransformPoint(position);
        }

        public float GetSquaredDistanceToBatch(Vector3 p, Int3 batch)
        {
            return global::UWE.Utils.GetPointToBoxDistanceSquared(p, GetBatchMins(batch), GetBatchMaxs(batch));
        }

        public Int3 GetBatchOriginBlock(Int3 batchIndex)
        {
            return new Int3(batchIndex.x * treesPerBatch.x * data.biggestNode, batchIndex.y * treesPerBatch.y * data.biggestNode, batchIndex.z * treesPerBatch.z * data.biggestNode);
        }

        public Int3 GetContainingBatch(Vector3 wsPos)
        {
            if (!inited)
            {
                return new Int3(-1);
            }
            return Int3.Floor(land.transform.InverseTransformPoint(wsPos)) / blocksPerBatch;
        }

        public Int3 GetContainingTree(Vector3 wsPos)
        {
            return Int3.Floor(land.transform.InverseTransformPoint(wsPos)) / blocksPerTree;
        }

        private IEnumerator Start()
        {
            while (!inited)
            {
                yield return CoroutineUtils.waitForNextFrame;
            }
            while (!land)
            {
                yield return CoroutineUtils.waitForNextFrame;
            }
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> pooledStateMachine = CoroutineUtils.PumpCoroutine(LoopUpdateBatchStreamingAsync(), "UpdateBatchStreamingFSM", 1f);
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> pooledStateMachine2 = CoroutineUtils.PumpCoroutine(LoopUpdateCellStreamingAsync(), "UpdateCellStreamingFSM", 1f);
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> update1 = pooledStateMachine;
            PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> update2 = pooledStateMachine2;
            Stopwatch watch = new Stopwatch();
            while (true)
            {
                Transform transform = MainCamera.camera.transform;
                cachedCameraPosition = transform.position;
                cachedCameraForward = transform.forward;
                cachedCameraRight = transform.right;
                cachedTime = Time.realtimeSinceStartup;
                streamerV2.UpdateStreamingCenter(cachedCameraPosition);
                while (MainGameController.Instance.HasGarbageCollectedThisFrame())
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }
                ProfilingUtils.BeginSample("LargeWorldStreamer.Start");
                float num = settings.GetMaxFrameMs(Player.main);
                PooledStateMachine<CoroutineUtils.PumpCoroutineStateMachine> pooledStateMachine3 = update1;
                update1 = update2;
                update2 = pooledStateMachine3;
                float maxFrameMs = Mathf.Max(1f, num * 0.7f);
                update1.stateMachine.SetMaxFrameMs(maxFrameMs);
                watch.Restart();
                update1.MoveNext();
                watch.Stop();
                float timeElapsedMS = global::UWE.Utils.GetTimeElapsedMS(watch);
                float num2 = num - timeElapsedMS;
                float maxFrameMs2 = Mathf.Max(1f, num2 * 0.5f);
                update2.stateMachine.SetMaxFrameMs(maxFrameMs2);
                watch.Restart();
                update2.MoveNext();
                watch.Stop();
                ProfilingUtils.EndSample();
                bool wait = true;
                if (update1.Current is YieldInstruction)
                {
                    wait = false;
                    yield return update1.Current;
                }
                if (update2.Current is YieldInstruction)
                {
                    wait = false;
                    yield return update2.Current;
                }
                if (wait)
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }
            }
        }

        private IEnumerator LoopUpdateBatchStreamingAsync()
        {
            while (true)
            {
                yield return UpdateBatchStreaming();
                yield return CoroutineUtils.waitForNextFrame;
            }
        }

        private IEnumerator LoopUpdateCellStreamingAsync()
        {
            ProtobufSerializer serializer = new ProtobufSerializer();
            while (true)
            {
                yield return UpdateCellStreaming(serializer);
                yield return CoroutineUtils.waitForNextFrame;
            }
        }

        public IEnumerator UpdateBatchStreaming()
        {
            return updateBatchStreamingCoroutinePool.Get(this);
        }

        public IEnumerator LoadBatchLowDetailThreadedAsync(Int3 batch)
        {
            LoadBatchLowDetailTask loadBatchLowDetailTask = new LoadBatchLowDetailTask(this, batch);
            global::UWE.Utils.EnqueueWrap(workerThread, loadBatchLowDetailTask);
            return new AsyncAwaiter(loadBatchLowDetailTask);
        }

        public IEnumerator LoadBatchThreadedAsync(BatchCells batchCells, bool editMode)
        {
            LoadBatchTask loadBatchTask = new LoadBatchTask(this, batchCells, editMode);
            global::UWE.Utils.EnqueueWrap(workerThread, loadBatchTask);
            return new AsyncAwaiter(loadBatchTask);
        }

        private Int3.Bounds GetEffectiveBounds(Int3 camBatch)
        {
            return new Int3.Bounds(camBatch - batchLoadRings, camBatch + batchLoadRings);
        }

        private bool TryGetBestBatch(Vector3 camPos, Int3.Bounds effectiveBounds, out Int3 best)
        {
            ProfilingUtils.BeginSample("Load batch importance");
            best = Int3.zero;
            float num = float.MaxValue;
            bool result = false;
            Int3.RangeEnumerator enumerator = effectiveBounds.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int3 current = enumerator.Current;
                if (CheckBatch(current) && !loadedBatches.Contains(current) && !lockedBatches.Contains(current))
                {
                    float squaredDistanceToBatch = GetSquaredDistanceToBatch(camPos, current);
                    if (squaredDistanceToBatch < num)
                    {
                        best = current;
                        num = squaredDistanceToBatch;
                        result = true;
                    }
                }
            }
            ProfilingUtils.EndSample();
            return result;
        }

        private bool TryGetWorstBatch(Vector3 camPos, Int3.Bounds effectiveBounds, out Int3 worst)
        {
            ProfilingUtils.BeginSample("Unload batch importance");
            worst = Int3.zero;
            float num = float.MinValue;
            bool result = false;
            foreach (Int3 loadedBatch in loadedBatches)
            {
                if (!effectiveBounds.Contains(loadedBatch))
                {
                    float squaredDistanceToBatch = GetSquaredDistanceToBatch(camPos, loadedBatch);
                    if (squaredDistanceToBatch > num)
                    {
                        worst = loadedBatch;
                        num = squaredDistanceToBatch;
                        result = true;
                    }
                }
            }
            ProfilingUtils.EndSample();
            return result;
        }

        public IEnumerator UpdateCellStreaming(ProtobufSerializer serializer)
        {
            if (frozen)
            {
                return null;
            }
            if (cellManager.IsIdle())
            {
                return null;
            }
            return cellManager.UpdateCellManagement(serializer);
        }

        private void OnDestroy()
        {
            Deinitialize();
        }

        private bool TryUnloadBatch(Int3 batch)
        {
            if (SaveLoadManager.main != null && !SaveLoadManager.main.GetAllowWritingFiles())
            {
                return false;
            }
            _ = StopwatchProfiler.Instance;
            try
            {
                if (octCompiler != null)
                {
                    Int3.Bounds bounds = new Int3.Bounds(batch * treesPerBatch, (batch + 1) * treesPerBatch - 1);
                    if (!showLowDetailTerrain)
                    {
                        octCompiler.NotifyRootsUnloading(bounds);
                    }
                    if (octCompiler.GetNumOutstandingRequests() != 0)
                    {
                        return false;
                    }
                }
            }
            finally
            {
            }
            try
            {
                if (cellManager != null && cellManager.IsProcessingBatchCells(batch))
                {
                    return false;
                }
            }
            finally
            {
            }
            return true;
        }

        public void LoadBatches(Int3 batchMin, Int3 batchMax, bool progressBar)
        {
            (batchMax - batchMin + 1).Product();
            foreach (Int3 item in Int3.Range(batchMin, Int3.Min(batchMax, batchCount - 1)))
            {
                if (LargeWorldStreamer.onLoadActivity != null)
                {
                    LargeWorldStreamer.onLoadActivity(this, null);
                }
                if (CheckBatch(item))
                {
                    LoadBatch(item);
                }
            }
        }

        public void LoadBatchesForEdit(Int3.Bounds blocks, bool progressBar)
        {
            LoadBatchesForEdit(blocks.mins, blocks.maxs, progressBar);
        }

        public void LoadBatchesForEdit(Int3 minBlock, Int3 maxBlock, bool progressBar)
        {
            PrefabDatabase.LoadPrefabDatabase(SNUtils.prefabDatabaseFilename);
            Int3 @int = minBlock / blocksPerBatch;
            Int3 int2 = maxBlock / blocksPerBatch;
            ProfilingTimer.Begin(string.Concat("Loading batches for edit: ", @int, " to ", int2));
            LoadBatches(@int, int2, progressBar);
            ProfilingTimer.End();
            Int3.Bounds bounds = new Int3.Bounds(@int, int2);
            ProfilingTimer.Begin("Loading nbors for tile instances");
            int num = 0;
            foreach (Int3 item in bounds.Expanded(1))
            {
                if (!bounds.Contains(item))
                {
                    num++;
                }
            }
            int num2 = 0;
            foreach (Int3 item2 in bounds.Expanded(1))
            {
                if (!bounds.Contains(item2) && CheckBatch(item2))
                {
                    if (LargeWorldStreamer.onLoadActivity != null)
                    {
                        LargeWorldStreamer.onLoadActivity(this, null);
                    }
                    LoadBatchObjects(item2);
                    num2++;
                }
            }
            ProfilingTimer.End();
        }

        public void LockRelevantBatches(Int3 minBlock, Int3 maxBlock)
        {
            Int3 mins = new Int3(minBlock.x / data.biggestNode / treesPerBatch.x, minBlock.y / data.biggestNode / treesPerBatch.y, minBlock.z / data.biggestNode / treesPerBatch.z);
            Int3 a = new Int3(maxBlock.x / data.biggestNode / treesPerBatch.x, maxBlock.y / data.biggestNode / treesPerBatch.y, maxBlock.z / data.biggestNode / treesPerBatch.z);
            foreach (Int3 item in Int3.Range(mins, Int3.Min(a, batchCount - 1)))
            {
                lockedBatches.Add(item);
            }
        }

        public void ClearLocks()
        {
            lockedBatches.Clear();
        }

        public void EditorSaveRelevantBatches(Int3.Bounds blocks, bool collapseOctrees)
        {
            EditorSaveRelevantBatches(blocks.mins, blocks.maxs, collapseOctrees);
        }

        public void EditorSaveRelevantBatches(Int3 minBlock, Int3 maxBlock, bool collapseOctrees)
        {
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public static string GetPathIndexString(Int3 index)
        {
            if (index == cachedRequestedIndex)
            {
                return cachedPathIndexString;
            }
            cachedPathIndexString = $"{index.x}-{index.y}-{index.z}";
            cachedRequestedIndex = index;
            return cachedPathIndexString;
        }

        public static string GetBatchBinaryFilename(Int3 index)
        {
            return $"batch-{GetPathIndexString(index)}.octrees";
        }

        public static string GetBatchBinaryPath(string prefix, string filename)
        {
            return global::Platform.IO.Path.Combine(global::Platform.IO.Path.Combine(prefix, "BatchOctrees"), filename);
        }

        public static string GetCompiledOctreesCacheFilename(Int3 index)
        {
            return $"compiled-batch-{GetPathIndexString(index)}.optoctrees";
        }

        public static string GetCompiledOctreesCachePath(string prefix, string filename)
        {
            if (!combinedOctreeCachePrefix.TryGetValue(prefix, out var value))
            {
                value = global::Platform.IO.Path.Combine(prefix, "CompiledOctreesCache");
                combinedOctreeCachePrefix.Add(prefix, value);
            }
            return global::Platform.IO.Path.Combine(value, filename);
        }

        public static string GetBakedVoxelsFilename(Int3 index)
        {
            return $"batch-heightbaked-{GetPathIndexString(index)}.octrees";
        }

        public static string GetBakedVoxelsPath(string prefix, string filename)
        {
            return global::Platform.IO.Path.Combine(global::Platform.IO.Path.Combine(prefix, "BatchOctrees"), filename);
        }

        public static string GetBatchObjectsFilename(Int3 index)
        {
            return $"batch-objects-{GetPathIndexString(index)}.bin";
        }

        public static string GetBatchObjectsPath(string prefix, string filename)
        {
            return global::Platform.IO.Path.Combine(global::Platform.IO.Path.Combine(prefix, "BatchObjects"), filename);
        }

        public static string GetCacheBatchObjectsPath(string prefix, string filename)
        {
            return global::Platform.IO.Path.Combine(global::Platform.IO.Path.Combine(prefix, "BatchObjectsCache"), filename);
        }

        public static string GetGlobalRootPath(string prefix)
        {
            return global::Platform.IO.Path.Combine(prefix, "global-objects.bin");
        }

        public static string GetSceneObjectsPath(string prefix)
        {
            return global::Platform.IO.Path.Combine(prefix, "scene-objects.bin");
        }

        public static bool CacheExists(string pathPrefix)
        {
            return FileUtils.FileExists(global::Platform.IO.Path.Combine(pathPrefix, "index.txt"));
        }

        public static Int3 PeekCacheRes(string pathPrefix)
        {
            StreamReader streamReader = FileUtils.ReadTextFile(global::Platform.IO.Path.Combine(pathPrefix, "index.txt"));
            int.Parse(streamReader.ReadLine());
            Int3 result = Int3.ParseLine(streamReader);
            streamReader.Close();
            return result;
        }

        public static void CreateStreamableCache(string pathPrefix, VoxelandData data, Int3 treesPerBatch)
        {
            string path = global::Platform.IO.Path.Combine(pathPrefix, "index.txt");
            System.IO.Directory.CreateDirectory(global::Platform.IO.Path.GetDirectoryName(path));
            StreamWriter streamWriter = FileUtils.CreateTextFile(path);
            streamWriter.WriteLine(string.Concat(0));
            new Int3(data.sizeX, data.sizeY, data.sizeZ).Write(streamWriter);
            Int3 a = data.GetNodeCount();
            a.Write(streamWriter);
            streamWriter.WriteLine(data.biggestNode);
            treesPerBatch.Write(streamWriter);
            Int3 @int = Int3.CeilDiv(a, treesPerBatch);
            for (int i = 0; i < @int.Product(); i++)
            {
                streamWriter.WriteLine(0f);
            }
            streamWriter.Close();
        }

        public void UnloadBatchOctrees(Int3 index)
        {
            if (debugSkipTerrain)
            {
                return;
            }
            using (new LargeWorldOctreeCompiler.NotifyBlock(octCompiler))
            {
                if (verbose)
                {
                    global::UnityEngine.Debug.Log("unloading oct batch " + index);
                }
                for (int i = 0; i < treesPerBatch.x; i++)
                {
                    for (int j = 0; j < treesPerBatch.y; j++)
                    {
                        for (int k = 0; k < treesPerBatch.z; k++)
                        {
                            int num = index.x * treesPerBatch.x + i;
                            int num2 = index.y * treesPerBatch.y + j;
                            int num3 = index.z * treesPerBatch.z + k;
                            if (!CheckRoot(num, num2, num3))
                            {
                                continue;
                            }
                            int rootIndex = data.GetRootIndex(num, num2, num3);
                            data.roots[rootIndex].Clear();
                            compiledVoxels.roots[rootIndex].Clear();
                            bakedVoxels.roots[rootIndex].Clear();
                            CompactOctree compactOctree = compactTrees[num, num2, num3];
                            if (compactOctree != null)
                            {
                                if (showLowDetailTerrain)
                                {
                                    compactOctree.UnloadChildren();
                                }
                                else
                                {
                                    compactOctree.NotifyUnload();
                                    compactOctreePool.Return(compactOctree);
                                    compactTrees[num, num2, num3] = null;
                                }
                            }
                            if (octCompiler != null)
                            {
                                octCompiler.NotifyRootUnloadedNoLock(new Int3(num, num2, num3), showLowDetailTerrain);
                            }
                        }
                    }
                }
            }
        }

        public void UnloadBatch(Int3 index)
        {
            ProfilingUtils.BeginSample("LargeWorldStreamer.UnloadBatch");
            UnloadBatchOctrees(index);
            UnloadBatchObjects(index);
            cellManager.UnloadBatchCells(index);
            loadedBatches.Remove(index);
            ProfilingUtils.EndSample();
        }

        public void SaveSceneObjectsIntoCurrentSlot()
        {
            using Stream stream = FileUtils.CreateFile(GetSceneObjectsPath(tmpPathPrefix));
            SceneObjectManager.Instance.Save(stream);
        }

        public void LoadSceneObjects()
        {
            try
            {
                SceneObjectManager instance = SceneObjectManager.Instance;
                string sceneObjectsPath = GetSceneObjectsPath(tmpPathPrefix);
                if (FileUtils.FileExists(sceneObjectsPath))
                {
                    using Stream stream = FileUtils.ReadFile(sceneObjectsPath);
                    instance.Load(stream);
                }
                instance.OnLoaded();
            }
            catch (Exception exception)
            {
                global::UnityEngine.Debug.LogException(exception);
            }
        }

        public void SaveGlobalRootIntoCurrentSlot()
        {
            using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
            string globalRootPath = GetGlobalRootPath(tmpPathPrefix);
            pooledObject.Value.SaveObjectTreeToFile(globalRootPath, globalRoot);
        }

        public void UnloadGlobalRoot()
        {
            global::UnityEngine.Object.DestroyImmediate(globalRoot);
            globalRoot = null;
        }

        public IEnumerator LoadGlobalRootAsync()
        {
            UnloadGlobalRoot();
            string globalRootPath = GetGlobalRootPath(tmpPathPrefix);
            string globalRootPath2 = GetGlobalRootPath(fallbackPrefix);
            GameObject gameObject = null;
            using (Stream stream = global::UWE.Utils.TryOpenEither(globalRootPath, globalRootPath2))
            {
                if (stream != null)
                {
                    using PooledObject<ProtobufSerializer> serializerProxy = ProtobufSerializerPool.GetProxy();
                    ProtobufSerializer value = serializerProxy.Value;
                    if (value.TryDeserializeStreamHeader(stream))
                    {
                        CoroutineTask<GameObject> task = value.DeserializeObjectTreeAsync(stream, forceInactiveRoot: true, 0);
                        yield return task;
                        gameObject = task.GetResult();
                    }
                }
            }
            if (!gameObject)
            {
                gameObject = new GameObject("Global Root");
                gameObject.AddComponent<StoreInformationIdentifier>();
            }
            OnGlobalRootLoaded(gameObject);
        }

        private void OnGlobalRootLoaded(GameObject root)
        {
            root.transform.parent = base.transform.parent;
            globalRoot = root;
            globalRoot.SetActive(value: true);
            globalRoot.BroadcastMessage("OnGlobalEntitiesLoaded", SendMessageOptions.DontRequireReceiver);
        }

        public void MakeEntityTransient(GameObject entity)
        {
            entity.transform.parent = transientRoot.transform;
            cellManager.UnregisterEntity(entity);
        }

        private void ReadVoxelandOctreesBatch(PoolingBinaryReader reader, int version, VoxelandData dest, Int3 index, bool doCompact = false)
        {
            ProfilingUtils.BeginSample("ReadVoxelandOctreesBatch");
            foreach (Int3 item in Int3.Range(treesPerBatch))
            {
                Int3 p = index * treesPerBatch + item;
                if (CheckRoot(p.x, p.y, p.z))
                {
                    ProfilingUtils.BeginSample("OctNode.Read");
                    int rootIndex = dest.GetRootIndex(p.x, p.y, p.z);
                    dest.roots[rootIndex].Read(reader, version);
                    ProfilingUtils.EndSample();
                    if (doCompact)
                    {
                        ProfilingUtils.BeginSample("Compact");
                        compactTrees.Set(p, CompactOctree.CreateFrom(dest.roots[rootIndex]));
                        ProfilingUtils.EndSample();
                    }
                }
            }
            ProfilingUtils.EndSample();
        }

        private void LoadSourceVoxelsThreaded(Int3 index)
        {
            sourceVoxelsBuffer = null;
            string batchBinaryFilename = GetBatchBinaryFilename(index);
            string batchBinaryPath = GetBatchBinaryPath(pathPrefix, batchBinaryFilename);
            string batchBinaryPath2 = GetBatchBinaryPath(fallbackPrefix, batchBinaryFilename);
            if (!batchSizeMBs.CheckBounds(index))
            {
                global::UnityEngine.Debug.LogError("LoadBatch given out of bounds batch index: " + index);
                return;
            }
            if (verbose)
            {
                global::UnityEngine.Debug.Log("loading batch " + batchBinaryPath + ", " + batchSizeMBs.Get(index) + " MB");
            }
            if (FileUtils.FileExists(batchBinaryPath))
            {
                sourceVoxelsBuffer = global::Platform.IO.File.ReadAllBytes(batchBinaryPath);
            }
            else if (FileUtils.FileExists(batchBinaryPath2))
            {
                sourceVoxelsBuffer = global::Platform.IO.File.ReadAllBytes(batchBinaryPath2);
            }
        }

        private void FinalizeLoadSourceVoxels(Int3 index)
        {
            ProfilingUtils.BeginSample("FinalizeLoadSourceVoxels");
            if (sourceVoxelsBuffer != null)
            {
                using MemoryStream stream = new MemoryStream(sourceVoxelsBuffer);
                using PooledBinaryReader pooledBinaryReader = new PooledBinaryReader(stream);
                int version = pooledBinaryReader.ReadInt32();
                ReadVoxelandOctreesBatch(pooledBinaryReader, version, data, index);
            }
            sourceVoxelsBuffer = null;
            ProfilingUtils.EndSample();
        }

        private void MaybeLoadBakedVoxelsThreaded(Int3 batch)
        {
            bakedVoxelsBuffer = null;
            if (world.IsBatchBaked(batch))
            {
                string bakedVoxelsFilename = GetBakedVoxelsFilename(batch);
                string bakedVoxelsPath = GetBakedVoxelsPath(pathPrefix, bakedVoxelsFilename);
                if (FileUtils.FileExists(bakedVoxelsPath))
                {
                    bakedVoxelsBuffer = global::Platform.IO.File.ReadAllBytes(bakedVoxelsPath);
                }
            }
        }

        private void FinalizeMaybeLoadBakedVoxelsThreaded(Int3 batch)
        {
            if (bakedVoxelsBuffer == null)
            {
                return;
            }
            ProfilingUtils.BeginSample("FinalizeMaybeLoadBakedVoxelsThreaded");
            using (MemoryStream stream = new MemoryStream(bakedVoxelsBuffer))
            {
                using PooledBinaryReader pooledBinaryReader = new PooledBinaryReader(stream);
                int num = pooledBinaryReader.ReadInt32();
                if (num < 3)
                {
                    string bakedVoxelsFilename = GetBakedVoxelsFilename(batch);
                    global::UnityEngine.Debug.LogError("Too low version # of " + num + " for file " + GetBakedVoxelsPath(pathPrefix, bakedVoxelsFilename));
                }
                else
                {
                    ReadVoxelandOctreesBatch(pooledBinaryReader, num, bakedVoxels, batch);
                }
            }
            bakedVoxelsBuffer = null;
            ProfilingUtils.EndSample();
        }

        public void LoadBatchLowDetailThreaded(Int3 index)
        {
            if (!CheckBatch(index) || debugSkipTerrain || debugDisableLowDetailTerrain)
            {
                return;
            }
            string text = null;
            string compiledOctreesCacheFilename = GetCompiledOctreesCacheFilename(index);
            string compiledOctreesCachePath = GetCompiledOctreesCachePath(pathPrefix, compiledOctreesCacheFilename);
            if (global::Platform.IO.File.Exists(compiledOctreesCachePath))
            {
                text = compiledOctreesCachePath;
            }
            else
            {
                string compiledOctreesCachePath2 = GetCompiledOctreesCachePath(fallbackPrefix, compiledOctreesCacheFilename);
                if (global::Platform.IO.File.Exists(compiledOctreesCachePath2))
                {
                    text = compiledOctreesCachePath2;
                }
            }
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            using Stream stream = FileUtils.ReadFile(text);
            using PooledBinaryReader pooledBinaryReader = new PooledBinaryReader(stream);
            int num = pooledBinaryReader.ReadInt32();
            if (num <= 3)
            {
                global::UnityEngine.Debug.LogErrorFormat("Old version of cached octrees found for batch {0}, file '{1}'", index, text);
                return;
            }
            Int3.RangeEnumerator rangeEnumerator = Int3.Range(treesPerBatch);
            while (rangeEnumerator.MoveNext())
            {
                Int3 current = rangeEnumerator.Current;
                Int3 @int = index * treesPerBatch + current;
                if (CheckRoot(@int))
                {
                    CompactOctree compactOctree = compactOctreePool.Get();
                    compactOctree.Read(pooledBinaryReader, num, index, current);
                    compactOctree.UnloadChildren();
                    compactTrees.Set(@int, compactOctree);
                    if (octCompiler != null)
                    {
                        octCompiler.NotifyLowDetailRootAlreadyCompiled(@int);
                    }
                }
            }
        }

        public void LoadBatch(Int3 index)
        {
            using (new EditModeScopeTimer("LoadBatch " + index))
            {
                BatchCells batchCells = cellManager.InitializeBatchCells(index);
                LoadBatchThreaded(batchCells, !Application.isPlaying);
                FinalizeLoadBatch(index);
            }
        }

        public void LoadBatchThreaded(BatchCells batchCells, bool editMode)
        {
            Int3 batch = batchCells.batch;
            if (!CheckBatch(batch))
            {
                return;
            }
            _ = StopwatchProfiler.Instance;
            ProfilingUtils.BeginSample("LoadBatch");
            string text = null;
            string compiledOctreesCacheFilename = GetCompiledOctreesCacheFilename(batch);
            string compiledOctreesCachePath = GetCompiledOctreesCachePath(pathPrefix, compiledOctreesCacheFilename);
            if (FileUtils.FileExists(compiledOctreesCachePath))
            {
                text = compiledOctreesCachePath;
            }
            else
            {
                string compiledOctreesCachePath2 = GetCompiledOctreesCachePath(fallbackPrefix, compiledOctreesCacheFilename);
                if (FileUtils.FileExists(compiledOctreesCachePath2))
                {
                    text = compiledOctreesCachePath2;
                }
            }
            if (!debugSkipTerrain)
            {
                ProfilingUtils.BeginSample("LoadSourceVoxels");
                if (!editMode)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        LoadSourceVoxelsThreaded(batch);
                        MaybeLoadBakedVoxelsThreaded(batch);
                    }
                }
                else
                {
                    LoadSourceVoxelsThreaded(batch);
                    MaybeLoadBakedVoxelsThreaded(batch);
                }
                ProfilingUtils.EndSample();
            }
            OnLoadActivity(this, null);
            if (!editMode || (!world.loadingWindow && !world.editingWindow) || world.batchWindow.Contains(batch))
            {
                cellManager.LoadBatchCellsThreaded(batchCells, editMode);
                if ((bool)streamerV2 && streamerV2.clipmapStreamer != null)
                {
                    streamerV2.clipmapStreamer.NotifyListeners(GetBatchBlockBounds(batch));
                }
            }
            OnLoadActivity(this, null);
            if (editMode && !debugSkipTerrain)
            {
                LoadBatchHeightmapThreaded(batch.xz);
            }
            if (!debugSkipTerrain)
            {
                ProfilingUtils.BeginSample("OnRootChanged broadcast");
                foreach (Int3 item in Int3.Range(treesPerBatch))
                {
                    Int3 @int = batch * treesPerBatch + item;
                    if (CheckRoot(@int.x, @int.y, @int.z))
                    {
                        octCompiler.OnRootChanged(@int.x, @int.y, @int.z);
                    }
                }
                ProfilingUtils.EndSample();
            }
            if (!debugSkipTerrain)
            {
                LoadCompiledOctreesThreaded(batch, editMode, text);
            }
            LoadBatchObjectsThreaded(batch, editMode);
            ProfilingUtils.EndSample();
        }

        [ContextMenu("LoadAllBatchObjects", false, 0)]
        public GameObject LoadAllBatchObjects()
        {
            GameObject go = new GameObject("BatchObjects");
            using (PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy())
            {
                foreach (Int3 batch in new Int3.Bounds(Int3.zero, batchCount))
                {
                    string batchObjectsFilename = GetBatchObjectsFilename(batch);
                    string batchObjectsPath = GetBatchObjectsPath(pathPrefix, batchObjectsFilename);
                    if (global::Platform.IO.File.Exists(batchObjectsPath))
                    {
                        ProtobufSerializerExtensions.LoadObjectTreeFromFile(pooledObject, batchObjectsPath, delegate(GameObject root)
                        {
                            EnsureBatchRootSetup(root, batch);
                            root.transform.SetParent(go.transform, worldPositionStays: false);
                        }, 0);
                    }
                }
            }
            return go;
        }

        [ContextMenu("UpgradeAllBatchObjects", false, 0)]
        public void UpgradeAllBatchObjects()
        {
            PrefabIdentifier component = batchRootPrefab.GetComponent<PrefabIdentifier>();
            LargeWorldBatchRoot[] componentsInChildren = LoadAllBatchObjects().GetComponentsInChildren<LargeWorldBatchRoot>(includeInactive: true);
            LargeWorldBatchRoot[] array = componentsInChildren;
            foreach (LargeWorldBatchRoot largeWorldBatchRoot in array)
            {
                StoreInformationIdentifier component2 = largeWorldBatchRoot.GetComponent<StoreInformationIdentifier>();
                if ((bool)component2)
                {
                    global::UnityEngine.Object.DestroyImmediate(component2);
                    largeWorldBatchRoot.gameObject.AddComponent<PrefabIdentifier>().ClassId = component.ClassId;
                }
            }
            using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
            array = componentsInChildren;
            foreach (LargeWorldBatchRoot largeWorldBatchRoot2 in array)
            {
                string batchObjectsFilename = GetBatchObjectsFilename(largeWorldBatchRoot2.batchId);
                string batchObjectsPath = GetBatchObjectsPath(pathPrefix, batchObjectsFilename);
                ProtobufSerializerExtensions.SaveObjectTreeToFile(pooledObject, batchObjectsPath, largeWorldBatchRoot2.gameObject);
            }
        }

        [ContextMenu("BakeAndSaveAllCacheBatchObjects", false, 0)]
        public void BakeAndSaveAllCacheBatchObjects()
        {
            LargeWorldBatchRoot[] componentsInChildren = LoadAllBatchObjects().GetComponentsInChildren<LargeWorldBatchRoot>(includeInactive: true);
            foreach (LargeWorldBatchRoot root in componentsInChildren)
            {
                BakeAndSaveCacheBatchObjects(root);
            }
        }

        private void FinalizeLoadBatch(Int3 index)
        {
            CoroutineUtils.PumpCoroutine(FinalizeLoadBatchAsync(index, !Application.isPlaying));
        }

        public IEnumerator FinalizeLoadBatchAsync(Int3 index, bool editMode)
        {
            ProfilingUtils.BeginSample("LoadBatch finalize");
            _ = StopwatchProfiler.Instance;
            if (LargeWorldStreamer.onLoadActivity != null)
            {
                LargeWorldStreamer.onLoadActivity(this, null);
            }
            FinalizeLoadSourceVoxels(index);
            FinalizeMaybeLoadBakedVoxelsThreaded(index);
            if (editMode)
            {
                FinalizeLoadBatchHeightmap(index.xz);
            }
            FinalizeLoadCompiledOctrees(index);
            Int3 @int = index * treesPerBatch * data.biggestNode;
            Int3 int2 = (index + 1) * treesPerBatch * data.biggestNode - 1;
            land.DestroyRelevantChunks(@int.x, @int.y, @int.z, int2.x, int2.y, int2.z);
            ProfilingUtils.EndSample();
            return FinalizeLoadBatchObjectsAsync(index, reloadIfExists: true);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        [SuppressMessage("Gendarme.Rules.Performance", "AvoidConcatenatingCharsRule")]
        [SuppressMessage("Subnautica.Rules", "AvoidStringConcatenation")]
        private void LoadCompiledOctreesThreaded(Int3 index, bool editMode, string cachePath)
        {
            ProfilingUtils.BeginSample("Load compiled octrees");
            bool value = false;
            if (!editMode && !string.IsNullOrEmpty(cachePath))
            {
                using (Stream stream = FileUtils.ReadFile(cachePath))
                {
                    using PooledBinaryReader pooledBinaryReader = new PooledBinaryReader(stream);
                    int num = pooledBinaryReader.ReadInt32();
                    if (num <= 3)
                    {
                        global::UnityEngine.Debug.LogError(string.Concat("Old version of cached octrees found for batch ", index, ", file = ", cachePath));
                    }
                    else
                    {
                        ReadCompiledOctrees(pooledBinaryReader, num, index);
                    }
                }
                value = true;
            }
            batchOctreesCached.Set(index, value);
            ProfilingUtils.EndSample();
        }

        private void OnBatchObjectsLoaded(Int3 batch, GameObject rootObject)
        {
            ProfilingUtils.BeginSample("OnBatchObjectsLoaded");
            if (rootObject != null)
            {
                rootObject.name = string.Concat("Batch ", batch, " objects");
                batch2root[batch] = EnsureBatchRootSetup(rootObject, batch);
                rootObject.transform.position = land.transform.TransformPoint((batch * blocksPerBatch).ToVector3());
                UpdateBatchInstances(batch, rootObject);
                Light[] componentsInChildren = rootObject.GetComponentsInChildren<Light>();
                foreach (Light light in componentsInChildren)
                {
                    if (light.type == LightType.Directional && light.name.IndexOf("bounce", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        light.intensity = 0f;
                        DayNightLight component = light.gameObject.GetComponent<DayNightLight>();
                        if (component != null)
                        {
                            component.enabled = false;
                        }
                        global::UnityEngine.Object.Destroy(light.gameObject);
                    }
                }
            }
            OnBatchFullyLoaded(batch);
            ProfilingUtils.EndSample();
        }

        public bool IsBatchReadyToCompile(Int3 checkBatch)
        {
            if (Application.isPlaying)
            {
                if (CheckBatch(checkBatch))
                {
                    return loadedBatches.Contains(checkBatch);
                }
                return false;
            }
            bool result = true;
            foreach (Int3 item in Int3.Range(checkBatch - 1, checkBatch + 1))
            {
                if (CheckBatch(item) && !loadedBatches.Contains(item))
                {
                    return false;
                }
            }
            return result;
        }

        public void NotifyBatchReadyToCompile(Int3 batch)
        {
            using (new LargeWorldOctreeCompiler.NotifyBlock(octCompiler))
            {
                foreach (Int3 item in Int3.Range(treesPerBatch))
                {
                    Int3 @int = batch * treesPerBatch + item;
                    if (CheckRoot(@int))
                    {
                        if (Application.isPlaying)
                        {
                            octCompiler.NotifyRootReadyToCompileNoLock(@int);
                        }
                        else if ((world.loadingWindow || world.editingWindow) && world.blockWindow.Contains(@int * blocksPerTree))
                        {
                            octCompiler.RequestCompile(@int);
                        }
                    }
                }
            }
        }

        public bool CheckCompiledOctreeCacheVersion(Int3 batchId)
        {
            string compiledOctreesCacheFilename = GetCompiledOctreesCacheFilename(batchId);
            string compiledOctreesCachePath = GetCompiledOctreesCachePath(pathPrefix, compiledOctreesCacheFilename);
            string compiledOctreesCachePath2 = GetCompiledOctreesCachePath(fallbackPrefix, compiledOctreesCacheFilename);
            if (global::UWE.Utils.EitherExists(compiledOctreesCachePath, compiledOctreesCachePath2))
            {
                using (PooledBinaryReader pooledBinaryReader = global::UWE.Utils.OpenEitherBinary(compiledOctreesCachePath, compiledOctreesCachePath2))
                {
                    if (pooledBinaryReader.ReadInt32() <= 3)
                    {
                        global::UnityEngine.Debug.LogError("Old version of cached octrees found for batch " + batchId);
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        private void OnBatchFullyLoaded(Int3 batchId)
        {
            ProfilingUtils.BeginSample("OnBatchFullyLoaded");
            loadedBatches.Add(batchId);
            if (Application.isPlaying)
            {
                if (!debugSkipTerrain)
                {
                    NotifyBatchReadyToCompile(batchId);
                }
            }
            else
            {
                ProfilingUtils.BeginSample("Check compilable status");
                foreach (Int3 item in Int3.Range(batchId - 1, batchId + 1))
                {
                    if (CheckBatch(item) && (!(item == batchId) || !batchOctreesCached.Get(batchId)) && !batchOctreesCached.Get(item) && IsBatchReadyToCompile(item))
                    {
                        NotifyBatchReadyToCompile(item);
                    }
                }
                ProfilingUtils.EndSample();
            }
            ProfilingUtils.EndSample();
        }

        private LargeWorldBatchRoot EnsureBatchRootSetup(GameObject root, Int3 batch)
        {
            LargeWorldBatchRoot component = root.GetComponent<LargeWorldBatchRoot>();
            component.streamer = this;
            component.batchId = batch;
            if (!Application.isPlaying)
            {
                root.hideFlags |= HideFlags.NotEditable;
                if (!world.batchWindow.Contains(batch))
                {
                    root.name += " (preview)";
                    Transform[] componentsInChildren = root.GetComponentsInChildren<Transform>();
                    for (int i = 0; i < componentsInChildren.Length; i++)
                    {
                        componentsInChildren[i].gameObject.hideFlags |= HideFlags.NotEditable;
                    }
                }
            }
            ProfilingUtils.BeginSample("SetBatchRootParent");
            root.transform.SetParent(batchesRoot, worldPositionStays: false);
            ProfilingUtils.EndSample();
            return component;
        }

        public void LoadBatchObjects(Int3 index)
        {
            Utils.AssertEditMode();
            LoadBatchObjectsThreaded(index, editMode: true);
            CoroutineUtils.PumpCoroutine(FinalizeLoadBatchObjectsAsync(index, reloadIfExists: true));
        }

        public static ArrayAllocator<byte>.IAlloc ReadAllBytesPooled(string path)
        {
            ArrayAllocator<byte>.IAlloc alloc = null;
            using FileStream fileStream = global::Platform.IO.File.OpenRead(path);
            int num = 0;
            int num2 = (int)fileStream.Length;
            alloc = CommonByteArrayAllocator.Allocate(num2);
            while (true)
            {
                if (num2 > 0)
                {
                    int num3 = fileStream.Read(alloc.Array, alloc.Offset + num, num2);
                    if (num3 == 0)
                    {
                        break;
                    }
                    num += num3;
                    num2 -= num3;
                    continue;
                }
                return alloc;
            }
            return alloc;
        }

        private void LoadBatchObjectsThreaded(Int3 index, bool editMode)
        {
            ProfilingUtils.BeginSample("LoadBatchObjects");
            if (batchObjectsBuffer != null && batchObjectsBuffer.Length == 0)
            {
                CommonByteArrayAllocator.Free(batchObjectsBuffer);
                batchObjectsBuffer = null;
            }
            if (batchObjectsBuffer != null && batchObjectsBuffer.Length == 0)
            {
                batchObjectsBuffer = null;
            }
            string batchObjectsFilename = GetBatchObjectsFilename(index);
            string batchObjectsPath = GetBatchObjectsPath(tmpPathPrefix, batchObjectsFilename);
            string path = (editMode ? GetBatchObjectsPath(fallbackPrefix, batchObjectsFilename) : GetCacheBatchObjectsPath(fallbackPrefix, batchObjectsFilename));
            try
            {
                if (FileUtils.FileExists(batchObjectsPath, skipManifest: true))
                {
                    batchObjectsBuffer = ReadAllBytesPooled(batchObjectsPath);
                }
                else if (FileUtils.FileExists(path))
                {
                    batchObjectsBuffer = ReadAllBytesPooled(path);
                }
            }
            catch (Exception ex)
            {
                global::UnityEngine.Debug.LogErrorFormat(this, "Exception while loading batch {0}: {1}", index, ex);
                global::UnityEngine.Debug.LogException(ex, this);
            }
            ProfilingUtils.EndSample();
        }

        private IEnumerator FinalizeLoadBatchObjectsAsync(Int3 index, bool reloadIfExists)
        {
            _ = StopwatchProfiler.Instance;
            if (reloadIfExists && batch2root.ContainsKey(index))
            {
                UnloadBatchObjects(index);
            }
            if (batchObjectsBuffer != null)
            {
                GameObject rootObject = null;
                using (MemoryStream stream = new MemoryStream(batchObjectsBuffer.Array, batchObjectsBuffer.Offset, batchObjectsBuffer.Length, writable: false))
                {
                    using PooledObject<ProtobufSerializer> serializerProxy = ProtobufSerializerPool.GetProxy();
                    ProtobufSerializer value = serializerProxy.Value;
                    if (value.TryDeserializeStreamHeader(stream))
                    {
                        CoroutineTask<GameObject> task = value.DeserializeObjectTreeAsync(stream, forceInactiveRoot: false, 0);
                        yield return task;
                        rootObject = task.GetResult();
                    }
                }
                OnBatchObjectsLoaded(index, rootObject);
            }
            else
            {
                GameObject gameObject = global::UnityEngine.Object.Instantiate(batchRootPrefab);
                batch2root[index] = EnsureBatchRootSetup(gameObject, index);
                gameObject.transform.position = land.transform.TransformPoint((index * blocksPerBatch).ToVector3());
                OnBatchFullyLoaded(index);
            }
            if (batchObjectsBuffer != null)
            {
                CommonByteArrayAllocator.Free(batchObjectsBuffer);
                batchObjectsBuffer = null;
            }
        }

        private void SaveBatchObjects(Int3 index)
        {
            Utils.AssertEditMode();
            CoroutineUtils.PumpCoroutine(SaveBatchObjectsAsync(index, pathPrefix));
            BakeAndSaveCacheBatchObjects(index);
        }

        private bool BakeAndSaveCacheBatchObjects(Int3 index)
        {
            Utils.AssertEditMode();
            if (batch2root.TryGetValue(index, out var value))
            {
                return BakeAndSaveCacheBatchObjects(value);
            }
            return false;
        }

        private bool BakeAndSaveCacheBatchObjects(LargeWorldBatchRoot root)
        {
            Utils.AssertEditMode();
            Int3 batchId = root.batchId;
            using (PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy())
            {
                GameObject gameObject = null;
                try
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        ProtobufSerializerExtensions.SerializeObjectTree(pooledObject, memoryStream, root.gameObject);
                        memoryStream.Seek(0L, SeekOrigin.Begin);
                        gameObject = ProtobufSerializerExtensions.DeserializeObjectTree(pooledObject, memoryStream, 0);
                    }
                    TileInstance[] componentsInChildren = gameObject.GetComponentsInChildren<TileInstance>(includeInactive: true);
                    foreach (TileInstance tileInstance in componentsInChildren)
                    {
                        if ((bool)tileInstance)
                        {
                            global::UnityEngine.Object.DestroyImmediate(tileInstance.gameObject);
                        }
                    }
                    StoreInformationIdentifier[] componentsInChildren2 = gameObject.GetComponentsInChildren<StoreInformationIdentifier>(includeInactive: true);
                    int i = 0;
                    if (i < componentsInChildren2.Length)
                    {
                        StoreInformationIdentifier storeInformationIdentifier = componentsInChildren2[i];
                        global::UnityEngine.Debug.LogErrorFormat("Batch root {0} contains one-off entity '{1}'", batchId, storeInformationIdentifier.name);
                        return false;
                    }
                    string batchObjectsFilename = GetBatchObjectsFilename(batchId);
                    string cacheBatchObjectsPath = GetCacheBatchObjectsPath(pathPrefix, batchObjectsFilename);
                    string directoryName = global::Platform.IO.Path.GetDirectoryName(cacheBatchObjectsPath);
                    if (!global::Platform.IO.Directory.Exists(directoryName))
                    {
                        global::Platform.IO.Directory.CreateDirectory(directoryName);
                    }
                    ProtobufSerializerExtensions.SaveObjectTreeToFile(pooledObject, cacheBatchObjectsPath, gameObject);
                }
                finally
                {
                    global::UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }
            return true;
        }

        private void SaveBatchObjectsIntoCurrentSlot(Int3 index)
        {
            if (batch2root.TryGetValue(index, out var value))
            {
                string batchObjectsFilename = GetBatchObjectsFilename(index);
                string batchObjectsPath = GetBatchObjectsPath(tmpPathPrefix, batchObjectsFilename);
                if (verbose)
                {
                    global::UnityEngine.Debug.Log("saving " + batchObjectsPath);
                }
                using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
                pooledObject.Value.SaveObjectTreeToFile(batchObjectsPath, value.gameObject);
            }
        }

        private IEnumerator SaveBatchObjectsAsync(Int3 index, string targetPathPrefix)
        {
            _ = StopwatchProfiler.Instance;
            try
            {
                global::Platform.IO.Directory.CreateDirectory(global::Platform.IO.Path.Combine(targetPathPrefix, "BatchObjects"));
            }
            catch (UnauthorizedAccessException exception)
            {
                global::UnityEngine.Debug.LogException(exception, this);
                ErrorMessage.AddError(Language.main.Get("UnauthorizedAccessException"));
                yield break;
            }
            if (batch2root.TryGetValue(index, out var value))
            {
                string batchObjectsFilename = GetBatchObjectsFilename(index);
                string batchObjectsPath = GetBatchObjectsPath(targetPathPrefix, batchObjectsFilename);
                if (verbose)
                {
                    global::UnityEngine.Debug.LogFormat("saving {0}", batchObjectsPath);
                }
                ProfilingUtils.BeginSample("SaveBatchObjectsAsync-CreateFileAsync");
                BatchObjectFileSaveTask saveObjBatchTask = new BatchObjectFileSaveTask(batchObjectsPath);
                batchObjSavingThread.CreateFileForTaskAsync(saveObjBatchTask);
                ProfilingUtils.EndSample();
                using (PooledObject<ProtobufSerializer> serializerProxy = ProtobufSerializerPool.GetProxy())
                {
                    serializerProxy.Value.SerializeStreamHeader(saveObjBatchTask.intermediateSaveBuffer);
                    yield return serializerProxy.Value.SerializeObjectTreeAsync(saveObjBatchTask.intermediateSaveBuffer, value.gameObject);
                }
                while (!saveObjBatchTask.isDoneCreatingFile)
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }
                ProfilingUtils.BeginSample("SaveBatchObjectsAsync-SaveFileAsync");
                batchObjSavingThread.SaveIntermediateBufferToFile(saveObjBatchTask);
                ProfilingUtils.EndSample();
                while (!saveObjBatchTask.isDoneSavingFile)
                {
                    yield return CoroutineUtils.waitForNextFrame;
                }
            }
        }

        public GameObject GetBatchObjectsRoot(Int3 index)
        {
            GameObject result = null;
            if (batch2root.TryGetValue(index, out var value))
            {
                result = value.gameObject;
            }
            return result;
        }

        private void UnloadBatchObjects(Int3 index)
        {
            if (batch2root.TryGetValue(index, out var value))
            {
                if (value != null)
                {
                    GameObject o = value.gameObject;
                    ProfilingUtils.BeginSample("DestroyBatchRoot");
                    global::UWE.Utils.DestroyWrap(o);
                    ProfilingUtils.EndSample();
                }
                batch2root.Remove(index);
            }
        }

        public Int3.Bounds GetBatchTreeBounds(Int3 index)
        {
            return new Int3.Bounds(index * treesPerBatch, (index + 1) * treesPerBatch - 1);
        }

        public Int3.Bounds GetBatchBlockBounds(Int3 index)
        {
            return new Int3.Bounds(index * blocksPerBatch, (index + 1) * blocksPerBatch - 1);
        }

        public Int3.Bounds GetRootBlockBounds(Int3 root)
        {
            return new Int3.Bounds(root * data.biggestNode, (root + 1) * data.biggestNode - 1);
        }

        public void LayoutDebugGUI()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("-- LargeWorldStreamer --");
            if ((bool)land && land.overrideRasterizer != null)
            {
                land.overrideRasterizer.LayoutDebugGUI();
            }
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Cycle All Ents"))
            {
                ProfilingUtils.BeginSample("Cycle NO POOLING");
                ProfilingUtils.EndSample();
                using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
                foreach (EntityCell item in cellManager.LoadedCells())
                {
                    item.Cycle(pooledObject);
                }
            }
            GUILayout.EndHorizontal();
            debugDisableAllEnts = GUILayout.Toggle(debugDisableAllEnts, "Disable ALL Entities");
            debugDisableProceduralEnts = GUILayout.Toggle(debugDisableProceduralEnts, "Disable Procedural Entities");
            debugDisableSlotEnts = GUILayout.Toggle(debugDisableSlotEnts, "Disable Slot Entities");
            debugDisableInstanceEnts = GUILayout.Toggle(debugDisableInstanceEnts, "Disable Instance Entities");
            debugDrawLoadedBatches = GUILayout.Toggle(debugDrawLoadedBatches, "Debug Draw Loaded Batches");
            frozen = GUILayout.Toggle(frozen, "Freeze Streaming");
            debugSkipCppFaceScan = GUILayout.Toggle(debugSkipCppFaceScan, "Debug Skip CPP Face scan");
            settings.batchLoadRings = global::UWE.Utils.GUI.LayoutIntField(batchLoadRings, "Batch Load Rings");
            if (GUILayout.Button("Reload settings"))
            {
                ReloadSettings();
            }
            GUILayout.BeginHorizontal();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                if (GUILayout.Button(QualitySettings.names[i]))
                {
                    QualitySettings.SetQualityLevel(i, applyExpensiveChanges: true);
                    ReloadSettings();
                }
            }
            GUILayout.EndHorizontal();
            if (IsReady())
            {
                GUILayout.Label("Active prefix: " + pathPrefix + ". Fallback: " + fallbackPrefix);
                GUILayout.Label("Loaded " + loadedBatches.Count + " / " + batchCount.Product() + " batches");
                GUILayout.Label("Prefab cache size: " + PrefabDatabase.GetCacheSize());
                GUILayout.Label("Idle: " + isIdle);
                if ((bool)streamerV2 && streamerV2.clipmapStreamer != null)
                {
                    streamerV2.clipmapStreamer.OnGUI();
                }
                GUILayout.Label("Cell queue length: " + cellManager.GetQueueLength());
                Int3 containingBatch = GetContainingBatch(MainCamera.camera.transform.position);
                GUILayout.Label("Batch " + containingBatch);
                GUILayout.Label("- loaded? " + loadedBatches.Contains(containingBatch));
                GUILayout.Label("- ready to compile? " + IsBatchReadyToCompile(containingBatch));
                GUILayout.Label("- authored? " + IsAuthoredBatch(containingBatch));
                if (octCompiler != null)
                {
                    GUILayout.Label("# roots compiled: " + octCompiler.numRootsCompiled);
                    Int3 containingTree = GetContainingTree(MainCamera.camera.transform.position);
                    GUILayout.Label(string.Concat("Root ", containingTree, ", compiled? ", octCompiler.IsRootCompiled(containingTree, lowDetail: false).ToString(), ". ready to compile? ", octCompiler.IsRootReadyToCompile(containingTree).ToString()));
                    if (GUILayout.Button("Kill Compiler Dispatch Thread"))
                    {
                        octCompiler.RequestAbort();
                        octCompiler = null;
                    }
                }
            }
            else
            {
                GUILayout.Label("No world mounted");
            }
            if (land != null)
            {
                land.freeze = GUILayout.Toggle(land.freeze, "Freeze VL");
            }
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Hide All Ent Gfx"))
            {
                cellManager.HideAllEntityRenderers();
            }
            if (GUILayout.Button("Show All Ent Gfx"))
            {
                cellManager.ShowAllEntityRenderers();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (octCompiler != null)
            {
                octCompiler.LayoutGUI();
            }
        }

        private void UpdateBatchInstances(Int3 key, GameObject objectsRoot)
        {
            if (debugDisableInstances || Application.isPlaying)
            {
                return;
            }
            ProfilingUtils.BeginSample("UpdateBatchInstances");
            batch2insts[key] = new List<TileInstance>();
            bool flag = !Application.isPlaying;
            flag &= !debugBakedBatchCells;
            Bounds wsBounds = world.wsBounds;
            TileInstance[] componentsInChildren = objectsRoot.GetComponentsInChildren<TileInstance>();
            foreach (TileInstance tileInstance in componentsInChildren)
            {
                Int3.Bounds bounds = key.Refined(blocksPerBatch);
                if (!bounds.Contains(tileInstance.origin))
                {
                    global::UnityEngine.Debug.Log("Correcting tile origin from " + tileInstance.origin);
                    Int3 @int = tileInstance.origin % blocksPerBatch;
                    tileInstance.origin = bounds.mins + @int;
                    tileInstance.gridOffset = new Int3(tileInstance.origin.x.SafeMod(tileInstance.gridSize), tileInstance.origin.y.SafeMod(tileInstance.gridSize), tileInstance.origin.z.SafeMod(tileInstance.gridSize));
                    global::UnityEngine.Debug.Log("..corrected  to " + tileInstance.origin);
                }
                tileInstance.world = world;
                ProfilingUtils.BeginSample("Prepare");
                tileInstance.Prepare(flag, bounded: true, wsBounds);
                if (!world.batchWindow.Contains(key))
                {
                    tileInstance.gameObject.hideFlags |= HideFlags.NotEditable;
                }
                ProfilingUtils.EndSample();
                batch2insts[key].Add(tileInstance);
                maxInstanceLayer = Mathf.Max(maxInstanceLayer, tileInstance.layer);
                minInstanceLayer = Mathf.Min(minInstanceLayer, tileInstance.layer);
            }
            Light[] componentsInChildren2 = objectsRoot.GetComponentsInChildren<Light>();
            for (int j = 0; j < componentsInChildren2.Length; j++)
            {
                if (componentsInChildren2[j].type == LightType.Directional)
                {
                    componentsInChildren2[j].enabled = false;
                }
            }
            ProfilingUtils.EndSample();
        }

        public static void UpgradeAmbientSettings()
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            GameObject[] array = global::UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject gameObject in array)
            {
                if (!(gameObject.name == "Ambient Settings") && !(gameObject.name == "Additional Settings"))
                {
                    continue;
                }
                AmbientSettings component = gameObject.GetComponent<AmbientSettings>();
                if ((bool)component && !component.ambientLight.ApproxEquals(AmbientLightSettings.defaultColor, 0.003921569f))
                {
                    num2++;
                    Transform parent = gameObject.transform.parent;
                    if ((bool)parent)
                    {
                        AtmosphereVolume component2 = parent.GetComponent<AtmosphereVolume>();
                        if ((bool)component2)
                        {
                            component2.amb = component2.amb ?? new AmbientLightSettings();
                            component2.amb.enabled = true;
                            component2.amb.dayNightColor = global::UWE.Utils.DayNightGradient(component.ambientLight);
                            num3++;
                        }
                        LargeWorldBatchRoot component3 = parent.GetComponent<LargeWorldBatchRoot>();
                        if ((bool)component3)
                        {
                            component3.amb = component3.amb ?? new AmbientLightSettings();
                            component3.amb.enabled = true;
                            component3.amb.dayNightColor = global::UWE.Utils.DayNightGradient(component.ambientLight);
                            num4++;
                        }
                    }
                }
                global::UnityEngine.Object.DestroyImmediate(gameObject);
                num++;
            }
            if (num == 0)
            {
                global::UnityEngine.Debug.Log("All good. No obsolete ambient settings found");
                return;
            }
            global::UnityEngine.Debug.LogWarning("Killed " + num + " obsolete objects. " + num2 + " had data that got upgraded to " + num3 + " atmosphere volumes and " + num4 + " batch roots");
        }

        public void UpdateAllInstances()
        {
            using (new EditModeScopeTimer("UpdateAllInstances"))
            {
                batch2insts.Clear();
                foreach (KeyValuePair<Int3, LargeWorldBatchRoot> item in batch2root)
                {
                    UpdateBatchInstances(item.Key, item.Value.gameObject);
                }
            }
        }

        public List<TileInstance> GetInstances(Int3 batch)
        {
            if (batch2insts.TryGetValue(batch, out var value))
            {
                return value;
            }
            return null;
        }

        public void SaveAllBatchObjectsIntoCurrentSlot()
        {
            foreach (Int3 loadedBatch in loadedBatches)
            {
                SaveBatchObjectsIntoCurrentSlot(loadedBatch);
            }
        }

        public void OnChunkBuilt(Voxeland land, int cx, int cy, int cz)
        {
        }

        public void OnChunkHighLOD(Voxeland land, int cx, int cy, int cz)
        {
            Int3.Bounds blockRange = Int3.Bounds.FinerBounds(new Int3(cx, cy, cz), new Int3(land.chunkSize));
            cellManager.ShowEntities(blockRange);
        }

        public void OnChunkLowLOD(Voxeland land, int cx, int cy, int cz)
        {
            OnChunkDestroyedOrLowLOD(land, cx, cy, cz);
        }

        public void OnChunkDestroyed(Voxeland land, int cx, int cy, int cz)
        {
            OnChunkDestroyedOrLowLOD(land, cx, cy, cz);
        }

        public void OnChunkDestroyedOrLowLOD(Voxeland land, int cx, int cy, int cz)
        {
            if (Application.isPlaying && IsReady())
            {
                Int3.Bounds blockRange = Int3.Bounds.FinerBounds(new Int3(cx, cy, cz), new Int3(land.chunkSize));
                cellManager.HideEntities(blockRange);
            }
        }

        public Int3 GetBlock(Vector3 wsPos)
        {
            return Int3.Floor(land.transform.InverseTransformPoint(wsPos));
        }

        public string GetOverrideBiome(Int3 block)
        {
            string result = null;
            Int3 key = block / blocksPerBatch;
            if (batch2root.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.overrideBiome))
            {
                result = value.overrideBiome;
            }
            return result;
        }

        public void GenerateBiomeMap3D()
        {
            Int3 texelsPerBatch = blocksPerBatch / 32;
            Int3 upperBound = worldSize.CeilDiv(32);
            Array3<byte> biome3d = new Array3<byte>(upperBound.x, upperBound.y, upperBound.z);
            byte colorsSerial = 1;
            Dictionary<string, byte> biomeColors = new Dictionary<string, byte>();
            foreach (KeyValuePair<Int3, BiomeProperties> item in world.biomeMapLegend)
            {
                string text = item.Value.name;
                if (!string.IsNullOrEmpty(text))
                {
                    biomeColors[text.ToLowerInvariant()] = colorsSerial++;
                }
            }
            colorsSerial += 10;
            Int2.RangeEnumerator enumerator2 = Int2.Range(upperBound.xz).GetEnumerator();
            while (enumerator2.MoveNext())
            {
                Int2 current = enumerator2.Current;
                Int2 blockXZ = current * 32 + 16;
                BiomeProperties biomeProperties = world.GetBiomeProperties(blockXZ);
                byte value = biomeColors[biomeProperties.name.ToLowerInvariant()];
                for (int i = 0; i < upperBound.y; i++)
                {
                    biome3d[current.x, i, current.y] = value;
                }
            }
            using (PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy())
            {
                foreach (Int3 index in Int3.Range(batchCount))
                {
                    string batchObjectsFilename = GetBatchObjectsFilename(index);
                    string batchObjectsPath = GetBatchObjectsPath(tmpPathPrefix, batchObjectsFilename);
                    string batchObjectsPath2 = GetBatchObjectsPath(pathPrefix, batchObjectsFilename);
                    string batchObjectsPath3 = GetBatchObjectsPath(fallbackPrefix, batchObjectsFilename);
                    string text2 = global::UWE.Utils.Either(batchObjectsPath, batchObjectsPath2, batchObjectsPath3);
                    if (text2 == null)
                    {
                        continue;
                    }
                    pooledObject.Value.LoadObjectTreeFromFile(text2, delegate(GameObject go)
                    {
                        string overrideBiome = go.GetComponent<LargeWorldBatchRoot>().overrideBiome;
                        if (!string.IsNullOrEmpty(overrideBiome))
                        {
                            if (!biomeColors.TryGetValue(overrideBiome.ToLowerInvariant(), out var value2))
                            {
                                global::UnityEngine.Debug.LogErrorFormat(this, "Biome {0} in batch {1} not defined in biome legend {2}", overrideBiome, index, world.legendColorsPath);
                                byte b = colorsSerial;
                                colorsSerial = (byte)(b + 1);
                                byte b2 = b;
                                biomeColors[overrideBiome.ToLowerInvariant()] = b2;
                                value2 = b2;
                            }
                            Int3 @int = index * texelsPerBatch;
                            Int3 int2 = @int + texelsPerBatch;
                            foreach (Int3 item2 in Int3.Range(@int, int2 - 1))
                            {
                                biome3d.Set(item2, value2);
                            }
                        }
                        global::UnityEngine.Object.DestroyImmediate(go);
                    }, 0);
                }
            }
            using Stream stream = FileUtils.CreateFile("biome3d.map");
            foreach (Int3 item3 in Int3.Range(upperBound))
            {
                stream.WriteByte(biome3d.Get(item3));
            }
        }

        public static string GetBatchHeightmapPath(string prefix, Int2 batch)
        {
            return global::Platform.IO.Path.Combine(global::Platform.IO.Path.Combine(prefix, "BatchHeightmap"), "batch-heightmap-" + batch.x + "-" + batch.y + ".heightmap");
        }

        private void LoadBatchHeightmapThreaded(Int2 batchId)
        {
            ProfilingUtils.BeginSample("LoadBatchHeightmap");
            string batchHeightmapPath = GetBatchHeightmapPath(fallbackPrefix, batchId);
            heightLoadBufferLoaded = false;
            if (FileUtils.FileExists(batchHeightmapPath))
            {
                using Stream stream = FileUtils.ReadFile(batchHeightmapPath);
                using PooledBinaryReader pooledBinaryReader = new PooledBinaryReader(stream);
                pooledBinaryReader.ReadInt32();
                if (pooledBinaryReader.ReadInt32() != blocksPerBatch.x)
                {
                    throw new Exception("Mismatch between batch-heightmap " + batchHeightmapPath + " and current world.");
                }
                if (hmapBufferBytes == null || hmapBufferBytes.Length != hmapBufferSize)
                {
                    hmapBufferBytes = new byte[hmapBufferSize];
                }
                int num = 2 * blocksPerBatch.xz.Product();
                if (heightLoadBuffer == null || heightLoadBuffer.Length != num / 2)
                {
                    heightLoadBuffer = new ushort[num / 2];
                }
                int num2 = 0;
                int num3 = 0;
                while ((num3 = pooledBinaryReader.Read(hmapBufferBytes, 0, hmapBufferSize)) != 0)
                {
                    Buffer.BlockCopy(hmapBufferBytes, 0, heightLoadBuffer, num2, num3);
                    num2 += num3;
                }
                heightLoadBufferLoaded = true;
            }
            ProfilingUtils.EndSample();
        }

        private void FinalizeLoadBatchHeightmap(Int2 batchId)
        {
            if (heightLoadBufferLoaded)
            {
                heightLoadBufferLoaded = false;
                ProfilingUtils.BeginSample("FinalizeLoadBatchHeightmap");
                int num = 0;
                Int2.RangeEnumerator enumerator = Int2.Range(batchId * blocksPerBatch.xz, (batchId + 1) * blocksPerBatch.xz - 1).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Int2 current = enumerator.Current;
                    world.heightmap.SetHeightRaw(current, heightLoadBuffer[num++]);
                }
                ProfilingUtils.EndSample();
            }
        }

        private void SaveBatchHeightmap(Int2 batchId)
        {
            Utils.AssertEditMode();
            if (world.heightmap == null || !world.heightLockedBatches.Contains(batchId))
            {
                return;
            }
            using BinaryWriter binaryWriter = new BinaryWriter(FileUtils.CreateFile(GetBatchHeightmapPath(fallbackPrefix, batchId)));
            binaryWriter.Write(0);
            binaryWriter.Write(blocksPerBatch.x);
            Int2.RangeEnumerator enumerator = Int2.Range(batchId * blocksPerBatch.xz, (batchId + 1) * blocksPerBatch.xz - 1).GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int2 current = enumerator.Current;
                binaryWriter.Write(world.heightmap.GetHeightRaw(current));
            }
        }

        public void ReloadBiomeMap()
        {
            if (FileUtils.FileExists(world.biomeMapPath))
            {
                world.InitializeBiomeMap();
            }
            octCompiler.ResetCompiled();
            foreach (Int3 item in world.batchWindow)
            {
                NotifyBatchReadyToCompile(item);
            }
        }

        public bool OctreeRaycast(Vector3 startPoint, Vector3 endPoint, out Int3 hit)
        {
            using (new ProfilingUtils.Sample("OctreeRaycast"))
            {
                int num = blocksPerTree;
                int num2 = (int)(Vector3.Distance(startPoint, endPoint) / (float)num * 3f);
                Int3 block = GetBlock(startPoint);
                Int3 block2 = GetBlock(endPoint);
                for (int i = 0; i <= num2; i++)
                {
                    Int3 @int = Int3.Lerp(block, block2, 0, num2, i);
                    Int3 int2 = @int / num;
                    if (!CheckRoot(int2))
                    {
                        continue;
                    }
                    if ((bool)streamerV2)
                    {
                        BatchOctreesStreamer octreesStreamer = streamerV2.octreesStreamer;
                        if (octreesStreamer == null)
                        {
                            hit = Int3.zero;
                            return false;
                        }
                        Octree octree = octreesStreamer.GetOctree(int2);
                        if (octree != null && !octree.IsEmpty())
                        {
                            hit = @int;
                            return true;
                        }
                    }
                    else
                    {
                        CompactOctree compactOctree = compactTrees.Get(int2);
                        if (compactOctree != null && !compactOctree.IsEmpty())
                        {
                            hit = @int;
                            return true;
                        }
                    }
                }
                hit = Int3.zero;
                return false;
            }
        }

        public IEnumerable<Int3> LoadedBatches()
        {
            return loadedBatches;
        }

        public IEnumerable<TileInstance> LoadedInstances()
        {
            foreach (Int3 loadedBatch in loadedBatches)
            {
                if (!batch2insts.ContainsKey(loadedBatch))
                {
                    continue;
                }
                List<TileInstance> list = batch2insts[loadedBatch];
                if (list == null)
                {
                    continue;
                }
                foreach (TileInstance item in list)
                {
                    if (item != null)
                    {
                        yield return item;
                    }
                }
            }
        }

        public bool IsAuthoredBatch(Int3 batchId)
        {
            return world.existingBatches.Contains(batchId);
        }

        public byte GetBlockType(Vector3 wsPos)
        {
            Int3 block = GetBlock(wsPos);
            Int3 @int = block / blocksPerTree;
            Int3 int2 = block % blocksPerTree;
            if (!CheckRoot(@int))
            {
                return 0;
            }
            if ((bool)streamerV2)
            {
                return streamerV2.octreesStreamer?.GetBlockType(block) ?? 0;
            }
            CompactOctree compactOctree = compactTrees.Get(@int);
            if (compactOctree == null)
            {
                global::UnityEngine.Debug.LogWarningFormat(this, "Missing octree in LargeWorldStreamer.GetBlockType({0}) for (block {1}, root {2}, coords {3})", wsPos, block, @int, int2);
                return 0;
            }
            int nodeId = compactOctree.GetNodeId(int2, blocksPerTree);
            return compactOctree.GetType(nodeId);
        }

        public void PerformVoxelEdit(Bounds wsBounds, DistanceField df, bool isAdd = false, byte type = 1)
        {
            Int3 mins = Int3.Floor(land.transform.InverseTransformPoint(wsBounds.min));
            Int3 maxs = Int3.Floor(land.transform.InverseTransformPoint(wsBounds.max));
            PerformVoxelEdit(new Int3.Bounds(mins, maxs), df, isAdd, type);
        }

        public void PerformVoxelEdit(Int3.Bounds blockBounds, DistanceField df, bool isAdd = false, byte type = 1)
        {
            VoxelandData.OctNode.BlendArgs args = new VoxelandData.OctNode.BlendArgs((!isAdd) ? VoxelandData.OctNode.BlendOp.Subtraction : VoxelandData.OctNode.BlendOp.Union, replaceTypes: false, (byte)(isAdd ? type : 0));
            blockBounds = blockBounds.Expanded(1);
            foreach (Int3 item in blockBounds / blocksPerTree)
            {
                if (!CheckRoot(item))
                {
                    continue;
                }
                CompactOctree compactOctree = compactTrees.Get(item);
                if (compactOctree == null)
                {
                    continue;
                }
                Int3.Bounds bounds = item.Refined(blocksPerTree);
                VoxelandData.OctNode root = compactOctree.ToVLOctree();
                foreach (Int3 item2 in bounds.Intersect(blockBounds))
                {
                    Vector3 wsPos = land.transform.TransformPoint(item2 + global::UWE.Utils.half3);
                    float num = df(wsPos);
                    VoxelandData.OctNode n = new VoxelandData.OctNode((byte)((num >= 0f) ? type : 0), VoxelandData.OctNode.EncodeDensity(num));
                    int num2 = blocksPerTree;
                    int x = item2.x % num2;
                    int y = item2.y % num2;
                    int z = item2.z % num2;
                    VoxelandData.OctNode octNode = VoxelandData.OctNode.Blend(root.GetNode(x, y, z, num2 / 2), n, args);
                    root.SetNode(x, y, z, num2 / 2, octNode.type, octNode.density);
                }
                root.Collapse();
                compactTrees.Set(item, CompactOctree.CreateFrom(root));
                root.Clear();
            }
        }

        public void PerformSphereEdit(Vector3 center, float radius, bool isAdd = false, byte type = 1)
        {
            _ = center;
            float r = radius;
            Vector3 size = 2f * new Vector3(r, r, r);
            Bounds wsBounds = new Bounds(center, size);
            PerformVoxelEdit(wsBounds, (Vector3 wsPos) => r - Vector3.Distance(wsPos, center), isAdd, type);
        }

        public void PerformBoxEdit(Bounds bb, Quaternion rot, bool isAdd = false, byte type = 1)
        {
            Bounds wsBounds = bb;
            wsBounds.Expand(new Vector3(1f, 1f, 1f));
            Quaternion invRot = Quaternion.Inverse(rot);
            Vector3 c = bb.center;
            PerformVoxelEdit(wsBounds, (Vector3 wsPos) => VoxelandMisc.SignedDistToBox(bb, c + invRot * (wsPos - c)), isAdd, type);
        }

        public bool GetDisableFarColliders()
        {
            return settings.disableFarColliders;
        }

        public bool IsRangeActiveAndBuilt(Bounds bb)
        {
            if (!streamerV2 || streamerV2.clipmapStreamer == null)
            {
                return false;
            }
            Int3.Bounds blockRange = Int3.MinMax(GetBlock(bb.min), GetBlock(bb.max));
            return streamerV2.clipmapStreamer.IsRangeActiveAndBuilt(blockRange);
        }

        public int EstimateCompactOctreeBytes()
        {
            return CompactOctree.EstimateBytes();
        }

        public long EstimateClipMapManagerBytes()
        {
            return streamerV2 ? streamerV2.EstimateBytes() : (-1);
        }

        public Bounds GetBatchBounds(Int3 batch)
        {
            Int3.Bounds bounds = batch.Refined(blocksPerBatch);
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);
            result.SetMinMax(land.transform.TransformPoint(bounds.mins.ToVector3()), land.transform.TransformPoint((bounds.maxs + 1).ToVector3()));
            return result;
        }

        private static void OnLoadActivity(object sender, EventArgs args)
        {
            LargeWorldStreamer.onLoadActivity?.Invoke(sender, args);
        }
    }
}
