using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_PopupMessage : MonoBehaviour, IManagedUpdateBehaviour, IManagedBehaviour, ICanvasElement, ICompileTimeCheckable
    {
        public enum Phase : byte
        {
            Zero,
            Delay,
            In,
            One,
            Out
        }

        [AssertNotNull]
        public GameObject root;

        [AssertNotNull]
        public Graphic background;

        [AssertNotNull]
        public Text text;

        public TextAnchor anchor = TextAnchor.MiddleLeft;

        public float ox = 20f;

        public float oy = 20f;

        public bool useUnscaledDeltaTime = true;

        protected RectTransform rootRT;

        protected Phase phase;

        protected Sequence sequence;

        protected float delayTime;

        protected float inTime;

        protected float durationTime;

        protected float outTime;

        protected PopupMessageCallback doneCallback;

        protected float t = float.MinValue;

        protected int hash;

        public bool isShowingMessage => phase != Phase.Zero;

        public int messageHash => hash;

        public int managedUpdateIndex { get; set; }

        public string GetProfileTag()
        {
            return "uGUI_PopupMessage";
        }

        protected virtual void OnEnable()
        {
            BehaviourUpdateUtils.Register(this);
        }

        protected virtual void OnDisable()
        {
            BehaviourUpdateUtils.Deregister(this);
        }

        protected virtual void OnDestroy()
        {
            BehaviourUpdateUtils.Deregister(this);
        }

        protected virtual void Awake()
        {
            sequence = new Sequence();
            phase = Phase.Zero;
            sequence.Reset();
            root.SetActive(value: false);
            rootRT = root.GetComponent<RectTransform>();
            RectTransform component = rootRT.parent.GetComponent<RectTransform>();
            component.pivot = Vector2.zero;
            component.anchorMin = Vector2.zero;
            component.anchorMax = Vector2.one;
            component.anchoredPosition = Vector2.zero;
            SetPosition(0f);
        }

        public void ManagedUpdate()
        {
            sequence.Update(useUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime);
            if (sequence.active)
            {
                switch (phase)
                {
                    case Phase.Zero:
                        SetPosition(0f);
                        break;
                    case Phase.Delay:
                        SetPosition(0f);
                        break;
                    case Phase.In:
                        SetPosition(sequence.t);
                        break;
                    case Phase.One:
                        SetPosition(1f);
                        break;
                    case Phase.Out:
                        SetPosition(sequence.t);
                        break;
                }
            }
        }

        protected void SetPosition(float t)
        {
            t = Mathf.Clamp01(t);
            if (this.t != t)
            {
                this.t = t;
                CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(this);
            }
        }

        protected void Callback()
        {
            switch (phase)
            {
                case Phase.Delay:
                    phase = Phase.In;
                    sequence.Set(inTime, current: false, target: true, Callback);
                    SetPosition(0f);
                    break;
                case Phase.In:
                    phase = Phase.One;
                    if (durationTime >= 0f)
                    {
                        sequence.Set(durationTime, current: false, target: true, Callback);
                    }
                    SetPosition(1f);
                    break;
                case Phase.One:
                    phase = Phase.Out;
                    sequence.Set(outTime, current: true, target: false, Callback);
                    break;
                case Phase.Out:
                    phase = Phase.Zero;
                    root.SetActive(value: false);
                    SetText(string.Empty);
                    SetPosition(0f);
                    if (doneCallback != null)
                    {
                        doneCallback();
                    }
                    break;
            }
        }

        protected void GetCoords(TextAnchor anchor, ref Vector2 hidden, ref Vector2 visible)
        {
            Rect rect = rootRT.parent.GetComponent<RectTransform>().rect;
            Rect rect2 = rootRT.rect;
            float width = rect.width;
            float height = rect.height;
            float width2 = rect2.width;
            float height2 = rect2.height;
            switch (anchor)
            {
                case TextAnchor.LowerLeft:
                    hidden = new Vector2(0f - width2, 0f - height2);
                    visible = new Vector2(ox, oy);
                    break;
                case TextAnchor.LowerCenter:
                    hidden = new Vector2(0.5f * (width - width2), 0f - height2);
                    visible = new Vector2(0.5f * (width - width2), oy);
                    break;
                case TextAnchor.LowerRight:
                    hidden = new Vector2(width, 0f - height2);
                    visible = new Vector2(width - ox - width2, oy);
                    break;
                case TextAnchor.MiddleLeft:
                    hidden = new Vector2(0f - width2, 0.5f * (height - height2));
                    visible = new Vector2(ox, 0.5f * (height - height2));
                    break;
                case TextAnchor.MiddleCenter:
                    hidden = new Vector2(0f - width2, 0f - height2);
                    visible = new Vector2(0.5f * (width - width2), 0.5f * (height - height2));
                    break;
                case TextAnchor.MiddleRight:
                    hidden = new Vector2(width, 0.5f * (height - height2));
                    visible = new Vector2(width - ox - width2, 0.5f * (height - height2));
                    break;
                case TextAnchor.UpperLeft:
                    hidden = new Vector2(0f - width2, height);
                    visible = new Vector2(ox, height - oy - height2);
                    break;
                case TextAnchor.UpperCenter:
                    hidden = new Vector2(0.5f * (width - width2), height);
                    visible = new Vector2(0.5f * (width - width2), height - oy - height2);
                    break;
                case TextAnchor.UpperRight:
                    hidden = new Vector2(width, height);
                    visible = new Vector2(width - ox - width2, height - oy - height2);
                    break;
            }
        }

        public void SetBackgroundColor(Color color)
        {
            background.color = color;
        }

        public void SetText(string message, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            int num = ((!string.IsNullOrEmpty(message)) ? message.GetHashCode() : 0);
            if (hash != num)
            {
                hash = num;
                text.text = message;
                t = float.MinValue;
            }
            if (text.alignment != alignment)
            {
                text.alignment = alignment;
                t = float.MinValue;
            }
        }

        public void Show(float durationTime = 5f, float delayTime = 0f, float inTime = 0.25f, float outTime = 0.25f, PopupMessageCallback doneCallback = null)
        {
            this.durationTime = durationTime;
            this.delayTime = ((delayTime < 0f) ? 0f : delayTime);
            this.inTime = ((inTime < 0f) ? 0f : inTime);
            this.outTime = ((outTime < 0f) ? 0f : outTime);
            this.doneCallback = doneCallback;
            switch (phase)
            {
                case Phase.Zero:
                    root.SetActive(value: true);
                    phase = Phase.Delay;
                    sequence.Set(delayTime, current: false, target: true, Callback);
                    break;
                case Phase.Delay:
                    if (delayTime > 0f)
                    {
                        phase = Phase.Delay;
                        sequence.Set(delayTime, current: false, target: true, Callback);
                    }
                    else
                    {
                        phase = Phase.In;
                        sequence.Set(inTime, current: false, target: true, Callback);
                    }
                    break;
                case Phase.In:
                    phase = Phase.In;
                    sequence.Set(inTime, target: true, Callback);
                    break;
                case Phase.One:
                    if (durationTime > 0f)
                    {
                        sequence.Set(durationTime, current: false, target: true, Callback);
                    }
                    else
                    {
                        sequence.ForceState(state: true);
                    }
                    break;
                case Phase.Out:
                    phase = Phase.In;
                    sequence.Set(outTime, target: true, Callback);
                    break;
            }
            SetPosition(sequence.t);
        }

        public void Hide()
        {
            switch (phase)
            {
                case Phase.Delay:
                    phase = Phase.Out;
                    sequence.Set(0f, target: false, Callback);
                    break;
                case Phase.In:
                    phase = Phase.Out;
                    sequence.Set(outTime, target: false, Callback);
                    break;
                case Phase.One:
                    phase = Phase.Out;
                    sequence.Set(outTime, current: true, target: false, Callback);
                    break;
                case Phase.Out:
                    sequence.Set(outTime, target: false, Callback);
                    break;
                case Phase.Zero:
                    break;
            }
        }

        public void LayoutComplete()
        {
            Vector2 hidden = default(Vector2);
            Vector2 visible = default(Vector2);
            GetCoords(anchor, ref hidden, ref visible);
            float num = MathExtensions.EaseOutSine(t);
            rootRT.anchoredPosition = Vector2.Lerp(hidden, visible, num);
        }

        public bool IsDestroyed()
        {
            return this == null;
        }

        public void Rebuild(CanvasUpdate executing)
        {
        }

        public void GraphicUpdateComplete()
        {
        }

        public string CompileTimeCheck()
        {
            if (root == base.gameObject)
            {
                return $"uGUI_PopupMessage : uGUI_PopupMessage.root == uGUI_PopupMessage.gameObject in\n{Dbg.LogHierarchy(base.gameObject)}";
            }
            RectTransform component = root.GetComponent<RectTransform>();
            if (component == null)
            {
                return $"uGUI_PopupMessage : root has no RectTransform component in\n{Dbg.LogHierarchy(base.gameObject)}";
            }
            Transform parent = component.parent;
            if (parent == null)
            {
                return $"uGUI_PopupMessage : root.GetComponent<RectTransform>().parent == null in\n{Dbg.LogHierarchy(base.gameObject)}";
            }
            if (parent.GetComponent<RectTransform>() == null)
            {
                return $"uGUI_PopupMessage : root.GetComponent<RectTransform>().parent has no RectTransform component in\n{Dbg.LogHierarchy(base.gameObject)}";
            }
            return null;
        }

        [SpecialName]
        Transform ICanvasElement.transform => transform;
    }
}
