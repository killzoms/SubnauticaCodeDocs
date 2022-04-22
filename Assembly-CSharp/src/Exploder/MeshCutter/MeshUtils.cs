using System.Collections.Generic;
using AssemblyCSharp.Exploder.Utils;
using UnityEngine;

namespace AssemblyCSharp.Exploder.MeshCutter
{
    public static class MeshUtils
    {
        public static Vector3 ComputeBarycentricCoordinates(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            float num = b.x - a.x;
            float num2 = b.y - a.y;
            float num3 = b.z - a.z;
            float num4 = c.x - a.x;
            float num5 = c.y - a.y;
            float num6 = c.z - a.z;
            float num7 = p.x - a.x;
            float num8 = p.y - a.y;
            float num9 = p.z - a.z;
            float num10 = num * num + num2 * num2 + num3 * num3;
            float num11 = num * num4 + num2 * num5 + num3 * num6;
            float num12 = num4 * num4 + num5 * num5 + num6 * num6;
            float num13 = num7 * num + num8 * num2 + num9 * num3;
            float num14 = num7 * num4 + num8 * num5 + num9 * num6;
            float num15 = num10 * num12 - num11 * num11;
            float num16 = (num12 * num13 - num11 * num14) / num15;
            float num17 = (num10 * num14 - num11 * num13) / num15;
            return new Vector3(1f - num16 - num17, num16, num17);
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T val = a;
            a = b;
            b = val;
        }

        public static void CenterPivot(Vector3[] vertices, Vector3 centroid)
        {
            int num = vertices.Length;
            for (int i = 0; i < num; i++)
            {
                Vector3 vector = vertices[i];
                vector.x -= centroid.x;
                vector.y -= centroid.y;
                vector.z -= centroid.z;
                vertices[i] = vector;
            }
        }

        public static List<CutterMesh> IsolateMeshIslands(Mesh mesh)
        {
            int[] triangles = mesh.triangles;
            int vertexCount = mesh.vertexCount;
            int num = mesh.triangles.Length;
            Vector4[] tangents = mesh.tangents;
            Color32[] colors = mesh.colors32;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uv = mesh.uv;
            bool flag = tangents != null && tangents.Length != 0;
            bool flag2 = colors != null && colors.Length != 0;
            bool flag3 = normals != null && normals.Length != 0;
            if (num <= 3)
            {
                return null;
            }
            LSHash lSHash = new LSHash(0.1f, vertexCount);
            int[] array = new int[num];
            for (int i = 0; i < num; i++)
            {
                array[i] = lSHash.Hash(vertices[triangles[i]]);
            }
            List<HashSet<int>> list = new List<HashSet<int>>
            {
                new HashSet<int>
                {
                    array[0],
                    array[1],
                    array[2]
                }
            };
            List<List<int>> list2 = new List<List<int>>
            {
                new List<int>(num) { 0, 1, 2 }
            };
            bool[] array2 = new bool[num];
            array2[0] = true;
            array2[1] = true;
            array2[2] = true;
            HashSet<int> hashSet = list[0];
            List<int> list3 = list2[0];
            int num2 = 3;
            int num3 = -1;
            int num4 = 0;
            do
            {
                bool flag4 = false;
                for (int j = 3; j < num; j += 3)
                {
                    if (!array2[j])
                    {
                        if (hashSet.Contains(array[j]) || hashSet.Contains(array[j + 1]) || hashSet.Contains(array[j + 2]))
                        {
                            hashSet.Add(array[j]);
                            hashSet.Add(array[j + 1]);
                            hashSet.Add(array[j + 2]);
                            list3.Add(j);
                            list3.Add(j + 1);
                            list3.Add(j + 2);
                            array2[j] = true;
                            array2[j + 1] = true;
                            array2[j + 2] = true;
                            num2 += 3;
                            flag4 = true;
                        }
                        else
                        {
                            num3 = j;
                        }
                    }
                }
                if (num2 == num)
                {
                    break;
                }
                if (!flag4)
                {
                    hashSet = new HashSet<int>
                    {
                        array[num3],
                        array[num3 + 1],
                        array[num3 + 2]
                    };
                    list3 = new List<int>(num / 2)
                    {
                        num3,
                        num3 + 1,
                        num3 + 2
                    };
                    list.Add(hashSet);
                    list2.Add(list3);
                }
                num4++;
            }
            while (num4 <= 100);
            if (list.Count == 1)
            {
                return null;
            }
            List<CutterMesh> list4 = new List<CutterMesh>(list.Count);
            foreach (List<int> item2 in list2)
            {
                CutterMesh item = new CutterMesh
                {
                    mesh = new Mesh()
                };
                int count = item2.Count;
                Mesh mesh2 = item.mesh;
                List<int> list5 = new List<int>(count);
                List<Vector3> list6 = new List<Vector3>(count);
                List<Vector3> list7 = new List<Vector3>(count);
                List<Vector2> list8 = new List<Vector2>(count);
                List<Color32> list9 = new List<Color32>(count);
                List<Vector4> list10 = new List<Vector4>(count);
                Dictionary<int, int> dictionary = new Dictionary<int, int>(num);
                Vector3 zero = Vector3.zero;
                int num5 = 0;
                int num6 = 0;
                foreach (int item3 in item2)
                {
                    int num7 = triangles[item3];
                    int value = 0;
                    if (dictionary.TryGetValue(num7, out value))
                    {
                        list5.Add(value);
                        continue;
                    }
                    list5.Add(num6);
                    dictionary.Add(num7, num6);
                    num6++;
                    zero += vertices[num7];
                    num5++;
                    list6.Add(vertices[num7]);
                    list8.Add(uv[num7]);
                    if (flag3)
                    {
                        list7.Add(normals[num7]);
                    }
                    if (flag2)
                    {
                        list9.Add(colors[num7]);
                    }
                    if (flag)
                    {
                        list10.Add(tangents[num7]);
                    }
                }
                mesh2.SetVertices(list6);
                mesh2.SetUVs(0, list8);
                if (flag3)
                {
                    mesh2.SetNormals(list7);
                }
                if (flag2)
                {
                    mesh2.SetColors(list9);
                }
                if (flag)
                {
                    mesh2.SetTangents(list10);
                }
                mesh2.subMeshCount = 1;
                mesh2.SetTriangles(list5, 0);
                item.centroid = zero / num5;
                list4.Add(item);
            }
            return list4;
        }

        public static void GeneratePolygonCollider(PolygonCollider2D collider, Mesh mesh)
        {
            if ((bool)mesh && (bool)collider)
            {
                Vector3[] vertices = mesh.vertices;
                Vector2[] array = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    array[i] = vertices[i];
                }
                Vector2[] points = Hull2D.ChainHull2D(array);
                collider.SetPath(0, points);
            }
        }
    }
}
