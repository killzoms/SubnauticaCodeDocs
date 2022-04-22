using System;
using Gendarme;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class VoxelandCollisionMeshSimplifier : IEstimateBytes
    {
        [Serializable]
        public class Settings
        {
            public int prewarmTriangleCount;

            public int prewarmVertexCount;

            public int triangleSimplifyCutoff = 100;

            public int vertexSimplifyCutoff = 100;

            public bool skipRandomPhase;
        }

        private static readonly int[] LowResFaceVerts = new int[4] { 0, 2, 4, 6 };

        private readonly MeshBuffer meshBuffer = new MeshBuffer();

        [NonSerialized]
        private Settings _settings;

        private SimplifyMeshPlugin.Face[] simplifyTris;

        private Vector3[] simplifyVertices;

        private byte[] simplifyFixed;

        private int[] simplifyOld2New;

        private MeshBufferPools meshBufferPools;

        public bool inUse { get; set; }

        public Settings settings
        {
            get
            {
                return _settings;
            }
            set
            {
                _settings = value;
                if (_settings.prewarmVertexCount > 0)
                {
                    if (simplifyVertices == null)
                    {
                        simplifyVertices = new Vector3[_settings.prewarmVertexCount];
                    }
                    if (simplifyFixed == null)
                    {
                        simplifyFixed = new byte[_settings.prewarmVertexCount];
                    }
                    if (simplifyOld2New == null)
                    {
                        simplifyOld2New = new int[_settings.prewarmVertexCount];
                    }
                }
                if (settings.prewarmTriangleCount > 0 && simplifyTris == null)
                {
                    simplifyTris = new SimplifyMeshPlugin.Face[_settings.prewarmTriangleCount];
                }
            }
        }

        public void SetPools(MeshBufferPools pools)
        {
            meshBufferPools = pools;
        }

        public long EstimateBytes()
        {
            if (simplifyVertices == null)
            {
                return -1L;
            }
            return simplifyTris.Length * SimplifyMeshPlugin.Face.SizeBytes + simplifyVertices.Length * 4 * 3 + simplifyFixed.Length + simplifyOld2New.Length * 4;
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public void AttachTo(IVoxelandChunk2 chunk, MeshPool meshPool)
        {
            if (meshBuffer.numTris < 1)
            {
                MeshCollider collision = chunk.collision;
                if (collision != null)
                {
                    collision.sharedMesh = null;
                    collision.gameObject.SetActive(value: false);
                }
                return;
            }
            ProfilingUtils.BeginSample("Attach Simplified Collider");
            MeshCollider meshCollider = chunk.EnsureCollision();
            meshCollider.cookingOptions = MeshColliderCookingOptions.None;
            Mesh mesh = meshPool.Get();
            meshBuffer.Upload(mesh, keepVertexLayout: false);
            if (!meshPool.poolingEnabled)
            {
                mesh.UploadMeshData(markNoLongerReadable: true);
            }
            mesh.name = "TERRAIN collider for chunk";
            if (SNUtils.VerboseDebug)
            {
                Debug.LogFormat("setting collider sharedmesh to #verts = {0}, #inds = {1}", meshBuffer.numVerts, meshBuffer.numTris);
                global::UWE.Utils.DumpOBJFile(SNUtils.InsideDevTemp("last_colmesh.obj"), meshBuffer);
            }
            try
            {
                ProfilingUtils.BeginSample("SharedMesh Assign");
                meshCollider.sharedMesh = mesh;
                ProfilingUtils.EndSample();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                meshCollider.sharedMesh = null;
            }
            meshCollider.gameObject.SetActive(value: true);
            mesh.Clear();
            if (SNUtils.VerboseDebug)
            {
                Debug.Log("after set sharedMesh");
            }
            meshBuffer.Return();
            ProfilingUtils.EndSample();
        }

        public void Build(VoxelandChunkWorkspace ws, bool writeOutput = false)
        {
            if (ws.visibleFaces.Count == 0)
            {
                meshBuffer.Clear();
                return;
            }
            ProfilingUtils.BeginSample("VoxelandCollisionMeshSimplifier.Build");
            for (int i = 0; i < ws.verts.Count; i++)
            {
                ws.verts[i].layerVertIndex = -1;
                ws.verts[i].layerLowVertIndex = -1;
            }
            int numFaces = 2 * ws.visibleFaces.Count;
            int num = LowResFaceVerts.Length;
            SimplifyMeshPlugin.Face[] array = global::UWE.Utils.EnsureMinSize("simplifyTris", ref simplifyTris, numFaces);
            int numVerts = 0;
            for (int j = 0; j < ws.visibleFaces.Count; j++)
            {
                VoxelandChunk.VoxelandFace voxelandFace = ws.visibleFaces[j];
                for (int k = 0; k < num; k++)
                {
                    if (voxelandFace.verts[LowResFaceVerts[k]].layerLowVertIndex == -1)
                    {
                        voxelandFace.verts[LowResFaceVerts[k]].layerLowVertIndex = numVerts++;
                    }
                }
                array[2 * j].a = voxelandFace.verts[0].layerLowVertIndex;
                array[2 * j].b = voxelandFace.verts[2].layerLowVertIndex;
                array[2 * j].c = voxelandFace.verts[4].layerLowVertIndex;
                array[2 * j + 1].a = voxelandFace.verts[0].layerLowVertIndex;
                array[2 * j + 1].b = voxelandFace.verts[4].layerLowVertIndex;
                array[2 * j + 1].c = voxelandFace.verts[6].layerLowVertIndex;
            }
            Vector3[] array2 = global::UWE.Utils.EnsureMinSize("simplifyVertices", ref simplifyVertices, numVerts);
            byte[] array3 = global::UWE.Utils.EnsureMinSize("simplifyFixed", ref simplifyFixed, numVerts);
            foreach (VoxelandChunk.VoxelandVert vert in ws.verts)
            {
                if (vert.layerLowVertIndex != -1)
                {
                    array2[vert.layerLowVertIndex] = vert.pos;
                    array3[vert.layerLowVertIndex] = Convert.ToByte(vert.ComputeIsOnChunkBorder() ? 1 : 0);
                }
            }
            if (numFaces > settings.triangleSimplifyCutoff && numVerts > settings.vertexSimplifyCutoff)
            {
                int[] array4 = global::UWE.Utils.EnsureMinSize("simplifyOld2New", ref simplifyOld2New, numVerts);
                if (writeOutput)
                {
                    Debug.Log(numVerts + "/" + numFaces);
                }
                SimplifyMeshPlugin.SimplifyMesh(0.8f, 0f, ref array2[0], ref array3[0], ref numVerts, ref array[0], ref numFaces, ref array4[0], settings.skipRandomPhase, writeOutput);
                if (numFaces == 0)
                {
                    Debug.Log("WARNING WARNING WARNING: Some how ended up with 0 tris after simplification!");
                    meshBuffer.Clear();
                    ProfilingUtils.EndSample();
                    return;
                }
                if (numVerts == 0)
                {
                    Debug.Log("WARNING WARNING WARNING: Some how ended up with 0 verts but " + numFaces + " tris after simp..??");
                    meshBuffer.Clear();
                    ProfilingUtils.EndSample();
                    return;
                }
            }
            meshBuffer.Acquire(meshBufferPools, numVerts, numFaces * 3, MeshBuffer.Mask.None);
            Array.Copy(array2, 0, meshBuffer.vertices.Array, meshBuffer.vertices.Offset, numVerts);
            for (int l = 0; l < numFaces; l++)
            {
                meshBuffer.triangles[3 * l] = (ushort)array[l].a;
                meshBuffer.triangles[3 * l + 1] = (ushort)array[l].b;
                meshBuffer.triangles[3 * l + 2] = (ushort)array[l].c;
            }
            for (int m = 0; m < numVerts; m++)
            {
                Vector3 v = meshBuffer.vertices[m];
                if (v.HasAnyNaNs() || v.HasAnyInfs())
                {
                    Vector3 value = ((m > 0) ? meshBuffer.vertices[m - 1] : Vector3.zero);
                    meshBuffer.vertices[m] = value;
                }
            }
            ProfilingUtils.EndSample();
        }
    }
}
