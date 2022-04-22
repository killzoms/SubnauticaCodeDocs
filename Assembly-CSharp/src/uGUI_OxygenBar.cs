using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_OxygenBar : MonoBehaviour
    {
        [AssertNotNull]
        public uGUI_CircularBar bar;

        [AssertNotNull]
        public Text text;

        public float dampSpeed = 0.1f;

        [Space]
        public float overlay1Speed = 0.25f;

        public float overlay2Speed = 0.7f;

        [Space]
        [Range(0f, 1f)]
        public float overlay1Alpha = 0.75f;

        [Range(0f, 1f)]
        public float overlay2Alpha = 0.5f;

        [Space]
        public float pulseReferenceCapacity = 45f;

        public AnimationCurve pulseDelayCurve = new AnimationCurve();

        public AnimationCurve pulseTimeCurve = new AnimationCurve();

        [AssertNotNull]
        public Animation pulseAnimation;

        private float curr;

        private float vel;

        private int cachedValue = int.MinValue;

        private int cachedCapacity = int.MinValue;

        private CoroutineTween pulseTween;

        private float pulseDelay = -1f;

        private float pulseTime = -1f;

        private AnimationState pulseAnimationState;

        private void Awake()
        {
            pulseTween = new CoroutineTween(this)
            {
                mode = CoroutineTween.Mode.Loop,
                duration = 0f,
                onUpdate = OnPulse
            };
            pulseAnimation.wrapMode = WrapMode.Loop;
            pulseAnimation.Stop();
            pulseAnimationState = pulseAnimation.GetState(0);
            if (pulseAnimationState != null)
            {
                pulseAnimationState.blendMode = AnimationBlendMode.Blend;
                pulseAnimationState.weight = 1f;
                pulseAnimationState.layer = 0;
                pulseAnimationState.speed = 0f;
            }
        }

        private void OnEnable()
        {
            if (pulseAnimationState != null)
            {
                pulseAnimationState.enabled = true;
            }
            pulseTween.Start();
        }

        private void LateUpdate()
        {
            Player main = Player.main;
            if (main != null)
            {
                float oxygenAvailable = main.GetOxygenAvailable();
                float oxygenCapacity = main.GetOxygenCapacity();
                SetValue(oxygenAvailable, oxygenCapacity);
                float num = Mathf.Clamp01(oxygenAvailable / pulseReferenceCapacity);
                float time = 1f - num;
                pulseDelay = pulseDelayCurve.Evaluate(time);
                if (pulseDelay < 0f)
                {
                    pulseDelay = 0f;
                }
                pulseTime = pulseTimeCurve.Evaluate(time);
                if (pulseTime < 0f)
                {
                    pulseTime = 0f;
                }
                float num2 = pulseDelay + pulseTime;
                if (pulseTween.duration > 0f && num2 <= 0f)
                {
                    pulseAnimationState.normalizedTime = 0f;
                }
                pulseTween.duration = num2;
                Vector4 vector = bar.overlay1ST;
                vector.w = (0f - Time.time) * overlay1Speed;
                bar.overlay1ST = vector;
                vector = bar.overlay2ST;
                vector.w = (0f - Time.time) * overlay2Speed;
                bar.overlay2ST = vector;
                float num3 = Mathf.Clamp01(MathExtensions.EvaluateLine(0.5f, 1f, 1f, 0f, oxygenAvailable / oxygenCapacity));
                bar.overlay1Alpha = num3 * overlay1Alpha;
                bar.overlay2Alpha = num3 * overlay2Alpha;
            }
        }

        private void OnDisable()
        {
            pulseTween.Stop();
        }

        private void SetValue(float has, float capacity)
        {
            float target = has / capacity;
            int num = Mathf.RoundToInt(has);
            int num2 = Mathf.RoundToInt(capacity);
            curr = Mathf.SmoothDamp(curr, target, ref vel, dampSpeed);
            if (cachedValue != num || cachedCapacity != num2)
            {
                cachedValue = num;
                cachedCapacity = num2;
                text.text = IntStringCache.GetStringForInt(num);
            }
            bar.value = curr;
        }

        private void OnPulse(float scalar)
        {
            if (!(pulseAnimationState == null))
            {
                pulseAnimationState.normalizedTime = Mathf.Clamp01((pulseTween.duration * scalar - pulseDelay) / pulseTime);
            }
        }
    }
}
