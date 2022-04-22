using System;
using System.Collections;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    [AddComponentMenu("")]
    public class VirtualPrefabIdentifier : UniqueIdentifier
    {
        [NonSerialized]
        public bool highPriority;

        private IEnumerator Start()
        {
            IPrefabRequest request = PrefabDatabase.GetPrefabAsync(base.ClassId);
            yield return request;
            if (!request.TryGetPrefab(out var prefab))
            {
                Debug.LogErrorFormat(this, "Failed to request prefab for '{0}'", base.ClassId);
                global::UnityEngine.Object.Destroy(base.gameObject);
                yield break;
            }
            DeferredSpawner.Task deferredTask = DeferredSpawner.instance.InstantiateAsync(prefab, base.transform.localPosition, base.transform.localRotation, instantiateDeactivated: true, highPriority);
            yield return deferredTask;
            GameObject result = deferredTask.result;
            DeferredSpawner.instance.ReturnTask(deferredTask);
            result.transform.SetParent(base.transform.parent, worldPositionStays: false);
            result.transform.localScale = base.transform.localScale;
            result.SetActive(value: true);
            global::UnityEngine.Object.Destroy(base.gameObject);
        }

        public void SpawnImmediate(bool forcePreview)
        {
            ProfilingUtils.BeginSample("VirtualPrefabIdentifier-SpawnImmediate");
            try
            {
                if (!PrefabDatabase.TryGetPrefab(base.ClassId, out var prefab))
                {
                    Debug.LogErrorFormat(this, "Failed to request prefab for '{0}'", base.ClassId);
                    return;
                }
                GameObject gameObject = Spawn(prefab);
                if (forcePreview)
                {
                    UniqueIdentifier[] componentsInChildren = gameObject.GetComponentsInChildren<UniqueIdentifier>(includeInactive: true);
                    for (int i = 0; i < componentsInChildren.Length; i++)
                    {
                        global::UnityEngine.Object.DestroyImmediate(componentsInChildren[i]);
                    }
                    gameObject.name = gameObject.name.Replace("(Clone)", "(Preview)");
                    gameObject.SetHideFlagRecursive(HideFlags.NotEditable);
                }
                global::UnityEngine.Object.DestroyImmediate(base.gameObject);
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        private GameObject Spawn(GameObject prefab)
        {
            ProfilingUtils.BeginSample("VirtualPrefabIdentifier-Spawn");
            GameObject result = global::UWE.Utils.Instantiate(prefab, base.transform.parent, base.transform.localPosition, base.transform.localRotation, base.transform.localScale);
            ProfilingUtils.EndSample();
            return result;
        }

        public override bool ShouldSerialize(Component comp)
        {
            return comp is Transform;
        }

        public override bool ShouldCreateEmptyObject()
        {
            return false;
        }

        public override bool ShouldMergeObject()
        {
            return false;
        }

        public override bool ShouldOverridePrefab()
        {
            return true;
        }

        public override bool ShouldStoreClassId()
        {
            return true;
        }
    }
}
