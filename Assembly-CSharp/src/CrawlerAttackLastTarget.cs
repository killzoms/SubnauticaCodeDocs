using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(LastTarget))]
    public class CrawlerAttackLastTarget : CreatureAction
    {
        public float moveVelocity = 10f;

        public float aggressionThreshold = 0.8f;

        public bool jumpToTarget = true;

        [AssertNotNull]
        public CaveCrawler crawler;

        [AssertNotNull]
        public LastTarget lastTarget;

        private float timeNextJump;

        public override float Evaluate(Creature creature)
        {
            if (!(lastTarget.target != null) || !(Time.time <= lastTarget.targetTime + 5f) || !(creature.Aggression.Value >= aggressionThreshold))
            {
                return 0f;
            }
            return GetEvaluatePriority();
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (crawler.IsOnSurface() && lastTarget.target != null)
            {
                Vector3 value = lastTarget.target.transform.position - base.transform.position;
                Rigidbody component = GetComponent<Rigidbody>();
                if (jumpToTarget && timeNextJump <= Time.time && value.magnitude < 7f)
                {
                    Vector3 force = Vector3.Normalize(value) * 6f;
                    force.y += 3f;
                    component.AddForce(force, ForceMode.VelocityChange);
                    crawler.OnJump();
                    timeNextJump = Time.time + 5f + 2f * Random.value;
                }
                else
                {
                    base.swimBehaviour.SwimTo(lastTarget.target.transform.position, moveVelocity);
                }
            }
        }
    }
}
