using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Gendarme;
using LitJson;
using UnityEngine;

namespace AssemblyCSharp
{
    public class Language : MonoBehaviour, ILanguage
    {
        public class LineData
        {
            public string text { get; private set; }

            public float delay { get; private set; }

            public float duration { get; private set; }

            public LineData(string text, float delay, float duration)
            {
                this.text = text;
                this.delay = delay;
                this.duration = duration;
            }
        }

        public class MetaData
        {
            private string _text;

            private List<LineData> _lines;

            public string text => _text;

            public int lineCount => _lines.Count;

            public MetaData(string text, List<LineData> lines)
            {
                _text = text;
                _lines = lines;
            }

            public LineData GetLine(int index)
            {
                if (index < 0 || index >= _lines.Count)
                {
                    return null;
                }
                return _lines[index];
            }
        }

        private readonly string[,] cultureToLanguage = new string[29, 2]
        {
            { "bg-BG", "Bulgarian" },
            { "zh-CN", "Chinese (Simplified)" },
            { "hr-HR", "Croatian" },
            { "cs-CZ", "Czech" },
            { "da-DK", "Danish" },
            { "nl-BE", "Dutch" },
            { "en-US", "English" },
            { "fi-FI", "Finnish" },
            { "fr-FR", "French" },
            { "de-DE", "German" },
            { "el-GR", "Greek" },
            { "hu-HU", "Hungarian" },
            { "it-IT", "Italian" },
            { "ja-JP", "Japanese" },
            { "ko-KR", "Korean" },
            { "lv-LV", "Latvian" },
            { "lt-LT", "Lithuanian" },
            { "pl-PL", "Polish" },
            { "pt-BR", "Portuguese (Brazil)" },
            { "pt-PT", "Portuguese" },
            { "ro-RO", "Romanian" },
            { "ru-RU", "Russian" },
            { "sr-Cyrl", "Serbian" },
            { "sk-SK", "Slovak" },
            { "es-ES", "Spanish" },
            { "sv-SE", "Swedish" },
            { "th-TH", "Thai" },
            { "tr-TR", "Turkish" },
            { "uk-UA", "Ukrainian" }
        };

        private static readonly char[] messageSeparators = new char[1] { '\n' };

        private const string delayHintSequence = "###";

        public const string defaultLanguage = "English";

        public static Language main = null;

        public bool debug;

        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();

        private Dictionary<string, MetaData> metadata = new Dictionary<string, MetaData>();

        private StringBuilder sb = new StringBuilder();

        private bool _showSubtitles = true;

        private string currentLanguage;

        private CultureInfo currentCultureInfo;

        public bool showSubtitles
        {
            get
            {
                return _showSubtitles;
            }
            set
            {
                _showSubtitles = value;
            }
        }

        public event Action OnLanguageChanged;

        private void Awake()
        {
            main = this;
            Initialize();
        }

        public void Initialize()
        {
            if (PlatformUtils.hasFixedCulture)
            {
                string currentCultureName = PlatformUtils.GetCurrentCultureName();
                Debug.LogFormat("Current culture: {0}, C# Current Culture: {1}", currentCultureName, CultureInfo.CurrentCulture.Name);
                string languageFromCultureTag = GetLanguageFromCultureTag(currentCultureName);
                Debug.LogFormat("Language from culture: {0}", languageFromCultureTag);
                Initialize(languageFromCultureTag);
            }
            else
            {
                string @string = PlayerPrefs.GetString("Language", GetDefaultLanguage());
                Initialize(@string);
            }
        }

        public void Initialize(string language)
        {
            SetCurrentLanguage(language);
        }

        private void Start()
        {
            DevConsole.RegisterConsoleCommand(this, "language", caseSensitiveArgs: true, combineArgs: true);
            DevConsole.RegisterConsoleCommand(this, "translationkey", caseSensitiveArgs: false, combineArgs: true);
        }

        private void OnConsoleCommand_language(NotificationCenter.Notification n)
        {
            if (n.data != null && n.data.Count == 1)
            {
                string text = (string)n.data[0];
                SetCurrentLanguage(text);
            }
            else
            {
                ErrorMessage.AddDebug("usage: language <language>");
            }
        }

        private static string GetDefaultLanguage()
        {
            SystemLanguage systemLanguage = Application.systemLanguage;
            if (systemLanguage == SystemLanguage.Chinese || (uint)(systemLanguage - 40) <= 1u)
            {
                return "Chinese (Simplified)";
            }
            return systemLanguage.ToString();
        }

