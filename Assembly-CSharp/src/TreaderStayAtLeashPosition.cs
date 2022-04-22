using UnityEngine;

namespace AssemblyCSharp
{
    public class TreaderStayAtLeashPosition : CreatureAction
    {
        public float velocity = 1f;

        public float updateTargetInterval = 1f;

        private SeaTreader treader;

        private float timeNextMove;

        private void Start()
        {
            treader = GetComponent<SeaTreader>();
        }

        public override float Evaluate(Creature creature)
        {
            if (Vector3.Distance(treader.leashPosition, base.transform.position) > treader.leashDistance)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            timeNextMove = Time.time;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (Time.time >= timeNextMove)
            {
                timeNextMove = Time.time + updateTargetInterval;
                treader.MoveTo(treader.leashPosition, velocity);
            }
        }
    }
}
