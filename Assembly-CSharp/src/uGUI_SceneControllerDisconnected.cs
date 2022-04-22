using System;
using UnityEngine;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI_SceneControllerDisconnected : MonoBehaviour
    {
        public Text text1;

        public Text text2;

        public Text usernameText;

        private bool initialized;

        private PlatformUtils platformUtils;

        private Language language;

        public void Initialize()
        {
            platformUtils = PlatformUtils.main;
            PlatformUtils obj = platformUtils;
            obj.OnControllerDisconnected = (PlatformUtils.ControllerDisconnectedDelegate)Delegate.Combine(obj.OnControllerDisconnected, new PlatformUtils.ControllerDisconnectedDelegate(OnControllerDisconnected));
            language = Language.main;
            language.OnLanguageChanged += OnLanguageChanged;
            initialized = true;
        }

        private void OnDestroy()
        {
            if (initialized)
            {
                language.OnLanguageChanged -= OnLanguageChanged;
                PlatformUtils obj = platformUtils;
                obj.OnControllerDisconnected = (PlatformUtils.ControllerDisconnectedDelegate)Delegate.Remove(obj.OnControllerDisconnected, new PlatformUtils.ControllerDisconnectedDelegate(OnControllerDisconnected));
            }
        }

        private void Awake()
        {
            UpdateMessageText();
            usernameText.text = PlatformUtils.main.GetLoggedInUserName();
        }

        private void UpdateMessageText()
        {
            text1.text = Language.main.Get("ControllerDisconnected1");
            text2.text = Language.main.Get("ControllerDisconnected2");
        }

        private void OnLanguageChanged()
        {
            UpdateMessageText();
        }

        private void LateUpdate()
        {
            int num = -1;
            for (int i = 0; i < 8; i++)
            {
                int num2 = 350 + i * 20;
                for (int j = 0; j < 20; j++)
                {
                    if (Input.GetKeyUp((KeyCode)(num2 + j)))
                    {
                        num = i;
                    }
                }
            }
            if (num < 0)
            {
                if (Application.platform == RuntimePlatform.PS4)
                {
                    if (Mathf.Max(Input.GetAxis("ControllerAxis8"), 0f) > 0f)
                    {
                        num = 0;
                    }
                }
                else if (Mathf.Max(Input.GetAxis("ControllerAxis3"), 0f) > 0f)
                {
                    num = 0;
                }
                if (Mathf.Max(0f - Input.GetAxis("ControllerAxis3"), 0f) > 0f)
                {
                    num = 0;
                }
            }
            if (num != -1 && PlatformUtils.main.ReconnectController(num))
            {
                base.gameObject.SetActive(value: false);
                if (IngameMenu.main != null)
                {
                    IngameMenu.main.gameObject.GetComponent<Canvas>().enabled = true;
                }
            }
        }

        private void OnControllerDisconnected()
        {
            if (uGUI.main != null && uGUI.main.loading.IsLoading)
            {
                Invoke("OnControllerDisconnected", 0.1f);
                return;
            }
            usernameText.text = PlatformUtils.main.GetLoggedInUserName();
            base.gameObject.SetActive(value: true);
            if (IngameMenu.main != null)
            {
                IngameMenu.main.Open();
                IngameMenu.main.gameObject.GetComponent<Canvas>().enabled = false;
            }
        }
    }
}
