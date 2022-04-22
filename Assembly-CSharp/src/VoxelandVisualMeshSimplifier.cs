using System;
using Gendarme;
using UnityEngine;
using UnityEngine.Rendering;
using UWE;

namespace AssemblyCSharp
{
    public class VoxelandVisualMeshSimplifier : IEstimateBytes
    {
        [Serializable]
        public class Settings
        {
            public bool useLowMesh;

            public bool skipSimplify;

            public SimplifyMeshPlugin.Settings simplify = new SimplifyMeshPlugin.Settings();
        }

        public enum State
        {
            None,
            Ready,
            BuffersReady
        }

        private static readonly int[,] faceHiTri2Verts = new int[8, 3]
        {
            { 7, 0, 1 },
            { 1, 8, 7 },
            { 8, 1, 2 },
            { 2, 3, 8 },
            { 5, 8, 3 },
            { 3, 4, 5 },
            { 6, 7, 8 },
            { 8, 5, 6 }
        };

        private static readonly int[] hiFaceVerts = new int[9] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        private static readonly int[,] faceLoTri2Verts = new int[2, 3]
        {
            { 0, 2, 6 },
            { 6, 2, 4 }
        };

        private static readonly int[] loFaceVerts = new int[4] { 0, 2, 4, 6 };

        [NonSerialized]
        public Settings settings;

        public bool debugUseMeshBuffers = true;

        public bool debugUseLQShader;

        public bool debugAllOpaque;

        public bool debugSkipMaterials;

        public static bool debugForceAlphaTest = false;

        private MeshBufferPools pools;

        private State state = State.Ready;

        private SimplifyMeshPlugin.Face[] simplifyTris;

        private Vector3[] simplifyVertices;

        private byte[] simplifyFixed;

        private int[] simplifyOld2New;

        private int numTris;

        private int numVerts;

        private int numOrigVisibleVerts;

        private int[] simp2wsVert;

        private bool[] simpTriInLayer;

        private int[] simp2layerVert;

        private VoxelandChunkWorkspace ws;

        private MeshBuffer[] builtLayers;

        private int numBuiltLayers;

        public bool inUse { get; set; }

        public long EstimateBytes()
        {
            if (settings == null || settings.skipSimplify)
            {
                return 0L;
            }
            return simplifyTris.Length * SimplifyMeshPlugin.Face.SizeBytes + 8 + simplifyVertices.Length * 4 * 3 + 8 + simplifyFixed.Length + 8 + simplifyOld2New.Length * 4 + 8 + simp2wsVert.Length * 4 + 8 + simpTriInLayer.Length + 8 + simp2layerVert.Length * 4 + 8;
        }

        public void Reset()
        {
            state = State.Ready;
            numTris = -1;
            numVerts = -1;
            numOrigVisibleVerts = -1;
        }

        public void PrepareBuffers(VoxelandChunkWorkspace ws)
        {
            this.ws = ws;
            int[,] array = (settings.useLowMesh ? faceLoTri2Verts : faceHiTri2Verts);
            int[] array2 = (settings.useLowMesh ? loFaceVerts : hiFaceVerts);
            for (int i = 0; i < ws.verts.Count; i++)
            {
                ws.verts[i].layerVertIndex = -1;
            }
            int length = array.GetLength(0);
            numTris = length * ws.visibleFaces.Count;
            SimplifyMeshPlugin.Face[] array3 = global::UWE.Utils.EnsureMinSize("simplifyTris", ref simplifyTris, numTris);
            numVerts = 0;
            for (int j = 0; j < ws.visibleFaces.Count; j++)
            {
                VoxelandChunk.VoxelandFace voxelandFace = ws.visibleFaces[j];
                foreach (int num in array2)
                {
                    if (voxelandFace.verts[num].layerVertIndex == -1)
                    {
                        voxelandFace.verts[num].layerVertIndex = numVerts++;
                    }
                }
                VoxelandChunk.VoxelandVert[] verts = voxelandFace.verts;
                for (int l = 0; l < length; l++)
                {
                    int num2 = length * j + l;
                    array3[num2].a = verts[array[l, 0]].layerVertIndex;
                    array3[num2].b = verts[array[l, 1]].layerVertIndex;
                    array3[num2].c = verts[array[l, 2]].layerVertIndex;
                    array3[num2].material = voxelandFace.type;
                }
            }
            Vector3[] array4 = global::UWE.Utils.EnsureMinSize("simplifyVertices", ref simplifyVertices, numVerts);
            byte[] array5 = global::UWE.Utils.EnsureMinSize("simplifyFixed", ref simplifyFixed, numVerts);
            foreach (VoxelandChunk.VoxelandVert vert in ws.verts)
            {
                if (vert.layerVertIndex != -1)
                {
                    array4[vert.layerVertIndex] = vert.pos;
                    array5[vert.layerVertIndex] = Convert.ToByte(vert.ComputeIsOnChunkBorder() ? 1 : 0);
                }
            }
            numOrigVisibleVerts = numVerts;
            state = State.BuffersReady;
        }

