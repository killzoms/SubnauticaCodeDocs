using System;
using UnityEngine;

namespace AssemblyCSharp.Exploder.MeshCutter.Math
{
    public class Plane
    {
        [Flags]
        public enum PointClass
        {
            Coplanar = 0x0,
            Front = 0x1,
            Back = 0x2,
            Intersection = 0x3
        }

        private const float epsylon = 0.0001f;

        public Vector3 Normal;

        public float Distance;

        public Vector3 Pnt { get; private set; }

        public Plane(Vector3 a, Vector3 b, Vector3 c)
        {
            Normal = Vector3.Cross(b - a, c - a).normalized;
            Distance = Vector3.Dot(Normal, a);
            Pnt = a;
        }

        public Plane(Vector3 normal, Vector3 p)
        {
            Normal = normal.normalized;
            Distance = Vector3.Dot(Normal, p);
            Pnt = p;
        }

        public Plane(Plane instance)
        {
            Normal = instance.Normal;
            Distance = instance.Distance;
            Pnt = instance.Pnt;
        }

        public PointClass ClassifyPoint(Vector3 p)
        {
            float num = Vector3.Dot(p, Normal) - Distance;
            if (!(num < -0.0001f))
            {
                if (!(num > 0.0001f))
                {
                    return PointClass.Coplanar;
                }
                return PointClass.Front;
            }
            return PointClass.Back;
        }

        public bool GetSide(Vector3 n)
        {
            return Vector3.Dot(n, Normal) - Distance > 0.0001f;
        }

        public void Flip()
        {
            Normal = -Normal;
            Distance = 0f - Distance;
        }

        public bool GetSideFix(ref Vector3 n)
        {
            float num = n.x * Normal.x + n.y * Normal.y + n.z * Normal.z - Distance;
            float num2 = 1f;
            float num3 = num;
            if (num < 0f)
            {
                num2 = -1f;
                num3 = 0f - num;
            }
            if (num3 < 0.0011f)
            {
                n.x += Normal.x * 0.001f * num2;
                n.y += Normal.y * 0.001f * num2;
                n.z += Normal.z * 0.001f * num2;
                num = n.x * Normal.x + n.y * Normal.y + n.z * Normal.z - Distance;
            }
            return num > 0.0001f;
        }

        public bool SameSide(Vector3 a, Vector3 b)
        {
            throw new NotImplementedException();
        }

        public bool IntersectSegment(Vector3 a, Vector3 b, out float t, ref Vector3 q)
        {
            float num = b.x - a.x;
            float num2 = b.y - a.y;
            float num3 = b.z - a.z;
            float num4 = Normal.x * a.x + Normal.y * a.y + Normal.z * a.z;
            float num5 = Normal.x * num + Normal.y * num2 + Normal.z * num3;
            t = (Distance - num4) / num5;
            if (t >= -0.0001f && t <= 1.0001f)
            {
                q.x = a.x + t * num;
                q.y = a.y + t * num2;
                q.z = a.z + t * num3;
                return true;
            }
            q = Vector3.zero;
            return false;
        }

        public void InverseTransform(Transform transform)
        {
            Vector3 vector = transform.InverseTransformDirection(Normal);
            Vector3 rhs = transform.InverseTransformPoint(Pnt);
            Normal = vector;
            Distance = Vector3.Dot(vector, rhs);
        }

        public Matrix4x4 GetPlaneMatrix()
        {
            Matrix4x4 result = default(Matrix4x4);
            Quaternion q = Quaternion.LookRotation(Normal);
            result.SetTRS(Pnt, q, Vector3.one);
            return result;
        }
    }
}
