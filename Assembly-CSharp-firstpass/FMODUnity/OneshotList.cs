using System.Collections.Generic;
using FMOD;
using FMOD.Studio;

namespace FMODUnity
{
    public class OneshotList
    {
        private List<EventInstance> instances = new List<EventInstance>();

        public void Add(EventInstance instance)
        {
            instances.Add(instance);
        }

        public void Update(ATTRIBUTES_3D attributes)
        {
            PLAYBACK_STATE state;
            foreach (EventInstance item in instances.FindAll(delegate(EventInstance x)
            {
                x.getPlaybackState(out state);
                return state == PLAYBACK_STATE.STOPPED;
            }))
            {
                item.release();
            }
            instances.RemoveAll((EventInstance x) => !x.isValid());
            foreach (EventInstance instance in instances)
            {
                instance.set3DAttributes(attributes);
            }
        }

        public void SetParameterValue(string name, float value)
        {
            foreach (EventInstance instance in instances)
            {
                instance.setParameterValue(name, value);
            }
        }

        public void StopAll(FMOD.Studio.STOP_MODE stopMode)
        {
            foreach (EventInstance instance in instances)
            {
                instance.stop(stopMode);
                instance.release();
            }
            instances.Clear();
        }
    }
}
