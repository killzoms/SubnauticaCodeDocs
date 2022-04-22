using LibNoise.Unity;
using UnityEngine;

namespace UWE
{
    public class Voronoi2D
    {
        public int seed;

        public float deviation = 1f;

        public Vector2 GetControlPoint(Int2 cell)
        {
            float x = (float)cell.x + 0.5f + deviation * LibNoise.Unity.Utils.ValueNoise3DSingle(cell.x, cell.y, 0, seed);
            float y = (float)cell.y + 0.5f + deviation * LibNoise.Unity.Utils.ValueNoise3DSingle(cell.x, cell.y, 0, seed + 1);
            return new Vector2(x, y);
        }

        public Int2 GetCellId(Vector2 x)
        {
            Int2 @int = Int2.FloorToInt2(x);
            Int2 result = new Int2(0, 0);
            float num = 8f;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Int2 int2 = @int + new Int2(j, i);
                    Vector2 vector = GetControlPoint(int2) - x;
                    float num2 = Vector2.Dot(vector, vector);
                    if (num2 < num)
                    {
                        num = num2;
                        result = int2;
                    }
                }
            }
            return result;
        }

        public Vector2 Query(Vector2 x, out float borderDist, out Int2 cellId)
        {
            Int2 @int = Int2.FloorToInt2(x);
            Vector2 vector = Vector2.zero;
            cellId = new Int2(0, 0);
            float num = 8f;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    Int2 int2 = @int + new Int2(j, i);
                    Vector2 vector2 = GetControlPoint(int2) - x;
                    float num2 = Vector2.Dot(vector2, vector2);
                    if (num2 < num)
                    {
                        num = num2;
                        vector = vector2;
                        cellId = int2;
                    }
                }
            }
            borderDist = 8f;
            for (int k = -2; k <= 2; k++)
            {
                for (int l = -2; l <= 2; l++)
                {
                    Int2 cell = cellId + new Int2(l, k);
                    Vector2 vector3 = GetControlPoint(cell) - x;
                    if (Vector2.Dot(vector - vector3, vector - vector3) > 1E-06f)
                    {
                        float b = Vector2.Dot(0.5f * (vector + vector3), (vector3 - vector).normalized);
                        borderDist = Mathf.Min(borderDist, b);
                    }
                }
            }
            return vector;
        }
    }
}
