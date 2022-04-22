using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace AssemblyCSharp
{
    internal class SoundSystem : MonoBehaviour
    {
        private static SoundSystem instance;

        public const float defaultMasterVolume = 1f;

        public const float defaultMusicVolume = 1f;

        public const float defaultVoiceVolume = 1f;

        public const float defaultAmbientVolume = 1f;

        private static float masterVolume = 1f;

        private static float musicVolume = 1f;

        private static float voiceVolume = 1f;

        private static float ambientVolume = 1f;

        private static Bus musicBus;

        private static Bus masterBus;

        private static Bus voiceBus;

        private static Bus ambientBus;

        private static void Initialize()
        {
            RuntimeUtils.EnforceLibraryOrder();
            int defaultDevice = GetDefaultDevice();
            if (defaultDevice != -1)
            {
                SetDevice(defaultDevice);
            }
        }

        private static void GetBuses()
        {
            FMOD.Studio.System studioSystem = RuntimeManager.StudioSystem;
            studioSystem.getBus("bus:/master", out masterBus);
            studioSystem.getBus("{0534364c-c6e1-45eb-bc69-5a779840ae85}", out musicBus);
            studioSystem.getBus("{a68e2c97-dd83-4a19-83d4-aff5ed1a5443}", out voiceBus);
            studioSystem.getBus("{7333b845-4d29-4635-abec-f6676c753475}", out ambientBus);
            if (masterBus.hasHandle())
            {
                masterBus.setVolume(masterVolume);
            }
            if (musicBus.hasHandle())
            {
                musicBus.setVolume(musicVolume);
            }
            if (voiceBus.hasHandle())
            {
                voiceBus.setVolume(voiceVolume);
            }
            if (ambientBus.hasHandle())
            {
                ambientBus.setVolume(ambientVolume);
            }
        }

        public static string[] GetDeviceOptions(out int currentIndex)
        {
            FMOD.System lowlevelSystem = RuntimeManager.LowlevelSystem;
            lowlevelSystem.getNumDrivers(out var numdrivers);
            string[] array = new string[numdrivers];
            for (int i = 0; i < numdrivers; i++)
            {
                lowlevelSystem.getDriverInfo(i, out var text, 256, out var _, out var _, out var _, out var _);
                array[i] = text;
            }
            lowlevelSystem.getDriver(out currentIndex);
            return array;
        }

        public static void SetDevice(int deviceIndex)
        {
            FMOD.System lowlevelSystem = RuntimeManager.LowlevelSystem;
            if (deviceIndex == -1)
            {
                deviceIndex = GetDefaultDevice();
            }
            if (deviceIndex == -1)
            {
                deviceIndex = 0;
            }
            lowlevelSystem.setDriver(deviceIndex);
        }

        private static int GetDeviceByGuid(Guid guid)
        {
            FMOD.System lowlevelSystem = RuntimeManager.LowlevelSystem;
            lowlevelSystem.getNumDrivers(out var numdrivers);
            for (int i = 0; i < numdrivers; i++)
            {
                lowlevelSystem.getDriverInfo(i, out var _, 256, out var guid2, out var _, out var _, out var _);
                if (guid2 == guid)
                {
                    return i;
                }
            }
            return -1;
        }

        private static Guid GetDeviceGuid(int deviceIndex)
        {
            RuntimeManager.LowlevelSystem.getDriverInfo(deviceIndex, out var _, 256, out var guid, out var _, out var _, out var _);
            return guid;
        }

        public static int GetDefaultDevice()
        {
            if (VRUtil.GetAudioDeviceGuid(out var guid))
            {
                return GetDeviceByGuid(guid);
            }
            return -1;
        }

        public static void SetMasterVolume(float value)
        {
            masterVolume = value;
            if (masterBus.hasHandle())
            {
                masterBus.setVolume(value);
            }
        }

        public static float GetMasterVolume()
        {
            return masterVolume;
        }

        public static void SetMusicVolume(float value)
        {
            musicVolume = value;
            if (musicBus.hasHandle())
            {
                musicBus.setVolume(value);
            }
        }

        public static float GetMusicVolume()
        {
            return musicVolume;
        }

        public static void SetVoiceVolume(float value)
        {
            voiceVolume = value;
            if (voiceBus.hasHandle())
            {
                voiceBus.setVolume(value);
            }
        }

        public static float GetVoiceVolume()
        {
            return voiceVolume;
        }

        public static void SetAmbientVolume(float value)
        {
            ambientVolume = value;
            if (ambientBus.hasHandle())
            {
                ambientBus.setVolume(value);
            }
        }

        public static float GetAmbientVolume()
        {
            return ambientVolume;
        }

        private void Awake()
        {
            if (instance != null)
            {
                global::UnityEngine.Object.Destroy(base.gameObject);
                return;
            }
            instance = this;
            Initialize();
        }

        private void Update()
        {
            if (!masterBus.hasHandle())
            {
                GetBuses();
            }
        }
    }
}
