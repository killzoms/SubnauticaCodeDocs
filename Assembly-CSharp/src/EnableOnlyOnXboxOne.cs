using UnityEngine;

namespace AssemblyCSharp
{
    public class EnableOnlyOnXboxOne : MonoBehaviour
    {
        private void Start()
        {
            if (!PlatformUtils.isXboxOnePlatform)
            {
                base.gameObject.SetActive(value: false);
            }
        }
    }
}
