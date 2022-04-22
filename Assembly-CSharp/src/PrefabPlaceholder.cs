using System;
using AssemblyCSharp.UWE;
using UnityEngine;

namespace AssemblyCSharp
{
    [AddComponentMenu("")]
    public class PrefabPlaceholder : MonoBehaviour, ICompileTimeCheckable, ICompileTimeSetupable
    {
        public string prefabClassId;

        public bool highPriority;

        public void Spawn(bool immediate, bool preview)
        {
            if (!base.gameObject.activeSelf || string.IsNullOrEmpty(prefabClassId))
            {
                return;
            }
            if (!WorldEntityDatabase.TryGetInfo(prefabClassId, out var info))
            {
                Debug.LogErrorFormat(this, "Missing world entity info for prefab '{0}'", prefabClassId);
                return;
            }
            ProfilingUtils.BeginSample("spawn virtual entities");
            GameObject virtualEntityPrefab = VirtualEntitiesManager.GetVirtualEntityPrefab();
            virtualEntityPrefab.SetActive(value: false);
            GameObject obj = global::UWE.Utils.InstantiateDeactivated(virtualEntityPrefab, base.transform.parent, base.transform.localPosition, base.transform.localRotation, base.transform.localScale);
            VirtualPrefabIdentifier component = obj.GetComponent<VirtualPrefabIdentifier>();
            component.ClassId = prefabClassId;
            component.highPriority = highPriority;
            LargeWorldEntity component2 = obj.GetComponent<LargeWorldEntity>();
            component2.cellLevel = info.cellLevel;
            ProfilingUtils.BeginSample("unregister");
            if (LargeWorldStreamer.main != null)
            {
                LargeWorldStreamer.main.cellManager.UnregisterEntity(component2);
            }
            ProfilingUtils.EndSample();
            obj.SetActive(value: true);
            if (immediate)
            {
                component.SpawnImmediate(preview);
            }
            ProfilingUtils.EndSample();
        }

        public string CompileTimeCheck()
        {
            Transform parent = base.transform.parent;
            if (!parent)
            {
                return $"PrefabPlaceholder {base.name} without parent. The entire hierarchy must be identifiable.";
            }
            if (!parent.GetComponent<UniqueIdentifier>())
            {
                return $"PrefabPlaceholder {base.name}'s parent {parent.name} has no unique identifier.";
            }
            PrefabPlaceholdersGroup[] componentsInParent = GetComponentsInParent<PrefabPlaceholdersGroup>(includeInactive: true);
            if (componentsInParent.Length != 1)
            {
                return string.Format("PrefabPlaceholder {0} has no PrefabPlaceholdersGroup component in parents.", base.name, parent.name);
            }
            if (componentsInParent[0].prefabPlaceholders == null || Array.IndexOf(componentsInParent[0].prefabPlaceholders, this) < 0)
            {
                return string.Format("PrefabPlaceholdersGroup component has no link to its child PrefabPlaceholder {0}.", base.name, parent.name);
            }
            return null;
        }

        public string CompileTimeSetup()
        {
            PrefabPlaceholdersGroup[] array = GetComponentsInParent<PrefabPlaceholdersGroup>(includeInactive: true);
            if (array.Length < 1)
            {
                PrefabIdentifier[] componentsInParent = GetComponentsInParent<PrefabIdentifier>(includeInactive: true);
                if (componentsInParent.Length != 1)
                {
                    return $"Missing prefab identifier on root of PrefabPlaceholder {base.name}";
                }
                array = new PrefabPlaceholdersGroup[1] { componentsInParent[0].gameObject.AddComponent<PrefabPlaceholdersGroup>() };
            }
            else if (array.Length > 1)
            {
                for (int i = 1; i < array.Length; i++)
                {
                    global::UnityEngine.Object.DestroyImmediate(array[i]);
                }
            }
            PrefabPlaceholdersGroup prefabPlaceholdersGroup = array[0];
            if (prefabPlaceholdersGroup.prefabPlaceholders == null || Array.IndexOf(prefabPlaceholdersGroup.prefabPlaceholders, this) < 0)
            {
                prefabPlaceholdersGroup.prefabPlaceholders = prefabPlaceholdersGroup.GetComponentsInChildren<PrefabPlaceholder>(includeInactive: true);
            }
            return null;
        }
    }
}
