using UnityEngine;

namespace AssemblyCSharp
{
    public class SeaDragonAnimationEventsHandler : MonoBehaviour
    {
        [AssertNotNull]
        public SeaDragonMeleeAttack seaDragonMeleeAttack;

        private void OnSwatAttackHit()
        {
            seaDragonMeleeAttack.OnSwatAttackHit();
        }
    }
}
