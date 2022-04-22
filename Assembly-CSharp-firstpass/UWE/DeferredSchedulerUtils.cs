using UnityEngine;

namespace UWE
{
    public static class DeferredSchedulerUtils
    {
        public static void Schedule(IScheduledUpdateBehaviour behaviour)
        {
            DeferredScheduler instance = DeferredScheduler.Instance;
            if (!instance)
            {
                Debug.LogError("DeferredScheduler not instantiated", behaviour as Object);
            }
            else
            {
                instance.Enqueue(UpdateBehaviour, behaviour, null);
            }
        }

        private static void UpdateBehaviour(object owner, object state)
        {
            ((IScheduledUpdateBehaviour)owner).ScheduledUpdate();
        }
    }
}
