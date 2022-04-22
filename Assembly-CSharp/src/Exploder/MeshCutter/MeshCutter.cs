using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Plane = AssemblyCSharp.Exploder.MeshCutter.Math.Plane;

namespace AssemblyCSharp.Exploder.MeshCutter
{
    public class MeshCutter
    {
        private struct Triangle
        {
            public int[] ids;

            public Vector3[] pos;

            public Vector3[] normal;

            public Vector2[] uvs;

            public Vector4[] tangents;

            public Color32[] colors;
        }

        private List<int>[] triangles;

        private List<Vector3>[] vertices;

        private List<Vector3>[] normals;

        private List<Vector2>[] uvs;

        private List<Vector4>[] tangents;

        private List<Color32>[] vertexColors;

        private List<int> cutTris;

        private int[] triCache;

        private Vector3[] centroid;

        private int[] triCounter;

        private Contour contour;

        private Dictionary<long, int>[] cutVertCache;

        private Dictionary<int, int>[] cornerVertCache;

        private int contourBufferSize;

        private Color crossSectionVertexColour;

        private Vector4 crossSectionUV;

        public void Init(int trianglesNum, int verticesNum)
        {
            AllocateBuffers(trianglesNum, verticesNum, useMeshTangents: false, useVertexColors: false);
            AllocateContours(trianglesNum / 2);
        }

        private void AllocateBuffers(int trianglesNum, int verticesNum, bool useMeshTangents, bool useVertexColors)
        {
            if (triangles == null || triangles[0].Capacity < trianglesNum)
            {
                triangles = new List<int>[2]
                {
                    new List<int>(trianglesNum),
                    new List<int>(trianglesNum)
                };
            }
            else
            {
                triangles[0].Clear();
                triangles[1].Clear();
            }
            if (vertices == null || vertices[0].Capacity < verticesNum || triCache.Length < verticesNum || (useMeshTangents && (tangents == null || tangents[0].Capacity < verticesNum)) || (useVertexColors && (vertexColors == null || vertexColors[0].Capacity < verticesNum)))
            {
                vertices = new List<Vector3>[2]
                {
                    new List<Vector3>(verticesNum),
                    new List<Vector3>(verticesNum)
                };
                normals = new List<Vector3>[2]
                {
                    new List<Vector3>(verticesNum),
                    new List<Vector3>(verticesNum)
                };
                uvs = new List<Vector2>[2]
                {
                    new List<Vector2>(verticesNum),
                    new List<Vector2>(verticesNum)
                };
                if (useMeshTangents)
                {
                    tangents = new List<Vector4>[2]
                    {
                        new List<Vector4>(verticesNum),
                        new List<Vector4>(verticesNum)
                    };
                }
                else
                {
                    tangents = new List<Vector4>[2]
                    {
                        new List<Vector4>(0),
                        new List<Vector4>(0)
                    };
                }
                if (useVertexColors)
                {
                    vertexColors = new List<Color32>[2]
                    {
                        new List<Color32>(verticesNum),
                        new List<Color32>(verticesNum)
                    };
                }
                else
                {
                    vertexColors = new List<Color32>[2]
                    {
                        new List<Color32>(0),
                        new List<Color32>(0)
                    };
                }
                centroid = new Vector3[2];
                triCache = new int[verticesNum + 1];
                triCounter = new int[2];
                cutTris = new List<int>(verticesNum / 3);
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    vertices[i].Clear();
                    normals[i].Clear();
                    uvs[i].Clear();
                    tangents[i].Clear();
                    vertexColors[i].Clear();
                    centroid[i] = Vector3.zero;
                    triCounter[i] = 0;
                }
                cutTris.Clear();
                for (int j = 0; j < triCache.Length; j++)
                {
                    triCache[j] = 0;
                }
            }
        }

