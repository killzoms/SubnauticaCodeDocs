using System.Collections.Generic;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    public class Subtitles : MonoBehaviour
    {
        private struct Entry
        {
            public float delay;

            public float duration;

            public string text;
        }

        public float speed = 15f;

        private const int maxMessageLength = 250;

        private const char wordSeparator = ' ';

        private static readonly char[] sentenceSeparators = new char[3] { '.', '?', '!' };

        public uGUI_PopupMessage popup;

        private Queue<Entry> queue = new Queue<Entry>();

        public static Subtitles main { get; private set; }

        public bool isShowingMessage => popup.isShowingMessage;

        private void Awake()
        {
            if (main != null)
            {
                Object.Destroy(base.gameObject);
                return;
            }
            main = this;
            DevConsole.RegisterConsoleCommand(this, "subtitles", caseSensitiveArgs: true);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        private void OnConsoleCommand_subtitles(NotificationCenter.Notification n)
        {
            if (n.data != null && n.data.Count == 1)
            {
                string key = (string)n.data[0];
                Add(key);
            }
            else
            {
                ErrorMessage.AddDebug("usage: subtitles key");
            }
        }

        private void Start()
        {
            popup.ox = 0f;
            popup.oy = 210f;
            popup.anchor = TextAnchor.LowerCenter;
        }

        private void OnDestroy()
        {
            if (main == this)
            {
                main = null;
            }
        }

        public void Add(string key)
        {
            if (!Language.main.showSubtitles || string.IsNullOrEmpty(key))
            {
                return;
            }
            Language.MetaData metaData = Language.main.GetMetaData(key);
            if (metaData != null)
            {
                for (int i = 0; i < metaData.lineCount; i++)
                {
                    Language.LineData line = metaData.GetLine(i);
                    AddRawLong(line.text, line.delay, line.duration);
                }
            }
            else
            {
                string text = Language.main.Get(key);
                AddRawLong(text, 0f, 0f);
            }
        }

        public void Add(string key, params object[] args)
        {
            if (!Language.main.showSubtitles || string.IsNullOrEmpty(key))
            {
                return;
            }
            Language.MetaData metaData = Language.main.GetMetaData(key);
            if (metaData != null)
            {
                for (int i = 0; i < metaData.lineCount; i++)
                {
                    Language.LineData line = metaData.GetLine(i);
                    string text = Language.main.FormatString(line.text, args);
                    AddRawLong(text, line.delay, line.duration);
                }
            }
            else
            {
                string format = Language.main.GetFormat(key, args);
                AddRawLong(format, 0f, 0f);
            }
        }

        private void AddRawLong(string text, float delay, float duration)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            if (text.Length > 250)
            {
                int num = 0;
                float num2 = text.Length;
                while (num < text.Length)
                {
                    int num3 = num + 250;
                    int num4 = text.Length;
                    if (num4 > num3)
                    {
                        num4 = text.LastIndexOfAny(sentenceSeparators, num3);
                        if (num4 <= num)
                        {
                            num4 = text.LastIndexOf(' ', num3);
                            if (num4 <= num)
                            {
                                num4 = num3 - 1;
                            }
                        }
                        num4++;
                    }
                    string text2 = text.Substring(num, num4 - num);
                    float duration2 = duration * ((float)text2.Length / num2);
                    AddRaw(text2, delay, duration2);
                    num = num4;
                    delay = 0f;
                }
            }
            else
            {
                AddRaw(text, delay, duration);
            }
        }

        private void AddRaw(string text, float delay, float duration)
        {
            if (isShowingMessage)
            {
                queue.Enqueue(new Entry
                {
                    delay = delay,
                    duration = duration,
                    text = text
                });
            }
            else
            {
                Show(delay, duration, text);
            }
        }

        private void Show(float delay, float duration, string text)
        {
            duration = Mathf.Max(GetCharDelay() * (float)text.Length + duration, 0.01f);
            popup.SetText(text, TextAnchor.MiddleLeft);
            popup.Show(duration, delay, 0.25f, 0.1f, Next);
        }

        private void Next()
        {
            if (queue.Count != 0)
            {
                Entry entry = queue.Dequeue();
                Show(entry.delay, entry.duration, entry.text);
            }
        }

        private float GetCharDelay()
        {
            return 1f / speed;
        }
    }
}