        public void DoSimplify()
        {
            global::UWE.Utils.EnsureMinSize("simplifyOld2New", ref simplifyOld2New, numVerts);
            SimplifyMeshPlugin.SimplifyMesh(settings.simplify, simplifyVertices, simplifyFixed, ref numVerts, simplifyTris, ref numTris, simplifyOld2New);
            global::UWE.Utils.EnsureMinSize("simp2wsVert", ref simp2wsVert, numVerts);
            for (int i = 0; i < ws.verts.Count; i++)
            {
                VoxelandChunk.VoxelandVert voxelandVert = ws.verts[i];
                if (voxelandVert.layerVertIndex != -1)
                {
                    int num = i;
                    int num2 = simplifyOld2New[voxelandVert.layerVertIndex];
                    if (num2 != -1)
                    {
                        simp2wsVert[num2] = num;
                    }
                }
            }
        }

        public void ComputerLayersPhase1(IVoxelandChunk chunk, MeshBufferPools pools)
        {
            this.pools = pools;
            _ = chunk.usedTypes;
            global::UWE.Utils.EnsureMinSize("simpTriInLayer", ref simpTriInLayer, numTris);
            global::UWE.Utils.EnsureMinSize("simp2layerVert", ref simp2layerVert, numVerts);
            global::UWE.Utils.EnsureMinSize("builtLayers", ref builtLayers, chunk.usedTypes.Count);
            numBuiltLayers = chunk.usedTypes.Count;
            for (int i = 0; i < chunk.usedTypes.Count; i++)
            {
                int num = 0;
                int num2 = 0;
                Array.Clear(simpTriInLayer, 0, simpTriInLayer.Length);
                Array.Clear(simp2layerVert, 0, simp2layerVert.Length);
                for (int j = 0; j < numTris; j++)
                {
                    SimplifyMeshPlugin.Face face = simplifyTris[j];
                    bool flag = false;
                    if (i == 0)
                    {
                        flag = true;
                    }
                    else
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            if (ws.verts[simp2wsVert[face.GetVert(k)]].GetCachedBlendWeight(i) > 0f)
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                    if (!flag)
                    {
                        continue;
                    }
                    simpTriInLayer[j] = true;
                    num2++;
                    for (int l = 0; l < 3; l++)
                    {
                        if (simp2layerVert[face.GetVert(l)] == 0)
                        {
                            simp2layerVert[face.GetVert(l)] = num + 1;
                            num++;
                        }
                    }
                }
                MeshBuffer meshBuffer = new MeshBuffer();
                builtLayers[i] = meshBuffer;
                if (num2 == 0)
                {
                    meshBuffer.Clear();
                    continue;
                }
                MeshBuffer.Mask attrsMask = MeshBuffer.Mask.Normals | MeshBuffer.Mask.UVs;
                meshBuffer.Acquire(pools, num, num2 * 3, attrsMask);
                for (int m = 0; m < numVerts; m++)
                {
                    if (simp2layerVert[m] != 0)
                    {
                        int index = simp2layerVert[m] - 1;
                        int index2 = simp2wsVert[m];
                        VoxelandChunk.VoxelandVert voxelandVert = ws.verts[index2];
                        meshBuffer.vertices[index] = simplifyVertices[m];
                        meshBuffer.normals[index] = voxelandVert.normal;
                        Vector2 value = new Vector2((i == 0) ? 1f : voxelandVert.GetCachedBlendWeight(i), voxelandVert.gloss);
                        meshBuffer.uvs[index] = value;
                    }
                }
                int num3 = 0;
                for (int n = 0; n < numTris; n++)
                {
                    if (simpTriInLayer[n])
                    {
                        SimplifyMeshPlugin.Face face2 = simplifyTris[n];
                        for (int num4 = 0; num4 < 3; num4++)
                        {
                            int vert = face2.GetVert(num4);
                            int num5 = simp2layerVert[vert] - 1;
                            meshBuffer.triangles[3 * num3 + num4] = (ushort)num5;
                        }
                        num3++;
                    }
                }
            }
            ws = null;
        }

