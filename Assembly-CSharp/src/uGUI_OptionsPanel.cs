using System;
using System.Collections.Generic;
using System.Linq;
using Gendarme;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;

namespace AssemblyCSharp
{
    public class uGUI_OptionsPanel : uGUI_TabbedControlsPanel
    {
        public class ResolutionEqualityComparer : IEqualityComparer<Resolution>
        {
            public bool Equals(Resolution x, Resolution y)
            {
                if (x.width == y.width)
                {
                    return x.height == y.height;
                }
                return false;
            }

            public int GetHashCode(Resolution obj)
            {
                Resolution resolution = obj;
                resolution.refreshRate = 0;
                return resolution.GetHashCode();
            }
        }

        public bool showLanguageOption = true;

        private string[] languages;

        private Toggle subtitlesOption;

        private List<Resolution> resolutions;

        private float vrRenderScale;

        private Resolution resolution;

        public Button applyButton;

        public Text terrainChangeRequiresRestartText;

        public Text displayChangeRequiresRestartText;

        public GameObject bindingsHeaderPrefab;

        public GameObject keyRedemptionPrefab;

        public GameObject troubleshootingPrefab;

        private uGUI_Choice controllerMoveStickOption;

        private uGUI_Choice controllerLookStickOption;

        private uGUI_Choice qualityPresetOption;

        private uGUI_Choice detailOption;

        private uGUI_Choice aaModeOption;

        private uGUI_Choice waterQualityOption;

        private uGUI_Choice aaQualityOption;

        private Toggle bloomOption;

        private Toggle lensDirtOption;

        private Toggle dofOption;

        private uGUI_Choice motionBlurQualityOption;

        private uGUI_Choice aoQualityOption;

        private uGUI_Choice ssrQualityOption;

        private Toggle ditheringOption;

        private bool m_syncingGraphicsSettings;

        private static string[] GetLanguageOptions(out int currentIndex)
        {
            string currentLanguage = Language.main.GetCurrentLanguage();
            string[] array = Language.main.GetLanguages();
            currentIndex = Array.IndexOf(array, currentLanguage);
            return array;
        }

        private static string[] GetDisplayOptions(out int currentIndex)
        {
            currentIndex = -1;
            Display[] displays = Display.displays;
            string[] array = new string[displays.Length];
            for (int i = 0; i < displays.Length; i++)
            {
                array[i] = (i + 1).ToString();
                if (Display.main == displays[i])
                {
                    currentIndex = i;
                }
            }
            return array;
        }

        private static string[] GetAntiAliasingOptions(out int currentIndex)
        {
            currentIndex = UwePostProcessingManager.GetAaMode();
            return new string[2] { "FXAA", "TAA" };
        }

        private static string[] GetColorGradingOptions(out int currentIndex)
        {
            currentIndex = UwePostProcessingManager.GetColorGradingMode();
            return new string[3] { "Off", "Neutral", "Filmic" };
        }

        private static string[] GetPostFXQualityNames()
        {
            return new string[4] { "Off", "Low", "Medium", "High" };
        }

        private static string[] GetResolutionOptions(out List<Resolution> resolutions, out int currentIndex)
        {
            resolutions = new List<Resolution>();
            resolutions.AddRange(Screen.resolutions.Distinct(new ResolutionEqualityComparer()));
            string[] array = new string[resolutions.Count];
            Resolution desktopResolution = EditorModifications.desktopResolution;
            float a = (float)desktopResolution.width / (float)desktopResolution.height;
            currentIndex = -1;
            for (int i = 0; i < resolutions.Count; i++)
            {
                Resolution resolution = resolutions[i];
                float b = (float)resolution.width / (float)resolution.height;
                if (Mathf.Approximately(a, b))
                {
                    array[i] = $"{resolution.width} x {resolution.height} *";
                }
                else
                {
                    array[i] = $"{resolution.width} x {resolution.height}";
                }
                if (Screen.width == resolution.width && Screen.height == resolution.height)
                {
                    currentIndex = i;
                }
            }
            return array;
        }

