using AssemblyCSharp.Story;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PrisonPredatorSwimToPlayer : CreatureAction
    {
        public float swimVelocity = 3f;

        public float swimInterval = 1f;

        public float maxDistance = 30f;

        public float minDistance = 3f;

        public float pauseInterval = 10f;

        public float maxDuration = 7f;

        private float timeNextSwim;

        private float timeStartSwim;

        private float timeStopSwim;

        public override float Evaluate(Creature creature)
        {
            if (Time.time < timeStopSwim + pauseInterval)
            {
                return 0f;
            }
            if (Time.time < timeStartSwim + maxDuration)
            {
                return 0f;
            }
            Player main = Player.main;
            if (main == null || !main.CanBeAttacked())
            {
                return 0f;
            }
            float num = Vector3.Distance(main.transform.position, base.transform.position);
            if (num > maxDistance || num < minDistance)
            {
                return 0f;
            }
            if (!creature.GetCanSeeObject(main.gameObject))
            {
                return 0f;
            }
            return GetEvaluatePriority();
        }

        public override void StartPerform(Creature creature)
        {
            timeStartSwim = Time.time;
            new StoryGoal("Precursor_Prison_Aquarium_EnvironmentLog3", Story.GoalType.PDA, 0f).Trigger();
        }

        public override void StopPerform(Creature creature)
        {
            timeStopSwim = Time.time;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            Transform transform = Player.main.transform;
            _ = (transform.position - base.transform.position).magnitude;
            if (Time.time > timeNextSwim)
            {
                Vector3 targetDirection = -MainCamera.camera.transform.forward;
                base.swimBehaviour.SwimTo(transform.position, targetDirection, swimVelocity);
            }
        }
    }
}
