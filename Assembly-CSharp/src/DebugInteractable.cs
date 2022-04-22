using UnityEngine;

namespace AssemblyCSharp
{
    public class DebugInteractable : MonoBehaviour
    {
        public void OnHandClick(GUIHand hand)
        {
            ErrorMessage.AddDebug("OnHandClick");
        }
    }
}
