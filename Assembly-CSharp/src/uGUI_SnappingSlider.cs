using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_SnappingSlider : Slider
    {
        public RectTransform defaultValueRect;

        private float snappedValue;

        public float _defaultValue = 0.5f;

        private static Vector3[] fourCorners = new Vector3[4];

        public float defaultValue
        {
            get
            {
                return _defaultValue;
            }
            set
            {
                _defaultValue = value;
                UpdateDefaultValueRect();
            }
        }

        public override float value
        {
            get
            {
                return snappedValue;
            }
            set
            {
                snappedValue = SnapValue(value);
                base.value = value;
            }
        }

        public float normalizedUnsnappedValue
        {
            get
            {
                float num = base.maxValue - base.minValue;
                return (base.value - base.minValue) / num;
            }
            set
            {
                base.normalizedValue = value;
                snappedValue = SnapValue(base.value);
            }
        }

        protected override void Start()
        {
            base.Start();
            snappedValue = base.value;
            UpdateDefaultValueRect();
        }

        private float SnapValue(float value)
        {
            GetComponent<RectTransform>().GetWorldCorners(fourCorners);
            Camera uICamera = ManagedCanvasUpdate.GetUICamera();
            Vector3 vector = uICamera.WorldToScreenPoint(fourCorners[0]);
            float num = Mathf.Abs(uICamera.WorldToScreenPoint(fourCorners[2]).x - vector.x);
            float num2 = 10f;
            float num3 = (base.maxValue - base.minValue) * num2 / num;
            if (Mathf.Abs(value - defaultValue) < num3)
            {
                value = defaultValue;
            }
            return value;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateDefaultValueRect();
        }

        private void UpdateDefaultValueRect()
        {
            if (defaultValueRect != null)
            {
                bool flag = base.direction == Direction.RightToLeft || base.direction == Direction.TopToBottom;
                int index = ((base.direction != 0 && base.direction != Direction.RightToLeft) ? 1 : 0);
                float num = (defaultValue - base.minValue) / (base.maxValue - base.minValue);
                Vector2 zero = Vector2.zero;
                Vector2 one = Vector2.one;
                float num4 = (zero[index] = (one[index] = (flag ? (1f - num) : num)));
                defaultValueRect.anchorMin = zero;
                defaultValueRect.anchorMax = one;
            }
        }
    }
}
