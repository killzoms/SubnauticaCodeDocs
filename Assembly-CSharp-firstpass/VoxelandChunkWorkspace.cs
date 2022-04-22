using System;
using System.Collections.Generic;
using Gendarme;
using UnityEngine;

[Serializable]
public class VoxelandChunkWorkspace : IEstimateBytes
{
    [Serializable]
    public struct ChunkFaceId
    {
        public byte dx;

        public byte dy;

        public byte dz;

        public byte dir;

        public const int Bytes = 4;

        public override string ToString()
        {
            return dx + "," + dy + "," + dz + ":" + dir;
        }

        public void Reset()
        {
            dx = 0;
            dy = 0;
            dz = 0;
            dir = 0;
        }
    }

    [NonSerialized]
    private readonly LinkedList<VoxelandChunk.VoxelandBlock> blockPool = new LinkedList<VoxelandChunk.VoxelandBlock>();

    [NonSerialized]
    private readonly LinkedList<VoxelandChunk.VoxelandFace> facePool = new LinkedList<VoxelandChunk.VoxelandFace>();

    [NonSerialized]
    private readonly LinkedList<VoxelandChunk.VoxelandVert> vertPool = new LinkedList<VoxelandChunk.VoxelandVert>();

    [NonSerialized]
    private LinkedListNode<VoxelandChunk.VoxelandBlock> nextBlock;

    [NonSerialized]
    private LinkedListNode<VoxelandChunk.VoxelandFace> nextFace;

    [NonSerialized]
    private LinkedListNode<VoxelandChunk.VoxelandVert> nextVert;

    [NonSerialized]
    public Array3<VoxelandChunk.VoxelandBlock> blocks;

    public Int3 blocksLen;

    public int maxMeshRes;

    [NonSerialized]
    public readonly List<VoxelandChunk.VoxelandFace> faces = new List<VoxelandChunk.VoxelandFace>();

    [NonSerialized]
    public readonly List<VoxelandChunk.VoxelandFace> visibleFaces = new List<VoxelandChunk.VoxelandFace>();

    [NonSerialized]
    public readonly List<VoxelandChunk.VoxelandVert> verts = new List<VoxelandChunk.VoxelandVert>();

    public Voxeland.RasterWorkspace rws;

    private readonly LinkedList<VoxelandChunk.VLGrassVert> grassVertPool = new LinkedList<VoxelandChunk.VLGrassVert>();

    private readonly LinkedList<VoxelandChunk.VLGrassTri> grassTriPool = new LinkedList<VoxelandChunk.VLGrassTri>();

    private LinkedListNode<VoxelandChunk.VLGrassVert> nextGrassVert;

    private LinkedListNode<VoxelandChunk.VLGrassTri> nextGrassTri;

    [NonSerialized]
    public readonly List<VoxelandChunk.VLGrassMesh> grassMeshes = new List<VoxelandChunk.VLGrassMesh>();

    public int nextGrassMesh;

    [NonSerialized]
    public ChunkFaceId[] faceList;

    private static int nextWorkspaceId;

    private int workspaceId;

    [NonSerialized]
    public VoxelandChunk.VoxelandFace[] layerFaces;

    [NonSerialized]
    public VoxelandChunk.VoxelandVert[] layerVerts;

    [NonSerialized]
    public VoxelandChunk.VoxelandVert[] lowLayerVerts;

    public int WorkspaceId => workspaceId;

    public long EstimateBytes()
    {
        return rws.EstimateBytes() + blockPool.Count * VoxelandChunk.VoxelandBlock.EstimateBytes() + facePool.Count * VoxelandChunk.VoxelandFace.EstimateBytes() + vertPool.Count * VoxelandChunk.VoxelandVert.EstimateBytes() + blocks.Length * 4 + faceList.Length * 4;
    }

