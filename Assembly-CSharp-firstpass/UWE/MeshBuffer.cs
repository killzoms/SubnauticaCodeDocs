using System;
using UnityEngine;

namespace UWE
{
    public class MeshBuffer
    {
        [Flags]
        public enum Mask
        {
            None = 0x0,
            Normals = 0x1,
            Tangents = 0x2,
            UVs = 0x4,
            Colors = 0x8
        }

        private MeshBufferPools pools;

        public int numVerts { get; set; }

        public int numTris { get; set; }

        public LinearArrayHeap<Vector3>.Alloc vertices { get; private set; }

        public LinearArrayHeap<Vector3>.Alloc normals { get; private set; }

        public LinearArrayHeap<Vector4>.Alloc tangents { get; private set; }

        public LinearArrayHeap<Vector2>.Alloc uvs { get; private set; }

        public LinearArrayHeap<Color32>.Alloc colors32 { get; private set; }

        public LinearArrayHeap<ushort>.Alloc triangles { get; private set; }

        public void Clear(bool clearCaches = false)
        {
            pools = null;
            numVerts = 0;
            numTris = 0;
            vertices = null;
            normals = null;
            tangents = null;
            uvs = null;
            colors32 = null;
            triangles = null;
        }

        public void Clamp(int maxVerts, int maxTris)
        {
            numVerts = Mathf.Min(numVerts, maxVerts);
            numTris = Mathf.Min(numTris, maxTris);
        }

        public void Acquire(MeshBufferPools pools, int numVerts, int numTris, Mask attrsMask)
        {
            this.pools = pools;
            this.numVerts = numVerts;
            this.numTris = numTris;
            vertices = pools.v3.Allocate(numVerts);
            normals = (HasFlag(attrsMask, Mask.Normals) ? pools.v3.Allocate(numVerts) : null);
            tangents = (HasFlag(attrsMask, Mask.Tangents) ? pools.v4.Allocate(numVerts) : null);
            uvs = (HasFlag(attrsMask, Mask.UVs) ? pools.v2.Allocate(numVerts) : null);
            colors32 = (HasFlag(attrsMask, Mask.Colors) ? pools.c32.Allocate(numVerts) : null);
            triangles = pools.ints.Allocate(numTris);
        }

        public void Return()
        {
            if (pools != null)
            {
                pools.v3.Free(vertices);
                if (normals != null)
                {
                    pools.v3.Free(normals);
                }
                if (tangents != null)
                {
                    pools.v4.Free(tangents);
                }
                if (uvs != null)
                {
                    pools.v2.Free(uvs);
                }
                if (colors32 != null)
                {
                    pools.c32.Free(colors32);
                }
                pools.ints.Free(triangles);
                Clear();
            }
        }

        public void Upload(Mesh m, bool keepVertexLayout = true)
        {
            m.PreAllocateMeshData(numVerts, normals != null, tangents != null, colors32 != null, (uvs != null) ? 1 : 0);
            m.SetVertices(vertices.Array, numVerts, vertices.Offset);
            if (normals != null)
            {
                m.SetNormals(normals.Array, numVerts, normals.Offset);
            }
            if (tangents != null)
            {
                m.SetTangents(tangents.Array, numVerts, tangents.Offset);
            }
            if (uvs != null)
            {
                m.SetUVs(0, uvs.Array, numVerts, uvs.Offset);
            }
            if (colors32 != null)
            {
                m.SetColors(colors32.Array, numVerts, colors32.Offset);
            }
            m.SetIndices(triangles.Array, triangles.Offset, numTris, MeshTopology.Triangles, 0, calculateBounds: true, 0);
            m.RecalculateBounds();
        }

        private static bool HasFlag(Mask mask, Mask flag)
        {
            return (mask & flag) > Mask.None;
        }
    }
}
