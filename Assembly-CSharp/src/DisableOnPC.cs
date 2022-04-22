using UnityEngine;

namespace AssemblyCSharp
{
    public class DisableOnPC : MonoBehaviour
    {
        private void Start()
        {
            if (!PlatformUtils.isConsolePlatform)
            {
                base.gameObject.SetActive(value: false);
            }
        }
    }
}
