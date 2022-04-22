using UnityEngine;

namespace AssemblyCSharp
{
    public sealed class MainMenuSwitchUser : MonoBehaviour
    {
        private void Start()
        {
            bool supportsDynamicLogOn = PlatformUtils.main.GetServices().GetSupportsDynamicLogOn();
            base.gameObject.SetActive(supportsDynamicLogOn);
        }
    }
}
