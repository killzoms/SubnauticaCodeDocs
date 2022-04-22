using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    [RequireComponent(typeof(LiveMixin))]
    [RequireComponent(typeof(SwimBehaviour))]
    [RequireComponent(typeof(LastTarget))]
    public class Crash : Creature, IPropulsionCannonAmmo
    {
        public enum State
        {
            DetonateOnImpact,
            FrozenInTime,
            Resting,
            Agitated,
            Angry,
            Attacking,
            Inflating
        }

        private float inflateRange = 5f;

        private float detonateRadius = 5f;

        private float maxDamage = 50f;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter idleLoop;

        [AssertNotNull]
        public FMOD_CustomLoopingEmitter angryLoop;

        [AssertNotNull]
        public FMOD_CustomEmitter attackSound;

        [AssertNotNull]
        public FMOD_CustomEmitter inflateSound;

        public GameObject detonateParticlePrefab;

        private bool calmingDown;

        private State state = State.Resting;

        private State requestedState = State.Resting;

        public float swimVelocity = 10f;

        public float maxAttackTime = 2f;

        public float calmDownDelay = 1f;

        private bool isWaterParkCreature;

        [AssertNotNull]
        public ProtectCrashHome protectCrashHome;

        [AssertNotNull]
        public FleeWhenScared fleeWhenScared;

        [AssertNotNull]
        public LastScarePosition lastScarePosition;

        [AssertNotNull]
        public SwimBehaviour swimBehaviour;

        [AssertNotNull]
        public Rigidbody useRigidbody;

        [AssertNotNull]
        public LastTarget lastTarget;

        void IPropulsionCannonAmmo.OnGrab()
        {
            OnState(State.FrozenInTime, forced: true);
            CancelInvoke("Inflate");
            CancelInvoke("AnimateInflate");
            CancelInvoke("Detonate");
            CancelInvoke("OnCalmDown");
        }

        void IPropulsionCannonAmmo.OnShoot()
        {
            OnState(State.DetonateOnImpact, forced: true);
        }

        void IPropulsionCannonAmmo.OnRelease()
        {
        }

        void IPropulsionCannonAmmo.OnImpact()
        {
            Detonate();
        }

        bool IPropulsionCannonAmmo.GetAllowedToGrab()
        {
            return true;
        }

        bool IPropulsionCannonAmmo.GetAllowedToShoot()
        {
            return true;
        }

        public override void Start()
        {
            isWaterParkCreature = GetComponent<WaterParkCreature>() != null;
            if (isWaterParkCreature)
            {
                fleeWhenScared.enabled = true;
                protectCrashHome.enabled = false;
                lastScarePosition.lastScarePosition = base.transform.position + Random.onUnitSphere;
                Scared.Add(1f);
                if (LargeWorld.main != null && LargeWorld.main.streamer != null && LargeWorld.main.streamer.cellManager != null)
                {
                    base.transform.parent = null;
                    LargeWorld.main.streamer.cellManager.UnregisterEntity(base.gameObject);
                }
                global::UWE.Utils.InvokeOnce(this, Inflate, 20f);
            }
            base.Start();
        }

        public void Update()
        {
            if (isWaterParkCreature)
            {
                Scared.Add(1f);
            }
            else if (IsAttacking() && (bool)lastTarget.target)
            {
                Vector3 position = lastTarget.target.transform.position;
                Vector3 vector = -lastTarget.target.transform.forward;
                position -= vector;
                swimBehaviour.Attack(position, vector, swimVelocity);
                if (CloseToPosition(position, inflateRange))
                {
                    Inflate();
                }
            }
        }

        public bool IsResting()
        {
            return state == State.Resting;
        }

        private bool IsAttacking()
        {
            return state >= State.Attacking;
        }

        public void AttackLastTarget()
        {
            if (!IsAttacking())
            {
                OnState(State.Attacking);
                attackSound.Play();
                SafeAnimator.SetBool(GetAnimator(), "attacking", value: true);
                useRigidbody.isKinematic = false;
                global::UWE.Utils.InvokeOnce(this, Inflate, maxAttackTime);
            }
        }

        public void RequestState(State newState)
        {
            if (newState > state)
            {
                StopCalmDown();
                OnState(newState);
            }
            else if (newState < state)
            {
                StartCalmDown(newState);
            }
        }

        private void OnState(State newState, bool forced = false)
        {
            if (newState != state && (state != State.Inflating || forced))
            {
                switch (newState)
                {
                    case State.Agitated:
                        angryLoop.Stop();
                        idleLoop.Play();
                        break;
                    case State.DetonateOnImpact:
                    case State.Angry:
                    case State.Attacking:
                        idleLoop.Stop();
                        angryLoop.Play();
                        break;
                    default:
                        idleLoop.Stop();
                        angryLoop.Stop();
                        break;
                }
                state = newState;
            }
        }

        private void StartCalmDown(State targetState)
        {
            requestedState = targetState;
            if (!calmingDown)
            {
                calmingDown = true;
                Invoke("OnCalmDown", calmDownDelay);
            }
        }

        private void StopCalmDown()
        {
            calmingDown = false;
            CancelInvoke("OnCalmDown");
        }

        private void OnCalmDown()
        {
            if (requestedState < state)
            {
                OnState(state - 1);
            }
            calmingDown = false;
            if (requestedState < state)
            {
                StartCalmDown(requestedState);
            }
        }

        private void Inflate()
        {
            if (state != State.Inflating)
            {
                OnState(State.Inflating);
                inflateSound.Play();
                global::UWE.Utils.InvokeOnce(this, AnimateInflate, 2f);
                global::UWE.Utils.InvokeOnce(this, Detonate, 2.5f);
            }
        }

        private void AnimateInflate()
        {
            SafeAnimator.SetBool(GetAnimator(), "explode", value: true);
        }

        private void Detonate()
        {
            if ((bool)detonateParticlePrefab)
            {
                Utils.PlayOneShotPS(detonateParticlePrefab, base.transform.position, base.transform.rotation);
            }
            DamageSystem.RadiusDamage(maxDamage, base.transform.position, detonateRadius, DamageType.Explosive, base.gameObject);
            base.gameObject.GetComponent<LiveMixin>().Kill();
        }

        private bool CloseToPosition(Vector3 position, float range)
        {
            return (base.transform.position - position).sqrMagnitude < range * range;
        }
    }
}
