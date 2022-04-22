using System.Collections.Generic;
using UnityEngine;

namespace AssemblyCSharp
{
    public class ResourceTracker : MonoBehaviour, ICompileTimeCheckable
    {
        public class ResourceInfo
        {
            public string uniqueId;

            public Vector3 position;

            public TechType techType;
        }

        public delegate void OnResourceDiscovered(ResourceInfo info);

        public delegate void OnResourceRemoved(ResourceInfo info);

        public TechType overrideTechType;

        [AssertNotNull]
        public PrefabIdentifier prefabIdentifier;

        [Tooltip("Optional")]
        public Rigidbody rb;

        [Tooltip("Optional")]
        public Pickupable pickupable;

        private static readonly Dictionary<string, ResourceInfo>.ValueCollection emptyCollection = new Dictionary<string, ResourceInfo>().Values;

        private static readonly Dictionary<TechType, Dictionary<string, ResourceInfo>> resources = new Dictionary<TechType, Dictionary<string, ResourceInfo>>(TechTypeExtensions.sTechTypeComparer);

        private static List<ResourceTracker> activeFragmentTrackers = new List<ResourceTracker>();

        private string uniqueId = "";

        private TechType techType;

        public static event OnResourceDiscovered onResourceDiscovered;

        public static event OnResourceRemoved onResourceRemoved;

        public static void UpdateFragments()
        {
            for (int num = activeFragmentTrackers.Count - 1; num >= 0; num--)
            {
                ResourceTracker resourceTracker = activeFragmentTrackers[num];
                if (!PDAScanner.CanScan(resourceTracker.gameObject))
                {
                    resourceTracker.Unregister();
                    activeFragmentTrackers.RemoveAt(num);
                }
            }
        }

        private void Register()
        {
            if (techType == TechType.None)
            {
                return;
            }
            Dictionary<string, ResourceInfo> orAddNew = resources.GetOrAddNew(techType);
            string key = uniqueId;
            if (!orAddNew.TryGetValue(key, out var value))
            {
                value = new ResourceInfo();
                value.uniqueId = key;
                value.position = base.transform.position;
                value.techType = techType;
                orAddNew.Add(key, value);
                if (ResourceTracker.onResourceDiscovered != null)
                {
                    ResourceTracker.onResourceDiscovered(value);
                }
            }
            else
            {
                value.position = base.transform.position;
            }
        }

        private void StartUpdatePosition()
        {
            if (!GetComponent<ResourceTrackerUpdater>())
            {
                base.gameObject.AddComponent<ResourceTrackerUpdater>();
            }
        }

        private void StopUpdatePosition()
        {
            ResourceTrackerUpdater component = GetComponent<ResourceTrackerUpdater>();
            if ((bool)component)
            {
                Object.Destroy(component);
            }
        }

        private void Unregister()
        {
            StopUpdatePosition();
            if (techType == TechType.None || !resources.TryGetValue(techType, out var value))
            {
                return;
            }
            string key = uniqueId;
            if (value.TryGetValue(key, out var value2))
            {
                value.Remove(key);
                if (ResourceTracker.onResourceRemoved != null)
                {
                    ResourceTracker.onResourceRemoved(value2);
                }
            }
        }

        private void Start()
        {
            uniqueId = prefabIdentifier.Id;
            techType = ((overrideTechType == TechType.None) ? CraftData.GetTechType(base.gameObject) : overrideTechType);
            if ((bool)pickupable)
            {
                pickupable.pickedUpEvent.AddHandler(base.gameObject, OnPickedUp);
                pickupable.droppedEvent.AddHandler(base.gameObject, OnDropped);
            }
            bool flag = techType == TechType.Fragment && !PDAScanner.CanScan(base.gameObject);
            if ((!pickupable || !pickupable.attached) && !flag)
            {
                Register();
                if ((bool)rb && !rb.isKinematic)
                {
                    StartUpdatePosition();
                }
            }
            if (flag)
            {
                Unregister();
            }
            if (techType == TechType.Fragment)
            {
                activeFragmentTrackers.Add(this);
            }
        }

        private void OnDestroy()
        {
            if (techType == TechType.Fragment)
            {
                activeFragmentTrackers.Remove(this);
            }
        }

        public void OnExamine()
        {
            Unregister();
        }

        public void OnPickedUp(Pickupable p)
        {
            Unregister();
        }

        public void OnShinyPickUp(GameObject obj)
        {
            StartUpdatePosition();
        }

        public void OnDropped(Pickupable p)
        {
            bool num = techType != TechType.Fragment || PDAScanner.CanScan(base.gameObject);
            if (num)
            {
                Register();
            }
            if (num && (bool)rb && !rb.isKinematic)
            {
                StartUpdatePosition();
            }
        }

        public void OnBreakResource()
        {
            Unregister();
        }

        public void UpdatePosition()
        {
            Register();
        }

        public void OnScanned()
        {
            Unregister();
        }

        public string CompileTimeCheck()
        {
            Rigidbody component = GetComponent<Rigidbody>();
            if (rb != component)
            {
                if (!component)
                {
                    return "rb field must be empty";
                }
                return "rb field must reference existing Rigidbody component";
            }
            Pickupable component2 = GetComponent<Pickupable>();
            if (pickupable != component2)
            {
                pickupable = component2;
                if (!component2)
                {
                    return "pickupable field must be empty";
                }
                return "pickupable field must reference existing Pickupable component";
            }
            return null;
        }

        private static bool HasNodeNearby(Vector3 fromPosition, float distance, Dictionary<string, ResourceInfo> nodes)
        {
            float num = distance * distance;
            Dictionary<string, ResourceInfo>.Enumerator enumerator = nodes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if ((fromPosition - enumerator.Current.Value.position).sqrMagnitude <= num)
                {
                    return true;
                }
            }
            return false;
        }

        public static Dictionary<string, ResourceInfo>.ValueCollection GetNodes(TechType techType)
        {
            if (resources.TryGetValue(techType, out var value))
            {
                return value.Values;
            }
            return emptyCollection;
        }

        public static void GetNodes(Vector3 fromPosition, float distance, TechType techType, ICollection<ResourceInfo> outNodes)
        {
            float num = distance * distance;
            if (!resources.TryGetValue(techType, out var value))
            {
                return;
            }
            Dictionary<string, ResourceInfo>.Enumerator enumerator = value.GetEnumerator();
            while (enumerator.MoveNext())
            {
                ResourceInfo value2 = enumerator.Current.Value;
                if ((fromPosition - value2.position).sqrMagnitude <= num)
                {
                    outNodes.Add(value2);
                }
            }
        }

        public static ICollection<TechType> GetTechTypes()
        {
            return resources.Keys;
        }

        public static void GetTechTypesInRange(Vector3 fromPosition, float distance, ICollection<TechType> outTechTypes)
        {
            Dictionary<TechType, Dictionary<string, ResourceInfo>>.Enumerator enumerator = resources.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<TechType, Dictionary<string, ResourceInfo>> current = enumerator.Current;
                if (HasNodeNearby(fromPosition, distance, current.Value))
                {
                    outTechTypes.Add(current.Key);
                }
            }
        }
    }
}
