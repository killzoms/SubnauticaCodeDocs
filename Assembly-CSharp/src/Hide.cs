using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class Hide : CreatureAction
    {
        public float searchDistance = 50f;

        public float searchInterval = 1f;

        public float swimVelocity = 1f;

        private Vector3 hideout;

        public override float Evaluate(Creature creature)
        {
            return GetEvaluatePriority();
        }

        public override void StartPerform(Creature creature)
        {
            if (global::UWE.Utils.TraceForTerrain(new Ray(direction: (Vector3.down + Random.insideUnitSphere).normalized, origin: base.transform.position), searchDistance, out var hitInfo))
            {
                hideout = hitInfo.point;
            }
            else
            {
                hideout = creature.leashPosition;
            }
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            base.swimBehaviour.SwimTo(hideout, swimVelocity);
        }

        public override void StopPerform(Creature creature)
        {
            base.StopPerform(creature);
        }
    }
}