        private static string[] GetDetailOptions(out int currentIndex)
        {
            currentIndex = QualitySettings.GetQualityLevel();
            return QualitySettings.names;
        }

        public override void Awake()
        {
            base.Awake();
        }

        private void OnLanguageChanged(int currentIndex)
        {
            string text = languages[currentIndex];
            Language.main.SetCurrentLanguage(text);
            if (subtitlesOption != null)
            {
                subtitlesOption.isOn = !string.Equals(text, "English", StringComparison.OrdinalIgnoreCase);
            }
        }

        private void OnShowSubtitlesChanged(bool showSubtitles)
        {
            Language.main.showSubtitles = showSubtitles;
        }

        private void OnSubtitlesSpeedChanged(float speed)
        {
            Subtitles.main.speed = speed;
        }

        private void OnSoundDeviceChanged(int currentIndex)
        {
            SoundSystem.SetDevice(currentIndex);
        }

        private void OnResolutionChanged(int currentIndex)
        {
            resolution = resolutions[currentIndex];
            EnableApplyButton();
        }

        private void OnDisplayChanged(int currentIndex)
        {
            _ = Display.displays[currentIndex];
            PlayerPrefs.SetInt("UnitySelectMonitor", currentIndex);
            SyncDisplayChangeRequiresRestartText();
        }

        private void OnFullscreenChanged(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }

        private void OnVSyncChanged(bool vsync)
        {
            GraphicsUtil.SetVSyncEnabled(vsync);
        }

        private void OnQualityPresetChanged(int option)
        {
            GraphicsPreset[] presets = GraphicsPreset.GetPresets();
            if (option < presets.Length)
            {
                presets[option].Apply();
                SyncGraphicsSettingsSelection();
            }
            SyncTerrainChangeRequiresRestartText();
        }

        private void OnDetailChanged(int currentIndex)
        {
            GraphicsUtil.SetQualityLevel(currentIndex);
            SyncQualityPresetSelection();
        }

        private void OnWaterQualityChanged(WaterSurface.Quality currentQuality)
        {
            WaterSurface.SetQuality(currentQuality);
            SyncQualityPresetSelection();
        }

        private void OnAAmodeChanged(int mode)
        {
            UwePostProcessingManager.SetAaMode(mode);
            SyncQualityPresetSelection();
        }

        private void OnAAqualityChanged(int currentQuality)
        {
            UwePostProcessingManager.SetAaQuality(currentQuality);
            SyncQualityPresetSelection();
        }

        private void OnAOqualityChanged(int currentQuality)
        {
            UwePostProcessingManager.SetAoQuality(currentQuality);
            SyncQualityPresetSelection();
        }

        private void OnSSRqualityChanged(int currentQuality)
        {
            UwePostProcessingManager.SetSsrQuality(currentQuality);
            SyncQualityPresetSelection();
        }

        private void OnBloomChanged(bool enableComp)
        {
            UwePostProcessingManager.ToggleBloom(enableComp);
            SyncQualityPresetSelection();
        }

        private void OnBloomLensDirtChanged(bool enableComp)
        {
            UwePostProcessingManager.ToggleBloomLensDirt(enableComp);
            SyncQualityPresetSelection();
        }

        private void OnDofChanged(bool enableComp)
        {
            UwePostProcessingManager.ToggleDof(enableComp);
            SyncQualityPresetSelection();
        }

        private void OnMotionBlurQualityChanged(int currentQuality)
        {
            UwePostProcessingManager.SetMotionBlurQuality(currentQuality);
            SyncQualityPresetSelection();
        }

        private void OnDitheringChanged(bool enableComp)
        {
            UwePostProcessingManager.ToggleDithering(enableComp);
            SyncQualityPresetSelection();
        }

        private void OnColorGradingChanged(int mode)
        {
            UwePostProcessingManager.SetColorGradingMode(mode);
            SyncQualityPresetSelection();
        }

        private void OnMouseSensitivityChanged(float mouseSensitivity)
        {
            GameInput.SetMouseSensitivity(mouseSensitivity);
        }

        private void OnInvertMouseChanged(bool invertMouse)
        {
            GameInput.SetInvertMouse(invertMouse);
        }

