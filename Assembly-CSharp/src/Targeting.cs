using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class Targeting
    {
        public delegate bool FilterRaycast(RaycastHit hit);

        private static readonly float[] radiuses = new float[2] { 0.15f, 0.3f };

        private static List<Transform> ignoreList = new List<Transform>();

        public static void AddToIgnoreList(GameObject ignoreGameObject)
        {
            if (ignoreGameObject != null)
            {
                AddToIgnoreList(ignoreGameObject.transform);
            }
        }

        public static void AddToIgnoreList(Transform ignoreTransform)
        {
            if (ignoreTransform != null && !ignoreList.Contains(ignoreTransform))
            {
                ignoreList.Add(ignoreTransform);
            }
        }

        public static bool GetRoot(GameObject candidate, out TechType techType, out GameObject gameObject)
        {
            techType = TechType.None;
            gameObject = null;
            if (candidate == null)
            {
                return false;
            }
            GameObject go;
            TechType techType2 = CraftData.GetTechType(candidate, out go);
            if (techType2 == TechType.None || go == null)
            {
                Pickupable componentInParent = candidate.GetComponentInParent<Pickupable>();
                if (componentInParent != null)
                {
                    techType2 = componentInParent.GetTechType();
                    go = componentInParent.gameObject;
                }
            }
            if (techType2 != 0 && go != null)
            {
                techType = techType2;
                gameObject = go;
                return true;
            }
            return false;
        }

        public static bool GetTarget(GameObject ignoreObj, float maxDistance, out GameObject result, out float distance, FilterRaycast filter = null)
        {
            if (ignoreObj != null)
            {
                AddToIgnoreList(ignoreObj.transform);
            }
            return GetTarget(maxDistance, out result, out distance);
        }

        public static bool GetTarget(float maxDistance, out GameObject result, out float distance, FilterRaycast filter = null)
        {
            ProfilingUtils.BeginSample("Targeting.GetTarget");
            bool flag = false;
            Transform transform = MainCamera.camera.transform;
            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            Ray ray = new Ray(position, forward);
            int layerMask = -2097153;
            QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;
            int numHits = global::UWE.Utils.RaycastIntoSharedBuffer(ray, maxDistance, layerMask, queryTriggerInteraction);
            if (Filter(global::UWE.Utils.sharedHitBuffer, numHits, filter, out var resultHit))
            {
                flag = true;
            }
            if (!flag)
            {
                for (int i = 0; i < radiuses.Length; i++)
                {
                    float num = radiuses[i];
                    ray.origin = position + forward * num;
                    numHits = global::UWE.Utils.SpherecastIntoSharedBuffer(ray, num, maxDistance, layerMask, queryTriggerInteraction);
                    if (Filter(global::UWE.Utils.sharedHitBuffer, numHits, filter, out resultHit))
                    {
                        flag = true;
                        break;
                    }
                }
            }
            Reset();
            result = ((resultHit.collider != null) ? resultHit.collider.gameObject : null);
            distance = resultHit.distance;
            ProfilingUtils.EndSample();
            return flag;
        }

        private static bool Filter(RaycastHit[] hits, int numHits, FilterRaycast filter, out RaycastHit resultHit)
        {
            resultHit = default(RaycastHit);
            for (int i = 0; i < numHits; i++)
            {
                RaycastHit raycastHit = hits[i];
                Collider collider = raycastHit.collider;
                if (!(collider == null))
                {
                    GameObject gameObject = collider.gameObject;
                    Transform transform = collider.transform;
                    if (!(gameObject == null) && !(transform == null) && !Skip(transform) && (!collider.isTrigger || gameObject.layer == LayerID.Useable) && (resultHit.collider == null || raycastHit.distance < resultHit.distance))
                    {
                        resultHit = raycastHit;
                    }
                }
            }
            if (resultHit.collider != null && (filter == null || filter(resultHit)))
            {
                return true;
            }
            resultHit = default(RaycastHit);
            return false;
        }

        private static bool Skip(Transform child)
        {
            for (int i = 0; i < ignoreList.Count; i++)
            {
                if (child.IsAncestorOf(ignoreList[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static void Reset()
        {
            ignoreList.Clear();
        }
    }
}
