using System.Diagnostics;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class ExploderUtils
    {
        [Conditional("UNITY_EDITOR_DEBUG")]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                global::UnityEngine.Debug.Break();
            }
        }

        [Conditional("UNITY_EDITOR_DEBUG")]
        public static void Warning(bool condition, string message)
        {
            if (!condition)
            {
                global::UnityEngine.Debug.LogWarning("Warning! " + message);
            }
        }

        [Conditional("UNITY_EDITOR_DEBUG")]
        public static void Log(string message)
        {
            global::UnityEngine.Debug.Log(message);
        }

        public static Vector3 GetCentroid(GameObject obj)
        {
            MeshRenderer[] componentsInChildren = obj.GetComponentsInChildren<MeshRenderer>();
            Vector3 zero = Vector3.zero;
            if (componentsInChildren == null || componentsInChildren.Length == 0)
            {
                SkinnedMeshRenderer componentInChildren = obj.GetComponentInChildren<SkinnedMeshRenderer>();
                if ((bool)componentInChildren)
                {
                    return componentInChildren.bounds.center;
                }
                return obj.transform.position;
            }
            MeshRenderer[] array = componentsInChildren;
            foreach (MeshRenderer meshRenderer in array)
            {
                zero += meshRenderer.bounds.center;
            }
            return zero / componentsInChildren.Length;
        }

        public static void SetVisible(GameObject obj, bool status)
        {
            if ((bool)obj)
            {
                MeshRenderer[] componentsInChildren = obj.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].enabled = status;
                }
            }
        }

        public static void ClearLog()
        {
        }

        public static bool IsActive(GameObject obj)
        {
            if ((bool)obj)
            {
                return obj.activeSelf;
            }
            return false;
        }

        public static void SetActive(GameObject obj, bool status)
        {
            if ((bool)obj)
            {
                obj.SetActive(status);
            }
        }

        public static void SetActiveRecursively(GameObject obj, bool status)
        {
            if ((bool)obj)
            {
                int childCount = obj.transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    SetActiveRecursively(obj.transform.GetChild(i).gameObject, status);
                }
                obj.SetActive(status);
            }
        }

        public static void EnableCollider(GameObject obj, bool status)
        {
            if ((bool)obj)
            {
                Collider[] componentsInChildren = obj.GetComponentsInChildren<Collider>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].enabled = status;
                }
            }
        }
    }
}
