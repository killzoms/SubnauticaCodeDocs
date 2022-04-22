using UnityEngine;

namespace AssemblyCSharp
{
    public class AquariumFish : MonoBehaviour
    {
        public GameObject model;

        private void OnKill()
        {
            Object.Destroy(this);
        }
    }
}
