using UnityEngine;

namespace AssemblyCSharp
{
    public class DeadAnimationOnEnable : MonoBehaviour
    {
        [AssertNotNull]
        public LiveMixin liveMixin;

        [AssertNotNull]
        public Animator animator;

        public bool disableAnimatorInstead;

        private void OnEnable()
        {
            bool flag = !liveMixin.IsAlive();
            if (!disableAnimatorInstead)
            {
                SafeAnimator.SetBool(animator, "dead", flag);
            }
            else
            {
                animator.enabled = !flag;
            }
        }
    }
}
