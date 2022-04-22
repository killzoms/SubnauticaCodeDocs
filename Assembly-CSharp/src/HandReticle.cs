using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    public class HandReticle : MonoBehaviour
    {
        public enum IconType
        {
            None,
            Default,
            Hand,
            HandDeny,
            Scan,
            Progress,
            Info,
            Drill,
            PackUp,
            Rename,
            Interact
        }

        public enum Hand
        {
            None,
            Left,
            Right
        }

        public class TextCacheData
        {
            public Hand hand;

            public string inputText2;

            public string outputText1;

            public string outputText2;
        }

        public static HandReticle main;

        public RectTransform iconCanvas;

        public List<uGUI_HandReticleIcon> icons = new List<uGUI_HandReticleIcon>();

        public float iconScaleSpeed = 10f;

        public Text interactPrimaryText;

        public Text interactSecondaryText;

        public Text usePrimaryText;

        public Text useSecondaryText;

        public Image progressImage;

        public Text progressText;

        private IconType iconType;

        private IconType desiredIconType;

        private float desiredIconScale = 1f;

        private Dictionary<IconType, uGUI_HandReticleIcon> _icons;

        private int hideCount;

        private bool hideForScreenshots;

        private string interactText1;

        private string interactText2;

        private string useText1;

        private string useText2;

        private float progress;

        private float targetDistance;

        private string stringLeftHand;

        private string stringRightHand;

        private string stringBack;

        private float cachedProgress = float.MinValue;

        private Dictionary<string, TextCacheData> textCache = new Dictionary<string, TextCacheData>();

        public IconType CurrentIconType => iconType;

        private void Awake()
        {
            _icons = new Dictionary<IconType, uGUI_HandReticleIcon>();
            int i = 0;
            for (int count = icons.Count; i < count; i++)
            {
                uGUI_HandReticleIcon uGUI_HandReticleIcon2 = icons[i];
                IconType type = uGUI_HandReticleIcon2.type;
                uGUI_HandReticleIcon2.SetActive(active: false);
                if (type == IconType.None)
                {
                    Debug.LogError("HandReticle : Awake() : It is not allowed to explicitly assign IconType.None!");
                    continue;
                }
                if (!_icons.ContainsKey(type))
                {
                    _icons.Add(type, uGUI_HandReticleIcon2);
                    continue;
                }
                Debug.LogError(string.Concat("HandReticle : Awake() : Duplicate icon type '", type, "' found at index '", i, "' in the iconData list!"));
            }
            progressImage.type = Image.Type.Filled;
            progressImage.fillMethod = Image.FillMethod.Radial360;
            progressImage.fillOrigin = 2;
            progressImage.fillClockwise = true;
            UpdateProgress();
        }

        private void Start()
        {
            main = this;
            iconType = IconType.None;
            SetUseText("");
            SetInteractText("");
            UpdateBindingsText();
        }

        private void OnLanguageChanged()
        {
            textCache.Clear();
        }

        private void UpdateBindingsText()
        {
            stringLeftHand = uGUI.FormatButton(GameInput.Button.LeftHand);
            stringRightHand = uGUI.FormatButton(GameInput.Button.RightHand);
            stringBack = uGUI.FormatButton(GameInput.Button.Exit);
        }

        private void OnEnable()
        {
            GameInput.OnBindingsChanged += OnBindingsChanged;
            Language.main.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            GameInput.OnBindingsChanged -= OnBindingsChanged;
            if (Language.main != null)
            {
                Language.main.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        private void OnBindingsChanged()
        {
            textCache.Clear();
            UpdateBindingsText();
        }

        private void LateUpdate()
        {
            UpdateText(interactPrimaryText, interactText1);
            UpdateText(interactSecondaryText, interactText2);
            UpdateText(usePrimaryText, useText1);
            UpdateText(useSecondaryText, useText2);
            if (hideCount > 0 || AvatarInputHandler.main == null || !AvatarInputHandler.main.IsEnabled())
            {
                desiredIconType = IconType.None;
            }
            SetIconInternal(desiredIconType);
            UpdateScale();
            UpdateProgress();
            int i = 0;
            for (int count = icons.Count; i < count; i++)
            {
                icons[i].UpdateIcon();
            }
            if (XRSettings.enabled)
            {
                float num = targetDistance;
                if (num == 0f)
                {
                    num = 1000f;
                }
                Matrix4x4 worldToLocalMatrix = base.transform.parent.worldToLocalMatrix;
                Matrix4x4 localToWorldMatrix = ((SNCameraRoot.main != null) ? SNCameraRoot.main.guiCamera : MainCamera.camera).transform.localToWorldMatrix;
                float z = (worldToLocalMatrix * localToWorldMatrix).MultiplyPoint3x4(new Vector3(0f, 0f, num)).z;
                Vector3 localPosition = base.transform.localPosition;
                localPosition.z = z;
                base.transform.localPosition = localPosition;
            }
            desiredIconScale = 1f;
            desiredIconType = IconType.Default;
            progress = 0f;
            interactText1 = string.Empty;
            interactText2 = string.Empty;
            useText1 = string.Empty;
            useText2 = string.Empty;
        }

        public void SetIcon(IconType type, float size = 1f)
        {
            desiredIconType = type;
            desiredIconScale = size;
        }

        public void RequestCrosshairHide()
        {
            hideCount++;
        }

        public void UnrequestCrosshairHide()
        {
            hideCount--;
        }

        [Obsolete("Use SetInteractText instead")]
        public void SetInteractInfo(string text)
        {
            SetInteractText(text, string.Empty, translate1: true, translate2: false, Hand.None);
        }

        [Obsolete("Use SetInteractText instead")]
        public void SetInteractInfo(string text1, string text2)
        {
            SetInteractText(text1, text2, translate1: true, translate2: true, Hand.None);
        }

        public void SetInteractText(string text)
        {
            SetInteractText(text, string.Empty, translate1: true, translate2: false, Hand.Left);
        }

        public void SetInteractText(string text1, string text2)
        {
            SetInteractText(text1, text2, translate1: true, translate2: true, Hand.Left);
        }

        public void SetInteractText(string text, bool translate, Hand hand = Hand.None)
        {
            SetInteractText(text, string.Empty, translate, translate2: false, hand);
        }

        [Obsolete("Use SetInteractText(.., Hand hand) instead")]
        public void SetInteractText(string text1, string text2, bool translate1, bool translate2, bool addInstructions)
        {
            SetInteractText(text1, text2, translate1, translate2, addInstructions ? Hand.Left : Hand.None);
        }

        public void SetInteractText(string text1, string text2, bool translate1, bool translate2, Hand hand)
        {
            ProfilingUtils.BeginSample("HandReticle.SetInteractText");
            try
            {
                if (!textCache.TryGetValue(text1, out var value))
                {
                    value = new TextCacheData();
                    textCache.Add(text1, value);
                }
                if (value.inputText2 == text2 && value.hand == hand)
                {
                    SetInteractTextRaw(value.outputText1, value.outputText2);
                    return;
                }
                value.inputText2 = text2;
                if (translate1)
                {
                    text1 = Language.main.Get(text1);
                }
                if (translate2)
                {
                    text2 = Language.main.Get(text2);
                }
                switch (hand)
                {
                    case Hand.Left:
                        text1 = Language.main.GetFormat("HandReticleAddButtonFormat", text1, stringLeftHand);
                        break;
                    case Hand.Right:
                        text1 = Language.main.GetFormat("HandReticleAddButtonFormat", text1, stringRightHand);
                        break;
                }
                SetInteractTextRaw(text1, text2);
                value.hand = hand;
                value.outputText1 = interactText1;
                value.outputText2 = interactText2;
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        public void SetInteractTextRaw(string text1, string text2)
        {
            interactText1 = text1;
            interactText2 = text2;
        }

        public void SetUseText(string text1, string text2 = "")
        {
            SetUseText(text1, text2, translate1: true, translate2: true, addBackKeyInfo: true);
        }

        public void SetUseText(string text1, string text2, bool translate1, bool translate2, bool addBackKeyInfo)
        {
            if (!textCache.TryGetValue(text1, out var value))
            {
                value = new TextCacheData();
                textCache.Add(text1, value);
            }
            if (value.inputText2 == text2)
            {
                SetUseTextRaw(value.outputText1, value.outputText2);
                return;
            }
            value.inputText2 = text2;
            if (translate1)
            {
                text1 = Language.main.Get(text1);
            }
            if (translate2)
            {
                text2 = Language.main.Get(text2);
            }
            if (!string.IsNullOrEmpty(text1) && addBackKeyInfo)
            {
                text1 = Language.main.GetFormat("HandReticleAddButtonFormat", text1, stringBack);
            }
            SetUseTextRaw(text1, text2);
            value.outputText1 = useText1;
            value.outputText2 = useText2;
        }

        public void SetUseTextRaw(string text1, string text2)
        {
            useText1 = text1;
            useText2 = text2;
        }

        public void SetTargetDistance(float distance)
        {
            targetDistance = distance;
        }

        public void SetProgress(float progress)
        {
            this.progress = progress;
        }

        private bool ShouldHideAll()
        {
            if (!hideForScreenshots)
            {
                return AvatarInputHandler.main == null;
            }
            return true;
        }

        private void UpdateText(Text uiText, string textString)
        {
            uiText.text = textString;
            uiText.enabled = !ShouldHideAll() && !string.IsNullOrEmpty(textString);
        }

        private void SetIconInternal(IconType newIconType)
        {
            if (iconType != newIconType)
            {
                float duration = ((newIconType == IconType.None) ? 0f : 0.1f);
                if (iconType != 0 && _icons.TryGetValue(iconType, out var value))
                {
                    value.SetActive(active: false, duration);
                }
                iconType = newIconType;
                if (iconType != 0 && _icons.TryGetValue(iconType, out value))
                {
                    value.SetActive(active: true, duration);
                }
            }
        }

        private void UpdateScale()
        {
            Vector3 localScale = iconCanvas.localScale;
            localScale.x = (localScale.y = Mathf.Lerp(localScale.x, Mathf.Clamp(desiredIconScale, 0f, 2f), Time.deltaTime * iconScaleSpeed));
            iconCanvas.localScale = localScale;
        }

        private void UpdateProgress()
        {
            progress = Mathf.Clamp01(progress);
            if (cachedProgress != progress)
            {
                cachedProgress = progress;
                progressImage.fillAmount = progress;
                progressText.text = Language.main.GetFormat("HandReticleProgressPercentFormat", progress);
            }
        }

        private void HideForScreenshots()
        {
            hideForScreenshots = true;
        }

        private void UnhideForScreenshots()
        {
            hideForScreenshots = false;
        }
    }
}
