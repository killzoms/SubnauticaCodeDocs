using UnityEngine;

namespace AssemblyCSharp
{
    public class TreaderMoveOnSurface : CreatureAction
    {
        public float updateTargetInterval = 5f;

        public float moveRadius = 7f;

        private SeaTreader treader;

        private float timeNextTarget;

        private void Start()
        {
            treader = GetComponent<SeaTreader>();
        }

        public override void StartPerform(Creature creature)
        {
            timeNextTarget = Time.time;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (Time.time >= timeNextTarget)
            {
                timeNextTarget = Time.time + updateTargetInterval * (1f + 0.2f * Random.value);
                Vector3 target = FindRandomPosition();
                treader.MoveTo(target);
            }
        }

        private Vector3 FindRandomPosition()
        {
            Vector2 vector = Random.insideUnitCircle * treader.leashDistance;
            return new Vector3(vector.x, 0f, vector.y) + treader.leashPosition;
        }
    }
}
