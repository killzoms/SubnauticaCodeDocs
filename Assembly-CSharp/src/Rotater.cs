using UnityEngine;

namespace AssemblyCSharp
{
    public class Rotater : MonoBehaviour, IManagedUpdateBehaviour, IManagedBehaviour, IScheduledUpdateBehaviour
    {
        public Vector3 eulerAngleRate;

        [AssertNotNull]
        public BehaviourLOD levelOfDetail;

        private float nextUpdateTime;

        private float lastUpdateTime;

        private bool scheduledForLowLOD;

        public int managedUpdateIndex { get; set; }

        public int scheduledUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "Rotator";
        }

        public void ManagedUpdate()
        {
            if (!(Time.time < nextUpdateTime))
            {
                bool flag = scheduledForLowLOD;
                switch (levelOfDetail.current)
                {
                    case LODState.Full:
                        nextUpdateTime = Time.time;
                        scheduledForLowLOD = false;
                        break;
                    case LODState.Medium:
                        nextUpdateTime = Time.time + 0.1f;
                        scheduledForLowLOD = false;
                        break;
                    case LODState.Minimal:
                        nextUpdateTime = Time.time + 0.5f;
                        scheduledForLowLOD = true;
                        break;
                }
                float num = Time.time - lastUpdateTime;
                lastUpdateTime = Time.time;
                base.transform.Rotate(eulerAngleRate * num);
                if (flag && !scheduledForLowLOD)
                {
                    UpdateSchedulerUtils.Deregister(this);
                    BehaviourUpdateUtils.Register(this);
                }
                else if (!flag && scheduledForLowLOD)
                {
                    UpdateSchedulerUtils.Register(this);
                    BehaviourUpdateUtils.Deregister(this);
                }
            }
        }

        public void ScheduledUpdate()
        {
            ManagedUpdate();
        }

        private void OnEnable()
        {
            BehaviourUpdateUtils.Register(this);
        }

        private void OnDisable()
        {
            BehaviourUpdateUtils.Deregister(this);
            UpdateSchedulerUtils.Deregister(this);
        }

        private void OnDestroy()
        {
            BehaviourUpdateUtils.Deregister(this);
            UpdateSchedulerUtils.Deregister(this);
        }
    }
}
