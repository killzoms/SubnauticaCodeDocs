using System;
using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    [RequireComponent(typeof(StorageContainer))]
    public class SpawnStoredLoot : MonoBehaviour
    {
        private const int currentVersion = 1;

        [NonSerialized]
        [ProtoMember(1)]
        public int version = 1;

        [NonSerialized]
        [ProtoMember(2)]
        public bool lootSpawned;

        public int randomStartMinItems;

        public int randomStartMaxItems;

        public AnimationCurve distribution = new AnimationCurve();

        private void Start()
        {
            SpawnRandomStoredItems();
        }

        private void SpawnRandomStoredItems()
        {
            if (lootSpawned)
            {
                return;
            }
            ItemsContainer container = base.gameObject.GetComponent<StorageContainer>().container;
            float num = global::UWE.Utils.Sample(distribution);
            int count = Mathf.RoundToInt((float)randomStartMinItems + num * (float)(randomStartMaxItems - randomStartMinItems));
            TechType[] supplyTechTypes = LootSpawner.main.GetSupplyTechTypes(count);
            for (int i = 0; i < supplyTechTypes.Length; i++)
            {
                Pickupable component = CraftData.InstantiateFromPrefab(supplyTechTypes[i]).GetComponent<Pickupable>();
                component = component.Initialize();
                if (container.HasRoomFor(component))
                {
                    InventoryItem item = new InventoryItem(component);
                    container.UnsafeAdd(item);
                }
                else
                {
                    global::UnityEngine.Object.Destroy(component.gameObject);
                }
            }
            lootSpawned = true;
        }
    }
}
