using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LumenWorks.Framework.IO.Csv;
using UnityEngine;
using UnityEngine.XR;
using UWE;

namespace AssemblyCSharp
{
    public class GameSettings : MonoBehaviour
    {
        public interface ISerializer
        {
            bool IsReading();

            bool Serialize(string name, bool value);

            int Serialize(string name, int value);

            float Serialize(string name, float value);

            string Serialize(string name, string value);
        }

        private class Serializer : ISerializer
        {
            private bool reading;

            private SaveLoadManager.OptionsCache options;

            public Serializer(SaveLoadManager.OptionsCache _options, bool _reading)
            {
                options = _options;
                reading = _reading;
            }

            public bool IsReading()
            {
                return reading;
            }

            public bool Serialize(string name, bool value)
            {
                if (reading)
                {
                    value = options.GetBool(name, value);
                }
                else
                {
                    options.SetBool(name, value);
                }
                return value;
            }

            public int Serialize(string name, int value)
            {
                if (reading)
                {
                    value = options.GetInt(name, value);
                }
                else
                {
                    options.SetInt(name, value);
                }
                return value;
            }

            public float Serialize(string name, float value)
            {
                if (reading)
                {
                    value = options.GetFloat(name, value);
                }
                else
                {
                    options.SetFloat(name, value);
                }
                return value;
            }

            public string Serialize(string name, string value)
            {
                if (reading)
                {
                    value = options.GetString(name, value);
                }
                else
                {
                    options.SetString(name, value);
                }
                return value;
            }
        }

        private GameSettings instance;

        private const int settingsVersion = 5;

        private const string optionsContainerName = "options";

        private const string optionsFileName = "options.bin";

        private void Awake()
        {
            if (instance != null)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
            }
            else
            {
                instance = this;
            }
        }

        private void Start()
        {
            ValidateOptions();
        }

        public static float GetMinVrRenderScale()
        {
            return 0.5f;
        }

        private static IEnumerator GetGpuScoreAsync(IOut<int> score)
        {
            ResourceRequest request = Resources.LoadAsync<TextAsset>("gpu_scores");
            yield return request;
            score.Set(-1);
            if (!(request.asset != null))
            {
                yield break;
            }
            TextAsset obj = request.asset as TextAsset;
            string graphicsDeviceName = SystemInfo.graphicsDeviceName;
            using CsvReader csvReader = new CsvReader(new StringReader(obj.text), hasHeaders: false);
            while (csvReader.ReadNextRecord())
            {
                if (csvReader[0] == graphicsDeviceName)
                {
                    if (csvReader[1] == "medium")
                    {
                        score.Set(1);
                    }
                    else if (csvReader[1] == "high")
                    {
                        score.Set(2);
                    }
                    else
                    {
                        score.Set(0);
                    }
                    break;
                }
            }
        }

        public static IEnumerator SaveAsync()
        {
            Debug.Log("Saving game settings");
            SaveLoadManager.OptionsCache optionsCache = new SaveLoadManager.OptionsCache();
            Serializer serializer = new Serializer(optionsCache, _reading: false);
            SerializeSettings(serializer);
            ((ISerializer)serializer).Serialize("Version", 5);
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            using MemoryStream stream = new MemoryStream();
            using (PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy())
            {
                pooledObject.Value.Serialize(stream, optionsCache);
            }
            byte[] value = stream.ToArray();
            Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
            dictionary.Add("options.bin", value);
            yield return userStorage.SaveFilesAsync("options", dictionary);
        }

        public static CoroutineTask<bool> LoadAsync()
        {
            TaskResult<bool> taskResult = new TaskResult<bool>();
            return new CoroutineTask<bool>(LoadAsync(taskResult), taskResult);
        }

