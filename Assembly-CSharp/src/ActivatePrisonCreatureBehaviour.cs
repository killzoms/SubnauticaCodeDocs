using UnityEngine;

namespace AssemblyCSharp
{
    public class ActivatePrisonCreatureBehaviour : MonoBehaviour
    {
        private void OnTriggerEnter(Collider col)
        {
            PrisonCreatureBehaviour componentInHierarchy = global::UWE.Utils.GetComponentInHierarchy<PrisonCreatureBehaviour>(col.gameObject);
            if (componentInHierarchy != null)
            {
                componentInHierarchy.Activate();
            }
        }
    }
}