    public void LogMemoryProfile()
    {
        Debug.LogFormat("RWS: {0}", (float)rws.EstimateBytes() / 1024f / 1024f);
        Debug.LogFormat("blockPool: {0}", (float)(blockPool.Count * VoxelandChunk.VoxelandBlock.EstimateBytes()) / 1024f / 1024f);
        Debug.LogFormat("facePool: {0}", (float)(facePool.Count * VoxelandChunk.VoxelandFace.EstimateBytes()) / 1024f / 1024f);
        Debug.LogFormat("vertPool: {0}", (float)(vertPool.Count * VoxelandChunk.VoxelandVert.EstimateBytes()) / 1024f / 1024f);
        Debug.LogFormat("blocks grid: {0}", (float)(blocks.Length * 4) / 1024f / 1024f);
        Debug.LogFormat("faceList: {0}", (float)(faceList.Length * 4) / 1024f / 1024f);
    }

    [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
    public VoxelandChunkWorkspace()
    {
        maxMeshRes = 0;
        workspaceId = ++nextWorkspaceId;
    }

    public VoxelandChunkWorkspace(int maxMeshRes)
    {
        SetSize(maxMeshRes);
    }

    public void SetSize(int meshRes)
    {
        int num = meshRes + 4;
        blocksLen = new Int3(num, num, num);
        if (maxMeshRes < meshRes)
        {
            ProfilingUtils.BeginSample("VoxelandChunkWorkspace");
            maxMeshRes = meshRes;
            blocks = new Array3<VoxelandChunk.VoxelandBlock>(num, num, num);
            int num2 = maxMeshRes + 6;
            faceList = new ChunkFaceId[num2 * num2 * num2];
            for (int i = 0; i < faceList.Length; i++)
            {
                faceList[i].Reset();
            }
            ProfilingUtils.EndSample();
        }
        nextBlock = blockPool.First;
        nextFace = facePool.First;
        nextVert = vertPool.First;
        blocks.Clear();
        faces.Clear();
        visibleFaces.Clear();
        verts.Clear();
        nextGrassVert = grassVertPool.First;
        nextGrassTri = grassTriPool.First;
        nextGrassMesh = 0;
        rws.SetSize(meshRes);
    }

    public VoxelandChunk.VoxelandBlock NewBlock(int x, int y, int z)
    {
        if (nextBlock == null)
        {
            blockPool.AddLast(new VoxelandChunk.VoxelandBlock());
            nextBlock = blockPool.Last;
        }
        VoxelandChunk.VoxelandBlock result = nextBlock.Value.Reset(x, y, z);
        nextBlock = nextBlock.Next;
        return result;
    }

    public VoxelandChunk.VoxelandFace NewFace()
    {
        if (nextFace == null)
        {
            facePool.AddLast(new VoxelandChunk.VoxelandFace());
            nextFace = facePool.Last;
        }
        VoxelandChunk.VoxelandFace result = nextFace.Value.Reset();
        nextFace = nextFace.Next;
        return result;
    }

    public VoxelandChunk.VoxelandVert NewVert()
    {
        if (nextVert == null)
        {
            vertPool.AddLast(new VoxelandChunk.VoxelandVert());
            nextVert = vertPool.Last;
        }
        VoxelandChunk.VoxelandVert result = nextVert.Value.Reset();
        nextVert = nextVert.Next;
        return result;
    }

    public VoxelandChunk.VLGrassVert NewGrassVert()
    {
        if (nextGrassVert == null)
        {
            grassVertPool.AddLast(new VoxelandChunk.VLGrassVert());
            nextGrassVert = grassVertPool.Last;
        }
        VoxelandChunk.VLGrassVert value = nextGrassVert.Value;
        nextGrassVert = nextGrassVert.Next;
        return value;
    }

    public VoxelandChunk.VLGrassTri NewGrassTri()
    {
        if (nextGrassTri == null)
        {
            grassTriPool.AddLast(new VoxelandChunk.VLGrassTri());
            nextGrassTri = grassTriPool.Last;
        }
        VoxelandChunk.VLGrassTri value = nextGrassTri.Value;
        nextGrassTri = nextGrassTri.Next;
        return value;
    }
}
