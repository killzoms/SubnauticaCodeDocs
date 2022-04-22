using FMOD;
using FMOD.Studio;
using UnityEngine;

namespace AssemblyCSharp
{
    public class FMODTriggerSound : MonoBehaviour
    {
        private EventInstance testEvent;

        public string testEventName = "";

        public bool debug;

        private void OnTriggerEnter(Collider collider)
        {
            global::UnityEngine.Debug.Log("FMODTriggerSound." + base.name + ".OnTriggerEnter(" + collider.gameObject.name);
            if (collider.gameObject != Player.main.gameObject)
            {
                return;
            }
            if (!testEvent.hasHandle())
            {
                testEvent = FMODUWE.GetEvent(testEventName);
            }
            if (testEvent.hasHandle() && testEvent.getPlaybackState(out var state) == RESULT.OK && state != 0)
            {
                if (debug)
                {
                    global::UnityEngine.Debug.Log("Playing " + testEventName + " at time " + Time.time);
                }
                testEvent.start();
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            global::UnityEngine.Debug.Log("FMODTriggerSound." + base.name + ".OnTriggerExit(" + collider.gameObject.name);
            if (collider.gameObject != Player.main.gameObject)
            {
                return;
            }
            if (!testEvent.hasHandle())
            {
                testEvent = FMODUWE.GetEvent(testEventName);
            }
            if (testEvent.hasHandle())
            {
                if (debug)
                {
                    global::UnityEngine.Debug.Log("Stopping " + testEventName + " at time " + Time.time);
                }
                testEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            }
        }
    }
}
