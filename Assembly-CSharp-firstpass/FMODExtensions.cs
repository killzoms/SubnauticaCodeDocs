using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public static class FMODExtensions
{
    public static int GetLength(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            EventDescription eventDescription = RuntimeManager.GetEventDescription(path);
            if (eventDescription.isValid())
            {
                int length;
                RESULT length2 = eventDescription.getLength(out length);
                if (length2 == RESULT.OK)
                {
                    return length;
                }
                FMODUWE.CheckResult(length2);
            }
            else
            {
                UnityEngine.Debug.LogErrorFormat("EventDescription is invalid for '{0}'", path);
            }
        }
        return 0;
    }

    public static void SetParameterValue(this StudioEventEmitter emitter, int index, float value)
    {
        EventInstance eventInstance = emitter.EventInstance;
        if (eventInstance.isValid())
        {
            eventInstance.setParameterValueByIndex(index, value);
        }
    }

    public static int GetParameterIndex(this StudioEventEmitter emitter, string paramName)
    {
        EventInstance eventInstance = emitter.EventInstance;
        if (eventInstance.isValid())
        {
            eventInstance.getParameterCount(out var count);
            for (int i = 0; i < count; i++)
            {
                eventInstance.getParameterByIndex(i, out var instance);
                instance.getDescription(out var description);
                if ((string)description.name == paramName)
                {
                    return i;
                }
            }
        }
        return -1;
    }
}
