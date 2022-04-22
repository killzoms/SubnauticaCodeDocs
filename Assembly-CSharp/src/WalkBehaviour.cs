using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(OnSurfaceMovement))]
    public class WalkBehaviour : SwimBehaviour
    {
        [AssertNotNull]
        public OnSurfaceTracker onSurfaceTracker;

        [AssertNotNull]
        public OnSurfaceMovement onSurfaceMovement;

        public bool allowSwimming;

        public void WalkTo(Vector3 targetPosition, float velocity)
        {
            if (onSurfaceTracker.onSurface)
            {
                onSurfaceMovement.GoTo(targetPosition, velocity);
            }
        }

        protected override void SwimToInternal(Vector3 targetPosition, Vector3 targetDirection, float velocity, bool overshoot)
        {
            if (onSurfaceTracker.onSurface)
            {
                WalkTo(targetPosition, velocity);
            }
            else if (allowSwimming)
            {
                base.SwimToInternal(targetPosition, targetDirection, velocity, overshoot);
            }
        }
    }
}