        public void SetCurrentLanguage(string language)
        {
            if (!LoadLanguageFile(language))
            {
                LoadLanguageFile("English");
            }
            if (this.OnLanguageChanged == null)
            {
                return;
            }
            Delegate[] invocationList = this.OnLanguageChanged.GetInvocationList();
            foreach (Delegate @delegate in invocationList)
            {
                try
                {
                    @delegate.DynamicInvoke(null);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                }
            }
        }

        public string GetCurrentLanguage()
        {
            return currentLanguage;
        }

        public string[] GetLanguages()
        {
            string[] files = Directory.GetFiles(SNUtils.InsideUnmanaged("LanguageFiles"), "*.json");
            string[] array = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string text = (array[i] = Path.GetFileNameWithoutExtension(files[i]));
            }
            return array;
        }

        private string GetLanguageTag(string languageName)
        {
            int length = cultureToLanguage.GetLength(0);
            for (int i = 0; i < length; i++)
            {
                if (cultureToLanguage[i, 1] == languageName)
                {
                    return cultureToLanguage[i, 0];
                }
            }
            return "en-US";
        }

        private string GetLanguageFromCultureTag(string cultureTag)
        {
            int length = cultureToLanguage.GetLength(0);
            for (int i = 0; i < length; i++)
            {
                if (cultureToLanguage[i, 0] == cultureTag)
                {
                    return cultureToLanguage[i, 1];
                }
            }
            return "English";
        }

        private bool LoadLanguageFile(string language)
        {
            string path = SNUtils.InsideUnmanaged("LanguageFiles/" + language + ".json");
            if (!File.Exists(path))
            {
                return false;
            }
            JsonData jsonData;
            using (StreamReader reader = new StreamReader(path))
            {
                try
                {
                    jsonData = JsonMapper.ToObject(reader);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    return false;
                }
            }
            if (!PlatformUtils.hasFixedCulture)
            {
                PlayerPrefs.SetString("Language", language);
            }
            currentLanguage = language;
            CultureInfo.GetCultures(CultureTypes.AllCultures);
            try
            {
                currentCultureInfo = new CultureInfo(GetLanguageTag(language));
            }
            catch (Exception exception2)
            {
                Debug.LogException(exception2);
                currentCultureInfo = new CultureInfo("en-US");
            }
            strings.Clear();
            foreach (string key in jsonData.Keys)
            {
                strings[key] = (string)jsonData[key];
            }
            if (Application.platform == RuntimePlatform.PS4)
            {
                Remap(jsonData, ".PS4");
            }
            else if (Application.platform == RuntimePlatform.XboxOne)
            {
                Remap(jsonData, ".XB1");
            }
            ParseMetaData();
            LanguageCache.OnLanguageChanged();
            return true;
        }

        private void Remap(JsonData json, string platformSuffix)
        {
            foreach (string item in json.Keys.Where((string p) => p.EndsWith(platformSuffix, StringComparison.Ordinal)))
            {
                string key = item.Substring(0, item.Length - platformSuffix.Length);
                strings[key] = (string)json[item];
            }
        }

