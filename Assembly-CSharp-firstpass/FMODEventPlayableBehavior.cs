using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class FMODEventPlayableBehavior : PlayableBehaviour
{
    public string eventName;

    public STOP_MODE stopType;

    public ParamRef[] parameters = new ParamRef[0];

    public GameObject TrackTargetObject;

    public TimelineClip OwningClip;

    private bool isPlayheadInside;

    private EventInstance eventInstance;

    private int seek = -1;

    protected void PlayEvent()
    {
        if (eventName == null)
        {
            return;
        }
        if (!eventInstance.isValid())
        {
            eventInstance = RuntimeManager.CreateInstance(eventName);
        }
        PerformSeek();
        if (Application.isPlaying && (bool)TrackTargetObject)
        {
            Rigidbody component = TrackTargetObject.GetComponent<Rigidbody>();
            if ((bool)component)
            {
                RuntimeManager.AttachInstanceToGameObject(eventInstance, TrackTargetObject.transform, component);
            }
            else
            {
                RuntimeManager.AttachInstanceToGameObject(eventInstance, TrackTargetObject.transform, TrackTargetObject.GetComponent<Rigidbody2D>());
            }
        }
        else
        {
            eventInstance.set3DAttributes(Vector3.zero.To3DAttributes());
        }
        ParamRef[] array = parameters;
        foreach (ParamRef paramRef in array)
        {
            eventInstance.setParameterValue(paramRef.Name, paramRef.Value);
        }
        eventInstance.start();
    }

    public void OnEnter()
    {
        if (!isPlayheadInside)
        {
            PlayEvent();
            isPlayheadInside = true;
        }
    }

    public void OnExit()
    {
        if (!isPlayheadInside)
        {
            return;
        }
        if (eventInstance.isValid())
        {
            if (stopType != STOP_MODE.None)
            {
                eventInstance.stop((stopType == STOP_MODE.Immediate) ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
            eventInstance.release();
        }
        isPlayheadInside = false;
    }

    public void UpdateBehaviour(float time)
    {
        if ((double)time >= OwningClip.start && (double)time < OwningClip.end)
        {
            OnEnter();
        }
        else
        {
            OnExit();
        }
    }

    public override void OnGraphStop(Playable playable)
    {
        isPlayheadInside = false;
        if (eventInstance.isValid())
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }
    }

    public void Evaluate(double time, FrameData info, bool evaluate)
    {
        if (!info.timeHeld && time >= OwningClip.start && time < OwningClip.end)
        {
            if (isPlayheadInside)
            {
                if ((evaluate || info.seekOccurred || info.timeLooped || info.evaluationType == FrameData.EvaluationType.Evaluate) && eventInstance.isValid())
                {
                    seek = GetPosition(time);
                    PerformSeek();
                }
            }
            else
            {
                if (time - OwningClip.start > 0.1)
                {
                    seek = GetPosition(time);
                }
                OnEnter();
            }
        }
        else
        {
            OnExit();
        }
    }

    public int GetPosition(double time)
    {
        double start = OwningClip.start;
        int length = FMODExtensions.GetLength(eventName);
        double num = (double)length / 1000.0;
        return Mathf.Clamp((int)((time - start) / num * (double)length), 0, length);
    }

    private void PerformSeek()
    {
        if (seek >= 0)
        {
            FMODUWE.CheckResult(eventInstance.setTimelinePosition(seek));
            seek = -1;
        }
    }
}
