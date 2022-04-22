using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class AvoidObstacles : CreatureAction
    {
        public bool avoidTerrainOnly = true;

        public float avoidanceIterations = 10f;

        public float avoidanceDistance = 5f;

        public float avoidanceDuration = 2f;

        public float scanInterval = 1f;

        public float scanDistance = 2f;

        public float scanRadius;

        public float swimVelocity = 3f;

        public float swimInterval = 1f;

        private Vector3 avoidancePosition;

        private float timeStartAvoidance;

        private float timeNextScan;

        private float timeNextSwim;

        public override float Evaluate(Creature creature)
        {
            if (Time.time < timeStartAvoidance + avoidanceDuration)
            {
                return GetEvaluatePriority();
            }
            if (Time.time > timeNextScan)
            {
                timeNextScan = Time.time + scanInterval;
                Transform transform = creature.transform;
                if (scanRadius > 0f)
                {
                    if (Physics.SphereCast(transform.position, scanRadius, transform.forward, out var _, scanDistance, GetLayerMask(), QueryTriggerInteraction.Ignore))
                    {
                        return GetEvaluatePriority();
                    }
                }
                else if (Physics.Raycast(transform.position, transform.forward, scanDistance, GetLayerMask(), QueryTriggerInteraction.Ignore))
                {
                    return GetEvaluatePriority();
                }
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            Vector3 vector = (avoidancePosition = creature.transform.position);
            timeStartAvoidance = Time.time;
            int layerMask = GetLayerMask();
            for (int i = 0; (float)i < avoidanceIterations; i++)
            {
                Vector3 onUnitSphere = Random.onUnitSphere;
                if (!Physics.Raycast(vector, onUnitSphere, avoidanceDistance, layerMask, QueryTriggerInteraction.Ignore))
                {
                    avoidancePosition = vector + onUnitSphere * avoidanceDistance;
                    return;
                }
            }
            timeStartAvoidance = 0f;
        }

        public override void StopPerform(Creature creature)
        {
            timeStartAvoidance = 0f;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                base.swimBehaviour.SwimTo(avoidancePosition, swimVelocity);
            }
        }

        private int GetLayerMask()
        {
            if (!avoidTerrainOnly)
            {
                return -5;
            }
            return Voxeland.GetTerrainLayerMask();
        }
    }
}
