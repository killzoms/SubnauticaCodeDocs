using System;
using UnityEngine;

namespace AssemblyCSharp
{
    public class LightAnimator : MonoBehaviour
    {
        public enum Type
        {
            Flicker,
            Pulsate,
            Blink,
            Curve
        }

        [Serializable]
        public class FlickerParameters
        {
            public float minIntensity;

            public float maxIntensity = 10f;

            public float minTime;

            public float maxTime = 0.03f;
        }

        [Serializable]
        public class PulsateParameters
        {
            public float frequency;
        }

        [Serializable]
        public class CurveParameters
        {
            public float frequency;

            public AnimationCurve anim;
        }

        public Type type;

        private float waittime;

        public FlickerParameters flicker;

        public PulsateParameters pulsate;

        public CurveParameters curve;

        private float origIntensity;

        private Light lightComponent;

        private float startTime;

        private void Awake()
        {
            lightComponent = GetComponent<Light>();
            if (lightComponent != null)
            {
                origIntensity = lightComponent.intensity;
            }
            if (curve != null)
            {
                curve.anim.postWrapMode = WrapMode.Loop;
            }
        }

        private void Start()
        {
            startTime = Time.time;
        }

        private void Update()
        {
            if (!(lightComponent != null))
            {
                return;
            }
            switch (type)
            {
                case Type.Flicker:
                    if (waittime < 0f)
                    {
                        waittime = global::UnityEngine.Random.Range(flicker.minTime, flicker.maxTime);
                        lightComponent.intensity = Mathf.SmoothStep(flicker.minIntensity, flicker.maxIntensity, global::UnityEngine.Random.value * origIntensity);
                    }
                    waittime -= Time.deltaTime;
                    break;
                case Type.Pulsate:
                    lightComponent.intensity = global::UWE.Utils.Unlerp(Mathf.Sin((float)System.Math.PI * 2f * pulsate.frequency * Time.time), -1f, 1f) * origIntensity;
                    break;
                case Type.Curve:
                    lightComponent.intensity = curve.anim.Evaluate(curve.frequency * (Time.time - startTime)) * origIntensity;
                    break;
                case Type.Blink:
                    break;
            }
        }

        public void DefaultIntensity()
        {
            lightComponent.intensity = origIntensity;
        }
    }
}
