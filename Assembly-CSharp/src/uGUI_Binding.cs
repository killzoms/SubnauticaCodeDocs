using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_Binding : Selectable, IPointerClickHandler, IEventSystemHandler, ISubmitHandler, ICancelHandler
    {
        public class BindingEvent : UnityEvent<string>
        {
        }

        private bool active;

        private string currentValue;

        public GameInput.Device device;

        public Text currentText;

        private BindingEvent valueChanged = new BindingEvent();

        public string value
        {
            get
            {
                return currentValue;
            }
            set
            {
                if (!Application.isPlaying || !(currentValue == value))
                {
                    currentValue = value;
                    RefreshShownValue();
                    valueChanged.Invoke(currentValue);
                }
            }
        }

        public BindingEvent onValueChanged
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

        protected override void OnEnable()
        {
            base.OnEnable();
            GameInput.OnPrimaryDeviceChanged += OnPrimaryDeviceChanged;
            GameInput.OnBindingsChanged += OnBindingsChanged;
            UpdateHighlightEffect();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameInput.OnPrimaryDeviceChanged -= OnPrimaryDeviceChanged;
            GameInput.OnBindingsChanged -= OnBindingsChanged;
        }

        protected override void Start()
        {
            base.Start();
            RefreshShownValue();
        }

        private void Update()
        {
            if (active)
            {
                string pressedInput = GameInput.GetPressedInput(device);
                if (pressedInput != null && GameInput.IsBindable(pressedInput))
                {
                    SetActive(_active: false);
                    value = pressedInput;
                    GameInput.ClearInput();
                }
            }
            else if (base.gameObject == EventSystem.current.currentSelectedGameObject && GameInput.GetButtonDown(GameInput.Button.UIClear))
            {
                value = null;
            }
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            SetActive(!active);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            SetActive(_active: true);
        }

        public virtual void OnCancel(BaseEventData eventData)
        {
            SetActive(_active: false);
        }

        private void SetActive(bool _active)
        {
            active = _active;
            RefreshShownValue();
        }

        private void RefreshShownValue()
        {
            if (active || currentValue == null)
            {
                currentText.text = "";
            }
            else
            {
                currentText.text = uGUI.GetDisplayTextForBinding(GameInput.GetInputName(currentValue));
            }
        }

        private void OnPrimaryDeviceChanged()
        {
            UpdateHighlightEffect();
        }

        private void OnBindingsChanged()
        {
            RefreshShownValue();
        }

        private void UpdateHighlightEffect()
        {
            if (GameInput.GetPrimaryDevice() == GameInput.Device.Controller)
            {
                base.transition = Transition.SpriteSwap;
                return;
            }
            base.transition = Transition.None;
            base.image.overrideSprite = null;
        }
    }
}
