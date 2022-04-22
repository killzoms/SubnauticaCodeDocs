using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PlatformUtils : MonoBehaviour
    {
        public delegate void LoginFinishedDelegate(bool success);

        public delegate void ControllerDisconnectedDelegate();

        private UserStorage userStorage;

        public LoginFinishedDelegate OnLoginFinished;

        public ControllerDisconnectedDelegate OnControllerDisconnected;

        private PlatformServices services;

        private int CurrentControllerIndex = -1;

        private const string prefabPath = "PlatformUtils";

        private static PlatformUtils _main;

        private static string _temporaryCachePath;

        public static bool isConsolePlatform => Application.isConsolePlatform;

        public static bool isSwitchPlatform => false;

        public static bool isPS4Platform => false;

        public static bool isXboxOnePlatform => false;

        public static bool isPS4Pro => false;

        public static bool isXboxOneX => false;

        public static bool isShippingRelease => false;

        public static bool hasFixedCulture => isConsolePlatform;

        public static long preWarmManagedHeapSize => 0L;

        public static PlatformUtils main
        {
            get
            {
                if (_main == null)
                {
                    Initialize();
                }
                return _main;
            }
        }

        public static string temporaryCachePath
        {
            get
            {
                if (_temporaryCachePath == null)
                {
                    _temporaryCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Unknown Worlds\\Subnautica").Replace('\\', '/');
                }
                return _temporaryCachePath;
            }
        }

        public static RenderTextureFormat defaultHDRFormat
        {
            get
            {
                if (isPS4Platform || isXboxOnePlatform)
                {
                    return RenderTextureFormat.ARGB2101010;
                }
                return RenderTextureFormat.DefaultHDR;
            }
        }

        public static void Initialize()
        {
            if (!(_main != null))
            {
                GameObject gameObject = Resources.Load<GameObject>("PlatformUtils");
                if (gameObject == null)
                {
                    Debug.LogError("Cannot find PlatformUtils prefab");
                    Debug.Break();
                }
                else
                {
                    global::UnityEngine.Object.Instantiate(gameObject).name = gameObject.name;
                }
            }
        }

        public PlatformServices GetServices()
        {
            return services;
        }

        private void Awake()
        {
            if (_main != null)
            {
                Debug.LogError("Multiple PlatformUtils instances found in scene!", this);
                Debug.Break();
                global::UnityEngine.Object.DestroyImmediate(base.gameObject);
                return;
            }
            _main = this;
            PreWarmManagedHeap();
            if (isConsolePlatform)
            {
                Texture.streamingTextureDiscardUnusedMips = true;
            }
            global::UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
            StartCoroutine(PlatformInitAsync());
            if (isPS4Platform)
            {
                StartCoroutine(EarlyUserStorageInit());
            }
        }

        private void PreWarmManagedHeap()
        {
            long num = preWarmManagedHeapSize;
            if (num > 0)
            {
                List<byte[]> list = new List<byte[]>();
                long num2;
                for (num2 = GC.GetTotalMemory(forceFullCollection: true); num2 < num; num2 += 1048576)
                {
                    list.Add(new byte[1048576]);
                }
                num2 = GC.GetTotalMemory(forceFullCollection: true);
                Debug.LogFormat("Pre GC Size: {0}", num2);
                list.Clear();
                GC.Collect();
            }
        }

        private IEnumerator EarlyUserStorageInit()
        {
            yield return userStorage.InitializeAsync();
        }

        private IEnumerator PlatformInitAsync()
        {
            PlatformServices platformServices = null;
            if (!Application.isEditor)
            {
                if (platformServices == null && PlatformServicesSteam.IsPresent())
                {
                    Debug.LogFormat("Initializing Steam services");
                    PlatformServicesSteam platformServicesSteam = new PlatformServicesSteam();
                    if (platformServicesSteam.Initialize())
                    {
                        platformServices = platformServicesSteam;
                    }
                }
                if (platformServices == null && PlatformServicesEpic.IsPresent())
                {
                    Debug.LogFormat("Initializing Epic services");
                    PlatformServicesEpic platformServicesEpic = new PlatformServicesEpic();
                    if (platformServicesEpic.Initialize())
                    {
                        platformServices = platformServicesEpic;
                    }
                }
                if (platformServices == null && PlatformServicesDiscord.IsPresent())
                {
                    Debug.LogFormat("Initializing Discord services");
                    PlatformServicesDiscord discordServices = new PlatformServicesDiscord();
                    yield return discordServices.InitializeAsync();
                    platformServices = discordServices;
                }
                if (platformServices == null && PlatformServicesWindows.IsPresent())
                {
                    Debug.LogFormat("Initializing Windows Store services");
                    PlatformServicesWindows windowsServices = new PlatformServicesWindows();
                    yield return windowsServices.InitializeAsync();
                    platformServices = windowsServices;
                }
                if (platformServices == null && PlatformServicesArc.IsPresent())
                {
                    Debug.LogFormat("Initializing Arc services");
                    platformServices = new PlatformServicesArc();
                }
                if (platformServices == null && PlatformServicesRail.IsPresent())
                {
                    Debug.LogFormat("Initializing WeGame services");
                    PlatformServicesRail platformServicesRail = new PlatformServicesRail();
                    if (platformServicesRail.Initialize())
                    {
                        platformServices = platformServicesRail;
                    }
                }
                if (platformServices == null)
                {
                    Application.Quit();
                }
            }
            if (platformServices == null)
            {
                platformServices = new PlatformServicesNull(PlatformServicesNull.DefaultSavePath);
            }
            userStorage = platformServices.GetUserStorage();
            services = platformServices;
        }

        private void OnDestroy()
        {
            if (services != null)
            {
                services.Shutdown();
                services = null;
            }
        }

        public UserStorage GetUserStorage()
        {
            return userStorage;
        }

        public bool IsUserLoggedIn()
        {
            return services.IsUserLoggedIn();
        }

        public void StartLogOnUserAsync(int gamepadIndex)
        {
            CurrentControllerIndex = gamepadIndex;
            StartCoroutine(LogOnUserAsync(gamepadIndex));
        }

        private IEnumerator LogOnUserAsync(int gamepadIndex)
        {
            PlatformServicesUtils.AsyncOperation asyncOperation = services.LogOnUserAsync(gamepadIndex);
            yield return asyncOperation;
            OnLoginFinished(asyncOperation.GetSuccessful());
        }

        public void LogOffUser()
        {
            services.LogOffUser();
        }

        public string GetLoggedInUserName()
        {
            string userName = services.GetUserName();
            if (userName != null)
            {
                return userName;
            }
            return string.Empty;
        }

        public bool ReconnectController(int gamepadIndex)
        {
            return ReconnectControllerImpl(gamepadIndex);
        }

        public string GetCurrentUserId()
        {
            return services.GetUserId();
        }

        private bool ReconnectControllerImpl(int gamepadIndex)
        {
            return true;
        }

        private IEnumerator ScreenshotEncode(string fileName)
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false);
            texture.name = "PlatformUtils.ScreenshotEncode";
            texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            yield return null;
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(fileName, bytes);
            global::UnityEngine.Object.DestroyObject(texture);
        }

        public bool CaptureScreenshot(string fileName)
        {
            if (Application.isConsolePlatform)
            {
                return false;
            }
            ScreenCapture.CaptureScreenshot(fileName);
            return true;
        }

        public static bool SupportsComputeShaders()
        {
            if (GraphicsUtil.IsOpenGL())
            {
                return false;
            }
            return SystemInfo.supportsComputeShaders;
        }

        public static void SetLightbarColor(Color inColor, int userIndex = 0)
        {
        }

        public static void ResetLightbarColor(int userIndex = 0)
        {
        }

        private void Update()
        {
            if (services != null)
            {
                services.Update();
            }
        }

        public static string GetCurrentCultureName()
        {
            return CultureInfo.CurrentCulture.Name;
        }
    }
}
