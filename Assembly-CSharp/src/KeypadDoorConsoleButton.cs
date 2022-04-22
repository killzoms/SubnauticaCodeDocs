using UnityEngine;

namespace AssemblyCSharp
{
    public class KeypadDoorConsoleButton : MonoBehaviour
    {
        public int index;

        public void OnNumberButtonPress()
        {
            SendMessageUpwards("NumberButtonPress", index, SendMessageOptions.RequireReceiver);
        }

        public void OnBackspaceButtonPress()
        {
            SendMessageUpwards("BackspaceButtonPress", null, SendMessageOptions.RequireReceiver);
        }
    }
}
