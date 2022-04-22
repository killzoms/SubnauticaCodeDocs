using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class HeightFieldMesh
    {
        private struct Node
        {
            public Vector2 v0;

            public Vector2 v1;

            public Vector2 v2;

            public float height;

            public Node(Vector2 _v0, Vector2 _v1, Vector2 _v2, float _height)
            {
                v0 = _v0;
                v1 = _v1;
                v2 = _v2;
                height = _height;
            }
        }

        private Mesh patchMesh;

        private bool _castShadows;

        private bool _receiveShadows;

        private bool _frustumCull = true;

        private static float oneOverSqrt2 = 1f / Mathf.Sqrt(2f);

        private static Stack<Node> nodeStack = new Stack<Node>();

        public bool castShadows
        {
            get
            {
                return _castShadows;
            }
            set
            {
                _castShadows = value;
            }
        }

        public bool receiveShadows
        {
            get
            {
                return _receiveShadows;
            }
            set
            {
                _receiveShadows = value;
            }
        }

        public bool frustumCull
        {
            get
            {
                return _frustumCull;
            }
            set
            {
                _frustumCull = value;
            }
        }

        public HeightFieldMesh(int numTrianglesPerPatch)
        {
            int numRows = 2 * (int)Mathf.Ceil((-2f + Mathf.Sqrt(4f + 8f * (float)numTrianglesPerPatch)) / 8f);
            patchMesh = CreatePatchMesh(numRows);
            patchMesh.bounds = new Bounds(new Vector3(0.5f, 0f, 0.5f), new Vector3(100f, 100f, 100f));
        }

        private static Mesh CreatePatchMesh(int numRows)
        {
            int num = numRows * (numRows + 4) + 1;
            int num2 = 2 * numRows * (numRows + 1) * 3;
            Vector3 a = new Vector3(1f, 0f, 0f);
            Vector3 vector = new Vector3(0f, 0f, 0f);
            Vector3 a2 = new Vector3(0f, 0f, 1f);
            Vector3[] array = new Vector3[num];
            int num3 = 0;
            for (int i = 0; i < numRows; i++)
            {
                Vector3 a3 = Vector3.Lerp(a, vector, (float)i / (float)numRows);
                Vector3 vector2 = Vector3.Lerp(a2, vector, (float)i / (float)numRows);
                Vector3 b = Vector3.Lerp(a, vector, (float)(i + 1) / (float)numRows);
                Vector3 b2 = Vector3.Lerp(a2, vector, (float)(i + 1) / (float)numRows);
                Vector3 vector3 = Vector3.Lerp(a3, b, 0.5f);
                Vector3 vector4 = Vector3.Lerp(vector2, b2, 0.5f);
                array[num3] = vector3;
                num3++;
                int num4 = (numRows - i - 1) * 2 + 3;
                float num5 = 1f / ((float)num4 - 1f);
                for (int j = 0; j < num4; j++)
                {
                    Vector3 vector5 = (array[num3] = Vector3.Lerp(a3, vector2, (float)j * num5));
                    num3++;
                }
                array[num3] = vector4;
                num3++;
            }
            array[num3] = vector;
            num3++;
            int[] array2 = new int[num2];
            int num6 = 0;
            int num7 = 0;
            for (int k = 0; k < numRows; k++)
            {
                int num8 = (numRows - k - 1) * 2 + 3;
                int num9 = ((k + 1 != numRows) ? 1 : 0);
                array2[num6++] = num7 + 1;
                array2[num6++] = num7;
                array2[num6++] = num7 + 2;
                array2[num6++] = num7 + 2;
                array2[num6++] = num7;
                array2[num6++] = num7 + num8 + 2 + num9;
                for (int l = 0; l < (num8 - 3) / 2; l++)
                {
                    array2[num6++] = num7 + 2 + l * 2;
                    array2[num6++] = num7 + 2 + l * 2 + num8 + 1;
                    array2[num6++] = num7 + 3 + l * 2;
                    array2[num6++] = num7 + 3 + l * 2;
                    array2[num6++] = num7 + 2 + l * 2 + num8 + 1;
                    array2[num6++] = num7 + 2 + l * 2 + num8 + 2;
                    array2[num6++] = num7 + 3 + l * 2;
                    array2[num6++] = num7 + 2 + l * 2 + num8 + 2;
                    array2[num6++] = num7 + 2 + l * 2 + num8 + 3;
                    array2[num6++] = num7 + 3 + l * 2;
                    array2[num6++] = num7 + 2 + l * 2 + num8 + 3;
                    array2[num6++] = num7 + 4 + l * 2;
                }
                array2[num6++] = num7 + num8 - 1;
                array2[num6++] = num7 + num8 + 1;
                array2[num6++] = num7 + num8;
                array2[num6++] = num7 + num8 + 1;
                array2[num6++] = num7 + num8 - 1;
                array2[num6++] = num7 + 2 * num8 - 1 + num9;
                num7 += num8 + 2;
            }
            return new Mesh
            {
                vertices = array,
                triangles = array2
            };
        }

        public bool Render(Vector3 viewPoint, Vector3 center, float size, float minPatchSize, float errorThreshold, Material material, Camera camera = null)
        {
            global::UnityEngine.Plane[] array = null;
            if (camera == null)
            {
                camera = MainCamera.camera;
                array = MainCamera.GetCameraFrustumPlanes();
            }
            else
            {
                array = GeometryUtility.CalculateFrustumPlanes(camera);
            }
            Vector2 vector = new Vector2(size + center.x, 0f - size + center.z);
            Vector2 v = new Vector2(0f - size + center.x, 0f - size + center.z);
            Vector2 vector2 = new Vector2(0f - size + center.x, size + center.z);
            Vector2 v2 = new Vector2(size + center.x, size + center.z);
            float num = minPatchSize * oneOverSqrt2;
            float regularRightTriangleHeight = GetRegularRightTriangleHeight(vector, v, vector2);
            nodeStack.Push(new Node(vector, v, vector2, regularRightTriangleHeight));
            nodeStack.Push(new Node(vector2, v2, vector, regularRightTriangleHeight));
            Matrix4x4 identity = Matrix4x4.identity;
            identity.SetColumn(1, new Vector4(0f, 1f, 0f, 0f));
            float invErrorThreshold = 1f / errorThreshold;
            bool result = false;
            while (nodeStack.Count > 0)
            {
                Node n = nodeStack.Pop();
                if (!frustumCull || IsNodeInFrustum(n, center, size, array))
                {
                    Vector2 v3 = (n.v0 + n.v2) * 0.5f;
                    if (n.height > num && GetShouldSubdivide(viewPoint, n.height, invErrorThreshold, new Vector3(v3.x, center.y, v3.y)))
                    {
                        float height = n.height * oneOverSqrt2;
                        nodeStack.Push(new Node(n.v1, v3, n.v0, height));
                        nodeStack.Push(new Node(n.v2, v3, n.v1, height));
                        continue;
                    }
                    Vector2 vector3 = n.v0 - n.v1;
                    Vector2 vector4 = n.v2 - n.v1;
                    identity.SetColumn(0, new Vector4(vector3.x, 0f, vector3.y, 0f));
                    identity.SetColumn(2, new Vector4(vector4.x, 0f, vector4.y, 0f));
                    identity.SetColumn(3, new Vector4(n.v1.x, center.y, n.v1.y, 1f));
                    Graphics.DrawMesh(patchMesh, identity, material, 0, camera, 0, null, castShadows, receiveShadows);
                    result = true;
                }
            }
            return result;
        }

        private static bool GetShouldSubdivide(Vector3 viewPoint, float height, float invErrorThreshold, Vector3 mid)
        {
            float num = 2f * height * oneOverSqrt2;
            float sqrMagnitude = (viewPoint - mid).sqrMagnitude;
            float num2 = height * invErrorThreshold + num;
            return num2 * num2 > sqrMagnitude;
        }

        private float GetRegularRightTriangleHeight(Vector2 v0, Vector2 v1, Vector2 v2)
        {
            return Mathf.Sqrt((v2 - v1).sqrMagnitude * 0.5f);
        }

        private bool IsNodeInFrustum(Node n, Vector3 center, float size, global::UnityEngine.Plane[] frustumPlanes)
        {
            Vector3 point = new Vector3(n.v0.x, center.y, n.v0.y);
            Vector3 point2 = new Vector3(n.v1.x, center.y, n.v1.y);
            Vector3 point3 = new Vector3(n.v2.x, center.y, n.v2.y);
            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                global::UnityEngine.Plane plane = frustumPlanes[i];
                if (!plane.GetSide(point) && !plane.GetSide(point2) && !plane.GetSide(point3))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
