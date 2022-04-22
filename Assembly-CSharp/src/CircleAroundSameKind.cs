using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class CircleAroundSameKind : CreatureAction
    {
        public float circleRadius = 2f;

        public float swimVelocity = 3f;

        public float swimInterval = 2f;

        public float maxDistance = 20f;

        public float friendTimeOut = 30f;

        private GameObject friend;

        private float timeFriendFound = -100f;

        private float timeLastUpdate;

        private float timeNextSwim;

        private void Update()
        {
            ProfilingUtils.BeginSample("CircleAroundSameKind.Update");
            if (friend == null && timeLastUpdate + 2f < Time.time && timeFriendFound + friendTimeOut * 2f <= Time.time)
            {
                ProfilingUtils.BeginSample("CircleAroundSameKind.PhysicsOverlapSphere");
                int num = global::UWE.Utils.OverlapSphereIntoSharedBuffer(base.transform.position, maxDistance);
                ProfilingUtils.EndSample();
                ProfilingUtils.BeginSample("CircleAroundSameKind.LookingForFriend");
                for (int i = 0; i < num; i++)
                {
                    GameObject gameObject = global::UWE.Utils.sharedColliderBuffer[i].gameObject;
                    if (gameObject != base.gameObject && gameObject.name == base.gameObject.name)
                    {
                        friend = gameObject;
                        timeFriendFound = Time.time;
                        break;
                    }
                }
                timeLastUpdate = Time.time;
                ProfilingUtils.EndSample();
            }
            ProfilingUtils.EndSample();
        }

        public override float Evaluate(Creature creature)
        {
            if (friend != null)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            creature.Happy.Add(1f);
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            ProfilingUtils.BeginSample("CreatureAction::Perform (CircleAroundSameKind)");
            if (timeFriendFound + friendTimeOut <= Time.time)
            {
                friend = null;
            }
            if (Time.time > timeNextSwim && friend != null)
            {
                Vector3 vector = Vector3.Normalize(friend.transform.position - base.transform.position);
                Vector3 vector2 = Vector3.Cross(Vector3.up, vector);
                base.swimBehaviour.SwimTo(friend.transform.position + vector2 * circleRadius, vector, swimVelocity);
            }
            ProfilingUtils.EndSample();
        }
    }
}