        private void OnControllerEnabledChanged(bool controllerEnabled)
        {
            GameInput.SetControllerEnabled(controllerEnabled);
        }

        private string[] GetControllerLayoutOptions()
        {
            List<string> list = new List<string>();
            foreach (object value in Enum.GetValues(typeof(GameInput.ControllerLayout)))
            {
                list.Add("ControllerLayout" + value.ToString());
            }
            return list.ToArray();
        }

        private void OnControllerLayoutChanged(int layoutIndex)
        {
            GameInput.SetChosenControllerLayout((GameInput.ControllerLayout)layoutIndex);
        }

        private void OnGazeBasedCursorChanged(bool gazeBasedCursor)
        {
            VROptions.gazeBasedCursor = gazeBasedCursor;
        }

        private void OnControllerHorizontalSensitivityChanged(float sensitivity)
        {
            GameInput.SetControllerHorizontalSensitivity(sensitivity);
        }

        private void OnControllerVerticalSensitivityChanged(float sensitivity)
        {
            GameInput.SetControllerVerticalSensitivity(sensitivity);
        }

        private void OnInvertControllerChanged(bool invertController)
        {
            GameInput.SetInvertController(invertController);
        }

        private void OnVRRenderScaleChanged(float value)
        {
            vrRenderScale = value;
            EnableApplyButton();
        }

