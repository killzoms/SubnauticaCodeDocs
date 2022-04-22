using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class MainMenuPlayerName : MonoBehaviour
    {
        public Text text;

        private void Start()
        {
            if (!PlatformUtils.main.GetServices().GetSupportsDynamicLogOn())
            {
                base.gameObject.SetActive(value: false);
            }
            else
            {
                text.text = PlatformUtils.main.GetLoggedInUserName();
            }
        }

        private void Update()
        {
            if (!PlatformUtils.main.GetServices().GetSupportsDynamicLogOn())
            {
                base.gameObject.SetActive(value: false);
            }
            else
            {
                text.text = PlatformUtils.main.GetLoggedInUserName();
            }
        }
    }
}
