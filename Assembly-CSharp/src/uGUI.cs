using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AssemblyCSharp
{
    public class uGUI : MonoBehaviour, ICanvasElement
    {
        private static Dictionary<string, string> buttonCharacters = new Dictionary<string, string>
        {
            { "ControllerButtonA", "\ue016" },
            { "ControllerButtonB", "\ue011" },
            { "ControllerButtonX", "\ue018" },
            { "ControllerButtonY", "\ue008" },
            { "ControllerButtonLeftBumper", "\ue005" },
            { "ControllerButtonRightBumper", "\ue019" },
            { "ControllerButtonBack", "\ue00d" },
            { "ControllerButtonHome", "\ue00b" },
            { "ControllerButtonLeftStick", "\ue006" },
            { "ControllerButtonRightStick", "\ue017" },
            { "ControllerLeftTrigger", "\ue009" },
            { "ControllerRightTrigger", "\ue015" },
            { "ControllerDPadRight", "\ue000" },
            { "ControllerDPadLeft", "\ue00f" },
            { "ControllerDPadUp", "\ue012" },
            { "ControllerDPadDown", "\ue00e" },
            { "ControllerRightStick", "\ue002" },
            { "ControllerLeftStick", "\ue007" },
            { "ControllerLeftStickUp", "\ue067" },
            { "ControllerLeftStickDown", "\ue068" },
            { "ControllerLeftStickLeft", "\ue069" },
            { "ControllerLeftStickRight", "\ue06a" },
            { "ControllerRightStickUp", "\ue06b" },
            { "ControllerRightStickDown", "\ue06c" },
            { "ControllerRightStickLeft", "\ue06d" },
            { "ControllerRightStickRight", "\ue06e" },
            { "ControllerButtonPs4Cross", "\ue031" },
            { "ControllerButtonPs4Circle", "\ue036" },
            { "ControllerButtonPs4Triangle", "\ue028" },
            { "ControllerButtonPs4Square", "\ue038" },
            { "ControllerButtonPs4Share", "\ue022" },
            { "ControllerButtonPs4Options", "\ue029" },
            { "ControllerButtonPs4TouchPad", "\ue040" },
            { "ControllerPs4L1", "\ue025" },
            { "ControllerPs4L2", "\ue026" },
            { "ControllerPs4R1", "\ue02b" },
            { "ControllerPs4R2", "\ue039" },
            { "ControllerButtonPs4LeftStick", "\ue01a" },
            { "ControllerButtonPs4RightStick", "\ue01b" },
            { "ControllerPs4DPadRight", "\ue020" },
            { "ControllerPs4DPadLeft", "\ue02f" },
            { "ControllerPs4DPadUp", "\ue032" },
            { "ControllerPs4DPadDown", "\ue02e" },
            { "MouseButtonLeft", "\ue03b" },
            { "MouseButtonRight", "\ue03c" },
            { "MouseButtonMiddle", "\ue03d" },
            { "MouseWheelUp", "\ue03e" },
            { "MouseWheelDown", "\ue03f" }
        };

        private const string mainLevelName = "Main";

        private const string uGUIPrefabPath = "uGUI";

        private static bool isTerminating = false;

        private static uGUI _main;

        private static int _isMainLevel = -1;

        [AssertNotNull]
        public Canvas screenCanvas;

        [AssertNotNull]
        public uGUI_SceneHUD hud;

        [AssertNotNull]
        public uGUI_SceneLoading loading;

        [AssertNotNull]
        public uGUI_SceneRespawning respawning;

        [AssertNotNull]
        public uGUI_SceneIntro intro;

        [AssertNotNull]
        public uGUI_HardcoreGameOver hardcoreGameOver;

        [AssertNotNull]
        public uGUI_UserInput userInput;

        [AssertNotNull]
        public uGUI_QuickSlots quickSlots;

        [AssertNotNull]
        public uGUI_Overlays overlays;

        [AssertNotNull]
        public uGUI_SceneConfirmation confirmation;

        [AssertNotNull]
        public uGUI_SceneControllerDisconnected controllerDisconnected;

        [AssertNotNull]
        public uGUI_ItemSelector itemSelector;

        [AssertNotNull]
        public uGUI_CraftingMenu craftingMenu;

        public GameObject barsPanel;

        public const string formatButton = "<color=#ADF8FFFF>{0}</color>";

        private static StringBuilder sb = new StringBuilder();

        public static uGUI main
        {
            get
            {
                if (_main == null && !isTerminating)
                {
                    Initialize();
                }
                return _main;
            }
        }

        public static bool isMainLevel
        {
            get
            {
                if (_isMainLevel == -1)
                {
                    UpdateLevelIdentifier();
                }
                return _isMainLevel == 1;
            }
        }

        public static bool isIntro
        {
            get
            {
                if (!(main == null) && !(main.intro == null) && !main.intro.showing)
                {
                    return IntroVignette.isIntroActive;
                }
                return true;
            }
        }

        public static bool isLoading
        {
            get
            {
                if (main != null)
                {
                    return main.loading.IsLoading;
                }
                return false;
            }
        }

        public void Rebuild(CanvasUpdate executing)
        {
        }

        public void LayoutComplete()
        {
            ProfilingUtils.BeginSample("Canvas Graphic Update");
        }

        public void GraphicUpdateComplete()
        {
            ProfilingUtils.EndSample();
        }

        public bool IsDestroyed()
        {
            return this == null;
        }

        private void Update()
        {
            CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(this);
            CanvasUpdateRegistry.TryRegisterCanvasElementForGraphicRebuild(this);
        }

        private void Awake()
        {
            if (_main != null)
            {
                Debug.LogError("Multiple uGUI instances found in scene!", this);
                global::UnityEngine.Object.DestroyImmediate(base.gameObject);
                return;
            }
            _main = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            global::UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
            if ((bool)controllerDisconnected)
            {
                controllerDisconnected.Initialize();
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            UpdateLevelIdentifier();
        }

        private void OnApplicationQuit()
        {
            Deinitialize();
        }

        public static void Initialize()
        {
            if (!(_main != null))
            {
                GameObject gameObject = Resources.Load<GameObject>("uGUI");
                if (gameObject == null)
                {
                    Debug.LogError("Cannot find main uGUI prefab in Resources folder at path 'uGUI'");
                    Debug.Break();
                }
                else
                {
                    global::UnityEngine.Object.Instantiate(gameObject);
                }
            }
        }

        public static void Deinitialize()
        {
            isTerminating = true;
            if (_main != null)
            {
                global::UWE.Utils.DestroyWrap(_main.gameObject);
                _main = null;
            }
        }

        public void SetVisible(bool visible)
        {
            Canvas[] componentsInChildren = GetComponentsInChildren<Canvas>();
            foreach (Canvas canvas in componentsInChildren)
            {
                if (!(canvas.sortingLayerName == "DepthClear"))
                {
                    canvas.enabled = visible;
                }
            }
        }

        public static string GetDisplayTextForBinding(string bindingName)
        {
            if (!buttonCharacters.TryGetValue(bindingName, out var value))
            {
                return Language.main.Get(bindingName);
            }
            return value;
        }

        public static string FormatButton(GameInput.Button button, bool allBindingSets = false, string bindingSeparator = " / ", bool gamePadOnly = false)
        {
            sb.Length = 0;
            string text = null;
            int numBindingSets = GameInput.GetNumBindingSets();
            for (int i = 0; i < numBindingSets; i++)
            {
                text = GameInput.GetBindingName(button, (GameInput.BindingSet)i, gamePadOnly);
                if (text != null)
                {
                    string displayTextForBinding = GetDisplayTextForBinding(text);
                    if (i > 0)
                    {
                        sb.Append(bindingSeparator);
                    }
                    sb.AppendFormat("<color=#ADF8FFFF>{0}</color>", displayTextForBinding);
                    if (!allBindingSets)
                    {
                        break;
                    }
                }
            }
            if (sb.Length == 0)
            {
                sb.AppendFormat("<color=#ADF8FFFF>{0}</color>", Language.main.Get("NoInputAssigned"));
            }
            return sb.ToString();
        }

        private void HideForScreenshots()
        {
            SetVisible(visible: false);
        }

        private void UnhideForScreenshots()
        {
            SetVisible(visible: true);
        }

        private static void UpdateLevelIdentifier()
        {
            if (Application.loadedLevelName.StartsWith("Main", StringComparison.Ordinal))
            {
                _isMainLevel = 1;
            }
            else
            {
                _isMainLevel = 0;
            }
        }

        [SpecialName]
        Transform ICanvasElement.transform => transform;
    }
}