        [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
        private void OnFovChanged(float value)
        {
            MiscSettings.fieldOfView = value;
            if (SNCameraRoot.main != null)
            {
                SNCameraRoot.main.SyncFieldOfView();
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            SoundSystem.SetMasterVolume(value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            SoundSystem.SetMusicVolume(value);
        }

        private void OnVoiceVolumeChanged(float value)
        {
            SoundSystem.SetVoiceVolume(value);
        }

        private void OnAmbientVolumeChanged(float value)
        {
            SoundSystem.SetAmbientVolume(value);
        }

        private void SyncGraphicsSettingsSelection()
        {
            m_syncingGraphicsSettings = true;
            if (detailOption != null)
            {
                detailOption.value = QualitySettings.GetQualityLevel();
            }
            if (waterQualityOption != null)
            {
                waterQualityOption.value = (int)WaterSurface.GetQuality();
            }
            if (aaModeOption != null)
            {
                aaModeOption.value = UwePostProcessingManager.GetAaMode();
            }
            if (aaQualityOption != null)
            {
                aaQualityOption.value = UwePostProcessingManager.GetAaQuality();
            }
            if (aoQualityOption != null)
            {
                aoQualityOption.value = UwePostProcessingManager.GetAoQuality();
            }
            if (ssrQualityOption != null)
            {
                ssrQualityOption.value = UwePostProcessingManager.GetSsrQuality();
            }
            if (bloomOption != null)
            {
                bloomOption.isOn = UwePostProcessingManager.GetBloomEnabled();
            }
            if (lensDirtOption != null)
            {
                lensDirtOption.isOn = UwePostProcessingManager.GetBloomLensDirtEnabled();
            }
            if (dofOption != null)
            {
                dofOption.isOn = UwePostProcessingManager.GetDofEnabled();
            }
            if (motionBlurQualityOption != null)
            {
                motionBlurQualityOption.value = UwePostProcessingManager.GetMotionBlurQuality();
            }
            if (ditheringOption != null)
            {
                ditheringOption.isOn = UwePostProcessingManager.GetDitheringEnabled();
            }
            m_syncingGraphicsSettings = false;
        }

        private void SyncTerrainChangeRequiresRestartText()
        {
            if (terrainChangeRequiresRestartText != null && LargeWorldStreamer.main != null)
            {
                bool active = LargeWorldStreamer.main.streamerV2.GetActiveQualityLevel() != QualitySettings.GetQualityLevel();
                terrainChangeRequiresRestartText.gameObject.SetActive(active);
            }
        }

        private void SyncDisplayChangeRequiresRestartText()
        {
            if (!(displayChangeRequiresRestartText != null))
            {
                return;
            }
            int num = 0;
            Display[] displays = Display.displays;
            for (int i = 0; i < Display.displays.Length; i++)
            {
                if (Display.main == displays[i])
                {
                    num = i;
                    break;
                }
            }
            bool active = PlayerPrefs.GetInt("UnitySelectMonitor") != num;
            displayChangeRequiresRestartText.gameObject.SetActive(active);
        }

        private void SyncQualityPresetSelection()
        {
            if (!m_syncingGraphicsSettings && qualityPresetOption != null)
            {
                GraphicsPreset[] presets = GraphicsPreset.GetPresets();
                int num = GraphicsPreset.GetPresetIndexForCurrentOptions();
                if (num == -1)
                {
                    num = presets.Length;
                }
                qualityPresetOption.value = num;
            }
        }

        private void Start()
        {
            applyButton.onClick.AddListener(OnApplyButton);
            SyncTerrainChangeRequiresRestartText();
            SyncDisplayChangeRequiresRestartText();
        }

        private void EnableApplyButton()
        {
            applyButton.gameObject.SetActive(value: true);
        }

        private void OnApplyButton()
        {
            XRSettings.eyeTextureResolutionScale = vrRenderScale;
            DisplayManager.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
            applyButton.gameObject.SetActive(value: false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            applyButton.gameObject.SetActive(value: false);
            vrRenderScale = XRSettings.eyeTextureResolutionScale;
            resolution = Screen.currentResolution;
            AddTabs();
            HighlightCurrentTab();
        }

        private void OnDisable()
        {
            RemoveTabs();
        }

        private void AddTabs()
        {
            AddGeneralTab();
            AddGraphicsTab();
            if (GameInput.IsKeyboardAvailable())
            {
                AddKeyboardTab();
            }
            if (GameInput.IsControllerAvailable())
            {
                AddControllerTab();
            }
            if (!PlatformUtils.isConsolePlatform)
            {
                AddTroubleshootingTab();
            }
        }

        private void AddKeyboardTab()
        {
            int tabIndex = AddTab("Keyboard");
            AddToggleOption(tabIndex, "InvertLook", GameInput.GetInvertMouse(), OnInvertMouseChanged);
            AddSliderOption(tabIndex, "MouseSensitivity", GameInput.GetMouseSensitivity(), 0.15f, OnMouseSensitivityChanged);
            AddItem(tabIndex, bindingsHeaderPrefab);
            AddBindings(tabIndex, GameInput.Device.Keyboard);
        }

        private void AddControllerTab()
        {
            int tabIndex = AddTab("Controller");
            if (GameInput.IsKeyboardAvailable())
            {
                AddToggleOption(tabIndex, "EnableController", GameInput.GetControllerEnabled(), OnControllerEnabledChanged);
            }
            if (XRSettings.enabled)
            {
                AddToggleOption(tabIndex, "GazeBasedCursor", VROptions.gazeBasedCursor, OnGazeBasedCursorChanged);
            }
            if (!PlatformUtils.isConsolePlatform)
            {
                GameInput.ControllerLayout chosenControllerLayout = GameInput.GetChosenControllerLayout();
                AddChoiceOption(tabIndex, "ControllerLayout", GetControllerLayoutOptions(), (int)chosenControllerLayout, OnControllerLayoutChanged);
            }
            AddToggleOption(tabIndex, "InvertLook", GameInput.GetInvertController(), OnInvertControllerChanged);
            AddSliderOption(tabIndex, "HorizontalSensitivity", GameInput.GetControllerHorizontalSensitivity(), 0.405f, OnControllerHorizontalSensitivityChanged);
            AddSliderOption(tabIndex, "VerticalSensitivity", GameInput.GetControllerVerticalSensitivity(), 0.405f, OnControllerVerticalSensitivityChanged);
            AddItem(tabIndex, bindingsHeaderPrefab);
            AddBindings(tabIndex, GameInput.Device.Controller);
        }

        [SuppressMessage("Subnautica.Rules", "AvoidStringConcatenation")]
        private void AddGeneralTab()
        {
            int tabIndex = AddTab("General");
            if (showLanguageOption)
            {
                languages = GetLanguageOptions(out var currentIndex);
                string[] array = new string[languages.Length];
                for (int i = 0; i < languages.Length; i++)
                {
                    array[i] = "Language" + languages[i];
                }
                AddChoiceOption(tabIndex, "Language", array, currentIndex, OnLanguageChanged);
            }
            AddHeading(tabIndex, "Subtitles");
            subtitlesOption = AddToggleOption(tabIndex, "SubtitlesEnabled", Language.main.showSubtitles, OnShowSubtitlesChanged);
            AddSliderOption(tabIndex, "SubtitlesSpeed", Subtitles.main.speed, 1f, 100f, 15f, OnSubtitlesSpeedChanged);
            if (XRSettings.enabled || (!XRSettings.enabled && !PlatformUtils.isConsolePlatform))
            {
                AddHeading(tabIndex, "Display");
            }
            if (!XRSettings.enabled)
            {
                if (!PlatformUtils.isConsolePlatform)
                {
                    int currentIndex2;
                    string[] resolutionOptions = GetResolutionOptions(out resolutions, out currentIndex2);
                    AddChoiceOption(tabIndex, "Resolution", resolutionOptions, currentIndex2, OnResolutionChanged);
                    AddToggleOption(tabIndex, "Fullscreen", Screen.fullScreen, OnFullscreenChanged);
                    AddToggleOption(tabIndex, "Vsync", QualitySettings.vSyncCount > 0, OnVSyncChanged);
                    int currentIndex3;
                    string[] displayOptions = GetDisplayOptions(out currentIndex3);
                    AddChoiceOption(tabIndex, "Display", displayOptions, currentIndex3, OnDisplayChanged);
                    AddSliderOption(tabIndex, "FieldOfView", MiscSettings.fieldOfView, 40f, 90f, 60f, OnFovChanged);
                }
            }
            else
            {
                AddSliderOption(tabIndex, "VRRenderScale", XRSettings.eyeTextureResolutionScale, GameSettings.GetMinVrRenderScale(), 1f, 1f, OnVRRenderScaleChanged);
            }
            AddHeading(tabIndex, "Sound");
            AddSliderOption(tabIndex, "MasterVolume", SoundSystem.GetMasterVolume(), 1f, OnMasterVolumeChanged);
            AddSliderOption(tabIndex, "MusicVolume", SoundSystem.GetMusicVolume(), 1f, OnMusicVolumeChanged);
            AddSliderOption(tabIndex, "VoiceVolume", SoundSystem.GetVoiceVolume(), 1f, OnVoiceVolumeChanged);
            AddSliderOption(tabIndex, "AmbientVolume", SoundSystem.GetAmbientVolume(), 1f, OnAmbientVolumeChanged);
        }

        private void AddGraphicsTab()
        {
            int tabIndex = AddTab("Graphics");
            if (!PlatformUtils.isConsolePlatform)
            {
                qualityPresetOption = AddChoiceOption(tabIndex, "Preset", new string[4] { "Low", "Medium", "High", "Custom" }, 0, OnQualityPresetChanged);
                SyncQualityPresetSelection();
            }
            int currentIndex;
            string[] colorGradingOptions = GetColorGradingOptions(out currentIndex);
            AddChoiceOption(tabIndex, "ColorGrading", colorGradingOptions, currentIndex, OnColorGradingChanged);
            if (!PlatformUtils.isConsolePlatform)
            {
                AddHeading(tabIndex, "Advanced");
                if ((bool)uGUI_MainMenu.main)
                {
                    int currentIndex2;
                    string[] detailOptions = GetDetailOptions(out currentIndex2);
                    detailOption = AddChoiceOption(tabIndex, "Detail", detailOptions, currentIndex2, OnDetailChanged);
                }
                waterQualityOption = AddChoiceOption(tabIndex, "WaterQuality", WaterSurface.GetQualityOptions(), WaterSurface.GetQuality(), OnWaterQualityChanged);
                int currentIndex3;
                string[] antiAliasingOptions = GetAntiAliasingOptions(out currentIndex3);
                aaModeOption = AddChoiceOption(tabIndex, "Antialiasing", antiAliasingOptions, currentIndex3, OnAAmodeChanged);
                string[] postFXQualityNames = GetPostFXQualityNames();
                aaQualityOption = AddChoiceOption(tabIndex, "AntialiasingQuality", postFXQualityNames, UwePostProcessingManager.GetAaQuality(), OnAAqualityChanged);
                bloomOption = AddToggleOption(tabIndex, "Bloom", UwePostProcessingManager.GetBloomEnabled(), OnBloomChanged);
                if (!XRSettings.enabled)
                {
                    lensDirtOption = AddToggleOption(tabIndex, "LensDirt", UwePostProcessingManager.GetBloomLensDirtEnabled(), OnBloomLensDirtChanged);
                    dofOption = AddToggleOption(tabIndex, "DepthOfField", UwePostProcessingManager.GetDofEnabled(), OnDofChanged);
                    motionBlurQualityOption = AddChoiceOption(tabIndex, "MotionBlurQuality", postFXQualityNames, UwePostProcessingManager.GetMotionBlurQuality(), OnMotionBlurQualityChanged);
                }
                aoQualityOption = AddChoiceOption(tabIndex, "AmbientOcclusion", postFXQualityNames, UwePostProcessingManager.GetAoQuality(), OnAOqualityChanged);
                if (!XRSettings.enabled)
                {
                    ssrQualityOption = AddChoiceOption(tabIndex, "ScreenSpaceReflections", postFXQualityNames, UwePostProcessingManager.GetSsrQuality(), OnSSRqualityChanged);
                    ditheringOption = AddToggleOption(tabIndex, "Dithering", UwePostProcessingManager.GetDitheringEnabled(), OnDitheringChanged);
                }
            }
        }

        private void AddKeyRedemptionTab()
        {
            int tabIndex = AddTab("KeyRedemption");
            AddItem(tabIndex, keyRedemptionPrefab);
        }

        private void AddTroubleshootingTab()
        {
            int tabIndex = AddTab("Troubleshooting");
            AddItem(tabIndex, troubleshootingPrefab);
        }

        private uGUI_Choice AddStickOptions(int tabIndex, GameInput.Device device, string label, GameInput.Button buttonUp, GameInput.Button buttonDown, GameInput.Button buttonLeft, GameInput.Button buttonRight)
        {
            GameObject[] customBindingObjects = new GameObject[4];
            int num = 2;
            if (GameInput.GetBinding(device, buttonUp, GameInput.BindingSet.Secondary) == null && GameInput.GetBinding(device, buttonDown, GameInput.BindingSet.Secondary) == null && GameInput.GetBinding(device, buttonLeft, GameInput.BindingSet.Secondary) == null && GameInput.GetBinding(device, buttonRight, GameInput.BindingSet.Secondary) == null)
            {
                if (GameInput.GetBinding(device, buttonUp, GameInput.BindingSet.Primary) == "ControllerLeftStickUp" && GameInput.GetBinding(device, buttonDown, GameInput.BindingSet.Primary) == "ControllerLeftStickDown" && GameInput.GetBinding(device, buttonLeft, GameInput.BindingSet.Primary) == "ControllerLeftStickLeft" && GameInput.GetBinding(device, buttonRight, GameInput.BindingSet.Primary) == "ControllerLeftStickRight")
                {
                    num = 0;
                }
                if (GameInput.GetBinding(device, buttonUp, GameInput.BindingSet.Primary) == "ControllerRightStickUp" && GameInput.GetBinding(device, buttonDown, GameInput.BindingSet.Primary) == "ControllerRightStickDown" && GameInput.GetBinding(device, buttonLeft, GameInput.BindingSet.Primary) == "ControllerRightStickLeft" && GameInput.GetBinding(device, buttonRight, GameInput.BindingSet.Primary) == "ControllerRightStickRight")
                {
                    num = 1;
                }
            }
            UnityAction<int> callback = delegate(int option)
            {
                bool active = option == 2;
                customBindingObjects[0].SetActive(active);
                customBindingObjects[1].SetActive(active);
                customBindingObjects[2].SetActive(active);
                customBindingObjects[3].SetActive(active);
                switch (option)
                {
                    case 0:
                        GameInput.SetBinding(device, buttonUp, GameInput.BindingSet.Primary, "ControllerLeftStickUp");
                        GameInput.SetBinding(device, buttonDown, GameInput.BindingSet.Primary, "ControllerLeftStickDown");
                        GameInput.SetBinding(device, buttonLeft, GameInput.BindingSet.Primary, "ControllerLeftStickLeft");
                        GameInput.SetBinding(device, buttonRight, GameInput.BindingSet.Primary, "ControllerLeftStickRight");
                        break;
                    case 1:
                        GameInput.SetBinding(device, buttonUp, GameInput.BindingSet.Primary, "ControllerRightStickUp");
                        GameInput.SetBinding(device, buttonDown, GameInput.BindingSet.Primary, "ControllerRightStickDown");
                        GameInput.SetBinding(device, buttonLeft, GameInput.BindingSet.Primary, "ControllerRightStickLeft");
                        GameInput.SetBinding(device, buttonRight, GameInput.BindingSet.Primary, "ControllerRightStickRight");
                        break;
                }
                if (option != 2)
                {
                    GameInput.SetBinding(device, buttonUp, GameInput.BindingSet.Secondary, null);
                    GameInput.SetBinding(device, buttonDown, GameInput.BindingSet.Secondary, null);
                    GameInput.SetBinding(device, buttonLeft, GameInput.BindingSet.Secondary, null);
                    GameInput.SetBinding(device, buttonRight, GameInput.BindingSet.Secondary, null);
                }
            };
            string[] items = new string[3] { "\ue007", "\ue002", "Custom" };
            uGUI_Choice result = AddChoiceOption(tabIndex, label, items, num, callback);
            AddBindingOption(tabIndex, $"Option{buttonUp.ToString()}", device, buttonUp, out customBindingObjects[0]);
            AddBindingOption(tabIndex, $"Option{buttonDown.ToString()}", device, buttonDown, out customBindingObjects[1]);
            AddBindingOption(tabIndex, $"Option{buttonLeft.ToString()}", device, buttonLeft, out customBindingObjects[2]);
            AddBindingOption(tabIndex, $"Option{buttonRight.ToString()}", device, buttonRight, out customBindingObjects[3]);
            customBindingObjects[0].SetActive(num == 2);
            customBindingObjects[1].SetActive(num == 2);
            customBindingObjects[2].SetActive(num == 2);
            customBindingObjects[3].SetActive(num == 2);
            return result;
        }

        private void AddBindings(int tabIndex, GameInput.Device device)
        {
            if (device == GameInput.Device.Controller)
            {
                controllerMoveStickOption = AddStickOptions(tabIndex, device, "OptionMove", GameInput.Button.MoveForward, GameInput.Button.MoveBackward, GameInput.Button.MoveLeft, GameInput.Button.MoveRight);
                controllerLookStickOption = AddStickOptions(tabIndex, device, "OptionLook", GameInput.Button.LookUp, GameInput.Button.LookDown, GameInput.Button.LookLeft, GameInput.Button.LookRight);
            }
            foreach (GameInput.Button value in Enum.GetValues(typeof(GameInput.Button)))
            {
                if ((device != GameInput.Device.Controller || (uint)(value - 19) > 7u) && GameInput.IsBindable(device, value))
                {
                    string label = "Option" + value;
                    AddBindingOption(tabIndex, label, device, value);
                }
            }
            UnityAction callback = delegate
            {
                SetupDefaultBindings(device);
            };
            AddButton(tabIndex, "ResetToDefault", callback);
        }

        private void SetupDefaultBindings(GameInput.Device device)
        {
            GameInput.SetupDefaultBindings(device);
            if (controllerMoveStickOption != null)
            {
                controllerMoveStickOption.value = 0;
            }
            if (controllerLookStickOption != null)
            {
                controllerLookStickOption.value = 1;
            }
        }
    }
}
