using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace UWE
{
    public class FreezeTime
    {
        public delegate void OnFreeze();

        public delegate void OnUnfreeze();

        private static float oldTimeScale = 1f;

        private static HashSet<string> freezers = new HashSet<string>();

        public static OnFreeze onFreeze;

        public static OnUnfreeze onUnfreeze;

        public static void Begin(string userId, bool dontPauseSound = false)
        {
            if (freezers.Count == 0)
            {
                oldTimeScale = Time.timeScale;
                if (!dontPauseSound)
                {
                    PauseSound(pause: true);
                }
                if (onFreeze != null)
                {
                    onFreeze();
                }
            }
            if (!freezers.Contains(userId))
            {
                freezers.Add(userId);
            }
            Time.timeScale = 0f;
        }

        public static void End(string userId)
        {
            if (!freezers.Contains(userId))
            {
                return;
            }
            freezers.Remove(userId);
            if (freezers.Count == 0)
            {
                Time.timeScale = oldTimeScale;
                PauseSound(pause: false);
                if (onUnfreeze != null)
                {
                    onUnfreeze();
                }
            }
        }

        private static void PauseSound(bool pause)
        {
            FMOD.Studio.System studioSystem = RuntimeManager.StudioSystem;
            if (studioSystem.hasHandle())
            {
                FMODUWE.CheckResult(studioSystem.getBus("bus:/", out var bus));
                if (bus.hasHandle())
                {
                    bus.setPaused(pause);
                }
            }
        }
    }
}
