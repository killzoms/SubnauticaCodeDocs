using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class VoxelizeHeightmapTask : IVoxelGrid
    {
        public float[,] heightCache;

        public Int2 heightCacheOrigin;

        public Array3<byte> typeCache;

        public Int3 typeCacheOrigin;

        public bool debugDensityPerpendicular;

        private LargeWorld world;

        private HeightmapBoundsCache boundsCache;

        private VoxelandData dest;

        public LargeWorld.Heightmap heightmap => world.heightmap;

        public void Reset(HeightmapBoundsCache boundsCache, VoxelandData dest, LargeWorld world)
        {
            this.world = world;
            this.boundsCache = boundsCache;
            this.dest = dest;
        }

        public VoxelandData.OctNode GetVoxel(int x, int y, int z)
        {
            return GetHeightmapNode(new Int3(x, y, z));
        }

        public bool GetVoxelMask(int x, int y, int z)
        {
            return true;
        }

        public void CompileHeightmap(Int3 root)
        {
            Int3.Bounds rootBounds = dest.GetRootBounds(root);
            if (!((float)rootBounds.mins.y - 0.5f > boundsCache.GetMax(root.xz)))
            {
                Int3 size = rootBounds.size;
                if (heightCache == null || heightCache.GetLength(0) != size.x + 2)
                {
                    heightCache = new float[size.x + 2, size.z + 2];
                }
                heightCacheOrigin = rootBounds.mins.xz - 1;
                Int2.RangeEnumerator enumerator = Int2.Range(rootBounds.mins.xz - 1, rootBounds.maxs.xz + 1).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Int2 current = enumerator.Current;
                    heightCache.Set(current - rootBounds.mins.xz + 1, heightmap.GetHeight(current.x, current.y));
                }
                ProfilingUtils.BeginSample("Type Cache");
                if (typeCache == null || typeCache.sizeX != size.x)
                {
                    typeCache = new Array3<byte>(size.x, size.y, size.z);
                }
                typeCacheOrigin = rootBounds.mins;
                world.RasterHeightmapTypes(typeCacheOrigin, typeCache);
                ProfilingUtils.EndSample();
                ProfilingUtils.BeginSample("CompileHeightmap/SetBottomUp call");
                int rootIndex = dest.GetRootIndex(root);
                dest.roots[rootIndex].SetBottomUp(this, rootBounds.mins.x, rootBounds.mins.y, rootBounds.mins.z, dest.biggestNode / 2);
                ProfilingUtils.EndSample();
            }
        }

        private VoxelandData.OctNode GetHeightmapNode(Int3 a)
        {
            VoxelandData.OctNode result = default(VoxelandData.OctNode);
            if ((float)a.y + 1.5f < boundsCache.GetMin(a.xz / dest.biggestNode))
            {
                result.density = 0;
                result.type = typeCache.Get(a - typeCacheOrigin);
                return result;
            }
            if ((float)a.y - 0.5f > boundsCache.GetMax(a.xz / dest.biggestNode))
            {
                result.density = 0;
                result.type = 0;
                return result;
            }
            float num = heightCache.Get(a.xz - heightCacheOrigin);
            bool flag = (double)a.y + 0.5 <= (double)num;
            int num2 = 0;
            float num3 = float.MaxValue;
            Int3.VNNEnumerator enumerator = a.VNNbors().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Int3 current = enumerator.Current;
                float num4 = heightCache.Get(current.xz - heightCacheOrigin);
                if ((double)current.y + 0.5 <= (double)num4 != flag)
                {
                    num2++;
                    float num5 = 0f;
                    num5 = ((current.y == a.y) ? ((!debugDensityPerpendicular) ? ((!flag) ? (((float)a.y + 0.5f - num) / (num4 - num)) : ((num - (float)a.y - 0.5f) / (num - num4))) : ((!flag) ? (((float)a.y + 0.5f - num) / Mathf.Sqrt((num4 - num) * (num4 - num) + 1f)) : ((num - (float)a.y - 0.5f) / Mathf.Sqrt((num - num4) * (num - num4) + 1f)))) : ((!flag) ? ((float)a.y + 0.5f - num) : (num - (float)a.y - 0.5f)));
                    num3 = Mathf.Min(num3, num5);
                    if (float.IsNaN(num5))
                    {
                        Debug.LogError(string.Concat("NaN dist when processing edge ", a, " --> ", current));
                    }
                    if (num5 < 0f)
                    {
                        Debug.LogError(string.Concat("Negative dist when processing edge ", a, " --> ", current));
                    }
                }
            }
            if (num2 > 0)
            {
                result.type = (byte)(flag ? typeCache.Get(a - typeCacheOrigin) : 0);
                num3 = Mathf.Abs(num3) * (float)(flag ? 1 : (-1));
                result.density = VoxelandData.OctNode.EncodeDensity(num3);
                if (result.type == 0 && result.density >= 126)
                {
                    Debug.LogError("Mindistance calc appears to be faulty for a heightmap node");
                }
            }
            else
            {
                result.density = 0;
                if (flag)
                {
                    result.type = typeCache.Get(a - typeCacheOrigin);
                }
                else
                {
                    result.type = 0;
                }
            }
            return result;
        }
    }
}
