using UnityEngine;

namespace AssemblyCSharp
{
    public class Scareable : MonoBehaviour, IScheduledUpdateBehaviour, IManagedBehaviour
    {
        public EcoTargetType targetType = EcoTargetType.Shark;

        [AssertNotNull]
        public LastScarePosition lastScarePosition;

        [AssertNotNull]
        public Creature creature;

        public float scarePerSecond = 4f;

        public float maxRangeScalar = 10f;

        public float minMass = 50f;

        public float updateTargetInterval = 1f;

        public float updateRange = 100f;

        private float timeNextSearch;

        private EcoRegion.TargetFilter isTargetValidFilter;

        public int scheduledUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "Scareable";
        }

        private void Awake()
        {
            timeNextSearch = updateTargetInterval + (Random.value * 2f - 1f) * updateTargetInterval * 0.5f;
            if (isTargetValidFilter == null)
            {
                isTargetValidFilter = IsTargetValid;
            }
        }

        public void ScheduledUpdate()
        {
            if (!(Time.time > timeNextSearch) || !((Player.main.transform.position - base.transform.position).sqrMagnitude < updateRange * updateRange))
            {
                return;
            }
            timeNextSearch = Time.time + updateTargetInterval;
            float num = 0f;
            IEcoTarget ecoTarget = EcoRegionManager.main.FindNearestTarget(targetType, base.transform.position, isTargetValidFilter);
            if (ecoTarget == null)
            {
                return;
            }
            GameObject gameObject = ecoTarget.GetGameObject();
            if (!(gameObject == null))
            {
                Rigidbody component = gameObject.GetComponent<Rigidbody>();
                if (!(component == null))
                {
                    Vector3 vector = ((Player.main.gameObject == gameObject) ? Player.main.playerController.velocity : component.velocity);
                    float num2 = 1f - num / (maxRangeScalar * maxRangeScalar);
                    float num3 = scarePerSecond * num2 * Mathf.Clamp01(vector.magnitude) * updateTargetInterval;
                    Vector3 normalized = (base.transform.position - component.transform.position).normalized;
                    Vector3 normalized2 = vector.normalized;
                    float num4 = 0.6f + 0.4f * Vector3.Dot(normalized, normalized2);
                    creature.Scared.Add(num4 * num3);
                    lastScarePosition.lastScarePosition = component.transform.position;
                }
            }
        }

        private bool IsTargetValid(IEcoTarget ecoTarget)
        {
            GameObject gameObject = ecoTarget.GetGameObject();
            if (gameObject == Player.main.gameObject && !Player.main.CanBeAttacked())
            {
                return false;
            }
            if (gameObject == creature.friend)
            {
                return false;
            }
            Rigidbody component = gameObject.GetComponent<Rigidbody>();
            if (!component || component.mass < minMass)
            {
                return false;
            }
            return true;
        }

        private void OnEnable()
        {
            UpdateSchedulerUtils.Register(this);
        }

        private void OnDisable()
        {
            UpdateSchedulerUtils.Deregister(this);
        }

        private void OnDestroy()
        {
            UpdateSchedulerUtils.Deregister(this);
        }
    }
}
