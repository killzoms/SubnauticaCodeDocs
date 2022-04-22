using UnityEngine;

namespace AssemblyCSharp
{
    public abstract class PassSoundParam : MonoBehaviour
    {
        public FMOD_StudioEventEmitter[] emitters;

        private int[] emitterParamIndex;

        public FMOD_CustomEmitter[] customEmitters;

        private int[] customEmitterParamIndex;

        public abstract float GetParamValue();

        public abstract string GetParamName();

        private void Start()
        {
            string paramName = GetParamName();
            if (customEmitters.Length != 0)
            {
                customEmitterParamIndex = new int[customEmitters.Length];
                for (int i = 0; i < customEmitters.Length; i++)
                {
                    if (customEmitters[i] != null)
                    {
                        customEmitterParamIndex[i] = customEmitters[i].GetParameterIndex(paramName);
                    }
                    else
                    {
                        customEmitterParamIndex[i] = -1;
                    }
                }
            }
            if (emitters.Length == 0)
            {
                return;
            }
            emitterParamIndex = new int[emitters.Length];
            for (int j = 0; j < emitters.Length; j++)
            {
                if (emitters[j] != null)
                {
                    emitterParamIndex[j] = emitters[j].GetParameterIndex(paramName);
                }
                else
                {
                    emitterParamIndex[j] = -1;
                }
            }
        }

        private void Update()
        {
            float paramValue = GetParamValue();
            for (int i = 0; i < emitters.Length; i++)
            {
                if (emitterParamIndex[i] != -1)
                {
                    emitters[i].SetParameterValue(emitterParamIndex[i], paramValue);
                }
            }
            for (int j = 0; j < customEmitters.Length; j++)
            {
                if (customEmitterParamIndex[j] != -1)
                {
                    customEmitters[j].SetParameterValue(customEmitterParamIndex[j], paramValue);
                }
            }
        }
    }
}
