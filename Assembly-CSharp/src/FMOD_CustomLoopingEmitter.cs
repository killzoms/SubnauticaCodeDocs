using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace AssemblyCSharp
{
    public class FMOD_CustomLoopingEmitter : FMOD_CustomEmitter
    {
        public FMODAsset assetStart;

        public FMODAsset assetStop;

        public float stopSoundInterval;

        private float timeLastStopSound;

        protected override void OnPlay()
        {
            if (assetStart != null)
            {
                EventInstance @event = FMODUWE.GetEvent(assetStart);
                @event.set3DAttributes(base.transform.To3DAttributes());
                @event.start();
                @event.release();
                timeLastStopSound = Time.time;
            }
            base.OnPlay();
        }

        protected override void OnStop()
        {
            if (stopSoundInterval == -1f || timeLastStopSound + stopSoundInterval < Time.time)
            {
                PlayStopSound();
            }
            base.OnStop();
        }

        private void PlayStopSound()
        {
            if (assetStop != null)
            {
                EventInstance @event = FMODUWE.GetEvent(assetStop);
                @event.set3DAttributes(base.transform.To3DAttributes());
                @event.start();
                @event.release();
                timeLastStopSound = Time.time;
            }
        }
    }
}
