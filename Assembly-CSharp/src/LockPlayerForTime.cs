using UnityEngine;

namespace AssemblyCSharp
{
    public class LockPlayerForTime : MonoBehaviour
    {
        public bool lockInEditor;

        public float secondsToLock = 5f;

        private Player player;

        private void Start()
        {
            player = GetComponent<Player>();
            if (!Application.isEditor || lockInEditor)
            {
                LockForSeconds(5f);
            }
        }

        private void LeaveLocked()
        {
            player.ExitLockedMode(respawn: false, findNewPosition: false);
        }

        private void LockForSeconds(float seconds)
        {
            player.EnterLockedMode(null);
            Invoke("LeaveLocked", seconds);
        }
    }
}
