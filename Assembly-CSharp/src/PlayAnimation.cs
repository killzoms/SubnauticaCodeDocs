using UnityEngine;

namespace AssemblyCSharp
{
    public class PlayAnimation : CreatureAction
    {
        public float actionInterval = 5f;

        public float maxDistanceToFriend = 6f;

        public string[] animationParams;

        private float lastActionTime;

        private string currentAnimation;

        private void Start()
        {
            lastActionTime = Time.time - Random.value * actionInterval;
        }

        private bool CanAnimate(Creature creature)
        {
            if (maxDistanceToFriend > 0f)
            {
                GameObject friend = creature.friend;
                if (friend == null)
                {
                    return false;
                }
                Vector3 rhs = ((Player.main.gameObject == friend) ? MainCameraControl.main.transform.forward : friend.transform.forward);
                Vector3 vector = base.transform.position - friend.transform.position;
                if (vector.magnitude > maxDistanceToFriend)
                {
                    return false;
                }
                vector = vector.normalized;
                if (Vector3.Dot(vector, rhs) < 0.65f)
                {
                    return false;
                }
            }
            return true;
        }

        public override float Evaluate(Creature creature)
        {
            if (Time.time > lastActionTime + actionInterval && CanAnimate(creature))
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            lastActionTime = Time.time;
            currentAnimation = animationParams.GetRandom();
            if (!string.IsNullOrEmpty(currentAnimation))
            {
                SafeAnimator.SetBool(creature.GetAnimator(), currentAnimation, value: true);
            }
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (Time.time > lastActionTime + 0.2f)
            {
                if (!string.IsNullOrEmpty(currentAnimation))
                {
                    SafeAnimator.SetBool(creature.GetAnimator(), currentAnimation, value: false);
                }
                currentAnimation = "";
            }
        }

        public override void StopPerform(Creature creature)
        {
            if (!string.IsNullOrEmpty(currentAnimation))
            {
                SafeAnimator.SetBool(creature.GetAnimator(), currentAnimation, value: false);
            }
        }
    }
}
