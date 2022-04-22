using UnityEngine;

namespace AssemblyCSharp
{
    public class OnlyInEditor : MonoBehaviour
    {
        private void Start()
        {
            if (!Application.isEditor)
            {
                Object.Destroy(base.gameObject);
            }
        }
    }
}
