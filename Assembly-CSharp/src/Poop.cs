using UnityEngine;

namespace AssemblyCSharp
{
    public class Poop : CreatureAction
    {
        public SeaTreader treader;

        public string animationParameterName;

        public float actionInterval = 100f;

        public float animationDuration = 5.3f;

        public GameObject recourcePrefab;

        public Transform recourceSpawnPoint;

        public float spawnDelay;

        private bool isActive;

        private float nextActionTime;

        private float endActionTime;

        private bool recourceSpawned;

        private float recourceSpawnTime;

        public override void Awake()
        {
            base.Awake();
            nextActionTime = Time.time + Random.value * actionInterval;
        }

        public override float Evaluate(Creature creature)
        {
            if ((!isActive && Time.time >= nextActionTime && !treader.cinematicMode) || (isActive && Time.time < endActionTime))
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            isActive = true;
            endActionTime = Time.time + animationDuration;
            recourceSpawned = false;
            recourceSpawnTime = Time.time + spawnDelay;
            treader.cinematicMode = true;
            treader.Idle();
            SafeAnimator.SetBool(creature.GetAnimator(), animationParameterName, value: true);
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if (!recourceSpawned && Time.time >= recourceSpawnTime)
            {
                SafeAnimator.SetBool(creature.GetAnimator(), animationParameterName, value: false);
                recourceSpawned = true;
                Object.Instantiate(recourcePrefab, recourceSpawnPoint.position, recourceSpawnPoint.rotation);
            }
        }

        public override void StopPerform(Creature creature)
        {
            isActive = false;
            nextActionTime = Time.time + actionInterval * (1f + 0.2f * Random.value);
            treader.cinematicMode = false;
        }
    }
}