        public bool TryGet(string key, out string result)
        {
            if (string.IsNullOrEmpty(key))
            {
                result = string.Empty;
                return false;
            }
            return strings.TryGetValue(key, out result);
        }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            if (!TryGet(key, out var result))
            {
                if (debug)
                {
                    Debug.LogWarningFormat(this, "no translation for key: '{0}'", key);
                }
                return key;
            }
            return result;
        }

        public string GetOrFallback(string key, string fallbackKey)
        {
            if (TryGet(key, out var result) && !string.IsNullOrEmpty(result))
            {
                return result;
            }
            return Get(fallbackKey);
        }

        public string GetFormat(string key)
        {
            return Get(key);
        }

        public string GetFormat(string key, params object[] args)
        {
            return GetFormatImpl(key, args);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public string GetFormat<Arg0>(string key, Arg0 arg0)
        {
            return GetFormatImpl(key, arg0);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public string GetFormat<Arg0, Arg1>(string key, Arg0 arg0, Arg1 arg1)
        {
            return GetFormatImpl(key, arg0, arg1);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public string GetFormat<Arg0, Arg1, Arg2>(string key, Arg0 arg0, Arg1 arg1, Arg2 arg2)
        {
            return GetFormatImpl(key, arg0, arg1, arg2);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public string GetFormat<Arg0, Arg1, Arg2, Arg3>(string key, Arg0 arg0, Arg1 arg1, Arg2 arg2, Arg3 arg3)
        {
            return GetFormatImpl(key, arg0, arg1, arg2, arg3);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidBoxingRule")]
        public string GetFormat<Arg0, Arg1, Arg2, Arg3, Arg4>(string key, Arg0 arg0, Arg1 arg1, Arg2 arg2, Arg3 arg3, Arg4 arg4)
        {
            return GetFormatImpl(key, arg0, arg1, arg2, arg3, arg4);
        }

        private string GetFormatImpl(string key, params object[] args)
        {
            return FormatString(Get(key), args);
        }

        public string FormatString(string text, params object[] args)
        {
            if (args != null && args.Length != 0)
            {
                try
                {
                    return string.Format(currentCultureInfo, text, args);
                }
                catch (FormatException exception)
                {
                    Debug.LogException(exception);
                    return text;
                }
            }
            return text;
        }

        public bool Contains(string key)
        {
            return strings.ContainsKey(key);
        }

        public List<string> GetKeysFor(string translated, StringComparison comparison)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(translated))
            {
                return list;
            }
            foreach (KeyValuePair<string, string> @string in strings)
            {
                if (string.Equals(@string.Value, translated, comparison))
                {
                    list.Add(@string.Key);
                }
            }
            return list;
        }

        private void OnConsoleCommand_translationkey(NotificationCenter.Notification n)
        {
            if (n != null && n.data != null && n.data.Count > 0)
            {
                string text = (string)n.data[0];
                List<string> keysFor = GetKeysFor(text, StringComparison.OrdinalIgnoreCase);
                if (keysFor.Count > 0)
                {
                    ErrorMessage.AddDebug(string.Format("Translation key(s) for '{0}' string: '{1}'", text, string.Join("', '", keysFor.ToArray())));
                }
                else
                {
                    ErrorMessage.AddDebug($"Translation key for '{text}' string is not defined");
                }
            }
            else
            {
                ErrorMessage.AddDebug("Usage: translationkey translated_string");
            }
        }

        private void ParseMetaData()
        {
            metadata = new Dictionary<string, MetaData>();
            foreach (KeyValuePair<string, string> @string in strings)
            {
                string key = @string.Key;
                string value = @string.Value;
                string[] array = value.Split(messageSeparators, StringSplitOptions.None);
                string result;
                float delay;
                float duration;
                if (array.Length > 1)
                {
                    List<LineData> list = new List<LineData>();
                    foreach (string text in array)
                    {
                        ParseLineMeta(text, out result, out delay, out duration);
                        LineData item = new LineData(result, delay, duration);
                        list.Add(item);
                    }
                    sb.Length = 0;
                    for (int j = 0; j < list.Count; j++)
                    {
                        LineData lineData = list[j];
                        if (sb.Length > 0)
                        {
                            sb.Append('\n');
                        }
                        sb.Append(lineData.text);
                    }
                    MetaData value2 = new MetaData(sb.ToString(), list);
                    metadata.Add(key, value2);
                }
                else if (ParseLineMeta(value, out result, out delay, out duration))
                {
                    List<LineData> list2 = new List<LineData>();
                    LineData item2 = new LineData(result, delay, duration);
                    list2.Add(item2);
                    MetaData value3 = new MetaData(result, list2);
                    metadata.Add(key, value3);
                }
            }
            foreach (KeyValuePair<string, MetaData> metadatum in metadata)
            {
                string key2 = metadatum.Key;
                MetaData value4 = metadatum.Value;
                strings[key2] = value4.text;
            }
        }

        private bool ParseLineMeta(string text, out string result, out float delay, out float duration)
        {
            result = text;
            delay = 0f;
            duration = 0f;
            if (text == null)
            {
                return false;
            }
            bool result2 = false;
            sb.Length = 0;
            int i = 0;
            int length = text.Length;
            int length2 = "###".Length;
            while (true)
            {
                if (i < length)
                {
                    if (string.CompareOrdinal(text, i, "###", 0, length2) != 0)
                    {
                        char value = text[i];
                        i++;
                        sb.Append(value);
                        continue;
                    }
                    i += length2;
                }
                if (i == length)
                {
                    break;
                }
                int num = 1;
                switch (text[i])
                {
                    case '+':
                        num = 1;
                        i++;
                        break;
                    case '-':
                        num = -1;
                        i++;
                        break;
                }
                if (i == length)
                {
                    break;
                }
                int num2 = 0;
                for (; i < length; i++)
                {
                    char value;
                    if ((value = text[i]) < '0')
                    {
                        break;
                    }
                    if (value > '9')
                    {
                        break;
                    }
                    if (num2 >= int.MaxValue)
                    {
                        break;
                    }
                    num2 = num2 * 10 + value - 48;
                }
                num2 *= num;
                float num3 = (float)Mathf.Clamp(num2, -20000, 20000) / 1000f;
                if (sb.Length == 0)
                {
                    delay = num3;
                }
                else
                {
                    duration = num3;
                }
                result2 = true;
            }
            result = sb.ToString();
            return result2;
        }

        public MetaData GetMetaData(string key)
        {
            return metadata.GetOrDefault(key, null);
        }
    }
}
