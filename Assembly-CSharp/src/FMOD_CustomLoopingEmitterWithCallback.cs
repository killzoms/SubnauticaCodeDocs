using System;
using System.Collections.Generic;
using AOT;
using FMOD;
using FMOD.Studio;
using UnityEngine;

namespace AssemblyCSharp
{
    public class FMOD_CustomLoopingEmitterWithCallback : FMOD_CustomLoopingEmitter
    {
        [AssertNotNull]
        public Animator animator;

        [AssertNotNull]
        public string animatorTrigger;

        private static readonly HashSet<IntPtr> events = new HashSet<IntPtr>();

        [MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
        private static RESULT EventCallback(EVENT_CALLBACK_TYPE type, EventInstance eventInstance, IntPtr parameters)
        {
            events.Add(eventInstance.handle);
            return RESULT.OK;
        }

        protected override void OnSetEvent(EventInstance eventInstance)
        {
            base.OnSetEvent(eventInstance);
            if (eventInstance.hasHandle())
            {
                eventInstance.setCallback(EventCallback, EVENT_CALLBACK_TYPE.SOUND_PLAYED);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            EventInstance eventInstance = GetEventInstance();
            if (eventInstance.hasHandle() && events.Remove(eventInstance.handle))
            {
                animator.SetTrigger(animatorTrigger);
            }
        }
    }
}
