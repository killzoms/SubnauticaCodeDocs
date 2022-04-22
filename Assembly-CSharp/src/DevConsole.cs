using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class DevConsole : uGUI_InputGroup
    {
        private class CommandData
        {
            public bool caseSensitive;

            public bool combineArgs;
        }

        private static DevConsole instance = null;

        public static bool disableConsole = true;

        private const int characterLimit = 256;

        private const int maxHistory = 10;

        private static KeyCode[] keyCodes = new KeyCode[3]
        {
            KeyCode.BackQuote,
            KeyCode.Return,
            KeyCode.KeypadEnter
        };

        public Font font;

        public Sprite background;

        public Vector2 border = new Vector2(4f, 4f);

        private static readonly Dictionary<string, CommandData> commands = new Dictionary<string, CommandData>(StringComparer.InvariantCultureIgnoreCase);

        private bool hasUsedConsole;

        private bool state;

        private GameObject inputFieldGO;

        private RectTransform inputFieldTr;

        private ConsoleInput inputField;

        private Image inputImage;

        private Text textField;

        private List<string> history;

        protected override void Awake()
        {
            base.Awake();
            if (instance != null)
            {
                Debug.LogError("Multiple DevConsole instances detected!");
                global::UnityEngine.Object.Destroy(this);
                return;
            }
            instance = this;
            LoadHistory();
            _ = base.gameObject;
            RectTransform component = GetComponent<RectTransform>();
            inputFieldGO = new GameObject("InputField");
            inputImage = inputFieldGO.AddComponent<Image>();
            inputImage.sprite = background;
            inputImage.type = Image.Type.Sliced;
            inputField = inputFieldGO.AddComponent<ConsoleInput>();
            inputField.SetHistory(history);
            inputField.characterLimit = 256;
            inputField.selectionColor = new Color(0f, 0f, 0f, 0.753f);
            inputField.caretBlinkRate = 2f;
            inputField.ignoreKeyCodes = keyCodes;
            inputField.onSubmit += OnSubmit;
            inputFieldTr = inputFieldGO.GetComponent<RectTransform>();
            inputFieldTr.SetParent(component, worldPositionStays: false);
            inputFieldTr.anchorMin = new Vector2(0f, 0f);
            inputFieldTr.anchorMax = new Vector2(0f, 0f);
            inputFieldTr.pivot = new Vector2(0f, 0f);
            inputFieldTr.anchoredPosition = new Vector2(10f, 10f);
            GameObject gameObject = new GameObject("Text");
            textField = gameObject.AddComponent<Text>();
            textField.supportRichText = false;
            textField.fontSize = 16;
            textField.font = font;
            textField.alignment = TextAnchor.UpperLeft;
            textField.horizontalOverflow = HorizontalWrapMode.Wrap;
            textField.verticalOverflow = VerticalWrapMode.Overflow;
            textField.color = new Color(1f, 1f, 1f, 1f);
            RectTransform component2 = gameObject.GetComponent<RectTransform>();
            component2.SetParent(inputFieldTr, worldPositionStays: false);
            component2.anchorMin = new Vector2(0f, 0f);
            component2.anchorMax = new Vector2(1f, 1f);
            component2.offsetMin = border;
            component2.offsetMax = -border;
            inputField.textComponent = textField;
            inputFieldTr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300f);
            inputFieldTr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textField.preferredHeight + 2f * border.y);
            inputFieldTr.SetAsLastSibling();
            inputField.Deactivate();
            if (PlatformUtils.isConsolePlatform)
            {
                disableConsole = false;
            }
        }

        private void Start()
        {
            RegisterConsoleCommand(this, "commands");
            RegisterConsoleCommand(this, "clearhistory");
            RegisterConsoleCommand(this, "developermode");
        }

        protected override void Update()
        {
            if (disableConsole)
            {
                if (state)
                {
                    SetState(value: false);
                }
                return;
            }
            GameObject gameObject = null;
            if (EventSystem.current != null)
            {
                gameObject = EventSystem.current.currentSelectedGameObject;
            }
            if (state || gameObject == null || gameObject.GetComponent<InputField>() == null)
            {
                for (int i = 0; i < keyCodes.Length; i++)
                {
                    KeyCode keyCode = keyCodes[i];
                    if (!Input.GetKeyDown(keyCode))
                    {
                        continue;
                    }
                    if (keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter)
                    {
                        if (!state)
                        {
                            SetState(value: true);
                        }
                    }
                    else
                    {
                        ToggleState();
                    }
                    break;
                }
            }
            if (PlatformUtils.isConsolePlatform && !state && Input.GetKey(KeyCode.JoystickButton0) && Input.GetKey(KeyCode.JoystickButton4) && Input.GetKeyDown(KeyCode.JoystickButton5))
            {
                SetState(value: true);
                string defaultText = "";
                if (history.Count > 0)
                {
                    defaultText = history[history.Count - 1];
                }
                PlatformUtils.main.GetServices().ShowVirtualKeyboard("DevConsole", defaultText, OnVirtualKeyboardFinished);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetState(value: false);
            }
        }

        private void OnVirtualKeyboardFinished(bool success, bool canceled, string value)
        {
            if (success && !canceled)
            {
                if (OnSubmit(value))
                {
                    history.Add(value);
                }
            }
            else
            {
                SetState(value: false);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SaveHistory();
        }

        public static void RegisterConsoleCommand(Component originator, string command, bool caseSensitiveArgs = false, bool combineArgs = false)
        {
            if (!commands.ContainsKey(command))
            {
                CommandData commandData = new CommandData();
                commandData.caseSensitive = caseSensitiveArgs;
                commandData.combineArgs = combineArgs;
                commands.Add(command, commandData);
            }
            NotificationCenter.DefaultCenter.AddObserver(originator, "OnConsoleCommand_" + command);
        }

        public static void SendConsoleCommand(string value)
        {
            instance.Submit(value);
        }

        public static bool HasUsedConsole()
        {
            if (!instance)
            {
                return false;
            }
            return instance.hasUsedConsole;
        }

        private bool Submit(string value)
        {
            char[] separator = new char[2] { ' ', '\t' };
            string text = value.Trim();
            string[] array = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length == 0)
            {
                return false;
            }
            string text2 = array[0];
            if (commands.TryGetValue(text2, out var value2))
            {
                bool caseSensitive = value2.caseSensitive;
                bool combineArgs = value2.combineArgs;
                Hashtable hashtable = null;
                if (combineArgs)
                {
                    text = text.Substring(text2.Length).Trim();
                    if (!caseSensitive)
                    {
                        text = text.ToLower();
                    }
                    if (text.Length > 0)
                    {
                        hashtable = new Hashtable();
                        hashtable.Add(0, text);
                    }
                }
                else if (array.Length > 1)
                {
                    hashtable = new Hashtable();
                    for (int i = 1; i < array.Length; i++)
                    {
                        hashtable[i - 1] = (caseSensitive ? array[i] : array[i].ToLower());
                    }
                }
                if (hashtable != null)
                {
                    NotificationCenter.DefaultCenter.PostNotification(this, "OnConsoleCommand_" + text2, hashtable);
                }
                else
                {
                    NotificationCenter.DefaultCenter.PostNotification(this, "OnConsoleCommand_" + text2);
                }
                return true;
            }
            return false;
        }

        private bool OnSubmit(string value)
        {
            if (!state)
            {
                return false;
            }
            bool result = false;
            if (!string.IsNullOrEmpty(value))
            {
                hasUsedConsole = true;
                result = Submit(value);
            }
            SetState(value: false);
            return result;
        }

        private void OnConsoleCommand_developermode()
        {
            IngameMenu main = IngameMenu.main;
            if ((bool)main)
            {
                main.ActivateDeveloperMode();
            }
        }

        private void OnConsoleCommand_magic()
        {
            string[] array = new string[3] { "nocost", "item builder", "unlock all" };
            for (int i = 0; i < array.Length; i++)
            {
                Submit(array[i]);
            }
        }

        private void OnConsoleCommand_commands()
        {
            StringBuilder stringBuilder = new StringBuilder();
            Dictionary<string, CommandData>.KeyCollection.Enumerator enumerator = commands.Keys.GetEnumerator();
            int num = 0;
            while (enumerator.MoveNext())
            {
                stringBuilder.Append(enumerator.Current);
                stringBuilder.Append(' ');
                if (num % 4 == 0)
                {
                    stringBuilder.AppendLine();
                }
                num++;
            }
            ErrorMessage.AddDebug("Console commands: " + stringBuilder.ToString());
        }

        public void ToggleState()
        {
            SetState(!state);
        }

        public void SetState(bool value)
        {
            if (state != value)
            {
                state = value;
                if (state)
                {
                    Select(lockMovement: true);
                    inputField.Activate();
                }
                else
                {
                    inputField.Deactivate();
                    Deselect();
                }
            }
        }

        public override void OnReselect(bool lockMovement)
        {
            base.OnReselect(lockMovement: true);
        }

        public override void OnDeselect()
        {
            SetState(value: false);
            base.OnDeselect();
        }

        private void OnConsoleCommand_clearhistory()
        {
            history = new List<string>();
            inputField.SetHistory(history);
            MiscSettings.consoleHistory = "";
        }

        private void SaveHistory()
        {
            if (history == null)
            {
                return;
            }
            StringBuilder stringBuilder = new StringBuilder();
            int i = Mathf.Max(0, history.Count - 10);
            for (int num = history.Count - 1; i <= num; i++)
            {
                string value = history[i];
                if (i == num)
                {
                    stringBuilder.Append(value);
                }
                else
                {
                    stringBuilder.AppendLine(value);
                }
            }
            MiscSettings.consoleHistory = stringBuilder.ToString();
        }

        private void LoadHistory()
        {
            history = new List<string>();
            string consoleHistory = MiscSettings.consoleHistory;
            if (!string.IsNullOrEmpty(consoleHistory))
            {
                string[] collection = consoleHistory.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                history = new List<string>(collection);
            }
        }

        public static void InternalSendConsoleCommand(string consoleCommand)
        {
            NotificationCenter.DefaultCenter.PostNotification(instance, "OnConsoleCommand_" + consoleCommand);
        }
    }
}