        private void AllocateContours(int cutTrianglesNum)
        {
            if (contour == null)
            {
                contour = new Contour(cutTrianglesNum);
                cutVertCache = new Dictionary<long, int>[2]
                {
                    new Dictionary<long, int>(cutTrianglesNum * 2),
                    new Dictionary<long, int>(cutTrianglesNum * 2)
                };
                cornerVertCache = new Dictionary<int, int>[2]
                {
                    new Dictionary<int, int>(cutTrianglesNum),
                    new Dictionary<int, int>(cutTrianglesNum)
                };
                contourBufferSize = cutTrianglesNum;
                return;
            }
            if (contourBufferSize < cutTrianglesNum)
            {
                cutVertCache = new Dictionary<long, int>[2]
                {
                    new Dictionary<long, int>(cutTrianglesNum * 2),
                    new Dictionary<long, int>(cutTrianglesNum * 2)
                };
                cornerVertCache = new Dictionary<int, int>[2]
                {
                    new Dictionary<int, int>(cutTrianglesNum),
                    new Dictionary<int, int>(cutTrianglesNum)
                };
                contourBufferSize = cutTrianglesNum;
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    cutVertCache[i].Clear();
                    cornerVertCache[i].Clear();
                }
            }
            contour.AllocateBuffers(cutTrianglesNum);
        }

        public float Cut(Mesh mesh, Transform meshTransform, Plane plane, bool triangulateHoles, bool allowOpenMesh, ref List<CutterMesh> meshes, Color crossSectionVertexColor, Vector4 crossUV)
        {
            crossSectionVertexColour = crossSectionVertexColor;
            crossSectionUV = crossUV;
            return Cut(mesh, meshTransform, plane, triangulateHoles, allowOpenMesh, ref meshes);
        }

        private float Cut(Mesh mesh, Transform meshTransform, Plane plane, bool triangulateHoles, bool allowOpenMesh, ref List<CutterMesh> meshes)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int num = mesh.triangles.Length;
            int verticesNum = mesh.vertices.Length;
            int[] array = mesh.triangles;
            Vector4[] array2 = mesh.tangents;
            Color32[] colors = mesh.colors32;
            Vector3[] array3 = mesh.vertices;
            Vector3[] array4 = mesh.normals;
            Vector2[] uv = mesh.uv;
            bool flag = array2 != null && array2.Length != 0;
            bool flag2 = colors != null && colors.Length != 0;
            bool flag3 = array4 != null && array4.Length != 0;
            AllocateBuffers(num, verticesNum, flag, flag2);
            plane.InverseTransform(meshTransform);
            for (int i = 0; i < num; i += 3)
            {
                Vector3 n = array3[array[i]];
                Vector3 n2 = array3[array[i + 1]];
                Vector3 n3 = array3[array[i + 2]];
                bool sideFix = plane.GetSideFix(ref n);
                bool sideFix2 = plane.GetSideFix(ref n2);
                bool sideFix3 = plane.GetSideFix(ref n3);
                array3[array[i]] = n;
                array3[array[i + 1]] = n2;
                array3[array[i + 2]] = n3;
                if (sideFix == sideFix2 && sideFix2 == sideFix3)
                {
                    int num2 = ((!sideFix) ? 1 : 0);
                    _ = array[i];
                    _ = triCache.Length;
                    if (triCache[array[i]] == 0)
                    {
                        triangles[num2].Add(triCounter[num2]);
                        vertices[num2].Add(array3[array[i]]);
                        uvs[num2].Add(uv[array[i]]);
                        if (flag3)
                        {
                            normals[num2].Add(array4[array[i]]);
                        }
                        if (flag)
                        {
                            tangents[num2].Add(array2[array[i]]);
                        }
                        if (flag2)
                        {
                            vertexColors[num2].Add(colors[array[i]]);
                        }
                        centroid[num2] += array3[array[i]];
                        triCache[array[i]] = triCounter[num2] + 1;
                        triCounter[num2]++;
                    }
                    else
                    {
                        triangles[num2].Add(triCache[array[i]] - 1);
                    }
                    if (triCache[array[i + 1]] == 0)
                    {
                        triangles[num2].Add(triCounter[num2]);
                        vertices[num2].Add(array3[array[i + 1]]);
                        uvs[num2].Add(uv[array[i + 1]]);
                        if (flag3)
                        {
                            normals[num2].Add(array4[array[i + 1]]);
                        }
                        if (flag)
                        {
                            tangents[num2].Add(array2[array[i + 1]]);
                        }
                        if (flag2)
                        {
                            vertexColors[num2].Add(colors[array[i + 1]]);
                        }
                        centroid[num2] += array3[array[i + 1]];
                        triCache[array[i + 1]] = triCounter[num2] + 1;
                        triCounter[num2]++;
                    }
                    else
                    {
                        triangles[num2].Add(triCache[array[i + 1]] - 1);
                    }
                    if (triCache[array[i + 2]] == 0)
                    {
                        triangles[num2].Add(triCounter[num2]);
                        vertices[num2].Add(array3[array[i + 2]]);
                        uvs[num2].Add(uv[array[i + 2]]);
                        if (flag3)
                        {
                            normals[num2].Add(array4[array[i + 2]]);
                        }
                        if (flag)
                        {
                            tangents[num2].Add(array2[array[i + 2]]);
                        }
                        if (flag2)
                        {
                            vertexColors[num2].Add(colors[array[i + 2]]);
                        }
                        centroid[num2] += array3[array[i + 2]];
                        triCache[array[i + 2]] = triCounter[num2] + 1;
                        triCounter[num2]++;
                    }
                    else
                    {
                        triangles[num2].Add(triCache[array[i + 2]] - 1);
                    }
                }
                else
                {
                    cutTris.Add(i);
                }
            }
            if (vertices[0].Count == 0)
            {
                centroid[0] = array3[0];
            }
            else
            {
                centroid[0] /= (float)vertices[0].Count;
            }
            if (vertices[1].Count == 0)
            {
                centroid[1] = array3[1];
            }
            else
            {
                centroid[1] /= (float)vertices[1].Count;
            }
            CutterMesh item = default(CutterMesh);
            item.centroid = centroid[0];
            CutterMesh item2 = default(CutterMesh);
            item2.centroid = centroid[1];
            item.mesh = null;
            item2.mesh = null;
            if (cutTris.Count < 1)
            {
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            }
            AllocateContours(cutTris.Count);
            foreach (int cutTri in cutTris)
            {
                Triangle triangle = default(Triangle);
                triangle.ids = new int[3]
                {
                    array[cutTri],
                    array[cutTri + 1],
                    array[cutTri + 2]
                };
                triangle.pos = new Vector3[3]
                {
                    array3[array[cutTri]],
                    array3[array[cutTri + 1]],
                    array3[array[cutTri + 2]]
                };
                triangle.normal = ((!flag3) ? new Vector3[3]
                {
                    Vector3.zero,
                    Vector3.zero,
                    Vector3.zero
                } : new Vector3[3]
                {
                    array4[array[cutTri]],
                    array4[array[cutTri + 1]],
                    array4[array[cutTri + 2]]
                });
                triangle.uvs = new Vector2[3]
                {
                    uv[array[cutTri]],
                    uv[array[cutTri + 1]],
                    uv[array[cutTri + 2]]
                };
                triangle.tangents = ((!flag) ? new Vector4[3]
                {
                    Vector4.zero,
                    Vector4.zero,
                    Vector4.zero
                } : new Vector4[3]
                {
                    array2[array[cutTri]],
                    array2[array[cutTri + 1]],
                    array2[array[cutTri + 2]]
                });
                triangle.colors = ((!flag2) ? new Color32[3]
                {
                    Color.white,
                    Color.white,
                    Color.white
                } : new Color32[3]
                {
                    colors[array[cutTri]],
                    colors[array[cutTri + 1]],
                    colors[array[cutTri + 2]]
                });
                Triangle tri = triangle;
                bool side = plane.GetSide(tri.pos[0]);
                bool side2 = plane.GetSide(tri.pos[1]);
                bool side3 = plane.GetSide(tri.pos[2]);
                Vector3 q = Vector3.zero;
                Vector3 q2 = Vector3.zero;
                int num3 = ((!side) ? 1 : 0);
                int num4 = 1 - num3;
                float t;
                float t2;
                if (side == side2)
                {
                    plane.IntersectSegment(tri.pos[2], tri.pos[0], out t, ref q);
                    plane.IntersectSegment(tri.pos[2], tri.pos[1], out t2, ref q2);
                    int num5 = AddIntersectionPoint(q, tri, tri.ids[2], tri.ids[0], cutVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                    int num6 = AddIntersectionPoint(q2, tri, tri.ids[2], tri.ids[1], cutVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                    int item3 = AddTrianglePoint(tri.pos[0], tri.normal[0], tri.uvs[0], tri.tangents[0], tri.colors[0], tri.ids[0], triCache, cornerVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                    int item4 = AddTrianglePoint(tri.pos[1], tri.normal[1], tri.uvs[1], tri.tangents[1], tri.colors[1], tri.ids[1], triCache, cornerVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                    triangles[num3].Add(num5);
                    triangles[num3].Add(item3);
                    triangles[num3].Add(num6);
                    triangles[num3].Add(num6);
                    triangles[num3].Add(item3);
                    triangles[num3].Add(item4);
                    int num7 = AddIntersectionPoint(q, tri, tri.ids[2], tri.ids[0], cutVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                    int num8 = AddIntersectionPoint(q2, tri, tri.ids[2], tri.ids[1], cutVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                    int item5 = AddTrianglePoint(tri.pos[2], tri.normal[2], tri.uvs[2], tri.tangents[2], tri.colors[2], tri.ids[2], triCache, cornerVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                    triangles[num4].Add(item5);
                    triangles[num4].Add(num7);
                    triangles[num4].Add(num8);
                    if (triangulateHoles)
                    {
                        if (num3 == 0)
                        {
                            contour.AddTriangle(cutTri, num5, num6, q, q2);
                        }
                        else
                        {
                            contour.AddTriangle(cutTri, num7, num8, q, q2);
                        }
                    }
                    continue;
                }
                if (side == side3)
                {
                    plane.IntersectSegment(tri.pos[1], tri.pos[0], out t, ref q2);
                    plane.IntersectSegment(tri.pos[1], tri.pos[2], out t2, ref q);
                    int num9 = AddIntersectionPoint(q, tri, tri.ids[1], tri.ids[2], cutVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                    int num10 = AddIntersectionPoint(q2, tri, tri.ids[1], tri.ids[0], cutVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                    int item6 = AddTrianglePoint(tri.pos[0], tri.normal[0], tri.uvs[0], tri.tangents[0], tri.colors[0], tri.ids[0], triCache, cornerVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                    int item7 = AddTrianglePoint(tri.pos[2], tri.normal[2], tri.uvs[2], tri.tangents[2], tri.colors[2], tri.ids[2], triCache, cornerVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                    triangles[num3].Add(item7);
                    triangles[num3].Add(num10);
                    triangles[num3].Add(num9);
                    triangles[num3].Add(item7);
                    triangles[num3].Add(item6);
                    triangles[num3].Add(num10);
                    int num11 = AddIntersectionPoint(q, tri, tri.ids[1], tri.ids[2], cutVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                    int num12 = AddIntersectionPoint(q2, tri, tri.ids[1], tri.ids[0], cutVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                    int item8 = AddTrianglePoint(tri.pos[1], tri.normal[1], tri.uvs[1], tri.tangents[1], tri.colors[1], tri.ids[1], triCache, cornerVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                    triangles[num4].Add(num11);
                    triangles[num4].Add(num12);
                    triangles[num4].Add(item8);
                    if (triangulateHoles)
                    {
                        if (num3 == 0)
                        {
                            contour.AddTriangle(cutTri, num9, num10, q, q2);
                        }
                        else
                        {
                            contour.AddTriangle(cutTri, num11, num12, q, q2);
                        }
                    }
                    continue;
                }
                plane.IntersectSegment(tri.pos[0], tri.pos[1], out t, ref q);
                plane.IntersectSegment(tri.pos[0], tri.pos[2], out t2, ref q2);
                int num13 = AddIntersectionPoint(q, tri, tri.ids[0], tri.ids[1], cutVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                int num14 = AddIntersectionPoint(q2, tri, tri.ids[0], tri.ids[2], cutVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                int item9 = AddTrianglePoint(tri.pos[1], tri.normal[1], tri.uvs[1], tri.tangents[1], tri.colors[1], tri.ids[1], triCache, cornerVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                int item10 = AddTrianglePoint(tri.pos[2], tri.normal[2], tri.uvs[2], tri.tangents[2], tri.colors[2], tri.ids[2], triCache, cornerVertCache[num4], vertices[num4], normals[num4], uvs[num4], tangents[num4], vertexColors[num4], flag, flag2, flag3);
                triangles[num4].Add(item10);
                triangles[num4].Add(num14);
                triangles[num4].Add(item9);
                triangles[num4].Add(num14);
                triangles[num4].Add(num13);
                triangles[num4].Add(item9);
                int num15 = AddIntersectionPoint(q, tri, tri.ids[0], tri.ids[1], cutVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                int num16 = AddIntersectionPoint(q2, tri, tri.ids[0], tri.ids[2], cutVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                int item11 = AddTrianglePoint(tri.pos[0], tri.normal[0], tri.uvs[0], tri.tangents[0], tri.colors[0], tri.ids[0], triCache, cornerVertCache[num3], vertices[num3], normals[num3], uvs[num3], tangents[num3], vertexColors[num3], flag, flag2, flag3);
                triangles[num3].Add(num16);
                triangles[num3].Add(item11);
                triangles[num3].Add(num15);
                if (triangulateHoles)
                {
                    if (num3 == 0)
                    {
                        contour.AddTriangle(cutTri, num15, num16, q, q2);
                    }
                    else
                    {
                        contour.AddTriangle(cutTri, num13, num14, q, q2);
                    }
                }
            }
            if (triangulateHoles)
            {
                contour.FindContours();
                if (contour.contour.Count == 0 || contour.contour[0].Count < 3)
                {
                    if (!allowOpenMesh)
                    {
                        stopwatch.Stop();
                        return stopwatch.ElapsedMilliseconds;
                    }
                    triangulateHoles = false;
                }
            }
            List<int>[] array5 = null;
            if (triangulateHoles)
            {
                array5 = new List<int>[2]
                {
                    new List<int>(contour.MidPointsCount),
                    new List<int>(contour.MidPointsCount)
                };
                Triangulate(contour.contour, plane, vertices, normals, uvs, tangents, vertexColors, array5, uvCutMesh: true, flag, flag2, flag3);
            }
            if (vertices[0].Count > 3 && vertices[1].Count > 3)
            {
                item.mesh = new Mesh();
                item2.mesh = new Mesh();
                item.mesh.SetVertices(vertices[0]);
                item2.mesh.SetVertices(vertices[1]);
                item.mesh.SetUVs(0, uvs[0]);
                item2.mesh.SetUVs(0, uvs[1]);
                if (flag3)
                {
                    item.mesh.SetNormals(normals[0]);
                    item2.mesh.SetNormals(normals[1]);
                }
                if (flag)
                {
                    item.mesh.SetTangents(tangents[0]);
                    item2.mesh.SetTangents(tangents[1]);
                }
                if (flag2)
                {
                    item.mesh.SetColors(vertexColors[0]);
                    item2.mesh.SetColors(vertexColors[1]);
                }
                if (array5 != null && array5[0].Count > 3)
                {
                    triangles[0].AddRange(array5[0]);
                    triangles[1].AddRange(array5[1]);
                }
                item.mesh.subMeshCount = 1;
                item2.mesh.subMeshCount = 1;
                item.mesh.SetTriangles(triangles[0], 0);
                item2.mesh.SetTriangles(triangles[1], 0);
                if (!triangulateHoles)
                {
                    item.centroid = Vector3.zero;
                    item2.centroid = Vector3.zero;
                    foreach (Vector3 item12 in vertices[0])
                    {
                        item.centroid += item12;
                    }
                    item.centroid /= (float)vertices[0].Count;
                    foreach (Vector3 item13 in vertices[1])
                    {
                        item2.centroid += item13;
                    }
                    item2.centroid /= (float)vertices[1].Count;
                }
                meshes = new List<CutterMesh> { item, item2 };
                stopwatch.Stop();
                return stopwatch.ElapsedMilliseconds;
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private int AddIntersectionPoint(Vector3 pos, Triangle tri, int edge0, int edge1, Dictionary<long, int> cache, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<Vector4> tangents, List<Color32> colors32, bool useTangents, bool useColors, bool useNormals)
        {
            int num = ((edge0 < edge1) ? ((edge0 << 16) + edge1) : ((edge1 << 16) + edge0));
            if (cache.TryGetValue(num, out var value))
            {
                return value;
            }
            Vector3 vector = MeshUtils.ComputeBarycentricCoordinates(tri.pos[0], tri.pos[1], tri.pos[2], pos);
            vertices.Add(pos);
            if (useNormals)
            {
                normals.Add(new Vector3(vector.x * tri.normal[0].x + vector.y * tri.normal[1].x + vector.z * tri.normal[2].x, vector.x * tri.normal[0].y + vector.y * tri.normal[1].y + vector.z * tri.normal[2].y, vector.x * tri.normal[0].z + vector.y * tri.normal[1].z + vector.z * tri.normal[2].z));
            }
            uvs.Add(new Vector2(vector.x * tri.uvs[0].x + vector.y * tri.uvs[1].x + vector.z * tri.uvs[2].x, vector.x * tri.uvs[0].y + vector.y * tri.uvs[1].y + vector.z * tri.uvs[2].y));
            if (useTangents)
            {
                tangents.Add(new Vector4(vector.x * tri.tangents[0].x + vector.y * tri.tangents[1].x + vector.z * tri.tangents[2].x, vector.x * tri.tangents[0].y + vector.y * tri.tangents[1].y + vector.z * tri.tangents[2].y, vector.x * tri.tangents[0].z + vector.y * tri.tangents[1].z + vector.z * tri.tangents[2].z, vector.x * tri.tangents[0].w + vector.y * tri.tangents[1].w + vector.z * tri.tangents[2].z));
            }
            if (useColors)
            {
                colors32.Add(tri.colors[0]);
            }
            int num2 = vertices.Count - 1;
            cache.Add(num, num2);
            return num2;
        }

        private int AddTrianglePoint(Vector3 pos, Vector3 normal, Vector2 uv, Vector4 tangent, Color32 color, int idx, int[] triCache, Dictionary<int, int> cache, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<Vector4> tangents, List<Color32> colors, bool useTangents, bool useColors, bool useNormals)
        {
            if (triCache[idx] != 0)
            {
                return triCache[idx] - 1;
            }
            if (cache.TryGetValue(idx, out var value))
            {
                return value;
            }
            vertices.Add(pos);
            if (useNormals)
            {
                normals.Add(normal);
            }
            uvs.Add(uv);
            if (useTangents)
            {
                tangents.Add(tangent);
            }
            if (useColors)
            {
                colors.Add(color);
            }
            int num = vertices.Count - 1;
            cache.Add(idx, num);
            return num;
        }

        private void Triangulate(List<Dictionary<int, int>> contours, Plane plane, List<Vector3>[] vertices, List<Vector3>[] normals, List<Vector2>[] uvs, List<Vector4>[] tangents, List<Color32>[] colors, List<int>[] triangles, bool uvCutMesh, bool useTangents, bool useColors, bool useNormals)
        {
            if (contours.Count == 0 || contours[0].Count < 3)
            {
                return;
            }
            Matrix4x4 planeMatrix = plane.GetPlaneMatrix();
            Matrix4x4 inverse = planeMatrix.inverse;
            float z = 0f;
            List<Polygon> list = new List<Polygon>(contours.Count);
            Polygon polygon = null;
            foreach (Dictionary<int, int> contour in contours)
            {
                Vector2[] array = new Vector2[contour.Count];
                int num = 0;
                foreach (int value in contour.Values)
                {
                    Vector4 vector = inverse * vertices[0][value];
                    array[num++] = vector;
                    z = vector.z;
                }
                Polygon polygon2 = new Polygon(array);
                list.Add(polygon2);
                if (polygon == null || Mathf.Abs(polygon.Area) < Mathf.Abs(polygon2.Area))
                {
                    polygon = polygon2;
                }
            }
            if (list.Count > 0)
            {
                List<Polygon> list2 = new List<Polygon>();
                foreach (Polygon item4 in list)
                {
                    if (item4 != polygon && polygon.IsPointInside(item4.Points[0]))
                    {
                        polygon.AddHole(item4);
                        list2.Add(item4);
                    }
                }
                foreach (Polygon item5 in list2)
                {
                    list.Remove(item5);
                }
            }
            int num2 = vertices[0].Count;
            int num3 = vertices[1].Count;
            foreach (Polygon item6 in list)
            {
                List<int> list3 = item6.Triangulate();
                if (list3 == null)
                {
                    continue;
                }
                float num4 = Mathf.Min(item6.Min.x, item6.Min.y);
                float num5 = Mathf.Max(item6.Max.x, item6.Max.y) - num4;
                Vector2[] points = item6.Points;
                for (int i = 0; i < points.Length; i++)
                {
                    Vector2 vector2 = points[i];
                    Vector4 vector3 = planeMatrix * new Vector3(vector2.x, vector2.y, z);
                    vertices[0].Add(vector3);
                    vertices[1].Add(vector3);
                    if (useNormals)
                    {
                        normals[0].Add(-plane.Normal);
                        normals[1].Add(plane.Normal);
                    }
                    if (uvCutMesh)
                    {
                        Vector2 item = new Vector2((vector2.x - num4) / num5, (vector2.y - num4) / num5);
                        Vector2 item2 = new Vector2((vector2.x - num4) / num5, (vector2.y - num4) / num5);
                        float num6 = crossSectionUV.z - crossSectionUV.x;
                        float num7 = crossSectionUV.w - crossSectionUV.y;
                        item.x = crossSectionUV.x + item.x * num6;
                        item.y = crossSectionUV.y + item.y * num7;
                        item2.x = crossSectionUV.x + item2.x * num6;
                        item2.y = crossSectionUV.y + item2.y * num7;
                        uvs[0].Add(item);
                        uvs[1].Add(item2);
                    }
                    else
                    {
                        uvs[0].Add(Vector2.zero);
                        uvs[1].Add(Vector2.zero);
                    }
                    if (useTangents)
                    {
                        Vector3 normal = plane.Normal;
                        MeshUtils.Swap(ref normal.x, ref normal.y);
                        MeshUtils.Swap(ref normal.y, ref normal.z);
                        Vector4 item3 = Vector3.Cross(plane.Normal, normal);
                        item3.w = 1f;
                        tangents[0].Add(item3);
                        item3.w = -1f;
                        tangents[1].Add(item3);
                    }
                    if (useColors)
                    {
                        colors[0].Add(crossSectionVertexColour);
                        colors[1].Add(crossSectionVertexColour);
                    }
                }
                int count = list3.Count;
                int num8 = count - 1;
                for (int j = 0; j < count; j++)
                {
                    triangles[0].Add(num2 + list3[j]);
                    triangles[1].Add(num3 + list3[num8]);
                    num8--;
                }
                num2 += item6.Points.Length;
                num3 += item6.Points.Length;
            }
        }
    }
}
