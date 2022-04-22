using System.Collections.Generic;
using UnityEngine;
using UWE;

namespace AssemblyCSharp.WorldStreaming
{
    public sealed class MeshBuilder : IVoxelandChunk, IVoxelandChunkInfo, IVoxeland
    {
        private const bool debugSingleBlockType = false;

        private const bool debugUseLQShader = false;

        private const bool debugAllOpaque = false;

        private const bool debugSkipMaterials = false;

        private const bool debugOverrideDisableGrass = false;

        private const bool debugDisableRenderOrderOpt = false;

        private static readonly MeshPool meshPool = new MeshPool();

        public readonly MeshBufferPools meshBufferPools = new MeshBufferPools(131072, 524288);

        private readonly VoxelandCollisionMeshSimplifier collisionMeshSimplifier = new VoxelandCollisionMeshSimplifier();

        private readonly VoxelandVisualMeshSimplifier visualMeshSimplifier = new VoxelandVisualMeshSimplifier();

        private readonly VoxelandGrassBuilder grassBuilder = new VoxelandGrassBuilder();

        private readonly VoxelandChunkWorkspace chunkWorkspace = new VoxelandChunkWorkspace();

        private Int3 cellId;

        private int levelId;

        private ClipMapManager.LevelSettings levelSettings;

        private VoxelandBlockType[] blockTypes;

        private readonly List<VoxelandChunk.TypeUse> usedTypes = new List<VoxelandChunk.TypeUse>();

        private Int3 offset;

        private int meshRes;

        private readonly CppVoxelandFaceScanner faceScanner = new CppVoxelandFaceScanner();

        private BatchOctreesStreamer octreesStreamer;

        List<VoxelandChunk.TypeUse> IVoxelandChunkInfo.usedTypes => usedTypes;

        IVoxeland IVoxelandChunkInfo.land => this;

        VoxelandChunkWorkspace IVoxelandChunk.ws => chunkWorkspace;

        int IVoxelandChunk.downsamples => levelSettings.downsamples;

        int IVoxelandChunk.offsetX => offset.x;

        int IVoxelandChunk.offsetY => offset.y;

        int IVoxelandChunk.offsetZ => offset.z;

        int IVoxelandChunk.meshRes => meshRes;

        bool IVoxelandChunk.skipHiRes => levelSettings.visual.useLowMesh;

        float IVoxelandChunk.surfaceDensityValue => 0f;

        bool IVoxelandChunk.disableGrass => true;

        public bool debugThoroughTopologyChecks => false;

        bool IVoxeland.debugBlocky => false;

        bool IVoxeland.debugLogMeshing => false;

        bool IVoxeland.debugOneType => false;

        VoxelandNormalsSmooth IVoxeland.normalsSmooth => VoxelandNormalsSmooth.mesh;

        Voxeland.FaceCreator IVoxeland.faceCreator => faceScanner;

        VoxelandBlockType[] IVoxeland.types => blockTypes;

        Int3 IVoxeland.meshMins => Int3.zero;

        Int3 IVoxeland.meshMaxs => Int3.zero;

        VoxelandData IVoxeland.data => null;

        public void Reset(int levelId, Int3 cellId, int cellSize, ClipMapManager.LevelSettings levelSettings, VoxelandBlockType[] blockTypes)
        {
            this.cellId = cellId;
            this.levelId = levelId;
            this.levelSettings = levelSettings;
            this.blockTypes = blockTypes;
            int meshOverlap = levelSettings.meshOverlap;
            int downsamples = levelSettings.downsamples;
            offset = cellId * cellSize - (meshOverlap << downsamples);
            meshRes = (cellSize >> downsamples) + meshOverlap * 2;
            meshBufferPools.TryReset();
        }

        public void DoThreadablePart(BatchOctreesStreamer streamer, VoxelandCollisionMeshSimplifier.Settings colSettings)
        {
            octreesStreamer = streamer;
            MeshBufferPools pools = meshBufferPools;
            VoxelandCollisionMeshSimplifier voxelandCollisionMeshSimplifier = collisionMeshSimplifier;
            VoxelandVisualMeshSimplifier voxelandVisualMeshSimplifier = visualMeshSimplifier;
            VoxelandGrassBuilder voxelandGrassBuilder = grassBuilder;
            VoxelandChunkWorkspace voxelandChunkWorkspace = chunkWorkspace;
            _ = StopwatchProfiler.Instance;
            voxelandChunkWorkspace.SetSize(((IVoxelandChunk)this).meshRes);
            if (!levelSettings.ignoreMeshes)
            {
                VoxelandChunk.BuildMesh(this, levelSettings.skipRelax, levelSettings.maxBlockTypes);
            }
            if (voxelandChunkWorkspace.visibleFaces.Count > 0)
            {
                if (levelSettings.colliders)
                {
                    voxelandCollisionMeshSimplifier.inUse = true;
                    voxelandCollisionMeshSimplifier.settings = colSettings;
                    voxelandCollisionMeshSimplifier.SetPools(pools);
                    voxelandCollisionMeshSimplifier.Build(voxelandChunkWorkspace);
                }
                voxelandVisualMeshSimplifier.inUse = true;
                voxelandVisualMeshSimplifier.settings = levelSettings.visual;
                voxelandVisualMeshSimplifier.debugUseLQShader = false;
                voxelandVisualMeshSimplifier.debugAllOpaque = false;
                voxelandVisualMeshSimplifier.debugSkipMaterials = false;
                voxelandVisualMeshSimplifier.Build(this, pools, levelSettings.debug);
                if (levelSettings.grass)
                {
                    voxelandGrassBuilder.inUse = true;
                    voxelandGrassBuilder.Reset(pools);
                    voxelandGrassBuilder.CreateMeshData(this, levelSettings.grassSettings);
                }
            }
        }

