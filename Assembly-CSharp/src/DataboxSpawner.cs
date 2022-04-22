using UnityEngine;

namespace AssemblyCSharp
{
    public class DataboxSpawner : MonoBehaviour
    {
        [AssertNotNull]
        public GameObject databoxPrefab;

        private void Start()
        {
            BlueprintHandTarget component = databoxPrefab.GetComponent<BlueprintHandTarget>();
            if (component != null && !KnownTech.Contains(component.unlockTechType))
            {
                Object.Instantiate(databoxPrefab, base.transform.position, base.transform.rotation, base.transform.parent);
            }
            Object.Destroy(base.gameObject);
        }
    }
}