        public static IEnumerator LoadAsync(IOut<bool> success)
        {
            Debug.Log("Loading game settings");
            SaveLoadManager.OptionsCache options = new SaveLoadManager.OptionsCache();
            UserStorage userStorage = PlatformUtils.main.GetUserStorage();
            List<string> fileNames = new List<string> { "options.bin" };
            UserStorageUtils.LoadOperation loadOperation = userStorage.LoadFilesAsync("options", fileNames);
            yield return loadOperation;
            if (loadOperation.GetSuccessful())
            {
                using MemoryStream stream = new MemoryStream(loadOperation.files["options.bin"]);
                using PooledObject<ProtobufSerializer> pooledObject = ProtobufSerializerPool.GetProxy();
                pooledObject.Value.Deserialize(stream, options, verbose: false);
            }
            else if (!PlatformUtils.isConsolePlatform)
            {
                TaskResult<int> result = new TaskResult<int>();
                yield return GetGpuScoreAsync(result);
                int num = result.Get();
                if (num == -1)
                {
                    num = 2;
                }
                int xResolution = 1920;
                int yResolution = 1080;
                if (num == 0)
                {
                    xResolution = 1280;
                    yResolution = 720;
                }
                GraphicsUtil.GetClosestSupportedResolution(ref xResolution, ref yResolution);
                Screen.SetResolution(xResolution, yResolution, fullscreen: true);
                GraphicsPreset.GetPresets()[num].Apply();
            }
            Serializer serializer = new Serializer(options, _reading: true);
            SerializeSettings(serializer);
            UpgradeSettings(serializer);
            ValidateOptions();
            success.Set(value: true);
        }

        private static void ValidateOptions()
        {
            if (XRSettings.eyeTextureResolutionScale < GetMinVrRenderScale())
            {
                XRSettings.eyeTextureResolutionScale = 1f;
            }
            if (XRSettings.enabled || PlatformUtils.isConsolePlatform)
            {
                GameInput.SetControllerEnabled(_controllerEnabled: true);
            }
        }

        private static void UpgradeSettings(ISerializer serializer)
        {
            int num = serializer.Serialize("Version", 0);
            if (num < 1)
            {
                GameInput.SetBinding(GameInput.Device.Controller, GameInput.Button.Reload, GameInput.BindingSet.Primary, "ControllerButtonX");
                GameInput.SetBinding(GameInput.Device.Controller, GameInput.Button.Exit, GameInput.BindingSet.Primary, "ControllerButtonB");
            }
            if (num < 2)
            {
                GameInput.SetBinding(GameInput.Device.Controller, GameInput.Button.Deconstruct, GameInput.BindingSet.Primary, "ControllerDPadDown");
            }
            if (PlatformUtils.isConsolePlatform)
            {
                GraphicsPreset.GetPresets()[QualitySettings.GetQualityLevel()].Apply();
            }
            else if (num < 5)
            {
                MiscSettings.fieldOfView = serializer.Serialize("Graphics/FOV", MiscSettings.fieldOfView);
                GraphicsPreset.GetPresets()[QualitySettings.GetQualityLevel()].Apply();
                UwePostProcessingManager.SetAaMode(serializer.Serialize("Graphics/AntiAliasingMode", UwePostProcessingManager.GetAaMode()));
                UwePostProcessingManager.SetAaQuality(serializer.Serialize("Graphics/AntiAliasingQuality", UwePostProcessingManager.GetAaQuality()));
                UwePostProcessingManager.SetAoQuality(serializer.Serialize("Graphics/AmbientOcclusionQuality", UwePostProcessingManager.GetAoQuality()));
                UwePostProcessingManager.SetSsrQuality(serializer.Serialize("Graphics/ScreenSpaceReflectionsQuality", UwePostProcessingManager.GetSsrQuality()));
                UwePostProcessingManager.ToggleBloom(serializer.Serialize("Graphics/Bloom", UwePostProcessingManager.GetBloomEnabled()));
                UwePostProcessingManager.ToggleBloomLensDirt(serializer.Serialize("Graphics/BloomLensDirt", UwePostProcessingManager.GetBloomLensDirtEnabled()));
                UwePostProcessingManager.ToggleDof(serializer.Serialize("Graphics/DepthOfField", UwePostProcessingManager.GetDofEnabled()));
                UwePostProcessingManager.ToggleDithering(serializer.Serialize("Graphics/Dithering", UwePostProcessingManager.GetDitheringEnabled()));
                UwePostProcessingManager.SetMotionBlurQuality(serializer.Serialize("Graphics/MotionBlurQuality", UwePostProcessingManager.GetMotionBlurQuality()));
            }
        }

