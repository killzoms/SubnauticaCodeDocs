using LibNoise.Unity;
using UnityEngine;

namespace UWE
{
    public class Voronoi3D
    {
        public Vector3 frequency = new Vector3(1f, 1f, 1f);

        public int seed;

        public float deviation = 1f;

        public Vector3 GetControlPoint(Int3 cell)
        {
            float x = (float)cell.x + 0.5f + deviation * LibNoise.Unity.Utils.ValueNoise3DSingle(cell.x, cell.y, cell.z, seed);
            float y = (float)cell.y + 0.5f + deviation * LibNoise.Unity.Utils.ValueNoise3DSingle(cell.x, cell.y, cell.z, seed + 1);
            float z = (float)cell.z + 0.5f + deviation * LibNoise.Unity.Utils.ValueNoise3DSingle(cell.x, cell.y, cell.z, seed + 2);
            return new Vector3(x, y, z);
        }

        public Int3 GetCell(Vector3 p)
        {
            p = Vector3.Scale(p, frequency);
            int x = (((double)p.x > 0.0) ? ((int)p.x) : ((int)p.x - 1));
            int y = (((double)p.y > 0.0) ? ((int)p.y) : ((int)p.y - 1));
            int z = (((double)p.z > 0.0) ? ((int)p.z) : ((int)p.z - 1));
            Int3 @int = new Int3(x, y, z);
            float num = 0f;
            Int3 result = new Int3(-1, -1, -1);
            foreach (Int3 item in Int3.Range(new Int3(-2, -2, -2), new Int3(2, 2, 2)))
            {
                Int3 int2 = @int + item;
                float sqrMagnitude = (GetControlPoint(int2) - p).sqrMagnitude;
                if (result.x == -1 || sqrMagnitude < num)
                {
                    num = sqrMagnitude;
                    result = int2;
                }
            }
            return result;
        }

        public void RasterizeCellMask(Int3 cell, ref bool[,,] mask, out Int3 originPixel)
        {
            Vector3 vector = new Vector3(0.5f / frequency.x, 0.5f / frequency.y, 0.5f / frequency.z);
            Vector3 vector2 = Vector3.Scale(cell.ToVector3(), vector * 2f) + vector;
            Int3 @int = Int3.Floor(vector2 - vector * 3f) - 1;
            Int3 int2 = Int3.Floor(vector2 + vector * 3f) + 1;
            Int3 int3 = int2 - @int + 1;
            if (mask == null || mask.GetLength(0) != int3.x || mask.GetLength(1) != int3.y || mask.GetLength(2) != int3.z)
            {
                mask = new bool[int3.x, int3.y, int3.z];
            }
            foreach (Int3 item in Int3.Range(@int, int2))
            {
                Int3 cell2 = GetCell(item.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f));
                int num = item.x - @int.x;
                int num2 = item.y - @int.y;
                int num3 = item.z - @int.z;
                mask[num, num2, num3] = cell2 == cell;
            }
            originPixel = @int;
        }
    }
}
