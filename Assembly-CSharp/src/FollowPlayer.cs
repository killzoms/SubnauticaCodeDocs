using UnityEngine;

namespace AssemblyCSharp
{
    public class FollowPlayer : MonoBehaviour
    {
        public GameObject player;

        public float verticalOffset;

        private void Update()
        {
            if (player != null)
            {
                Vector3 position = player.transform.position;
                position.y += verticalOffset;
                base.transform.position = position;
                Vector3 eulerAngles = base.transform.rotation.eulerAngles;
                float num = (eulerAngles.y = player.transform.rotation.eulerAngles.y);
                base.transform.rotation = Quaternion.Euler(eulerAngles);
            }
        }
    }
}
