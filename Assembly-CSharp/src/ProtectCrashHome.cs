using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(LastTarget))]
    public class ProtectCrashHome : CreatureAction
    {
        [AssertNotNull]
        public Crash crash;

        public override float Evaluate(Creature creature)
        {
            if (creature.Aggression.Value > 0.3f)
            {
                return GetEvaluatePriority();
            }
            crash.RequestState(Crash.State.Resting);
            return 0f;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (Mathf.Approximately(creature.Aggression.Value, 1f))
            {
                crash.AttackLastTarget();
            }
            else if (creature.Aggression.Value > 0.8f)
            {
                crash.RequestState(Crash.State.Angry);
            }
            else if (creature.Aggression.Value > 0.3f)
            {
                crash.RequestState(Crash.State.Agitated);
            }
        }
    }
}
