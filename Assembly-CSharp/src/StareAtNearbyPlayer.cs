using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(PlayerDistanceTracker))]
    public class StareAtNearbyPlayer : CreatureAction
    {
        private PlayerDistanceTracker tracker;

        public Animator animator;

        public override void Awake()
        {
            base.Awake();
            tracker = GetComponent<PlayerDistanceTracker>();
            if (!animator)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        public override float Evaluate(Creature creature)
        {
            if (tracker.distanceToPlayer < 4f && Player.main.CanBeAttacked())
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature b)
        {
            b.Scared.Add(1f);
            if ((bool)animator)
            {
                SafeAnimator.SetBool(animator, "stare", value: true);
            }
        }

        public override void Perform(Creature b, float deltaTime)
        {
            base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(MainCamera.camera.transform.position - base.transform.position), deltaTime * 20f);
        }

        public override void StopPerform(Creature behaviour)
        {
            if ((bool)animator)
            {
                SafeAnimator.SetBool(animator, "stare", value: false);
            }
        }
    }
}
