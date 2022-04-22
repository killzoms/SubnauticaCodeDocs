using UnityEngine;

namespace AssemblyCSharp
{
    public class KeypadDoorConsoleUnlockButton : MonoBehaviour
    {
        public void OnButtonPress()
        {
            SendMessageUpwards("UnlockDoorButtonPress", null, SendMessageOptions.RequireReceiver);
        }
    }
}
