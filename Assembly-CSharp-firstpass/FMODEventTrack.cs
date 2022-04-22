using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.066f, 0.134f, 0.244f)]
[TrackClipType(typeof(FMODEventPlayable))]
[TrackBindingType(typeof(GameObject))]
public class FMODEventTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        GameObject trackTargetObject = go.GetComponent<PlayableDirector>().GetGenericBinding(this) as GameObject;
        foreach (TimelineClip clip in GetClips())
        {
            FMODEventPlayable fMODEventPlayable = clip.asset as FMODEventPlayable;
            if ((bool)fMODEventPlayable)
            {
                fMODEventPlayable.TrackTargetObject = trackTargetObject;
                fMODEventPlayable.OwningClip = clip;
            }
        }
        return ScriptPlayable<FMODEventMixerBehaviour>.Create(graph, inputCount);
    }
}
