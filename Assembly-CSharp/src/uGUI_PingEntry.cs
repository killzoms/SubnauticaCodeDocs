using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(RectTransform))]
    public class uGUI_PingEntry : MonoBehaviour
    {
        [AssertNotNull]
        public Toggle visibility;

        [AssertNotNull]
        public Image visibilityIcon;

        [AssertNotNull]
        public uGUI_Icon icon;

        [AssertNotNull]
        public Text label;

        [AssertNotNull]
        public RectTransform colorSelectionIndicator;

        public Toggle[] colorSelectors;

        [AssertNotNull]
        public Sprite spriteVisible;

        [AssertNotNull]
        public Sprite spriteHidden;

        private RectTransform _rectTransform;

        private int id;

        private List<ISelectable> selectables;

        public RectTransform rectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }

        public void Initialize(int id, bool visible, PingType type, string name, int colorIndex)
        {
            base.gameObject.SetActive(value: true);
            this.id = id;
            visibility.isOn = visible;
            visibilityIcon.sprite = (visible ? spriteVisible : spriteHidden);
            icon.sprite = SpriteManager.Get(SpriteManager.Group.Pings, PingManager.sCachedPingTypeStrings.Get(type));
            UpdateLabel(type, name);
            Color[] colorOptions = PingManager.colorOptions;
            int i = 0;
            for (int num = Mathf.Min(colorSelectors.Length, colorOptions.Length); i < num; i++)
            {
                Toggle obj = colorSelectors[i];
                Graphic targetGraphic = obj.targetGraphic;
                if (targetGraphic != null)
                {
                    targetGraphic.color = colorOptions[i];
                }
                obj.isOn = i == colorIndex;
            }
            icon.color = colorOptions[colorIndex];
            colorSelectionIndicator.position = colorSelectors[colorIndex].targetGraphic.rectTransform.position;
        }

        public void Uninitialize()
        {
            base.gameObject.SetActive(value: false);
            id = 0;
        }

        public void SetColor0(bool isOn)
        {
            if (isOn)
            {
                SetColor(0);
            }
        }

        public void SetColor1(bool isOn)
        {
            if (isOn)
            {
                SetColor(1);
            }
        }

        public void SetColor2(bool isOn)
        {
            if (isOn)
            {
                SetColor(2);
            }
        }

        public void SetColor3(bool isOn)
        {
            if (isOn)
            {
                SetColor(3);
            }
        }

        public void SetColor4(bool isOn)
        {
            if (isOn)
            {
                SetColor(4);
            }
        }

        public void SetVisible(bool visible)
        {
            visibilityIcon.sprite = (visible ? spriteVisible : spriteHidden);
            PingManager.SetVisible(id, visible);
        }

        public void UpdateLabel(PingType type, string name)
        {
            string text = Language.main.Get(PingManager.sCachedPingTypeTranslationStrings.Get(type));
            if (!string.IsNullOrEmpty(name))
            {
                text = $"{text} - {name}";
            }
            label.text = text;
        }

        public void GetSelectables(List<ISelectable> toFill)
        {
            if (selectables == null)
            {
                selectables = new List<ISelectable>();
                selectables.Add(new SelectableWrapper(visibility, delegate(GameInput.Button button)
                {
                    if (button == GameInput.Button.LeftHand)
                    {
                        visibility.isOn = !visibility.isOn;
                        return true;
                    }
                    return false;
                }));
                int i = 0;
                for (int num = colorSelectors.Length; i < num; i++)
                {
                    Toggle selectable = colorSelectors[i];
                    selectables.Add(new SelectableWrapper(selectable, delegate(GameInput.Button button)
                    {
                        if (button == GameInput.Button.LeftHand)
                        {
                            Toggle obj = ((SelectableWrapper)UISelection.selected).selectable as Toggle;
                            obj.isOn = !obj.isOn;
                            return true;
                        }
                        return false;
                    }));
                }
            }
            toFill.AddRange(selectables);
        }

        private void SetColor(int index)
        {
            if (index >= 0 && index < colorSelectors.Length)
            {
                Color color = PingManager.colorOptions[index];
                icon.color = color;
                colorSelectionIndicator.position = colorSelectors[index].targetGraphic.rectTransform.position;
                PingManager.SetColor(id, index);
            }
        }
    }
}
