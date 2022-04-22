using UnityEngine;

namespace AssemblyCSharp
{
    public class SpawnEscapePodSupplies : MonoBehaviour
    {
        public StorageContainer storageContainer;

        private void OnNewBorn()
        {
            ItemsContainer container = storageContainer.container;
            TechType[] escapePodStorageTechTypes = LootSpawner.main.GetEscapePodStorageTechTypes();
            for (int i = 0; i < escapePodStorageTechTypes.Length; i++)
            {
                Pickupable component = CraftData.InstantiateFromPrefab(escapePodStorageTechTypes[i]).GetComponent<Pickupable>();
                component = component.Initialize();
                if (container.HasRoomFor(component))
                {
                    InventoryItem item = new InventoryItem(component);
                    container.UnsafeAdd(item);
                }
                else
                {
                    Object.Destroy(component.gameObject);
                }
            }
        }
    }
}
