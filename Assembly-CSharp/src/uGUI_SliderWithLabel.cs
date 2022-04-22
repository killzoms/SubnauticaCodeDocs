using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_SliderWithLabel : MonoBehaviour
    {
        public Slider slider;

        public Text label;

        public float defaultValue = 0.5f;

        private void Start()
        {
            slider.onValueChanged.AddListener(OnValueChanged);
            UpdateLabel();
        }

        private void OnValueChanged(float value)
        {
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (slider.maxValue == 1f)
            {
                label.text = IntStringCache.GetStringForInt(Mathf.RoundToInt(slider.value * 100f));
            }
            else
            {
                label.text = IntStringCache.GetStringForInt(Mathf.RoundToInt(slider.value));
            }
        }
    }
}
