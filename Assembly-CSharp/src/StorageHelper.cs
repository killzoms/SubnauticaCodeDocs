using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AssemblyCSharp
{
    public static class StorageHelper
    {
        [Obsolete("Use ChildObjectIdentifier based serialization instead")]
        public static byte[] Save(ProtobufSerializer serializer, GameObject storageRoot)
        {
            using MemoryStream memoryStream = new MemoryStream();
            serializer.SerializeObjectTree(memoryStream, storageRoot);
            return memoryStream.ToArray();
        }

        [Obsolete("Use ChildObjectIdentifier based serialization plus TransferItems instead")]
        public static void RestoreItems(ProtobufSerializer serializer, byte[] serialData, ItemsContainer container)
        {
            if (serialData != null)
            {
                using MemoryStream stream = new MemoryStream(serialData);
                GameObject gameObject = serializer.DeserializeObjectTree(stream, 0);
                TransferItems(gameObject, container);
                global::UnityEngine.Object.Destroy(gameObject);
            }
        }

        [Obsolete("Use ChildObjectIdentifier based serialization plus TransferEquipment instead")]
        public static void RestoreEquipment(ProtobufSerializer serializer, byte[] serialItems, Dictionary<string, string> serialSlots, Equipment equipment)
        {
            if (serialItems != null)
            {
                using MemoryStream stream = new MemoryStream(serialItems);
                GameObject gameObject = serializer.DeserializeObjectTree(stream, 0);
                TransferEquipment(gameObject, serialSlots, equipment);
                global::UnityEngine.Object.Destroy(gameObject);
            }
        }

        [Obsolete("Use ChildObjectIdentifier based serialization plus TransferPendingItems instead")]
        public static void RestorePendingItems(ProtobufSerializer serializer, byte[] serialData, Transform newRoot, List<Pickupable> list)
        {
            if (serialData != null)
            {
                using MemoryStream stream = new MemoryStream(serialData);
                GameObject gameObject = serializer.DeserializeObjectTree(stream, 0);
                TransferPendingItems(gameObject, newRoot, list);
                global::UnityEngine.Object.Destroy(gameObject);
            }
        }

        public static void TransferItems(GameObject source, ItemsContainer destination)
        {
            UniqueIdentifier[] componentsInChildren = source.GetComponentsInChildren<UniqueIdentifier>(includeInactive: true);
            foreach (UniqueIdentifier uniqueIdentifier in componentsInChildren)
            {
                if (!(uniqueIdentifier.transform.parent != source.transform))
                {
                    Pickupable pickupable = uniqueIdentifier.gameObject.EnsureComponent<Pickupable>();
                    if (!destination.Contains(pickupable))
                    {
                        InventoryItem item = new InventoryItem(pickupable);
                        destination.UnsafeAdd(item);
                    }
                }
            }
        }

        public static void TransferEquipment(GameObject source, Dictionary<string, string> slots, Equipment equipment)
        {
            Dictionary<string, InventoryItem> dictionary = new Dictionary<string, InventoryItem>();
            UniqueIdentifier[] componentsInChildren = source.GetComponentsInChildren<UniqueIdentifier>(includeInactive: true);
            foreach (UniqueIdentifier uniqueIdentifier in componentsInChildren)
            {
                if (!(uniqueIdentifier.transform.parent != source.transform))
                {
                    InventoryItem value = new InventoryItem(uniqueIdentifier.gameObject.EnsureComponent<Pickupable>());
                    dictionary.Add(uniqueIdentifier.Id, value);
                }
            }
            equipment.RestoreEquipment(slots, dictionary);
        }

        public static void TransferPendingItems(GameObject source, Transform root, List<Pickupable> destination)
        {
            UniqueIdentifier[] componentsInChildren = source.GetComponentsInChildren<UniqueIdentifier>(includeInactive: true);
            foreach (UniqueIdentifier uniqueIdentifier in componentsInChildren)
            {
                Transform component = uniqueIdentifier.GetComponent<Transform>();
                if (!(component.parent != source.transform))
                {
                    Pickupable item = uniqueIdentifier.gameObject.EnsureComponent<Pickupable>();
                    component.parent = root;
                    destination.Add(item);
                }
            }
        }

        [Obsolete("Use ChildObjectIdentifier based serialization instead")]
        public static StoreInformationIdentifier RenewIdentifier(GameObject target)
        {
            if (target == null)
            {
                Debug.LogError("StorageHelper : RenewIdentifier(GameObject target) : target is null!");
                return null;
            }
            if (target.GetComponent<StoreInformationIdentifier>() != null)
            {
                global::UnityEngine.Object.DestroyImmediate(target.GetComponent<StoreInformationIdentifier>());
            }
            return target.AddComponent<StoreInformationIdentifier>();
        }

        public static Dictionary<string, InventoryItem> ScanItems(Transform root)
        {
            Dictionary<string, InventoryItem> dictionary = new Dictionary<string, InventoryItem>();
            int i = 0;
            for (int childCount = root.childCount; i < childCount; i++)
            {
                Transform child = root.GetChild(i);
                UniqueIdentifier component = child.GetComponent<UniqueIdentifier>();
                if (component != null)
                {
                    Pickupable component2 = child.GetComponent<Pickupable>();
                    if (component2 != null)
                    {
                        dictionary.Add(component.Id, new InventoryItem(component2));
                    }
                }
            }
            return dictionary;
        }

        public static IEnumerator DestroyDuplicatedItems(GameObject host)
        {
            Transform hostTransform = host.transform;
            StoreInformationIdentifier[] sids = host.GetComponentsInChildren<StoreInformationIdentifier>(includeInactive: true);
            for (int i = sids.Length - 1; i >= 0; i--)
            {
                StoreInformationIdentifier storeInformationIdentifier = sids[i];
                if ((bool)storeInformationIdentifier && storeInformationIdentifier.transform.parent == hostTransform)
                {
                    global::UnityEngine.Object.Destroy(storeInformationIdentifier.gameObject);
                    yield return null;
                }
            }
        }
    }
}
