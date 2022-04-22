using System;
using System.Collections.Generic;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public static class FMODUWE
{
    private static Dictionary<string, Guid> pathToGuidCache = new Dictionary<string, Guid>();

    public static void PlayOneShot(FMODAsset asset, Vector3 position, float volume = 1f)
    {
        PlayOneShotImpl(asset.path, position, volume);
    }

    [Obsolete("Use PlayOneShot(FMODAsset, ..) instead!")]
    public static void PlayOneShot(string eventPath, Vector3 position, float volume = 1f)
    {
        PlayOneShotImpl(eventPath, position, volume);
    }

    public static EventInstance GetEvent(FMODAsset asset)
    {
        return GetEventImpl(asset.path);
    }

    [Obsolete("Use GetEvent(FMODAsset) instead!")]
    public static EventInstance GetEvent(string eventPath)
    {
        return GetEventImpl(eventPath);
    }

    public static bool CheckResult(RESULT result)
    {
        if (result != 0)
        {
            UnityEngine.Debug.LogErrorFormat("FMOD Studio: Encountered Error: {0} {1}", result, Error.String(result));
            return false;
        }
        return true;
    }

    private static EventInstance GetEventImpl(string eventPath)
    {
        GetEventInstance(eventPath, out var instance);
        return instance;
    }

    private static void PlayOneShotImpl(string eventPath, Vector3 position, float volume)
    {
        ProfilingUtils.BeginSample("FMODUWE.PlayOneShot");
        EventInstance instance;
        RESULT eventInstance = GetEventInstance(eventPath, out instance);
        if (eventInstance != 0)
        {
            UnityEngine.Debug.LogErrorFormat("No FMOD event found for '{0}' ({1})", eventPath, Error.String(eventInstance));
        }
        else
        {
            instance.setVolume(volume);
            instance.set3DAttributes(position.To3DAttributes());
            instance.start();
            instance.release();
        }
        ProfilingUtils.EndSample();
    }

    private static RESULT GetEventInstance(string eventPath, out EventInstance instance)
    {
        instance = default(EventInstance);
        ProfilingUtils.BeginSample("GetEventDescription");
        EventDescription eventDesc;
        try
        {
            RESULT eventDescription = GetEventDescription(eventPath, out eventDesc);
            if (eventDescription != 0)
            {
                return eventDescription;
            }
        }
        finally
        {
            ProfilingUtils.EndSample();
        }
        ProfilingUtils.BeginSample("eventDesc.createInstance");
        try
        {
            RESULT rESULT = eventDesc.createInstance(out instance);
            if (rESULT != 0)
            {
                return rESULT;
            }
        }
        finally
        {
            ProfilingUtils.EndSample();
        }
        return RESULT.OK;
    }

    private static RESULT GetEventDescription(string eventPath, out EventDescription eventDesc)
    {
        eventDesc = default(EventDescription);
        ProfilingUtils.BeginSample("PathToGuid");
        Guid guid;
        try
        {
            RESULT rESULT = PathToGUID(eventPath, out guid);
            if (rESULT != 0)
            {
                return rESULT;
            }
        }
        finally
        {
            ProfilingUtils.EndSample();
        }
        ProfilingUtils.BeginSample("GetEventDescription");
        try
        {
            RESULT eventDescription = GetEventDescription(guid, out eventDesc);
            if (eventDescription != 0)
            {
                return eventDescription;
            }
        }
        finally
        {
            ProfilingUtils.EndSample();
        }
        return RESULT.OK;
    }

    private static RESULT PathToGUID(string path, out Guid guid)
    {
        if (pathToGuidCache.TryGetValue(path, out guid))
        {
            return RESULT.OK;
        }
        if (path.StartsWith("{"))
        {
            RESULT rESULT = Util.ParseID(path, out guid);
            if (rESULT != 0)
            {
                return rESULT;
            }
        }
        else
        {
            RESULT rESULT2 = RuntimeManager.StudioSystem.lookupID(path, out guid);
            if (rESULT2 != 0)
            {
                return rESULT2;
            }
        }
        pathToGuidCache[path] = guid;
        return RESULT.OK;
    }

    private static RESULT GetEventDescription(Guid guid, out EventDescription eventDesc)
    {
        IDictionary<Guid, EventDescription> cachedDescriptions = RuntimeManager.CachedDescriptions;
        if (cachedDescriptions.TryGetValue(guid, out eventDesc))
        {
            if (eventDesc.isValid())
            {
                return RESULT.OK;
            }
            cachedDescriptions.Remove(guid);
        }
        RESULT eventByID = RuntimeManager.StudioSystem.getEventByID(guid, out eventDesc);
        if (eventByID != 0)
        {
            return eventByID;
        }
        cachedDescriptions[guid] = eventDesc;
        return RESULT.OK;
    }

    public static int GetEventInstanceParameterIndex(EventInstance evt, string paramName)
    {
        evt.getParameterCount(out var count);
        for (int i = 0; i < count; i++)
        {
            if (evt.getParameterByIndex(i, out var instance) == RESULT.OK && instance.getDescription(out var description) == RESULT.OK && string.Equals(description.name, paramName, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return -1;
    }

    public static float GetMeteringVolume()
    {
        ProfilingUtils.BeginSample("GetMeteringVolume");
        try
        {
            ProfilingUtils.BeginSample("CheckResult / GetMasterChannelGroup");
            CheckResult(RuntimeManager.LowlevelSystem.getMasterChannelGroup(out var channelgroup));
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("CheckResult / GetDSP");
            CheckResult(channelgroup.getDSP(-1, out var dsp));
            ProfilingUtils.EndSample();
            ProfilingUtils.BeginSample("CheckResult / GetMeteringEnabled");
            CheckResult(dsp.getMeteringEnabled(out var inputEnabled, out var outputEnabled));
            ProfilingUtils.EndSample();
            if (!outputEnabled)
            {
                ProfilingUtils.BeginSample("CheckResult / SetMeteringEnabled");
                CheckResult(dsp.setMeteringEnabled(inputEnabled, outputEnabled: true));
                ProfilingUtils.EndSample();
            }
            ProfilingUtils.BeginSample("CheckResult / GetMeteringInfo");
            CheckResult(dsp.getMeteringInfo(IntPtr.Zero, out var outputInfo));
            ProfilingUtils.EndSample();
            float num = 0f;
            for (int i = 0; i < outputInfo.numchannels; i++)
            {
                num += outputInfo.rmslevel[i];
            }
            return num;
        }
        finally
        {
            ProfilingUtils.EndSample();
        }
    }
}
