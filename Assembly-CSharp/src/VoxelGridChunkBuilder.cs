using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class VoxelGridChunkBuilder : VoxelandBulkChunkBuilder, VoxelandChunkBuilder
    {
        private const int maxVerts = 1048576;

        private const int maxIndices = 2097152;

        private readonly VoxelandChunkWorkspace chunkWorkspace = new VoxelandChunkWorkspace();

        private readonly MeshBufferPools bufferPools = new MeshBufferPools(1048576, 2097152);

        private readonly MeshPool chunkMeshPool = new MeshPool();

        private readonly VoxelandVisualMeshSimplifier visSimp;

        private readonly bool skipHiRes;

        private readonly bool skipRelax;

        private readonly int maxMaterials;

        private int currentChunk;

        private int numTotalChunks = 1;

        public VoxelGridChunkBuilder(VoxelandVisualMeshSimplifier visSimp, bool skipHiRes, bool skipRelax, int maxMaterials)
        {
            this.visSimp = visSimp;
            this.skipHiRes = skipHiRes;
            this.skipRelax = skipRelax;
            this.maxMaterials = maxMaterials;
        }

        public bool CanBuildMore()
        {
            return true;
        }

        public void Build(ChunkState state)
        {
            VoxelandChunk chunk = state.chunk;
            chunkWorkspace.SetSize(chunk.meshRes);
            chunk.skipHiRes = skipHiRes;
            chunk.disableGrass = true;
            chunk.ws = chunkWorkspace;
            chunk.BuildMesh(skipRelax, maxMaterials);
            if (chunkWorkspace.visibleFaces.Count > 0)
            {
                visSimp.Build(chunk, bufferPools);
                visSimp.BuildLayerObjects(chunk, castShadows: false, 0, chunkMeshPool, null);
            }
            chunk.ws = null;
        }

        public int GetMaxBuildsThisFrame()
        {
            return int.MaxValue;
        }

        public void OnBeginBuildingChunks(Voxeland land, int totalChunks)
        {
            numTotalChunks = Mathf.Max(totalChunks, 1);
        }

        public void OnEndBuildingChunks(Voxeland land)
        {
        }
    }
}
