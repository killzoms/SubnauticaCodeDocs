using UnityEngine;

namespace AssemblyCSharp.Exploder.MeshCutter
{
    public class LSHash
    {
        private readonly Vector3[] buckets;

        private readonly float bucketSize2;

        private int count;

        public LSHash(float bucketSize, int allocSize)
        {
            bucketSize2 = bucketSize * bucketSize;
            buckets = new Vector3[allocSize];
            count = 0;
        }

        public int Capacity()
        {
            return buckets.Length;
        }

        public void Clear()
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = Vector3.zero;
            }
            count = 0;
        }

        public int Hash(Vector3 p)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 vector = buckets[i];
                float num = p.x - vector.x;
                float num2 = p.y - vector.y;
                float num3 = p.z - vector.z;
                if (num * num + num2 * num2 + num3 * num3 < bucketSize2)
                {
                    return i;
                }
            }
            if (count >= buckets.Length)
            {
                return count - 1;
            }
            buckets[count++] = p;
            return count - 1;
        }

        public void Hash(Vector3 p0, Vector3 p1, out int hash0, out int hash1)
        {
            float num = p0.x - p1.x;
            float num2 = p0.y - p1.y;
            float num3 = p0.z - p1.z;
            if (num * num + num2 * num2 + num3 * num3 < bucketSize2)
            {
                hash0 = Hash(p0);
                hash1 = hash0;
            }
            else
            {
                hash0 = Hash(p0);
                hash1 = Hash(p1);
            }
        }
    }
}
