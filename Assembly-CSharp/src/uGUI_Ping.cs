using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [RequireComponent(typeof(RectTransform))]
    public class uGUI_Ping : MonoBehaviour
    {
        [AssertNotNull]
        public uGUI_Icon icon;

        [AssertNotNull]
        public Text infoText;

        [AssertNotNull]
        public Text distanceText;

        [AssertNotNull]
        public Text suffixText;

        private RectTransform _rectTransform;

        private string _label;

        private int _distance = int.MinValue;

        private Material _iconMaterial;

        private Material _textMaterial;

        private bool _initialized;

        private Color _iconColor = Color.white;

        private Color _textColor = Color.white;

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

        public float currentFadeAlpha { get; set; }

        private void Awake()
        {
            _iconMaterial = new Material(icon.material);
            icon.material = _iconMaterial;
            _textMaterial = new Material(icon.material);
            infoText.material = _textMaterial;
            distanceText.material = _textMaterial;
            suffixText.material = _textMaterial;
            currentFadeAlpha = -1f;
        }

        public void Initialize()
        {
            _initialized = true;
        }

        public void Uninitialize()
        {
            _initialized = false;
            UpdateIconColor();
            UpdateTextColor();
        }

        public void SetColor(Color color)
        {
            _iconColor.r = color.r;
            _iconColor.g = color.g;
            _iconColor.b = color.b;
            _textColor.r = color.r;
            _textColor.g = color.g;
            _textColor.b = color.b;
            UpdateIconColor();
            UpdateTextColor();
        }

        public void SetIconAlpha(float alpha)
        {
            if (_iconColor.a != alpha)
            {
                _iconColor.a = alpha;
                UpdateIconColor();
            }
        }

        public void SetTextAlpha(float newAlpha)
        {
            if (_textColor.a != newAlpha)
            {
                _textColor.a = newAlpha;
                UpdateTextColor();
            }
        }

        public void SetIcon(Atlas.Sprite sprite)
        {
            icon.sprite = sprite;
        }

        public void SetLabel(string value)
        {
            _label = ((value != null) ? value : string.Empty);
            UpdateText();
        }

        public void SetDistance(float distance)
        {
            int num = Mathf.RoundToInt(distance);
            if (_distance != num)
            {
                _distance = num;
                UpdateText();
            }
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                _iconColor.a = 0f;
                _textColor.a = 0f;
            }
            UpdateIconColor();
            UpdateTextColor();
        }

        private void UpdateText()
        {
            string stringForInt = IntStringCache.GetStringForInt(_distance);
            if (string.IsNullOrEmpty(_label))
            {
                infoText.text = string.Empty;
                distanceText.text = stringForInt;
                suffixText.text = Language.main.Get("MeterSuffix");
            }
            else
            {
                infoText.text = _label;
                distanceText.text = stringForInt;
                suffixText.text = Language.main.Get("MeterSuffix");
            }
        }

        private void UpdateIconColor()
        {
            Color iconColor = _iconColor;
            iconColor.a = (_initialized ? iconColor.a : 0f);
            _iconMaterial.SetColor(ShaderPropertyID._Color, iconColor);
        }

        private void UpdateTextColor()
        {
            Color textColor = _textColor;
            textColor.a = (_initialized ? textColor.a : 0f);
            _textMaterial.SetColor(ShaderPropertyID._Color, textColor);
        }
    }
}
