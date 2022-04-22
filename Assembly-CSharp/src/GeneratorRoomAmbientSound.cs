using UnityEngine;

namespace AssemblyCSharp
{
    public class GeneratorRoomAmbientSound : MonoBehaviour
    {
        public static GeneratorRoomAmbientSound main;

        public bool isPlayerInside;

        public SphereCollider sphereCollider;

        private void Awake()
        {
            main = this;
        }

        private void Start()
        {
            InvokeRepeating("CheckPlayerPosition", 0f, 1f);
        }

        private void CheckPlayerPosition()
        {
            isPlayerInside = global::UWE.Utils.IsInsideCollider(sphereCollider, Player.main.transform.position);
        }
    }
}