        private void ComputeLayersPhase1NoSimplify(IVoxelandChunk chunk, MeshBufferPools pools)
        {
            ws = chunk.ws;
            int[,] array = (settings.useLowMesh ? faceLoTri2Verts : faceHiTri2Verts);
            if (!settings.useLowMesh)
            {
                _ = hiFaceVerts;
            }
            else
            {
                _ = loFaceVerts;
            }
            int length = array.GetLength(0);
            this.pools = pools;
            int count = chunk.usedTypes.Count;
            int count2 = ws.verts.Count;
            int count3 = ws.visibleFaces.Count;
            MeshBuffer.Mask attrsMask = MeshBuffer.Mask.Normals | MeshBuffer.Mask.UVs;
            global::UWE.Utils.EnsureMinSize("builtLayers", ref builtLayers, count);
            numBuiltLayers = count;
            for (int i = 0; i < count; i++)
            {
                int num = 0;
                int num2 = 0;
                for (int j = 0; j < count2; j++)
                {
                    ws.verts[j].layerVertIndex = -1;
                }
                MeshBuffer meshBuffer = null;
                builtLayers[i] = null;
                for (int k = 0; k < count3; k++)
                {
                    VoxelandChunk.VoxelandVert[] verts = ws.visibleFaces[k].verts;
                    for (int l = 0; l < length; l++)
                    {
                        bool flag = false;
                        if (i == 0)
                        {
                            flag = true;
                        }
                        else
                        {
                            for (int m = 0; m < 3; m++)
                            {
                                if (verts[array[l, m]].GetCachedBlendWeight(i) > 0f)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (!flag)
                        {
                            continue;
                        }
                        if (meshBuffer == null)
                        {
                            builtLayers[i] = new MeshBuffer();
                            builtLayers[i].Acquire(pools, count2, count3 * length * 3, attrsMask);
                            meshBuffer = builtLayers[i];
                        }
                        for (int n = 0; n < 3; n++)
                        {
                            int num3 = array[l, n];
                            VoxelandChunk.VoxelandVert voxelandVert = verts[num3];
                            if (voxelandVert.layerVertIndex == -1)
                            {
                                voxelandVert.layerVertIndex = num++;
                                meshBuffer.vertices[voxelandVert.layerVertIndex] = voxelandVert.pos;
                                meshBuffer.normals[voxelandVert.layerVertIndex] = voxelandVert.normal;
                                meshBuffer.uvs[voxelandVert.layerVertIndex] = new Vector2((i == 0) ? 1f : voxelandVert.GetCachedBlendWeight(i), voxelandVert.gloss);
                            }
                            meshBuffer.triangles[3 * num2 + n] = (ushort)voxelandVert.layerVertIndex;
                        }
                        num2++;
                    }
                }
                if (meshBuffer == null)
                {
                    continue;
                }
                meshBuffer.numVerts = num;
                meshBuffer.numTris = num2 * 3;
                if (num2 == 0)
                {
                    if (debugUseMeshBuffers)
                    {
                        meshBuffer.Return();
                    }
                    else
                    {
                        meshBuffer.Clear();
                    }
                }
            }
            ws = null;
        }

        public void DumpObj()
        {
            SimplifyMeshPlugin.DumpObj(ref simplifyVertices[0], ref simplifyFixed[0], numVerts, ref simplifyTris[0], numTris);
        }

        private GameObject CreateChunkLayer(GameObject layerObjectPrefab, IVoxelandChunk2 chunk, ref MeshFilter filter, ref MeshRenderer render)
        {
            GameObject gameObject = null;
            if (layerObjectPrefab != null)
            {
                gameObject = global::UWE.Utils.Instantiate(layerObjectPrefab, chunk.transform, Vector3.zero, Quaternion.identity, Vector3.one);
                render = gameObject.GetComponent<MeshRenderer>();
                filter = gameObject.GetComponent<MeshFilter>();
            }
            else
            {
                gameObject = new GameObject("chunkLayer");
                gameObject.transform.SetParent(chunk.transform, worldPositionStays: false);
                render = gameObject.AddComponent<MeshRenderer>();
                render.shadowCastingMode = ShadowCastingMode.Off;
                filter = gameObject.AddComponent<MeshFilter>();
            }
            chunk.hiFilters.Add(filter);
            chunk.hiRenders.Add(render);
            return gameObject;
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public void BuildLayerObjects(VoxelandChunk chunk, bool castShadows, int addSortingValue, MeshPool meshPool, GameObject layerObjectPrefab)
        {
            BuildLayerObjects(chunk, chunk, castShadows, addSortingValue, meshPool, layerObjectPrefab);
        }

        public void BuildLayerObjects(IVoxelandChunk2 chunk, IVoxelandChunkInfo info, bool castShadows, int addSortingValue, MeshPool meshPool, GameObject layerObjectPrefab)
        {
            ProfilingUtils.BeginSample("VoxelandVisualMeshSimplifier::BuildLayerObjects");
            int num = 0;
            for (num = 0; num < info.usedTypes.Count; num++)
            {
                MeshFilter filter = null;
                MeshRenderer render = null;
                MeshBuffer meshBuffer = builtLayers[num];
                if (meshBuffer == null || meshBuffer.vertices == null)
                {
                    continue;
                }
                if (num >= chunk.hiFilters.Count)
                {
                    ProfilingUtils.BeginSample("Create Chunk Layer");
                    CreateChunkLayer(layerObjectPrefab, chunk, ref filter, ref render);
                    ProfilingUtils.EndSample();
                }
                else
                {
                    filter = chunk.hiFilters[num];
                    render = chunk.hiRenders[num];
                }
                filter.sharedMesh = meshPool.Get();
                ProfilingUtils.BeginSample("Upload", filter.sharedMesh);
                meshBuffer.Upload(filter.sharedMesh);
                if (!meshPool.poolingEnabled)
                {
                    filter.sharedMesh.UploadMeshData(markNoLongerReadable: true);
                }
                filter.sharedMesh.name = "TERRAIN chunk mesh";
                meshBuffer.Return();
                ProfilingUtils.EndSample();
                if (num == 0)
                {
                    render.shadowCastingMode = (castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off);
                    VoxelandChunk.TypeUse typeUse = info.usedTypes[num];
                    VoxelandBlockType voxelandBlockType = info.land.types[typeUse.num];
                    if (voxelandBlockType == null)
                    {
                        Debug.LogFormat("No block type at index {0} - using a fallback type instead.", typeUse.num.ToString());
                        voxelandBlockType = VoxelandChunk.GetFallbackBlockType(info.land.types);
                    }
                    render.sharedMaterial = voxelandBlockType.opaqueMaterial;
                }
                else
                {
                    render.sortingOrder = num + addSortingValue;
                    render.shadowCastingMode = ShadowCastingMode.Off;
                    VoxelandBlockType voxelandBlockType2 = info.land.types[info.usedTypes[num].num];
                    render.sharedMaterial = (debugForceAlphaTest ? voxelandBlockType2.alphaTestMat : voxelandBlockType2.material);
                }
            }
            for (; num < chunk.hiFilters.Count; num++)
            {
                MeshFilter meshFilter = chunk.hiFilters[num];
                chunk.hiRenders[num].enabled = false;
                if (meshFilter.sharedMesh != null)
                {
                    meshFilter.sharedMesh.Clear(keepVertexLayout: false);
                }
            }
            ProfilingUtils.EndSample();
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public void Build(IVoxelandChunk chunk, MeshBufferPools pools, bool debug = false)
        {
            Reset();
            if (settings.skipSimplify)
            {
                ProfilingUtils.BeginSample("VisSimp ComputeLayersP1 No Simplify");
                ComputeLayersPhase1NoSimplify(chunk, pools);
                ProfilingUtils.EndSample();
                return;
            }
            ProfilingUtils.BeginSample("VisSimp PrepareBuffers");
            PrepareBuffers(chunk.ws);
            ProfilingUtils.EndSample();
            long num = DateTime.Now.Ticks % 10000;
            if (debug)
            {
                Debug.LogFormat("{0} Before #verts/tris: {1}/{2}", num, numVerts, numTris);
            }
            ProfilingUtils.BeginSample("VisSimp DoSimplify");
            DoSimplify();
            ProfilingUtils.EndSample();
            if (debug)
            {
                Debug.LogFormat("{0} After #verts/tris: {1}/{2}", num, numVerts, numTris);
            }
            ProfilingUtils.BeginSample("VisSimp ComputeLayersP1");
            ComputerLayersPhase1(chunk, pools);
            ProfilingUtils.EndSample();
        }
    }
}
