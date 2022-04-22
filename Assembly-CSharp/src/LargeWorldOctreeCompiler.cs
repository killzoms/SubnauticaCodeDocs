using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class LargeWorldOctreeCompiler : VoxelandData.EventHandler
    {
        public delegate void OnRequestsProgress(int numOutstanding);

        public class NotifyBlock : IDisposable
        {
            private LargeWorldOctreeCompiler compiler;

            public NotifyBlock(LargeWorldOctreeCompiler compiler)
            {
                this.compiler = compiler;
                if (compiler != null)
                {
                    Monitor.Enter(compiler.rootsLock);
                }
            }

            public void Dispose()
            {
                if (compiler != null)
                {
                    Monitor.PulseAll(compiler.rootsLock);
                    Monitor.Exit(compiler.rootsLock);
                    compiler = null;
                }
            }
        }

        private LargeWorldStreamer streamer;

        private bool abortRequested;

        private Int3 viewRootShared = new Int3(-1);

        private VoxelandData dest;

        private VoxelandData voxelSrc;

        private VoxelandData bakedSrc;

        private Array3<CompactOctree> compactDest;

        private readonly object compileLock = new object();

        private readonly HashSet<Int3> lowDetailRootsCompiled = new HashSet<Int3>(Int3.equalityComparer);

        private readonly HashSet<Int3> rootsCompiled = new HashSet<Int3>(Int3.equalityComparer);

        private readonly HashSet<Int3> rootsCompiling = new HashSet<Int3>(Int3.equalityComparer);

        private readonly HashSet<Int3> rootsReadyToCompile = new HashSet<Int3>(Int3.equalityComparer);

        private bool shouldRestartOutwardWalk;

        public bool debugFreeze;

        private HeightmapBoundsCache heightBounds;

        private ObjectPool<VoxelizeHeightmapTask> heightTaskPool = ObjectPoolHelper.CreatePool<VoxelizeHeightmapTask>("LargeWorldOctreeCompiler::VoxelizeHeightmapTask", Environment.ProcessorCount / 2);

        private object requestLock = new object();

        private object rootsLock = new object();

        private int numOutstandingRequests;

        private uint[] compressedCompiledRoots;

        private int compressedCompiledStrideX;

        private int compressedCompiledStrideY;

        private const int MAX_ENTRIES_PER_STRIDE = 10;

        private const int COMPILED_NUM_BITS_PER_ENTRY = 32;

        private uint[] bitPatternsToCheck = new uint[10];

        public bool debugDisableInstances { get; private set; }

        private LargeWorld.Heightmap heightmap => streamer.heightmap;

        public int numRootsCompiled { get; private set; }

        public int numRootsToCompile => rootsReadyToCompile.Count;

        public int GetNumOutstandingRequests()
        {
            return numOutstandingRequests;
        }

        public void WaitForRequests(OnRequestsProgress cb)
        {
            lock (requestLock)
            {
                while (numOutstandingRequests > 0)
                {
                    cb?.Invoke(numOutstandingRequests);
                    Monitor.Wait(requestLock);
                }
                cb?.Invoke(numOutstandingRequests);
            }
        }

        public void RequestCompile(Int3 root)
        {
            if (!CheckRoot(root))
            {
                return;
            }
            lock (rootsLock)
            {
                if (rootsCompiled.Contains(root) || !rootsCompiling.Add(root))
                {
                    return;
                }
            }
            lock (requestLock)
            {
                numOutstandingRequests++;
                Monitor.Pulse(requestLock);
            }
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                CompileRoot(root);
                lock (requestLock)
                {
                    numOutstandingRequests--;
                    Monitor.Pulse(requestLock);
                }
            });
        }

        public LargeWorldOctreeCompiler(LargeWorldStreamer streamer, VoxelandData voxelSrc, VoxelandData bakedSrc, VoxelandData dest, Array3<CompactOctree> compactDest)
        {
            this.streamer = streamer;
            this.voxelSrc = voxelSrc;
            this.bakedSrc = bakedSrc;
            this.dest = dest;
            this.compactDest = compactDest;
            heightBounds = new HeightmapBoundsCache(dest.biggestNode, dest.GetSize().xz / dest.biggestNode, (Int2 xz) => streamer.world.heightmap.GetHeight(xz.x, xz.y));
            if (compressedCompiledRoots == null)
            {
                Int3 @int = dest.GetSize() / streamer.blocksPerTree;
                @int.x /= 32;
                compressedCompiledRoots = new uint[@int.x * @int.y * @int.z];
                compressedCompiledStrideX = @int.x;
                compressedCompiledStrideY = @int.y;
                ClearCompressedCompiledRoots();
            }
        }

        public void OnHeightmapChanged()
        {
            heightBounds.Reset();
            ResetCompiled();
        }

        public void ResetCompiled()
        {
            lock (rootsLock)
            {
                rootsCompiled.Clear();
                ClearCompressedCompiledRoots();
                Monitor.PulseAll(rootsLock);
            }
        }

        public void UpdateViewerOctree(Int3 root)
        {
            lock (rootsLock)
            {
                if (root != viewRootShared)
                {
                    viewRootShared = root;
                    Monitor.PulseAll(rootsLock);
                }
            }
        }

        public void NotifyRootReadyToCompileNoLock(Int3 root)
        {
            if (!rootsCompiled.Contains(root) && !rootsCompiling.Contains(root) && rootsReadyToCompile.Add(root))
            {
                shouldRestartOutwardWalk = true;
            }
        }

        public void NotifyLowDetailRootAlreadyCompiled(Int3 root)
        {
            if (CheckRoot(root))
            {
                lock (compileLock)
                {
                    lowDetailRootsCompiled.Add(root);
                    Monitor.PulseAll(compileLock);
                }
            }
        }

        public void NotifyRootAlreadyCompiledNoLock(Int3 root)
        {
            rootsReadyToCompile.Remove(root);
            rootsCompiled.Add(root);
            setCompressedCompiledRoot(root, compiled: true);
            lowDetailRootsCompiled.Add(root);
        }

        public void NotifyRootUnloadedNoLock(Int3 root, bool keepLowDetailRoot)
        {
            rootsReadyToCompile.Remove(root);
            rootsCompiled.Remove(root);
            setCompressedCompiledRoot(root, compiled: false);
            if (!keepLowDetailRoot)
            {
                lowDetailRootsCompiled.Remove(root);
            }
        }

        public void NotifyRootsUnloading(Int3.Bounds bounds)
        {
            ProfilingUtils.BeginSample("LargeWorldOctreeCompiler.NotifyRootsUnloading");
            lock (rootsLock)
            {
                foreach (Int3 item in bounds)
                {
                    if (CheckRoot(item))
                    {
                        rootsReadyToCompile.Remove(item);
                        rootsCompiled.Remove(item);
                        setCompressedCompiledRoot(item, compiled: false);
                    }
                }
                Monitor.PulseAll(rootsLock);
            }
            ProfilingUtils.EndSample();
        }

        public void RequestAbort()
        {
            lock (rootsLock)
            {
                abortRequested = true;
                streamer = null;
                dest = null;
                compactDest = null;
                voxelSrc = null;
                rootsCompiled.Clear();
                rootsCompiling.Clear();
                rootsReadyToCompile.Clear();
                heightBounds = null;
                heightTaskPool = null;
                ClearCompressedCompiledRoots();
                Monitor.PulseAll(rootsLock);
            }
        }

        public void SetDisableInstances(bool disable)
        {
            if (disable != debugDisableInstances)
            {
                lock (rootsLock)
                {
                    debugDisableInstances = disable;
                    rootsCompiled.Clear();
                    ClearCompressedCompiledRoots();
                    Monitor.PulseAll(rootsLock);
                }
            }
        }

        public void MainLoop(object dummy)
        {
            try
            {
                global::Platform.Utils.ThreadUtils.SetThreadName("OctCompiler");
                global::Platform.Utils.ThreadUtils.SetThreadPriority(System.Threading.ThreadPriority.BelowNormal);
                while (!abortRequested)
                {
                    Int3 @int = Int3.negativeOne;
                    bool flag = true;
                    lock (rootsLock)
                    {
                        Int3 int2 = viewRootShared;
                        if (int2 != Int3.negativeOne)
                        {
                            HashSet<Int3>.Enumerator enumerator = rootsReadyToCompile.GetEnumerator();
                            int num = int.MaxValue;
                            Int3 int3 = Int3.negativeOne;
                            while (enumerator.MoveNext())
                            {
                                Int3 current = enumerator.Current;
                                int num2 = (int2 - current).SquareMagnitude();
                                if (num2 < num)
                                {
                                    num = num2;
                                    int3 = current;
                                }
                            }
                            if (int3 != Int3.negativeOne)
                            {
                                if (rootsCompiled.Contains(int3))
                                {
                                    rootsReadyToCompile.Remove(int3);
                                }
                                else
                                {
                                    @int = int3;
                                    rootsCompiling.Add(@int);
                                }
                                flag = false;
                            }
                        }
                        if (debugFreeze)
                        {
                            Monitor.Wait(rootsLock);
                            continue;
                        }
                    }
                    if (@int != Int3.negativeOne)
                    {
                        CompileRoot(@int);
                    }
                    if (flag && !abortRequested)
                    {
                        lock (rootsLock)
                        {
                            Monitor.Wait(rootsLock, 500);
                        }
                    }
                }
                global::UnityEngine.Profiling.Profiler.EndThreadProfiling();
                Debug.Log("MainLoop aborted");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        public bool IsRootCompiled(Int3 r, bool lowDetail)
        {
            if (!CheckRoot(r))
            {
                return true;
            }
            lock (rootsLock)
            {
                return (lowDetail ? lowDetailRootsCompiled : rootsCompiled).Contains(r);
            }
        }

        public bool IsRootReadyToCompile(Int3 r)
        {
            if (!CheckRoot(r))
            {
                return true;
            }
            lock (rootsLock)
            {
                return rootsReadyToCompile.Contains(r);
            }
        }

        public bool ContainsCompilingRoot(Int3.Bounds bounds)
        {
            lock (rootsLock)
            {
                foreach (Int3 item in bounds)
                {
                    if (CheckRoot(item) && rootsCompiling.Contains(item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void OnRootChanged(int rx, int ry, int rz)
        {
            Int3 @int = new Int3(rx, ry, rz);
            if (!CheckRoot(@int))
            {
                return;
            }
            lock (rootsLock)
            {
                if (rootsCompiled.Remove(@int))
                {
                    shouldRestartOutwardWalk = true;
                    Monitor.PulseAll(rootsLock);
                }
            }
        }

        public void OnRangeChanged(Int3.Bounds blocks)
        {
            lock (rootsLock)
            {
                foreach (Int3 item in blocks / streamer.blocksPerTree)
                {
                    OnRootChanged(item.x, item.y, item.z);
                }
            }
        }

        private void WriteAdditiveClearingInstances(Int3 root)
        {
            Int3 @int = root / streamer.treesPerBatch;
            Int3 mins = @int - 1;
            Int3 maxs = @int + 1;
            foreach (Int3 item in Int3.Range(mins, maxs))
            {
                List<TileInstance> instances = streamer.GetInstances(item);
                if (instances == null)
                {
                    continue;
                }
                foreach (TileInstance item2 in instances)
                {
                    if (item2.blendMode == VoxelBlendMode.Additive && item2.clearHeightmap)
                    {
                        item2.ClearRoot(dest, root);
                    }
                }
            }
        }

        private void WriteInstances(Int3 root, int firstLayer, int lastLayer)
        {
            if (firstLayer > lastLayer)
            {
                return;
            }
            Int3 @int = root / streamer.treesPerBatch;
            Int3 mins = @int - 1;
            Int3 maxs = @int + 1;
            for (int i = firstLayer; i <= lastLayer; i++)
            {
                foreach (Int3 item in Int3.Range(mins, maxs))
                {
                    List<TileInstance> instances = streamer.GetInstances(item);
                    if (instances == null)
                    {
                        continue;
                    }
                    foreach (TileInstance item2 in instances)
                    {
                        if (item2.layer == i)
                        {
                            item2.CopyIntoRoot(dest, root);
                        }
                    }
                }
            }
        }

        public bool CheckRoot(Int3 root)
        {
            if (!streamer)
            {
                return false;
            }
            return streamer.CheckRoot(root);
        }

        private void CompileRoot(Int3 root)
        {
            try
            {
                lock (rootsLock)
                {
                    if (abortRequested)
                    {
                        return;
                    }
                }
                int rootIndex = dest.GetRootIndex(root);
                if (rootIndex < 0)
                {
                    return;
                }
                ProfilingUtils.BeginSample("CompileRoot");
                dest.roots[rootIndex].Clear();
                Int3.Bounds rootBlockBounds = streamer.GetRootBlockBounds(root);
                Int3 b = root / streamer.treesPerBatch;
                if (!streamer.world.IsBatchBaked(b))
                {
                    if (streamer.world.heightmap != null)
                    {
                        ProfilingUtils.BeginSample("Voxelize Heightmap");
                        VoxelizeHeightmapTask voxelizeHeightmapTask = heightTaskPool.Get();
                        voxelizeHeightmapTask.Reset(heightBounds, dest, streamer.world);
                        voxelizeHeightmapTask.CompileHeightmap(root);
                        heightTaskPool.Return(voxelizeHeightmapTask);
                        ProfilingUtils.EndSample();
                    }
                }
                else
                {
                    dest.roots[rootIndex].SetBottomUp(bakedSrc, rootBlockBounds.mins.x, rootBlockBounds.mins.y, rootBlockBounds.mins.z, streamer.blocksPerTree / 2, new VoxelandData.OctNode.BlendArgs(VoxelandData.OctNode.BlendOp.Union, replaceTypes: true, 0));
                }
                if (!debugDisableInstances)
                {
                    WriteAdditiveClearingInstances(root);
                    WriteInstances(root, streamer.minInstanceLayer, -1);
                }
                ProfilingUtils.BeginSample("Add edited voxels");
                dest.roots[rootIndex].SetBottomUp(voxelSrc, rootBlockBounds.mins.x, rootBlockBounds.mins.y, rootBlockBounds.mins.z, streamer.blocksPerTree / 2, new VoxelandData.OctNode.BlendArgs(VoxelandData.OctNode.BlendOp.Union, replaceTypes: true, 0));
                ProfilingUtils.EndSample();
                if (!debugDisableInstances)
                {
                    WriteInstances(root, 0, streamer.maxInstanceLayer);
                }
                if (streamer.debugBiomeMaterials)
                {
                    dest.roots[rootIndex].SetBottomUp(new LargeWorld.BiomeVoxelGrid(streamer.world), rootBlockBounds.mins.x, rootBlockBounds.mins.y, rootBlockBounds.mins.z, streamer.blocksPerTree / 2, new VoxelandData.OctNode.BlendArgs(VoxelandData.OctNode.BlendOp.Intersection, replaceTypes: true, 0));
                }
                dest.roots[rootIndex].Collapse();
                compactDest.Set(root, CompactOctree.CreateFrom(dest.roots[rootIndex]));
                ProfilingUtils.EndSample();
                lock (rootsLock)
                {
                    rootsReadyToCompile.Remove(root);
                    rootsCompiling.Remove(root);
                    rootsCompiled.Add(root);
                    setCompressedCompiledRoot(root, compiled: true);
                    numRootsCompiled++;
                }
            }
            catch (NullReferenceException ex)
            {
                Debug.LogError(string.Concat("NullRef while updating root ", root, " - this is probably OK and just means the game is shutting down: ", ex.StackTrace));
            }
            catch (Exception ex2)
            {
                Debug.LogErrorFormat("Exception while updating root {0}: {1}", root, ex2);
                Debug.LogException(ex2);
            }
        }

        public void LayoutGUI()
        {
            bool flag = GUILayout.Toggle(debugFreeze, "Freeze octree compiler");
            if (debugFreeze != flag)
            {
                debugFreeze = flag;
                lock (rootsLock)
                {
                    Monitor.PulseAll(rootsLock);
                }
            }
        }

        private int getCompressedCompiledRootIndex(Int3 root)
        {
            return root.x / 32 + compressedCompiledStrideX * root.y + compressedCompiledStrideX * compressedCompiledStrideY * root.z;
        }

        private int getBitMaskPatternForRange(int inByte, int inBit, int rangeWidth)
        {
            int num = 0;
            if (inBit + rangeWidth < 32)
            {
                if (inBit == 0)
                {
                    bitPatternsToCheck[num] = (uint)((1 << rangeWidth) | ((1 << rangeWidth) - 1));
                }
                else
                {
                    int num2 = inBit + rangeWidth;
                    uint num3 = (uint)((1 << num2) | ((1 << num2) - 1));
                    uint num4 = (uint)((1 << inBit) - 1);
                    bitPatternsToCheck[num] = num4 ^ num3;
                }
                num++;
            }
            else
            {
                bitPatternsToCheck[num] = 0xFFFFFFFFu ^ (uint)((1 << inBit) - 1);
                num++;
                int num5 = rangeWidth - (32 - inBit);
                int num6 = num5 / 32;
                int num7 = num5 % 32;
                for (int i = 0; i < num6; i++)
                {
                    bitPatternsToCheck[num] = uint.MaxValue;
                    num++;
                }
                if (num7 != 0)
                {
                    bitPatternsToCheck[num] = (uint)((1 << num7) | ((1 << num7) - 1));
                    num++;
                }
            }
            return num;
        }

        private unsafe void setCompressedCompiledRoot(Int3 root, bool compiled)
        {
            int compressedCompiledRootIndex = getCompressedCompiledRootIndex(root);
            int num = root.x % 32;
            fixed (uint* ptr = &compressedCompiledRoots[compressedCompiledRootIndex])
            {
                int num2 = 0;
                int num3 = 0;
                do
                {
                    int* ptr2 = (int*)ptr;
                    num3 = *ptr2;
                    int value = (compiled ? (num3 | (1 << num)) : (num3 & ~(1 << num)));
                    num2 = Interlocked.Exchange(ref *ptr2, value);
                }
                while (num2 != num3);
            }
        }

        public bool IsRangeCompiled(Int3 min, Int3 max)
        {
            Int3 @int = max - min;
            Int3 zero = Int3.zero;
            int num = 0;
            int num2 = 0;
            zero.x = min.x;
            int inBit = zero.x % 32;
            for (int i = 0; i <= @int.z; i++)
            {
                zero.z = min.z + i;
                for (int j = 0; j <= @int.y; j++)
                {
                    zero.y = min.y + j;
                    num = getCompressedCompiledRootIndex(zero);
                    num2 = getBitMaskPatternForRange(num, inBit, @int.x);
                    for (int k = 0; k < num2; k++)
                    {
                        if ((compressedCompiledRoots[num + k] & bitPatternsToCheck[k]) != bitPatternsToCheck[k])
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private unsafe void ClearCompressedCompiledRoots()
        {
            for (int i = 0; i < compressedCompiledRoots.Length; i++)
            {
                fixed (uint* ptr = &compressedCompiledRoots[i])
                {
                    int* ptr2 = (int*)ptr;
                    Interlocked.Exchange(ref *ptr2, 0);
                }
            }
        }
    }
}
