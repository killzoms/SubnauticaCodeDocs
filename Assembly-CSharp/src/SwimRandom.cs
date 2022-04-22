using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    [RequireComponent(typeof(SwimBehaviour))]
    public class SwimRandom : CreatureAction
    {
        public Vector3 swimRadius = new Vector3(10f, 2f, 10f);

        public float swimForward = 0.5f;

        public float swimVelocity = 2f;

        public float swimInterval = 5f;

        public bool onSphere;

        private float timeNextSwim;

        public override void Perform(Creature creature, float deltaTime)
        {
            ProfilingUtils.BeginSample("CreatureAction::Perform (SwimRandom)");
            if (Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                Vector3 zero = Vector3.zero;
                zero = ((!onSphere) ? Random.insideUnitSphere : Random.onUnitSphere);
                zero += base.transform.forward * swimForward;
                zero = Vector3.Scale(zero, swimRadius);
                float velocity = Mathf.Lerp(swimVelocity, 0f, creature.Tired.Value);
                Vector3 vector = base.transform.position + zero;
                LastScarePosition component = base.gameObject.GetComponent<LastScarePosition>();
                if (component != null && Time.time < component.lastScareTime + 3f)
                {
                    Vector3 normalized = (component.lastScarePosition - base.transform.position).normalized;
                    Debug.DrawLine(base.transform.position, component.lastScarePosition, Color.red);
                    Debug.DrawLine(vector, vector - normalized, Color.green);
                    vector -= normalized;
                }
                base.swimBehaviour.SwimTo(vector, velocity);
            }
            ProfilingUtils.EndSample();
        }
    }
}
