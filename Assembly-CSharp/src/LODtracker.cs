using UnityEngine;

namespace AssemblyCSharp
{
    public class LODtracker : MonoBehaviour
    {
        public float level;

        public GameObject informObject;

        private void OnEnable()
        {
            informObject.SendMessage("OnLODChanged", level, SendMessageOptions.DontRequireReceiver);
        }
    }
}
