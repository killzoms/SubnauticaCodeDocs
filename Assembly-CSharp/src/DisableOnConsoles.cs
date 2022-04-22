using UnityEngine;

namespace AssemblyCSharp
{
    public class DisableOnConsoles : MonoBehaviour
    {
        private void Start()
        {
            if (PlatformUtils.isConsolePlatform)
            {
                base.gameObject.SetActive(value: false);
            }
        }
    }
}
