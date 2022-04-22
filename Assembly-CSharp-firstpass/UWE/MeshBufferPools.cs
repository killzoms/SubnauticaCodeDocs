using UnityEngine;

namespace UWE
{
    public class MeshBufferPools : IEstimateBytes
    {
        public LinearArrayHeap<Vector2> v2;

        public LinearArrayHeap<Vector3> v3;

        public LinearArrayHeap<Vector4> v4;

        public LinearArrayHeap<Color32> c32;

        public LinearArrayHeap<ushort> ints;

        public MeshBufferPools(int v2Max, int v3Max, int v4Max, int cMax, int indexMax)
        {
            v2 = new ThreadSafeLinearArrayHeap<Vector2>(8, v2Max);
            v3 = new ThreadSafeLinearArrayHeap<Vector3>(12, v3Max);
            v4 = new ThreadSafeLinearArrayHeap<Vector4>(16, v4Max);
            c32 = new ThreadSafeLinearArrayHeap<Color32>(4, cMax);
            ints = new ThreadSafeLinearArrayHeap<ushort>(2, indexMax);
        }

        public MeshBufferPools(int vertMax, int indexMax)
            : this(vertMax, vertMax, vertMax, vertMax, indexMax)
        {
        }

        public bool TryReset()
        {
            if (v2.Outstanding > 0 || v3.Outstanding > 0 || v4.Outstanding > 0 || c32.Outstanding > 0 || ints.Outstanding > 0)
            {
                return false;
            }
            v2.Reset();
            v3.Reset();
            v4.Reset();
            c32.Reset();
            ints.Reset();
            return true;
        }

        public long EstimateBytes()
        {
            return v2.EstimateBytes() + v3.EstimateBytes() + v4.EstimateBytes() + c32.EstimateBytes() + ints.EstimateBytes();
        }

        public static void LayoutGUI(MeshBufferPools[] pools)
        {
            float num = 9.536743E-07f;
            long num2 = 0L;
            long num3 = 0L;
            long num4 = 0L;
            long num5 = 0L;
            long num6 = 0L;
            foreach (MeshBufferPools meshBufferPools in pools)
            {
                num2 += meshBufferPools.v2.EstimateBytes();
                num3 += meshBufferPools.v3.EstimateBytes();
                num4 += meshBufferPools.v4.EstimateBytes();
                num5 += meshBufferPools.c32.EstimateBytes();
                num6 += meshBufferPools.ints.EstimateBytes();
            }
            GUILayout.Label($"V2 pools:  {(float)num2 * num:0.0} MB");
            GUILayout.Label($"V3 pools:  {(float)num3 * num:0.0} MB");
            GUILayout.Label($"V4 pools:  {(float)num4 * num:0.0} MB");
            GUILayout.Label($"Col pools: {(float)num5 * num:0.0} MB");
            GUILayout.Label($"Int pools: {(float)num6 * num:0.0} MB");
        }
    }
}
