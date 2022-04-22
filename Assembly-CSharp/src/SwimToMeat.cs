using ProtoBuf;
using UnityEngine;

namespace AssemblyCSharp
{
    [ProtoContract]
    [RequireComponent(typeof(SwimBehaviour))]
    public class SwimToMeat : CreatureAction
    {
        public Transform mouth;

        public GameObject meatTarget;

        public float swimVelocity = 4f;

        public float swimInterval = 1f;

        public float maxSearchRange = 20f;

        public float searchInterval = 1f;

        public float hungerThreshold = 0.2f;

        private float timeNextSwim;

        private float timeNextSearch;

        private EcoRegion.TargetFilter isTargetValidFilter;

        private void Start()
        {
            isTargetValidFilter = IsTargetValid;
        }

        public override float Evaluate(Creature creature)
        {
            if (creature.Hunger.Value < hungerThreshold)
            {
                return 0f;
            }
            if (Time.time > timeNextSearch)
            {
                timeNextSearch = Time.time + searchInterval;
                UpdateMeatTarget();
            }
            if (!meatTarget)
            {
                return 0f;
            }
            return GetEvaluatePriority();
        }

        private bool IsTargetValid(IEcoTarget target)
        {
            Vector3 direction = target.GetPosition() - mouth.position;
            float magnitude = direction.magnitude;
            if (magnitude > maxSearchRange)
            {
                return false;
            }
            if (magnitude > 0.5f && Physics.Raycast(base.transform.position, direction, magnitude - 0.5f, Voxeland.GetTerrainLayerMask()))
            {
                return false;
            }
            return true;
        }

        private void UpdateMeatTarget()
        {
            if (EcoRegionManager.main != null)
            {
                IEcoTarget ecoTarget = EcoRegionManager.main.FindNearestTarget(EcoTargetType.DeadMeat, base.transform.position, isTargetValidFilter);
                if (ecoTarget != null)
                {
                    meatTarget = ecoTarget.GetGameObject();
                    Debug.DrawLine(base.transform.position, ecoTarget.GetPosition(), Color.red, 10f);
                }
                else
                {
                    meatTarget = null;
                }
            }
        }

        public override void StartPerform(Creature creature)
        {
            SafeAnimator.SetBool(creature.GetAnimator(), "attacking", value: true);
        }

        public override void StopPerform(Creature creature)
        {
            SafeAnimator.SetBool(creature.GetAnimator(), "attacking", value: false);
        }

        public override void Perform(Creature creature, float deltaTime)
        {
            if ((bool)meatTarget && Time.time > timeNextSwim)
            {
                timeNextSwim = Time.time + swimInterval;
                base.swimBehaviour.SwimTo(meatTarget.transform.position, swimVelocity);
            }
        }
    }
}
