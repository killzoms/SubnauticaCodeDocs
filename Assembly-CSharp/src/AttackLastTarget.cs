using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(LastTarget))]
    [RequireComponent(typeof(SwimBehaviour))]
    public class AttackLastTarget : CreatureAction
    {
        public float swimVelocity = 10f;

        public float swimInterval = 0.8f;

        public float aggressionThreshold = 0.75f;

        public float minAttackDuration = 3f;

        public float maxAttackDuration = 7f;

        public float pauseInterval = 20f;

        public float rememberTargetTime = 5f;

        public bool resetAggressionOnTime = true;

        [AssertNotNull]
        public LastTarget lastTarget;

        public FMOD_CustomEmitter attackStartSound;

        public VFXController attackStartFXcontrol;

        private float timeStartAttack;

        private float timeStopAttack;

        private float timeNextSwim;

        protected GameObject currentTarget;

        public override void StartPerform(Creature creature)
        {
            timeStartAttack = Time.time;
            if ((bool)attackStartSound)
            {
                attackStartSound.Play();
            }
            if (attackStartFXcontrol != null)
            {
                attackStartFXcontrol.Play();
            }
            SafeAnimator.SetBool(creature.GetAnimator(), "attacking", value: true);
        }

        public override void StopPerform(Creature creature)
        {
            SafeAnimator.SetBool(creature.GetAnimator(), "attacking", value: false);
            if (attackStartFXcontrol != null)
            {
                attackStartFXcontrol.Stop();
            }
            currentTarget = null;
            timeStopAttack = Time.time;
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            ProfilingUtils.BeginSample("CreatureAction::Perform (AttackEcoTarget)");
            if (Time.time > timeNextSwim && currentTarget != null)
            {
                timeNextSwim = Time.time + swimInterval;
                Vector3 position = currentTarget.transform.position;
                Vector3 targetDirection = ((!(currentTarget.GetComponent<Player>() != null)) ? (currentTarget.transform.position - base.transform.position).normalized : (-MainCamera.camera.transform.forward));
                base.swimBehaviour.Attack(position, targetDirection, swimVelocity);
            }
            if (resetAggressionOnTime && Time.time > timeStartAttack + maxAttackDuration)
            {
                StopAttack();
            }
            ProfilingUtils.EndSample();
        }

        public override float Evaluate(Creature creature)
        {
            if (((creature.Aggression.Value > aggressionThreshold) | (Time.time < timeStartAttack + minAttackDuration)) & (Time.time > timeStopAttack + pauseInterval))
            {
                if (lastTarget.target != null && Time.time <= lastTarget.targetTime + rememberTargetTime && !lastTarget.targetLocked)
                {
                    currentTarget = lastTarget.target;
                }
                if (!CanAttackTarget(currentTarget))
                {
                    currentTarget = null;
                }
                if (currentTarget != null)
                {
                    return GetEvaluatePriority();
                }
            }
            return 0f;
        }

        public void OnMeleeAttack(GameObject target)
        {
            if (target == currentTarget)
            {
                StopAttack();
            }
        }

        protected virtual bool CanAttackTarget(GameObject target)
        {
            if (target == null)
            {
                return false;
            }
            LiveMixin component = target.GetComponent<LiveMixin>();
            if (!component)
            {
                return false;
            }
            if (!component.IsAlive())
            {
                return false;
            }
            if (target == Player.main.gameObject && !Player.main.CanBeAttacked())
            {
                return false;
            }
            return true;
        }

        protected virtual void StopAttack()
        {
            creature.Aggression.Value = 0f;
            timeStopAttack = Time.time;
            lastTarget.target = null;
            if (attackStartFXcontrol != null)
            {
                attackStartFXcontrol.Stop();
            }
        }
    }
}
