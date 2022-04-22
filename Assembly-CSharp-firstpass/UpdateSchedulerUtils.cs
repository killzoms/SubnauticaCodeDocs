using UnityEngine;

public class UpdateSchedulerUtils
{
    public static void Register(IScheduledUpdateBehaviour behaviour)
    {
        UpdateScheduler instance = UpdateScheduler.Instance;
        if (!instance)
        {
            Debug.LogError("UpdateScheduler does not exist in the scene.");
        }
        else
        {
            instance.updateSet.Add(behaviour);
        }
    }

    public static void Deregister(IScheduledUpdateBehaviour behaviour)
    {
        UpdateScheduler instance = UpdateScheduler.Instance;
        if (!instance)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("UpdateScheduler does not exist in the scene.");
            }
        }
        else
        {
            instance.updateSet.Remove(behaviour);
        }
    }
}
