using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_HealthBar : MonoBehaviour
    {
        private const float punchDamp = 100f;

        private const float puchFrequency = 5f;

        [AssertNotNull]
        public uGUI_CircularBar bar;

        [AssertNotNull]
        public RectTransform icon;

        [AssertNotNull]
        public Text text;

        public float dampSpeed = 0.1f;

        [Space]
        public float pulseReferenceCapacity = 100f;

        public AnimationCurve pulseDelayCurve = new AnimationCurve();

        public AnimationCurve pulseTimeCurve = new AnimationCurve();

        [AssertNotNull]
        public Animation pulseAnimation;

        public float rotationSpringCoef = 100f;

        public float rotationVelocityDamp = 0.9f;

        public float rotationVelocityMax = -1f;

        public float rotationRandomVelocity = 1000f;

        private float curr;

        private float vel;

        private float punchSeed;

        private float punchMaxScale = 2f;

        private Vector3 punchInitialScale;

        private Vector3 punchScale = new Vector3(0f, 0f, 0f);

        private CoroutineTween punchTween;

        private bool subscribed;

        private CoroutineTween pulseTween;

        private float pulseDelay = -1f;

        private float pulseTime = -1f;

        private AnimationState pulseAnimationState;

        private int cachedValue = int.MinValue;

        private float rotationCurrent;

        private float rotationVelocity;

        private bool showNumbers;

        private float lastFixedUpdateTime;

        private void Awake()
        {
            punchTween = new CoroutineTween(this)
            {
                mode = CoroutineTween.Mode.Once,
                onStart = OnPunchStart,
                onUpdate = OnPunchUpdate,
                onStop = OnPunchStop
            };
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
            lastFixedUpdateTime = Time.time;
            if (pulseAnimationState != null)
            {
                pulseAnimationState.enabled = true;
            }
            pulseTween.Start();
        }

        private void LateUpdate()
        {
            bool num = showNumbers;
            showNumbers = false;
            Player main = Player.main;
            if (main != null)
            {
                LiveMixin component = main.GetComponent<LiveMixin>();
                if (component != null)
                {
                    if (!subscribed)
                    {
                        subscribed = true;
                        component.onHealDamage.AddHandler(base.gameObject, OnHealDamage);
                    }
                    float num2 = component.health - component.tempDamage;
                    float maxHealth = component.maxHealth;
                    SetValue(num2, maxHealth);
                    float num3 = Mathf.Clamp01(num2 / pulseReferenceCapacity);
                    float time = 1f - num3;
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
                    float num4 = pulseDelay + pulseTime;
                    if (pulseTween.duration > 0f && num4 <= 0f)
                    {
                        pulseAnimationState.normalizedTime = 0f;
                    }
                    pulseTween.duration = num4;
                }
                PDA pDA = main.GetPDA();
                if (pDA != null && pDA.isInUse)
                {
                    showNumbers = true;
                }
            }
            if (pulseAnimationState != null && pulseAnimationState.enabled)
            {
                icon.localScale += punchScale;
            }
            else
            {
                icon.localScale = punchScale;
            }
            if (num != showNumbers)
            {
                rotationVelocity += Random.Range(0f - rotationRandomVelocity, rotationRandomVelocity);
            }
            float time2 = Time.time;
            float num5 = 0.02f;
            float num6 = time2 - lastFixedUpdateTime;
            int num7 = Mathf.FloorToInt(num6);
            if (num7 > 20)
            {
                num7 = 1;
                num5 = num6;
            }
            lastFixedUpdateTime += (float)num7 * num5;
            for (int i = 0; i < num7; i++)
            {
                float num8 = rotationCurrent;
                float num9 = (showNumbers ? 180f : 0f);
                MathExtensions.Spring(ref rotationVelocity, ref rotationCurrent, num9, rotationSpringCoef, num5, rotationVelocityDamp, rotationVelocityMax);
                if (Mathf.Abs(num9 - rotationCurrent) < 1f && Mathf.Abs(rotationVelocity) < 1f)
                {
                    rotationVelocity = 0f;
                    rotationCurrent = num9;
                }
                if (num8 != rotationCurrent)
                {
                    icon.localRotation = Quaternion.Euler(0f, rotationCurrent, 0f);
                }
            }
        }

        private void OnDisable()
        {
            punchTween.Stop();
            pulseTween.Stop();
            if (!subscribed)
            {
                return;
            }
            subscribed = false;
            Player main = Player.main;
            if (main != null)
            {
                LiveMixin component = main.GetComponent<LiveMixin>();
                if (component != null)
                {
                    component.onHealDamage.RemoveHandler(base.gameObject, OnHealDamage);
                }
            }
        }

        private void SetValue(float has, float capacity)
        {
            float target = has / capacity;
            curr = Mathf.SmoothDamp(curr, target, ref vel, dampSpeed);
            bar.value = curr;
            int num = Mathf.CeilToInt(curr * capacity);
            if (cachedValue != num)
            {
                cachedValue = num;
                text.text = IntStringCache.GetStringForInt(cachedValue);
            }
        }

        private void OnHealDamage(float damage)
        {
            float maxScale = 1f + Mathf.Clamp01(damage / 100f);
            Punch(2.5f, maxScale);
        }

        private void Punch(float duration, float maxScale)
        {
            punchTween.duration = duration;
            punchMaxScale = maxScale;
            punchTween.Start();
        }

        private void OnPunchStart()
        {
            punchInitialScale = icon.localScale;
            punchSeed = Random.value;
        }

        private void OnPunchUpdate(float t)
        {
            float o = 0f;
            MathExtensions.Oscillation(100f, 5f, punchSeed, t, out var o2, out o);
            punchScale = new Vector3(o2 * punchMaxScale, o * punchMaxScale, 0f);
        }

        private void OnPunchStop()
        {
            punchScale = new Vector3(0f, 0f, 0f);
            if (!(icon == null))
            {
                icon.localScale = punchInitialScale;
            }
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
