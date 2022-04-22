using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    [RequireComponent(typeof(LastScarePosition))]
    public class FleeWhenScared : CreatureAction
    {
        public FMOD_StudioEventEmitter scaredSound;

        public float swimVelocity = 10f;

        public float swimInterval = 1f;

        public int avoidanceIterations = 10;

        [AssertNotNull]
        public CreatureTrait exhausted;

        public float swimExhaustion = 0.25f;

        public float exhaustedVelocity = 1f;

        private float timeNextSwim;

        public override float Evaluate(Creature creature)
        {
            return Mathf.Lerp(GetEvaluatePriority() * creature.Scared.Value, 0f, exhausted.Value);
        }

        private void Flee()
        {
            if (!(Time.time > timeNextSwim))
            {
                return;
            }
            timeNextSwim = Time.time + swimInterval;
            float velocity = Mathf.Lerp(swimVelocity, exhaustedVelocity, exhausted.Value);
            exhausted.Add(swimExhaustion * swimInterval);
            Vector3 position = base.transform.position;
            Vector3 lastScarePosition = GetComponent<LastScarePosition>().lastScarePosition;
            Vector3 vector = Vector3.Normalize(position - lastScarePosition);
            Vector3 direction = vector;
            Vector3 targetPosition = position + base.transform.forward * swimVelocity;
            for (int i = 0; i < avoidanceIterations; i++)
            {
                if (!Physics.Raycast(position, direction, swimVelocity))
                {
                    targetPosition = position + vector * swimVelocity;
                    break;
                }
                direction = Vector3.Normalize(vector + Random.onUnitSphere);
            }
            base.swimBehaviour.SwimTo(targetPosition, velocity);
            if (scaredSound != null && !scaredSound.GetIsPlaying())
            {
                scaredSound.StartEvent();
            }
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            Flee();
            exhausted.UpdateTrait(deltaTime);
        }
    }
}
