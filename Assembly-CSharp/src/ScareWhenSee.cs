using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(LastScarePosition))]
    public class ScareWhenSee : MonoBehaviour
    {
        public EcoTargetType targetType;

        public float scarePerSecond = 1f;

        public float maxRangeScalar = 10f;

        private const float kUpdateRate = 0.33f;

        private Creature creature;

        private EcoRegion.TargetFilter isTargetValidFilter;

        private void Start()
        {
            creature = GetComponent<Creature>();
            isTargetValidFilter = IsTargetValid;
            InvokeRepeating("ScanForScareTarget", 0f, 0.33f);
        }

        private void ScanForScareTarget()
        {
            if (!base.gameObject.activeInHierarchy)
            {
                return;
            }
            ProfilingUtils.BeginSample("ScareWhenSee.ScanForScareTarget");
            if (EcoRegionManager.main != null)
            {
                IEcoTarget ecoTarget = EcoRegionManager.main.FindNearestTarget(targetType, base.transform.position, isTargetValidFilter);
                if (ecoTarget != null)
                {
                    float num = Vector3.Distance(ecoTarget.GetPosition(), base.transform.position);
                    float num2 = (maxRangeScalar - num) / maxRangeScalar;
                    Debug.DrawLine(ecoTarget.GetPosition(), base.transform.position, Color.white);
                    creature.Scared.Add(scarePerSecond * num2 * 0.33f);
                    base.gameObject.GetComponent<LastScarePosition>().lastScarePosition = ecoTarget.GetPosition();
                }
            }
            ProfilingUtils.EndSample();
        }

        private bool IsTargetValid(IEcoTarget target)
        {
            if ((target.GetPosition() - base.transform.position).sqrMagnitude > maxRangeScalar * maxRangeScalar)
            {
                return false;
            }
            return creature.GetCanSeeObject(target.GetGameObject());
        }
    }
}
