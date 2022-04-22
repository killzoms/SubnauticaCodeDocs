using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_Choice : Selectable
    {
        public class ChoiceEvent : UnityEvent<int>
        {
        }

        private List<string> options;

        private int currentIndex = -1;

        public Text currentText;

        public Button previousButton;

        public Button nextButton;

        private ChoiceEvent valueChanged = new ChoiceEvent();

        public int value
        {
            get
            {
                return currentIndex;
            }
            set
            {
                if (!Application.isPlaying || (value != currentIndex && options.Count != 0))
                {
                    currentIndex = Mathf.Clamp(value, 0, options.Count - 1);
                    RefreshShownValue();
                    valueChanged.Invoke(currentIndex);
                }
            }
        }

        public ChoiceEvent onValueChanged
        {
            get
            {
                return valueChanged;
            }
            set
            {
                valueChanged = value;
            }
        }

        public void SetOptions(string[] _options)
        {
            options = new List<string>(_options);
            currentIndex = -1;
            RefreshShownValue();
        }

        protected override void Start()
        {
            base.Start();
            RefreshShownValue();
            nextButton.onClick.AddListener(NextChoice);
            previousButton.onClick.AddListener(PreviousChoice);
        }

        private void RefreshShownValue()
        {
            if (currentIndex == -1)
            {
                currentText.text = "";
                return;
            }
            currentText.text = Language.main.Get(options[currentIndex]);
            currentText.gameObject.GetComponentInChildren<TranslationLiveUpdate>().translationKey = options[currentIndex];
        }

        public void NextChoice()
        {
            value = (value + 1) % options.Count;
            RefreshShownValue();
        }

        public void PreviousChoice()
        {
            value = (value + options.Count - 1) % options.Count;
            RefreshShownValue();
        }
    }
}
