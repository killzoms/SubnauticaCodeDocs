using UnityEngine;

namespace AssemblyCSharp
{
    public class MushroomAttack : CreatureAction, IOnTakeDamage
    {
        public float targetCloseDistance = 15f;

        public float targetMediumDistance = 25f;

        public float chanceExitMushroom = 0.15f;

        public float attackDuration = 6.5f;

        public float attackPause = 4f;

        public float maxInMusroomTime = 15f;

        public float heightForHighAttack = 1f;

        private CrabSnake crabsnakeBehaviour;

        private EcoTargetDistanceTracker targetTracker;

        private bool exitOnNextAttack;

        private float lastAttackTime;

        private float enterMushroomTime = -1f;

        public override void OnEnable()
        {
            base.OnEnable();
            crabsnakeBehaviour = GetComponent<CrabSnake>();
            targetTracker = GetComponent<EcoTargetDistanceTracker>();
        }

        public override float Evaluate(Creature creature)
        {
            if (crabsnakeBehaviour.IsInMushroom())
            {
                return GetEvaluatePriority();
            }
            return 0f;
        }

        public override void StartPerform(Creature creature)
        {
            exitOnNextAttack = Random.value < chanceExitMushroom;
            base.swimBehaviour.Idle();
            if (enterMushroomTime < 0f)
            {
                enterMushroomTime = Time.time - Random.value * maxInMusroomTime;
            }
            else
            {
                enterMushroomTime = Time.time;
            }
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            CrabSnake.State state = crabsnakeBehaviour.state;
            if (state == CrabSnake.State.InMushroom && Time.time > enterMushroomTime + maxInMusroomTime)
            {
                crabsnakeBehaviour.ExitMushroom();
                return;
            }
            if (state == CrabSnake.State.MushroomAttack && Time.time > lastAttackTime + attackDuration)
            {
                lastAttackTime = Time.time;
                exitOnNextAttack = Random.value < chanceExitMushroom;
                crabsnakeBehaviour.EndMushroomAttack();
            }
            if (state == CrabSnake.State.InMushroom && Time.time > lastAttackTime + attackPause && crabsnakeBehaviour.Scared.Value < 0.1f)
            {
                float distanceToTarget = targetTracker.distanceToTarget;
                if (exitOnNextAttack && distanceToTarget < targetMediumDistance)
                {
                    crabsnakeBehaviour.Aggression.Add(1f);
                    crabsnakeBehaviour.ExitMushroom(targetTracker.targetPosition);
                }
                else if (distanceToTarget < targetCloseDistance)
                {
                    lastAttackTime = Time.time;
                    crabsnakeBehaviour.Aggression.Add(1f);
                    crabsnakeBehaviour.StartMushroomAttack(targetTracker.targetPosition, IsHighAttack());
                }
            }
        }

        public void OnTakeDamage(DamageInfo damageInfo)
        {
            if (crabsnakeBehaviour.state == CrabSnake.State.InMushroom)
            {
                crabsnakeBehaviour.ExitMushroom();
            }
            else if (crabsnakeBehaviour.state == CrabSnake.State.MushroomAttack)
            {
                crabsnakeBehaviour.EndMushroomAttack();
                crabsnakeBehaviour.Scared.Value = 1f;
            }
        }

        private bool IsHighAttack()
        {
            float y = targetTracker.targetPosition.y;
            float y2 = base.transform.position.y;
            return y - y2 >= heightForHighAttack;
        }
    }
}
