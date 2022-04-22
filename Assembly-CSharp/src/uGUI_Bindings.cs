using UnityEngine;
using UnityEngine.Events;

namespace AssemblyCSharp
{
    public class uGUI_Bindings : MonoBehaviour
    {
        public uGUI_Binding[] bindings;

        private GameInput.Button button;

        private GameInput.Device device;

        private void Start()
        {
        }

        private void OnEnable()
        {
            GameInput.OnBindingsChanged += OnBindingsChanged;
            RefreshValue();
        }

        private void OnDisable()
        {
            GameInput.OnBindingsChanged -= OnBindingsChanged;
        }

        public void Initialize(GameInput.Device _device, GameInput.Button _button)
        {
            button = _button;
            device = _device;
            for (int i = 0; i < bindings.Length; i++)
            {
                uGUI_Binding obj = bindings[i];
                obj.device = device;
                GameInput.BindingSet bindingSet = (GameInput.BindingSet)i;
                UnityAction<string> call = delegate(string input)
                {
                    GameInput.SetBinding(device, button, bindingSet, input);
                };
                obj.onValueChanged.AddListener(call);
            }
            RefreshValue();
        }

        public void RefreshValue()
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i].value = GameInput.GetBinding(device, button, (GameInput.BindingSet)i);
            }
        }

        private void OnBindingsChanged()
        {
            RefreshValue();
        }
    }
}
