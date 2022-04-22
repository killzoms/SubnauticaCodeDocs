using UnityEngine;

namespace AssemblyCSharp
{
    public class IntroSoundHandler : MonoBehaviour
    {
        public FMOD_StudioEventEmitter emitter;

        public float loadSoundFadeTime = 3f;

        private float timeLoaded;

        private int fmodIndexLoad = -1;

        private void OnLoaded()
        {
            timeLoaded = Time.time;
            InvokeRepeating("FadeLoadParam", 0f, 0f);
        }

        private void FadeLoadParam()
        {
            if (fmodIndexLoad < 0)
            {
                fmodIndexLoad = emitter.GetParameterIndex("load");
            }
            float value = Mathf.Clamp01((Time.time - timeLoaded) / loadSoundFadeTime);
            emitter.SetParameterValue(fmodIndexLoad, value);
        }
    }
}