        private static void SerializeSettings(ISerializer serializer)
        {
            SerializeGraphicsSettings(serializer);
            SerializeInputSettings(serializer);
            SerializeSoundSettings(serializer);
            SerializeVRSettings(serializer);
            SerializeLocaleSettings(serializer);
            SerializeMiscSettings(serializer);
        }

        private static T Serialize<T>(ISerializer serializer, string name, T value)
        {
            string value2 = serializer.Serialize(name, value.ToString());
            try
            {
                value = (T)Enum.Parse(typeof(T), value2);
                return value;
            }
            catch
            {
                return value;
            }
        }

        private static void SerializeGraphicsSettings(ISerializer serializer)
        {
            if (!PlatformUtils.isConsolePlatform)
            {
                WaterSurface.SetQuality(Serialize(serializer, "Graphics/WaterQuality", WaterSurface.GetQuality()));
                GraphicsUtil.SetQualityLevel(serializer.Serialize("Graphics/Quality", QualitySettings.GetQualityLevel()));
                GraphicsUtil.SetVSyncEnabled(serializer.Serialize("Graphics/VSync", GraphicsUtil.GetVSyncEnabled()));
                MiscSettings.fieldOfView = serializer.Serialize("Graphics/FOV", MiscSettings.fieldOfView);
                UwePostProcessingManager.SetAaMode(serializer.Serialize("Graphics/AntiAliasingMode", UwePostProcessingManager.GetAaMode()));
                UwePostProcessingManager.SetAaQuality(serializer.Serialize("Graphics/AntiAliasingQuality", UwePostProcessingManager.GetAaQuality()));
                UwePostProcessingManager.SetAoQuality(serializer.Serialize("Graphics/AmbientOcclusionQuality", UwePostProcessingManager.GetAoQuality()));
                UwePostProcessingManager.SetSsrQuality(serializer.Serialize("Graphics/ScreenSpaceReflectionsQuality", UwePostProcessingManager.GetSsrQuality()));
                UwePostProcessingManager.ToggleBloom(serializer.Serialize("Graphics/Bloom", UwePostProcessingManager.GetBloomEnabled()));
                UwePostProcessingManager.ToggleBloomLensDirt(serializer.Serialize("Graphics/BloomLensDirt", UwePostProcessingManager.GetBloomLensDirtEnabled()));
                UwePostProcessingManager.ToggleDof(serializer.Serialize("Graphics/DepthOfField", UwePostProcessingManager.GetDofEnabled()));
                UwePostProcessingManager.SetMotionBlurQuality(serializer.Serialize("Graphics/MotionBlurQuality", UwePostProcessingManager.GetMotionBlurQuality()));
                UwePostProcessingManager.ToggleDithering(serializer.Serialize("Graphics/Dithering", UwePostProcessingManager.GetDitheringEnabled()));
            }
            UwePostProcessingManager.SetColorGradingMode(serializer.Serialize("Graphics/ColorGrading", UwePostProcessingManager.GetColorGradingMode()));
        }

