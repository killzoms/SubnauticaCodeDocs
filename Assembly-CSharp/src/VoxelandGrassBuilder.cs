using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UWE;

namespace AssemblyCSharp
{
    public class VoxelandGrassBuilder : IEstimateBytes
    {
        private enum State
        {
            Init,
            Setup,
            Meshed
        }

        [Serializable]
        public class Settings
        {
            public float reduction;

            public int maxVerts = 10000;

            public int maxTris = 10000;
        }

        private readonly List<MeshBuffer> builtMeshes = new List<MeshBuffer>();

        private readonly List<VoxelandBlockType> types = new List<VoxelandBlockType>();

        private MeshBufferPools pools;

        private State state;

        public bool inUse { get; set; }

        public long EstimateBytes()
        {
            return 52L;
        }

        public void Reset(MeshBufferPools pools)
        {
            this.pools = pools;
            builtMeshes.Clear();
            types.Clear();
            state = State.Setup;
        }

        public void CreateMeshData(IVoxelandChunk chunk, Settings settings)
        {
            int num = 0;
            int num2 = 0;
            int count = chunk.usedTypes.Count;
            for (int i = 0; i < count; i++)
            {
                byte num3 = chunk.usedTypes[i].num;
                VoxelandBlockType voxelandBlockType = chunk.land.types[num3];
                if (!voxelandBlockType.hasGrassAbove || !voxelandBlockType.hasGrassAbove)
                {
                    continue;
                }
                VoxelandBlockType voxelandBlockType2 = voxelandBlockType;
                int randSeed = num3;
                if (voxelandBlockType2.grassVerts == null)
                {
                    continue;
                }
                int num4 = Mathf.Min(settings.maxVerts - num, 65535);
                int num5 = System.Math.Min(val2: (settings.maxTris * 3 - num2) / voxelandBlockType2.grassTris.Length, val1: num4 / voxelandBlockType2.grassVerts.Length);
                num5 = (int)((float)num5 * 0.8f);
                ProfilingUtils.BeginSample("Count Grass");
                int num6 = chunk.EnumerateGrass(voxelandBlockType2, num3, randSeed, settings.reduction).Count();
                ProfilingUtils.EndSample();
                if (num6 == 0)
                {
                    continue;
                }
                float num7 = settings.reduction;
                if (num6 > num5)
                {
                    num7 = Mathf.Lerp(settings.reduction, 1f, 1f - (float)num5 / (float)num6);
                    num6 = num5;
                }
                int num8 = voxelandBlockType2.grassVerts.Length * num6;
                int num9 = voxelandBlockType2.grassTris.Length * num6;
                if (num8 == 0 || num9 == 0)
                {
                    continue;
                }
                MeshBuffer meshBuffer = new MeshBuffer();
                meshBuffer.Acquire(pools, num8, num9, MeshBuffer.Mask.Normals | MeshBuffer.Mask.Tangents | MeshBuffer.Mask.UVs | MeshBuffer.Mask.Colors);
                if (meshBuffer.vertices == null || meshBuffer.triangles == null || meshBuffer.normals == null || meshBuffer.colors32 == null || meshBuffer.tangents == null)
                {
                    Debug.LogFormat("Failed to get grass buffer for {0} verts", num8);
                    continue;
                }
                builtMeshes.Add(meshBuffer);
                types.Add(voxelandBlockType2);
                int num10 = 0;
                int maxTris = 0;
                int num11 = 0;
                System.Random rng = new System.Random(chunk.offsetX * 9999 + chunk.offsetY * 999 + chunk.offsetZ * 99);
                ProfilingUtils.BeginSample("Build Grass");
                foreach (VoxelandChunk.GrassPos item in chunk.EnumerateGrass(voxelandBlockType2, num3, randSeed, num7))
                {
                    if (num11 >= num5)
                    {
                        break;
                    }
                    item.ComputeTransform(rng, voxelandBlockType2);
                    int num12 = num10;
                    for (int j = 0; j < voxelandBlockType2.grassVerts.Length; j++)
                    {
                        meshBuffer.vertices[num10] = item.csOrigin + item.quat * (item.scale * voxelandBlockType2.grassVerts[j]);
                        meshBuffer.normals[num10] = item.quat * voxelandBlockType2.grassNormals[j];
                        meshBuffer.tangents[num10] = item.quat * voxelandBlockType2.grassTangents[j];
                        meshBuffer.uvs[num10] = voxelandBlockType2.grassUVs[j];
                        float num13 = Vector3.Dot(meshBuffer.vertices[num10] - item.csOrigin, item.faceNormal);
                        meshBuffer.colors32[num10] = new Color32(rng.NextByte(), rng.NextByte(), rng.NextByte(), Convert.ToByte(255f * Mathf.Clamp01(num13 / 5f)));
                        num10++;
                    }
                    for (int k = 0; k < voxelandBlockType2.grassTris.Length; k++)
                    {
                        int num14 = num12 + voxelandBlockType2.grassTris[k];
                        meshBuffer.triangles[maxTris++] = (ushort)num14;
                    }
                    num11++;
                }
                ProfilingUtils.EndSample();
                meshBuffer.Clamp(num10, maxTris);
                num += num8;
                num2 += num9;
            }
            state = State.Meshed;
        }

        public void CreateUnityMeshes(IVoxelandChunk2 chunk, MeshPool meshPool)
        {
            while (chunk.grassFilters.Count < builtMeshes.Count)
            {
                ProfilingUtils.BeginSample("Create Grass Obj");
                GameObject gameObject = new GameObject("chunk grass");
                gameObject.transform.SetParent(chunk.transform, worldPositionStays: false);
                gameObject.transform.localPosition = Vector3.zero;
                gameObject.transform.localRotation = Quaternion.identity;
                gameObject.transform.localScale = Vector3.one;
                MeshFilter item = gameObject.AddComponent<MeshFilter>();
                chunk.grassFilters.Add(item);
                MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                chunk.grassRenders.Add(meshRenderer);
                ProfilingUtils.EndSample();
            }
            int num = 0;
            for (num = 0; num < builtMeshes.Count; num++)
            {
                MeshFilter meshFilter = chunk.grassFilters[num];
                meshFilter.gameObject.SetActive(value: true);
                MeshRenderer meshRenderer2 = chunk.grassRenders[num];
                VoxelandBlockType voxelandBlockType = types[num];
                if (!Application.isPlaying)
                {
                    meshFilter.sharedMesh.hideFlags = HideFlags.DontSave;
                }
                meshFilter.sharedMesh = meshPool.Get();
                meshFilter.sharedMesh.name = "TERRAIN grass layer";
                meshRenderer2.sharedMaterial = voxelandBlockType.grassMaterial;
                MeshBuffer meshBuffer = builtMeshes[num];
                ProfilingUtils.BeginSample("Upload Grass Mesh", meshFilter.sharedMesh);
                meshBuffer.Upload(meshFilter.sharedMesh);
                if (!meshPool.poolingEnabled)
                {
                    meshFilter.sharedMesh.UploadMeshData(markNoLongerReadable: true);
                }
                ProfilingUtils.EndSample();
                meshBuffer.Return();
            }
            for (; num < chunk.grassFilters.Count; num++)
            {
                if (chunk.grassFilters[num].sharedMesh != null)
                {
                    chunk.grassFilters[num].sharedMesh.Clear(keepVertexLayout: false);
                    chunk.grassFilters[num].sharedMesh.name = "TERRAIN grass unused";
                    chunk.grassFilters[num].gameObject.SetActive(value: false);
                }
            }
            state = State.Init;
        }
    }
}
