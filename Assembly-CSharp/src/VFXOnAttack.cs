using UnityEngine;

namespace AssemblyCSharp
{
    public class VFXOnAttack : MonoBehaviour
    {
        [AssertNotNull]
        public VFXController fxControl;

        public void OnAttackStart()
        {
            fxControl.Play();
        }

        public void OnAttackEnd()
        {
            fxControl.Stop();
        }
    }
}
