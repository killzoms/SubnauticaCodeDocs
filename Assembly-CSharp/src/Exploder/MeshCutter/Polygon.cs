using System.Collections.Generic;
using Poly2Tri;
using UnityEngine;

namespace AssemblyCSharp.Exploder.MeshCutter
{
    public class Polygon
    {
        public Vector2[] Points;

        public readonly float Area;

        public Vector2 Min;

        public Vector2 Max;

        private readonly List<Polygon> holes;

        public Polygon(Vector2[] pnts)
        {
            Points = pnts;
            Area = GetArea();
            holes = new List<Polygon>();
        }

        public float GetArea()
        {
            Min.x = float.MaxValue;
            Min.y = float.MaxValue;
            Max.x = float.MinValue;
            Max.y = float.MinValue;
            int num = Points.Length;
            float num2 = 0f;
            int num3 = num - 1;
            int num4 = 0;
            while (num4 < num)
            {
                Vector2 vector = Points[num3];
                Vector2 vector2 = Points[num4];
                num2 += vector.x * vector2.y - vector2.x * vector.y;
                if (vector.x < Min.x)
                {
                    Min.x = vector.x;
                }
                if (vector.y < Min.y)
                {
                    Min.y = vector.y;
                }
                if (vector.x > Max.x)
                {
                    Max.x = vector.x;
                }
                if (vector.y > Max.y)
                {
                    Max.y = vector.y;
                }
                num3 = num4++;
            }
            return num2 * 0.5f;
        }

        public bool IsPointInside(Vector3 p)
        {
            int num = Points.Length;
            Vector2 vector = Points[num - 1];
            bool flag = vector.y >= p.y;
            Vector2 zero = Vector2.zero;
            bool flag2 = false;
            for (int i = 0; i < num; i++)
            {
                zero = Points[i];
                bool flag3 = zero.y >= p.y;
                if (flag != flag3 && (zero.y - p.y) * (vector.x - zero.x) >= (zero.x - p.x) * (vector.y - zero.y) == flag3)
                {
                    flag2 = !flag2;
                }
                flag = flag3;
                vector = zero;
            }
            return flag2;
        }

        public bool IsPolygonInside(Polygon polygon)
        {
            if (Area > polygon.Area)
            {
                return IsPointInside(polygon.Points[0]);
            }
            return false;
        }

        public void AddHole(Polygon polygon)
        {
            holes.Add(polygon);
        }

        public List<int> Triangulate()
        {
            if (holes.Count == 0)
            {
                List<int> list = new List<int>(Points.Length);
                int num = Points.Length;
                if (num < 3)
                {
                    return list;
                }
                int[] array = new int[num];
                if (Area > 0f)
                {
                    for (int i = 0; i < num; i++)
                    {
                        array[i] = i;
                    }
                }
                else
                {
                    for (int j = 0; j < num; j++)
                    {
                        array[j] = num - 1 - j;
                    }
                }
                int num2 = num;
                int num3 = 2 * num2;
                int num4 = 0;
                int num5 = num2 - 1;
                while (num2 > 2)
                {
                    if (num3-- <= 0)
                    {
                        return list;
                    }
                    int num6 = num5;
                    if (num2 <= num6)
                    {
                        num6 = 0;
                    }
                    num5 = num6 + 1;
                    if (num2 <= num5)
                    {
                        num5 = 0;
                    }
                    int num7 = num5 + 1;
                    if (num2 <= num7)
                    {
                        num7 = 0;
                    }
                    if (Snip(num6, num5, num7, num2, array))
                    {
                        int item = array[num6];
                        int item2 = array[num5];
                        int item3 = array[num7];
                        list.Add(item);
                        list.Add(item2);
                        list.Add(item3);
                        num4++;
                        int num8 = num5;
                        for (int k = num5 + 1; k < num2; k++)
                        {
                            array[num8] = array[k];
                            num8++;
                        }
                        num2--;
                        num3 = 2 * num2;
                    }
                }
                list.Reverse();
                return list;
            }
            List<PolygonPoint> list2 = new List<PolygonPoint>(Points.Length);
            Vector2[] points = Points;
            for (int l = 0; l < points.Length; l++)
            {
                Vector2 vector = points[l];
                list2.Add(new PolygonPoint(vector.x, vector.y));
            }
            Poly2Tri.Polygon polygon = new Poly2Tri.Polygon(list2);
            foreach (Polygon hole in holes)
            {
                List<PolygonPoint> list3 = new List<PolygonPoint>(hole.Points.Length);
                points = hole.Points;
                for (int l = 0; l < points.Length; l++)
                {
                    Vector2 vector2 = points[l];
                    list3.Add(new PolygonPoint(vector2.x, vector2.y));
                }
                polygon.AddHole(new Poly2Tri.Polygon(list3));
            }
            try
            {
                P2T.Triangulate(polygon);
            }
            catch
            {
                return null;
            }
            int count = polygon.Triangles.Count;
            List<int> list4 = new List<int>(count * 3);
            Points = new Vector2[count * 3];
            int num9 = 0;
            Min.x = float.MaxValue;
            Min.y = float.MaxValue;
            Max.x = float.MinValue;
            Max.y = float.MinValue;
            for (int m = 0; m < count; m++)
            {
                list4.Add(num9);
                list4.Add(num9 + 1);
                list4.Add(num9 + 2);
                Points[num9 + 2].x = (float)polygon.Triangles[m].Points._0.X;
                Points[num9 + 2].y = (float)polygon.Triangles[m].Points._0.Y;
                Points[num9 + 1].x = (float)polygon.Triangles[m].Points._1.X;
                Points[num9 + 1].y = (float)polygon.Triangles[m].Points._1.Y;
                Points[num9].x = (float)polygon.Triangles[m].Points._2.X;
                Points[num9].y = (float)polygon.Triangles[m].Points._2.Y;
                for (int n = 0; n < 3; n++)
                {
                    if (Points[num9 + n].x < Min.x)
                    {
                        Min.x = Points[num9 + n].x;
                    }
                    if (Points[num9 + n].y < Min.y)
                    {
                        Min.y = Points[num9 + n].y;
                    }
                    if (Points[num9 + n].x > Max.x)
                    {
                        Max.x = Points[num9 + n].x;
                    }
                    if (Points[num9 + n].y > Max.y)
                    {
                        Max.y = Points[num9 + n].y;
                    }
                }
                num9 += 3;
            }
            return list4;
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            Vector2 a = Points[V[u]];
            Vector2 b = Points[V[v]];
            Vector2 c = Points[V[w]];
            if (Mathf.Epsilon > (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x))
            {
                return false;
            }
            for (int i = 0; i < n; i++)
            {
                if (i != u && i != v && i != w)
                {
                    Vector2 p = Points[V[i]];
                    if (InsideTriangle(a, b, c, p))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float num = C.x - B.x;
            float num2 = C.y - B.y;
            float num3 = A.x - C.x;
            float num4 = A.y - C.y;
            float num5 = B.x - A.x;
            float num6 = B.y - A.y;
            float num7 = P.x - A.x;
            float num8 = P.y - A.y;
            float num9 = P.x - B.x;
            float num10 = P.y - B.y;
            float num11 = P.x - C.x;
            float num12 = P.y - C.y;
            float num13 = num * num10 - num2 * num9;
            float num14 = num5 * num8 - num6 * num7;
            float num15 = num3 * num12 - num4 * num11;
            if (num13 >= 0f && num15 >= 0f)
            {
                return num14 >= 0f;
            }
            return false;
        }
    }
}
