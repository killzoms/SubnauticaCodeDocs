using Gendarme;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
    [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
    public class uGUI_CameraCyclops : MonoBehaviour
    {
        public static uGUI_CameraCyclops main;

        [AssertNotNull]
        public GameObject content;

        [AssertNotNull]
        public Image fader;

        [AssertNotNull]
        public Text textTitle;

        [AssertNotNull]
        public RectTransform arrow;

        public float transitionEffectDuration = 0.2f;

        private string[] cameraNames = new string[3];

        private string stringControls;

        private int cameraIndex = -1;

        private Sequence sequence = new Sequence();

        private void Awake()
        {
            if (main != null)
            {
                global::UWE.Utils.DestroyWrap(this);
                return;
            }
            main = this;
            sequence.ForceState(state: false);
            content.SetActive(value: false);
        }

        private void OnEnable()
        {
            UpdateTexts();
            GameInput.OnBindingsChanged += OnBindingsChanged;
            Language.main.OnLanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            GameInput.OnBindingsChanged -= OnBindingsChanged;
            Language.main.OnLanguageChanged -= OnLanguageChanged;
        }

        private void Update()
        {
            if (sequence.active)
            {
                sequence.Update();
                if (Player.main != null)
                {
                    float a = 0.5f * (1f - Mathf.Cos((float)System.Math.PI * sequence.t));
                    Color color = fader.color;
                    color.a = a;
                    fader.color = color;
                }
            }
            if (content.activeSelf)
            {
                HandReticle.main.SetUseTextRaw(stringControls, string.Empty);
            }
        }

        private void OnBindingsChanged()
        {
            UpdateBindings();
        }

        private void OnLanguageChanged()
        {
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            UpdateBindings();
            cameraNames[0] = Language.main.Get("CyclopsExternalCam1");
            cameraNames[1] = Language.main.Get("CyclopsExternalCam2");
            cameraNames[2] = Language.main.Get("CyclopsExternalCam3");
        }

        private void UpdateBindings()
        {
            string arg = uGUI.FormatButton(GameInput.Button.CyclePrev);
            string arg2 = uGUI.FormatButton(GameInput.Button.CycleNext);
            string arg3 = uGUI.FormatButton(GameInput.Button.LeftHand);
            string arg4 = uGUI.FormatButton(GameInput.Button.RightHand);
            stringControls = Language.main.GetFormat("CyclopsExternalCamControls", arg, arg2, arg3, arg4);
        }

        public void SetCamera(int index)
        {
            if (cameraIndex != index)
            {
                cameraIndex = index;
                if (cameraIndex >= 0 && cameraIndex < cameraNames.Length)
                {
                    textTitle.text = cameraNames[cameraIndex];
                }
                else
                {
                    textTitle.text = string.Empty;
                }
                bool flag = cameraIndex >= 0;
                if (flag)
                {
                    sequence.ForceState(state: true);
                    sequence.Set(transitionEffectDuration, target: false);
                }
                else
                {
                    sequence.ForceState(state: false);
                }
                content.SetActive(flag);
            }
        }

        public void SetDirection(float angle)
        {
            arrow.localRotation = Quaternion.Euler(0f, 0f, 0f - angle);
        }
    }
}
