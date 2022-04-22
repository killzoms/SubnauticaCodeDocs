using UnityEngine;

namespace AssemblyCSharp
{
    public class HandHoverText : MonoBehaviour, IHandTarget
    {
        [AssertNotNull]
        public string text;

        public virtual void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetInteractInfo(text);
        }

        public virtual void OnHandClick(GUIHand hand)
        {
        }
    }
}
