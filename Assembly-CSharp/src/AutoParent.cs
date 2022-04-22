using UnityEngine;

namespace AssemblyCSharp
{
    public class AutoParent : MonoBehaviour
    {
        public Transform parentTransform;

        public bool makeLocalsIdentity = true;

        private void Start()
        {
            base.transform.parent = parentTransform;
            if (makeLocalsIdentity)
            {
                base.transform.MakeLocalsIdentity();
            }
        }
    }
}
