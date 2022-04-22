using UnityEngine;

namespace AssemblyCSharp
{
    public class RandomModel : MonoBehaviour
    {
        public GameObject[] modelVariants;

        private void Start()
        {
            Utils.SpawnZeroedAt(modelVariants.GetRandom(), base.transform);
        }
    }
}
