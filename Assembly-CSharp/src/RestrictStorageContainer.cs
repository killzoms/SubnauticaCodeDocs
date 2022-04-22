using UnityEngine;

namespace AssemblyCSharp
{
    public class RestrictStorageContainer : MonoBehaviour
    {
        public TechType[] allowedTechTypes;

        public StorageContainer storageContainer;

        private void Start()
        {
            storageContainer.container.SetAllowedTechTypes(allowedTechTypes);
        }
    }
}
