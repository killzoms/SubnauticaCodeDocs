using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_Fader : MonoBehaviour
    {
        public float fadeInTime = 1f;

        public float fadeOutTime = 0.5f;

        private Sequence sequence = new Sequence();

        private Graphic[] graphic;

        private bool state;

        private float alpha = 1f;

        private void Awake()
        {
            graphic = GetComponentsInChildren<Graphic>();
            SetState(enabled: false);
        }

        private void Start()
        {
            ApplyState();
        }

        private void Update()
        {
            ApplyState();
        }

        private void ApplyState()
        {
            if (sequence.active)
            {
                sequence.Update();
                float t = sequence.t;
                state = t != 0f;
                alpha = 0.5f * (1f - Mathf.Cos((float)Math.PI * t));
                for (int i = 0; i < graphic.Length; i++)
                {
                    Graphic obj = graphic[i];
                    obj.enabled = state;
                    Color color = obj.color;
                    color.a = alpha;
                    obj.color = color;
                }
            }
        }

        public void SetState(bool enabled)
        {
            sequence.ForceState(enabled);
        }

        public void SetColor(Color color)
        {
            for (int i = 0; i < this.graphic.Length; i++)
            {
                Graphic graphic = this.graphic[i];
                color.a = graphic.color.a;
                graphic.color = color;
            }
        }

        public void FadeOut(float duration, SequenceCallback callback)
        {
            sequence.Set(duration, target: false, callback);
        }

        public void FadeIn(float duration, SequenceCallback callback)
        {
            sequence.Set(duration, target: true, callback);
        }

        public void FadeOut(SequenceCallback callback = null)
        {
            FadeOut(fadeInTime, callback);
        }

        public void FadeIn(SequenceCallback callback = null)
        {
            FadeIn(fadeOutTime, callback);
        }

        public void DelayedFadeIn(float delay, SequenceCallback callback = null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeWait(delay, callback));
        }

        private IEnumerator FadeWait(float delay, SequenceCallback callback)
        {
            yield return new WaitForSeconds(delay);
            alpha = 0f;
            FadeIn(callback);
        }
    }
}
