using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(CaveCrawler))]
    public class CrawlerJumpRandom : CreatureAction
    {
        public CaveCrawler crawler;

        public float jumpRadius = 5f;

        private float timeNextJump;

        private bool isActive;

        private void Start()
        {
            if (crawler == null)
            {
                crawler = GetComponent<CaveCrawler>();
            }
            timeNextJump = Time.time + 5f + Random.value * 5f;
        }

        public override float Evaluate(Creature creature)
        {
            if (!isActive && timeNextJump <= Time.time && crawler.IsOnSurface())
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            isActive = true;
            Vector3 direction = Random.onUnitSphere * jumpRadius;
            direction.y = 6f + 3f * Random.value;
            Vector3 force = crawler.transform.TransformDirection(direction);
            force.y = Mathf.Max(-1f, force.y);
            GetComponent<Rigidbody>().AddForce(force, ForceMode.VelocityChange);
            crawler.OnJump();
        }

        public override void StopPerform(Creature creature)
        {
            isActive = false;
            timeNextJump = Time.time + 5f + Random.value * 5f;
        }
    }
}
