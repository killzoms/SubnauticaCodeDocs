using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class ErrorMessage : MonoBehaviour
    {
        private sealed class _Message
        {
            public Text entry;

            public string messageText;

            public int num;

            public float timeEnd;
        }

        private static ErrorMessage main;

        [AssertNotNull]
        public Text prefabMessage;

        [AssertNotNull]
        public RectTransform messageCanvas;

        public Vector2 offset = new Vector2(140f, 140f);

        public float ySpacing = 10f;

        private float timeFlyIn = 0.3f;

        private float timeDelay = 5f;

        private float timeFadeOut = 0.6f;

        private float timeInvisible = 0.1f;

        private List<_Message> messages = new List<_Message>();

        private float offsetY;

        private const int poolChunkSize = 4;

        private List<Text> pool = new List<Text>();

        private void Awake()
        {
            main = this;
        }

        private void OnEnable()
        {
            ManagedUpdate.Subscribe(ManagedUpdate.Queue.Canvas, OnUpdate);
        }

        private void OnDisable()
        {
            ManagedUpdate.Unsubscribe(OnUpdate);
        }

        private void OnUpdate()
        {
            float time = Time.time;
            for (int i = 0; i < messages.Count; i++)
            {
                _Message message = messages[i];
                if (time > message.timeEnd)
                {
                    offsetY += message.entry.preferredHeight;
                    messages.Remove(message);
                    ReleaseEntry(message.entry);
                }
            }
            float num = offsetY * 7f;
            offsetY -= num * Time.deltaTime;
            if (offsetY < 1f)
            {
                offsetY = 0f;
            }
            Rect rect = messageCanvas.rect;
            for (int j = 0; j < messages.Count; j++)
            {
                _Message message2 = messages[j];
                Text entry = message2.entry;
                RectTransform rectTransform = entry.rectTransform;
                Vector2 a = new Vector2(-0.5f * Mathf.Min(rectTransform.rect.width, entry.preferredWidth), 0.5f * entry.preferredHeight);
                Vector2 b = new Vector2(rect.x + offset.x, 0f - rect.y + GetYPos(j));
                float value = Mathf.Clamp01(MathExtensions.EvaluateLine(message2.timeEnd - timeInvisible - timeFadeOut - timeDelay - timeFlyIn, 0f, message2.timeEnd - timeInvisible - timeFadeOut - timeDelay, 1f, time));
                rectTransform.localPosition = Vector2.Lerp(a, b, MathExtensions.EaseOutSine(value));
                float value2 = Mathf.Clamp01(MathExtensions.EvaluateLine(message2.timeEnd - timeInvisible - timeFadeOut, 1f, message2.timeEnd - timeInvisible, 0f, time));
                entry.canvasRenderer.SetAlpha(MathExtensions.EaseOutSine(value2));
            }
        }

        public static void AddError(string message)
        {
            if (main != null)
            {
                main._AddMessage(message);
            }
        }

        public static void AddMessage(string message)
        {
            AddError(message);
        }

        public static void AddWarning(string message)
        {
            AddError(message);
        }

        public static void AddDebug(string message)
        {
            AddError(message);
        }

        private _Message GetExistingMessage(string messageText)
        {
            _Message result = null;
            for (int i = 0; i < messages.Count; i++)
            {
                _Message message = messages[i];
                if (message.messageText == messageText)
                {
                    result = message;
                    break;
                }
            }
            return result;
        }

        private void _AddMessage(string messageText)
        {
            if (!string.IsNullOrEmpty(messageText))
            {
                _Message existingMessage = GetExistingMessage(messageText);
                if (existingMessage == null)
                {
                    _ = messageCanvas.rect;
                    Text entry = GetEntry();
                    entry.gameObject.SetActive(value: true);
                    _ = entry.rectTransform;
                    entry.text = messageText;
                    existingMessage = new _Message();
                    existingMessage.entry = entry;
                    existingMessage.messageText = messageText;
                    existingMessage.num = 1;
                    existingMessage.timeEnd = Time.time + timeFlyIn + timeDelay + timeFadeOut + timeInvisible;
                    messages.Add(existingMessage);
                }
                else
                {
                    Text entry2 = existingMessage.entry;
                    existingMessage.timeEnd = Time.time + timeDelay + timeFadeOut + timeInvisible;
                    existingMessage.num++;
                    entry2.text = $"{messageText} (x{existingMessage.num.ToString()})";
                }
            }
        }

        private float GetYPos(int index = -1)
        {
            if (index == -1)
            {
                index = messages.Count;
            }
            float num = 0f;
            int i = 0;
            for (int num2 = Mathf.Min(messages.Count, index); i < num2; i++)
            {
                Text entry = messages[i].entry;
                num += entry.preferredHeight;
            }
            return 0f - (offset.y + offsetY + num + (float)index * ySpacing);
        }

        private Text GetEntry()
        {
            Text component;
            if (pool.Count == 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    GameObject obj = Object.Instantiate(prefabMessage.gameObject);
                    component = obj.GetComponent<Text>();
                    component.rectTransform.SetParent(messageCanvas, worldPositionStays: false);
                    obj.SetActive(value: false);
                    pool.Add(component);
                }
            }
            int index = pool.Count - 1;
            component = pool[index];
            pool.RemoveAt(index);
            return component;
        }

        private void ReleaseEntry(Text entry)
        {
            if (!(entry == null))
            {
                entry.gameObject.SetActive(value: false);
                pool.Add(entry);
            }
        }
    }
}
