using UnityEngine.Playables;

public class FMODEventMixerBehaviour : PlayableBehaviour
{
    private bool evaluate;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        int inputCount = playable.GetInputCount();
        double time = playable.GetGraph().GetRootPlayable(0).GetTime();
        for (int i = 0; i < inputCount; i++)
        {
            ((ScriptPlayable<FMODEventPlayableBehavior>)playable.GetInput(i)).GetBehaviour().Evaluate(time, info, evaluate);
        }
        evaluate = false;
    }

    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (!info.timeHeld && (info.seekOccurred || info.timeLooped || info.evaluationType == FrameData.EvaluationType.Evaluate))
        {
            evaluate = true;
        }
    }
}
