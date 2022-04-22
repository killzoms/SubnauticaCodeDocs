using System;
using System.IO;
using UWE;

namespace AssemblyCSharp.WorldStreaming
{
    public sealed class Octree
    {
        public const int BytesPerNode = 4;

        private readonly Int3 id;

        private LinearArrayHeap<byte>.Alloc data;

        public Octree(Int3 id)
        {
            this.id = id;
            data = null;
        }

        public void Clear(LinearArrayHeap<byte> allocator)
        {
            if (data != null)
            {
                allocator.Free(data);
                data = null;
            }
        }

        public bool IsEmpty()
        {
            if (data != null)
            {
                if (data.Length == 4)
                {
                    return GetType(0) == 0;
                }
                return false;
            }
            return true;
        }

        private int GetNodeId(int nid, int x, int y, int z, int halfsize)
        {
            int firstChildId = GetFirstChildId(nid);
            if (firstChildId == 0)
            {
                return nid;
            }
            int x2 = ((x < halfsize) ? x : (x - halfsize));
            int y2 = ((y < halfsize) ? y : (y - halfsize));
            int z2 = ((z < halfsize) ? z : (z - halfsize));
            int nid2 = firstChildId + ((x >= halfsize) ? 4 : 0) + ((y >= halfsize) ? 2 : 0) + ((z >= halfsize) ? 1 : 0);
            return GetNodeId(nid2, x2, y2, z2, halfsize >> 1);
        }

        public int GetNodeId(Int3 coords, int treeSize)
        {
            return GetNodeId(0, coords.x, coords.y, coords.z, treeSize >> 1);
        }

        public byte GetType(int node)
        {
            return data[node * 4];
        }

        public byte GetDensity(int node)
        {
            return data[node * 4 + 1];
        }

        private int GetFirstChildId(int node)
        {
            int num = node * 4;
            return (data[num + 3] << 8) + data[num + 2];
        }

        private bool IsLeaf(int node)
        {
            int num = node * 4;
            if (data[num + 2] == 0)
            {
                return data[num + 3] == 0;
            }
            return false;
        }

        private void MakeLeaf(int node)
        {
            int num = node * 4;
            data[num + 2] = 0;
            data[num + 3] = 0;
        }

        public void Read(BinaryReader reader, Int3 batchId, LinearArrayHeap<byte> allocator)
        {
            int num = reader.ReadUInt16() * 4;
            Clear(allocator);
            data = allocator.Allocate(num);
            reader.Read(data.Array, data.Offset, num);
        }

        public void UnloadChildren(int lod, LinearArrayHeap<byte> allocator)
        {
            if (lod >= 1 && !IsLeaf(0) && data.Length != 36)
            {
                LinearArrayHeap<byte>.Alloc alloc = allocator.Allocate(36);
                Array.Copy(data.Array, data.Offset, alloc.Array, alloc.Offset, 36);
                Clear(allocator);
                data = alloc;
                for (int i = 0; i < 8; i++)
                {
                    int node = 1 + i;
                    MakeLeaf(node);
                }
            }
        }

        public void RasterizeNative(int nodeId, Array3<byte> typesOut, Array3<byte> densityOut, Int3 size, Int3 w, Int3 o, int h)
        {
            if (data != null && data.Length != 0)
            {
                UnityUWE.RasterizeNativeEntry(ref data.Array[data.Offset], Convert.ToUInt16(nodeId), ref typesOut.data[0], ref densityOut.data[0], size, new Int3(typesOut.sizeX, typesOut.sizeY, typesOut.sizeZ), w.x, w.y, w.z, o.x, o.y, o.z, h);
            }
        }
    }
}
