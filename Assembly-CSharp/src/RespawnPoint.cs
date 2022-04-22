using UnityEngine;

namespace AssemblyCSharp
{
    public class RespawnPoint : MonoBehaviour
    {
        public Vector3 GetSpawnPosition()
        {
            return base.transform.position;
        }
    }
}
