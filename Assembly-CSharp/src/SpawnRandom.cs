using UnityEngine;

namespace AssemblyCSharp
{
    public class SpawnRandom : MonoBehaviour
    {
        [AssertNotNull]
        public GameObject[] prefabs;

        private void Start()
        {
            Object.Instantiate(prefabs.GetRandom(), base.transform.localPosition, base.transform.localRotation).transform.SetParent(base.transform.parent, worldPositionStays: false);
            Object.Destroy(base.gameObject);
        }
    }
}
