using UnityEngine;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(Creature))]
    public class StayNearFriend : MonoBehaviour, IScheduledUpdateBehaviour, IManagedBehaviour
    {
        public float updateInterval = 1f;

        public float friendlinessThreshold = 0.5f;

        public float friendDistance = 4f;

        private Creature creature;

        private float timeNextUpdate;

        public int scheduledUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "StayNearFriend";
        }

        private void Start()
        {
            creature = GetComponent<Creature>();
        }

        public void ScheduledUpdate()
        {
            if (!(Time.time > timeNextUpdate) || !creature.friend)
            {
                return;
            }
            timeNextUpdate = Time.time + updateInterval;
            if (creature.Friendliness.Value > friendlinessThreshold)
            {
                Transform transform = creature.friend.transform;
                if (creature.friend == Player.main.gameObject)
                {
                    transform = MainCamera.camera.transform;
                }
                Vector3 vector = transform.position + transform.forward * friendDistance;
                creature.leashPosition = vector;
                Debug.DrawLine(base.transform.position, vector, Color.magenta, updateInterval);
            }
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
