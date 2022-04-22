using UnityEngine;

namespace AssemblyCSharp
{
    public class CrashedShipAmbientSound : MonoBehaviour
    {
        public static CrashedShipAmbientSound main;

        public bool isPlayerInside;

        public CapsuleCollider capsuleCollider;

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
            isPlayerInside = global::UWE.Utils.IsInsideCollider(capsuleCollider, Player.main.transform.position);
        }
    }
}