        public ClipmapChunk DoFinalizePart(Transform chunkRoot, ClipmapChunk chunkPrefab, GameObject chunkLayerPrefab)
        {
            VoxelandCollisionMeshSimplifier voxelandCollisionMeshSimplifier = collisionMeshSimplifier;
            VoxelandVisualMeshSimplifier voxelandVisualMeshSimplifier = visualMeshSimplifier;
            VoxelandGrassBuilder voxelandGrassBuilder = grassBuilder;
            VoxelandChunkWorkspace voxelandChunkWorkspace = chunkWorkspace;
            _ = StopwatchProfiler.Instance;
            ClipmapChunk clipmapChunk = Object.Instantiate(chunkPrefab, chunkRoot);
            clipmapChunk.SetPosition(levelSettings.downsamples, offset);
            if (voxelandChunkWorkspace.visibleFaces.Count > 0)
            {
                clipmapChunk.gameObject.SetActive(value: true);
                int addSortingValue = levelId;
                voxelandVisualMeshSimplifier.BuildLayerObjects(clipmapChunk, this, levelSettings.castShadows, addSortingValue, meshPool, chunkLayerPrefab);
                voxelandVisualMeshSimplifier.inUse = false;
                if (levelSettings.colliders)
                {
                    voxelandCollisionMeshSimplifier.AttachTo(clipmapChunk, meshPool);
                    voxelandCollisionMeshSimplifier.inUse = false;
                }
                if (levelSettings.grass)
                {
                    voxelandGrassBuilder.CreateUnityMeshes(clipmapChunk, meshPool);
                    voxelandGrassBuilder.inUse = false;
                }
            }
            clipmapChunk.SetRenderersEnabled(enabled: false, fade: false);
            return clipmapChunk;
        }

        public static void DestroyMeshes(IVoxelandChunk2 chunk)
        {
            ProfilingUtils.BeginSample("DestroyMeshes");
            DestroyMeshes(chunk.hiFilters);
            DestroyMeshes(chunk.grassFilters);
            DestroyMeshes(chunk.collision);
            ProfilingUtils.EndSample();
        }

        private static void DestroyMeshes(List<MeshFilter> filters)
        {
            foreach (MeshFilter filter in filters)
            {
                if ((bool)filter)
                {
                    Object.Destroy(filter.sharedMesh);
                }
            }
        }

        private static void DestroyMeshes(MeshCollider collider)
        {
            if ((bool)collider)
            {
                Object.Destroy(collider.sharedMesh);
            }
        }

        void IVoxelandChunk.OnTypeUsed(byte typeNum)
        {
            VoxelandChunk.OnTypeUsed(usedTypes, typeNum);
        }

        bool IVoxelandChunk.IsBlockVisible(int x, int y, int z)
        {
            return VoxelandChunk.IsBlockVisible(meshRes, x, y, z);
        }

        Vector3 IVoxelandChunk.ComputeSurfaceIntersection(Vector3 p0, Vector3 p1, byte d0, byte d1)
        {
            return VoxelandChunk.ComputeSurfaceIntersection(p0, p1, d0, d1, ((IVoxelandChunk)this).surfaceDensityValue, ((IVoxelandChunk)this).downsamples);
        }

        IEnumerable<VoxelandChunk.GrassPos> IVoxelandChunk.EnumerateGrass(VoxelandTypeBase settings, byte typeFilter, int randSeed, double reduction)
        {
            return VoxelandChunk.EnumerateGrass(this, settings, typeFilter, randSeed, reduction);
        }

        void IVoxeland.RasterizeVoxels(Voxeland.RasterWorkspace ws, int wx0, int wy0, int wz0, int downsampleLevels)
        {
            Rasterizer.Rasterize(origin: new Int3(wx0, wy0, wz0), streamer: octreesStreamer, typesGrid: ws.typesGrid, densityGrid: ws.densityGrid, size: ws.size, chunkId: cellId, downsamples: downsampleLevels);
        }

        bool IVoxeland.IsLimitedMeshing()
        {
            return false;
        }
    }
}
