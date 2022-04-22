using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(SwimBehaviour))]
    public class MoveOnGround : CreatureAction
    {
        public float swimVelocity = 5f;

        public float swimRadius = 10f;

        public float swimForward = 0.5f;

        public float swimInterval = 5f;

        public float targetYOffset;

        private float timeNextSwim;

        [AssertNotNull]
        public OnGroundTracker onGroundTracker;

        [AssertNotNull]
        public ConstantForce descendForce;

        public float descendForceValue = 10f;

        public override float Evaluate(Creature creature)
        {
            if (onGroundTracker.onSurface)
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void Perform(Creature b, float deltaTime)
        {
            if (Time.time > timeNextSwim)
            {
                Vector3 insideUnitSphere = Random.insideUnitSphere;
                insideUnitSphere += base.transform.forward * swimForward;
                insideUnitSphere = Vector3.Scale(insideUnitSphere, new Vector3(swimRadius, 0f, swimRadius));
                Vector3 origin = base.transform.position + insideUnitSphere + Vector3.up;
                if (global::UWE.Utils.TraceForTerrain(new Ray(origin, Vector3.down), 30f, out var hitInfo))
                {
                    base.swimBehaviour.SwimTo(hitInfo.point + targetYOffset * base.transform.localScale.y * Vector3.up, swimVelocity);
                    timeNextSwim = Time.time + swimInterval;
                }
                else
                {
                    timeNextSwim = Time.time + 0.1f;
                }
            }
        }

        public override void StartPerform(Creature creature)
        {
            descendForce.enabled = true;
            descendForce.force = new Vector3(0f, 0f - descendForceValue, 0f);
        }

        public override void StopPerform(Creature creature)
        {
            descendForce.enabled = false;
        }
    }
}