        private static void SerializeInputSettings(ISerializer serializer)
        {
            GameInput.SetInvertMouse(serializer.Serialize("Input/InvertMouse", GameInput.GetInvertMouse()));
            GameInput.SetMouseSensitivity(serializer.Serialize("Input/MouseSensitivity", GameInput.GetMouseSensitivity()));
            GameInput.SetControllerEnabled(serializer.Serialize("Input/ControllerEnabled", GameInput.GetControllerEnabled()));
            GameInput.SetControllerHorizontalSensitivity(serializer.Serialize("Input/ControllerSensitivityX", GameInput.GetControllerHorizontalSensitivity()));
            GameInput.SetControllerVerticalSensitivity(serializer.Serialize("Input/ControllerSensitivityY", GameInput.GetControllerVerticalSensitivity()));
            GameInput.SetInvertController(serializer.Serialize("Input/InvertController", GameInput.GetInvertController()));
            GameInput.SetChosenControllerLayout(Serialize(serializer, "Input/ControllerLayout", GameInput.GetChosenControllerLayout()));
            foreach (GameInput.Device value in Enum.GetValues(typeof(GameInput.Device)))
            {
                foreach (GameInput.Button value2 in Enum.GetValues(typeof(GameInput.Button)))
                {
                    if (!GameInput.IsBindable(value, value2))
                    {
                        continue;
                    }
                    foreach (GameInput.BindingSet value3 in Enum.GetValues(typeof(GameInput.BindingSet)))
                    {
                        string binding = GameInput.GetBinding(value, value2, value3);
                        string text = $"Input/Binding/{value}/{value2}/{value3}";
                        GameInput.SetBinding(value, value2, value3, serializer.Serialize(text, binding));
                    }
                }
            }
        }

        private static void SerializeSoundSettings(ISerializer serializer)
        {
            SoundSystem.SetMasterVolume(serializer.Serialize("Sound/MasterVolume", SoundSystem.GetMasterVolume()));
            SoundSystem.SetMusicVolume(serializer.Serialize("Sound/MusicVolume", SoundSystem.GetMusicVolume()));
            SoundSystem.SetVoiceVolume(serializer.Serialize("Sound/VoiceVolume", SoundSystem.GetVoiceVolume()));
            SoundSystem.SetAmbientVolume(serializer.Serialize("Sound/AmbientVolume", SoundSystem.GetAmbientVolume()));
        }

        private static void SerializeVRSettings(ISerializer serializer)
        {
            XRSettings.eyeTextureResolutionScale = serializer.Serialize("VR/RenderScale", XRSettings.eyeTextureResolutionScale);
            VROptions.gazeBasedCursor = serializer.Serialize("VR/GazeBasedCursor", VROptions.gazeBasedCursor);
        }

        private static void SerializeLocaleSettings(ISerializer serializer)
        {
            Language.main.SetCurrentLanguage(serializer.Serialize("Locale/Language", Language.main.GetCurrentLanguage()));
            Language.main.showSubtitles = serializer.Serialize("Locale/Subtitles", Language.main.showSubtitles);
            Subtitles.main.speed = serializer.Serialize("Locale/SubtitlesSpeed", Subtitles.main.speed);
        }

        private static void SerializeMiscSettings(ISerializer serialize)
        {
            if (!PlatformUtils.isConsolePlatform)
            {
                MiscSettings.fieldOfView = serialize.Serialize("Misc/FieldOfView", MiscSettings.fieldOfView);
            }
            MiscSettings.consoleHistory = serialize.Serialize("Misc/ConsoleHistory", MiscSettings.consoleHistory);
            MiscSettings.cameraBobbing = serialize.Serialize("Misc/CameraBobbing", MiscSettings.cameraBobbing);
            MiscSettings.email = serialize.Serialize("Misc/Email", MiscSettings.email);
            MiscSettings.rememberEmail = serialize.Serialize("Misc/RememberEmail", MiscSettings.rememberEmail);
            MiscSettings.hideEmailBox = serialize.Serialize("Misc/HideEmailBox", MiscSettings.hideEmailBox);
        }
    }
}
