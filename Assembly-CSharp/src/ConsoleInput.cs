using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class ConsoleInput : InputFieldFixed
    {
        public delegate bool Submit(string text);

        public KeyCode[] ignoreKeyCodes = new KeyCode[1];

        private Navigation _navigation = new Navigation
        {
            mode = Navigation.Mode.None
        };

        private const char emptyChar = '\0';

        private const char newLineChar = '\n';

        private int enabledInFrame = -1;

        private GameObject go;

        private List<string> history;

        private int historyIndex;

        private string cachedInput = string.Empty;

        private int cachedPos;

        private int cachedSelectPos;

        private bool activateNextUpdate;

        private Event processingEvent = new Event();

        private Queue<Event> eventQueue = new Queue<Event>();

        public new Navigation navigation => _navigation;

        public new Transition transition => Transition.None;

        public new ContentType contentType => ContentType.Custom;

        public new LineType lineType => LineType.SingleLine;

        public new InputType inputType => InputType.Standard;

        public new CharacterValidation characterValidation => CharacterValidation.None;

        public new TouchScreenKeyboardType keyboardType => TouchScreenKeyboardType.Default;

        public new OnValidateInput onValidateInput => null;

        public new SubmitEvent onEndEdit => new SubmitEvent();

        private bool enabledThisFrame
        {
            get
            {
                return enabledInFrame == Time.frameCount;
            }
            set
            {
                if (value)
                {
                    enabledInFrame = Time.frameCount;
                }
            }
        }

        public event Submit onSubmit;

        protected override void Awake()
        {
            base.Awake();
            base.contentType = ContentType.Custom;
            base.lineType = LineType.SingleLine;
            base.inputType = InputType.Standard;
            base.characterValidation = CharacterValidation.None;
            base.keyboardType = TouchScreenKeyboardType.Default;
            base.onValidateInput = Validate;
            go = base.gameObject;
        }

        protected override void Start()
        {
            base.Start();
            base.image = GetComponent<Image>();
        }

        private void Update()
        {
            if (activateNextUpdate)
            {
                ActivateInputField();
                if (!EventSystem.current.alreadySelecting)
                {
                    EventSystem.current.SetSelectedGameObject(go);
                    activateNextUpdate = false;
                }
            }
        }

        public void SetHistory(List<string> h)
        {
            history = h;
            historyIndex = history.Count;
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (enabledThisFrame)
            {
                if (base.isFocused)
                {
                    eventData.Use();
                }
                return;
            }
            eventQueue.Clear();
            if (base.isFocused)
            {
                while (Event.PopEvent(processingEvent))
                {
                    eventQueue.Enqueue(new Event(processingEvent));
                }
            }
            base.OnUpdateSelected(eventData);
            if (!base.isFocused)
            {
                return;
            }
            bool flag = false;
            while (eventQueue.Count > 0)
            {
                processingEvent = eventQueue.Dequeue();
                if (processingEvent.rawType == EventType.KeyDown)
                {
                    flag = true;
                    if (KeyPressedOverride(processingEvent) || KeyPressed(processingEvent) == EditState.Finish)
                    {
                        break;
                    }
                }
            }
            if (flag)
            {
                UpdateLabel();
            }
            eventData.Use();
        }

        private bool KeyPressedOverride(Event evt)
        {
            switch (processingEvent.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    SubmitInput(base.text);
                    return false;
                case KeyCode.Backspace:
                case KeyCode.Delete:
                    historyIndex = history.Count;
                    break;
                case KeyCode.UpArrow:
                    return ProcessUpKey();
                case KeyCode.DownArrow:
                    return ProcessDownKey();
            }
            return false;
        }

        private bool ProcessUpKey()
        {
            if (historyIndex == history.Count)
            {
                CacheInput();
            }
            if (historyIndex > 0)
            {
                historyIndex--;
                if (historyIndex != history.Count)
                {
                    string text = history[historyIndex];
                    m_CaretPosition = 0;
                    m_CaretSelectPosition = text.Length;
                    base.text = text;
                }
                return true;
            }
            return false;
        }

        private bool ProcessDownKey()
        {
            if (historyIndex < history.Count - 1)
            {
                historyIndex++;
                string text = history[historyIndex];
                m_CaretPosition = 0;
                m_CaretSelectPosition = text.Length;
                base.text = text;
                return true;
            }
            if (historyIndex == history.Count - 1)
            {
                historyIndex++;
                string text2 = cachedInput;
                m_CaretPosition = cachedPos;
                m_CaretSelectPosition = cachedSelectPos;
                base.text = text2;
                return true;
            }
            if (historyIndex == history.Count)
            {
                base.text = (cachedInput = string.Empty);
                base.caretPosition = 0;
                return true;
            }
            return false;
        }

        protected new char Validate(string text, int pos, char ch)
        {
            historyIndex = history.Count;
            if (enabledThisFrame)
            {
                return '\0';
            }
            for (int i = 0; i < ignoreKeyCodes.Length; i++)
            {
                KeyCode key = ignoreKeyCodes[i];
                if (Input.GetKey(key) || Input.GetKeyDown(key) || Input.GetKeyUp(key))
                {
                    return '\0';
                }
            }
            switch (ch)
            {
                case '\n':
                    return '\0';
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                    return ch;
                default:
                    if (ch >= 'A' && ch <= 'Z')
                    {
                        return ch;
                    }
                    if (ch >= '0' && ch <= '9')
                    {
                        return ch;
                    }
                    if (" -_./:()".IndexOf(ch) != -1)
                    {
                        return ch;
                    }
                    return '\0';
            }
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            activateNextUpdate = true;
        }

        public void Activate()
        {
            enabledThisFrame = true;
            base.enabled = true;
            SetElements(value: true);
            Select();
            base.text = cachedInput;
            ActivateInputField();
            MoveTextEnd(shift: false);
        }

        public void Deactivate()
        {
            if (historyIndex >= history.Count - 1)
            {
                CacheInput();
            }
            Clear();
            historyIndex = history.Count;
            DeactivateInputField();
            base.OnDeselect((BaseEventData)null);
            base.enabled = false;
            SetElements(value: false);
            Rebuild(CanvasUpdate.LatePreRender);
        }

        private void SetElements(bool value)
        {
            if (base.image != null)
            {
                base.image.enabled = value;
            }
            if (base.textComponent != null)
            {
                base.textComponent.enabled = value;
            }
        }

        private void Clear()
        {
            base.text = string.Empty;
        }

        private void SubmitInput(string value)
        {
            if (!enabledThisFrame)
            {
                ActivateInputField();
                Select();
                bool flag = false;
                if (this.onSubmit != null)
                {
                    flag = this.onSubmit(base.text);
                }
                cachedInput = string.Empty;
                Clear();
                if (flag && (history.Count == 0 || !string.Equals(value, history[history.Count - 1])))
                {
                    history.Add(value);
                }
                historyIndex = history.Count;
            }
        }

        public override void OnSubmit(BaseEventData eventData)
        {
        }

        protected new void SendOnSubmit()
        {
        }

        private void CacheInput()
        {
            cachedInput = base.text;
            cachedPos = m_CaretPosition;
            cachedSelectPos = m_CaretSelectPosition;
        }
    }
}
