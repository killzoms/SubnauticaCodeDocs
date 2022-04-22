using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace AssemblyCSharp
{
    public class Telemetry : MonoBehaviour
    {
        private string endpointURL = "https://analytics.unknownworlds.com/api";

        private int productId = 264710;

        private int sessionId;

        private int csId;

        private LogSettingsResponse logSettings = new LogSettingsResponse();

        private float lastUpdateTime;

        private float gameLoadTime;

        private string platformName;

        private string userId;

        public static Telemetry Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (!SessionStart())
            {
                Debug.LogError("Telemetry session could not be initialized");
            }
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup > lastUpdateTime + (float)logSettings.session_log_resolution && sessionId > 0)
            {
                lastUpdateTime = Time.realtimeSinceStartup;
                StartCoroutine(SendSessionUpdate());
            }
        }

        private void OnApplicationQuit()
        {
            SessionEnd();
        }

        private bool SessionStart()
        {
            StartCoroutine(DownloadLoggingSettings());
            StartCoroutine(SendSesionStart());
            return true;
        }

        public bool SessionEnd()
        {
            StartCoroutine(SendSesionEnd());
            return true;
        }

        public bool SendAnalyticsEvent(TelemetryEventCategory category, string name, string value)
        {
            int num = logSettings.category_settings.Length;
            bool flag = (int)category >= num || logSettings.category_settings[(int)category];
            if (sessionId > 0 && flag)
            {
                StartCoroutine(SendEvent(category, name, value));
            }
            return true;
        }

        private IEnumerator DownloadLoggingSettings()
        {
            UnityWebRequest webRequest = UnityWebRequest.Get($"{endpointURL}/log-settings");
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError)
            {
                Debug.LogError(webRequest.error);
            }
            else
            {
                LogSettingsResponse logSettingsResponse = (logSettings = LogSettingsResponse.CreateFromJSON(webRequest.downloadHandler.text));
            }
        }

        private IEnumerator SendSesionStart()
        {
            PlatformServices platformServices;
            do
            {
                platformServices = PlatformUtils.main.GetServices();
                yield return null;
            }
            while (platformServices == null);
            platformName = platformServices.GetName();
            userId = ((platformName == "Null") ? "null" : platformServices.GetUserId());
            csId = SNUtils.GetPlasticChangeSetOfBuild(0);
            WWWForm wWWForm = new WWWForm();
            wWWForm.AddField("product_id", productId);
            wWWForm.AddField("platform", platformName);
            wWWForm.AddField("platform_user_id", userId);
            wWWForm.AddField("cs_id", csId);
            wWWForm.AddField("language", Language.main.GetCurrentLanguage());
            wWWForm.AddField("arguments", string.Join(", ", Environment.GetCommandLineArgs()));
            wWWForm.AddField("used_cheats", DevConsole.HasUsedConsole().ToString());
            wWWForm.AddField("gpu_name", SystemInfo.graphicsDeviceName);
            wWWForm.AddField("gpu_memory", SystemInfo.graphicsMemorySize);
            wWWForm.AddField("gpu_api", SystemInfo.graphicsDeviceType.ToString());
            wWWForm.AddField("cpu_name", SystemInfo.processorType);
            wWWForm.AddField("system_memory", SystemInfo.systemMemorySize);
            wWWForm.AddField("system_os", SystemInfo.operatingSystem);
            wWWForm.AddField("quality", QualitySettings.GetQualityLevel());
            wWWForm.AddField("res_x", Screen.width);
            wWWForm.AddField("res_y", Screen.height);
            UnityWebRequest webRequest = UnityWebRequest.Post($"{endpointURL}/session-start", wWWForm);
            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                Debug.LogError(webRequest.error);
                yield break;
            }
            SessionStartResponse sessionStartResponse = SessionStartResponse.CreateFromJSON(webRequest.downloadHandler.text);
            sessionId = sessionStartResponse.session_id;
        }

        private IEnumerator SendSessionUpdate()
        {
            WWWForm wWWForm = new WWWForm();
            wWWForm.AddField("session_id", sessionId);
            wWWForm.AddField("used_cheats", DevConsole.HasUsedConsole().ToString());
            wWWForm.AddField("session_length", Mathf.RoundToInt(Time.realtimeSinceStartup));
            wWWForm.AddField("total_game_length", Mathf.RoundToInt(SaveLoadManager.main.timePlayedTotal));
            UnityWebRequest unityWebRequest = UnityWebRequest.Post($"{endpointURL}/session-update", wWWForm);
            yield return unityWebRequest.SendWebRequest();
        }

        private IEnumerator SendSesionEnd()
        {
            WWWForm wWWForm = new WWWForm();
            wWWForm.AddField("session_id", sessionId);
            wWWForm.AddField("used_cheats", DevConsole.HasUsedConsole().ToString());
            wWWForm.AddField("has_end", "true");
            wWWForm.AddField("session_length", Mathf.RoundToInt(Time.realtimeSinceStartup));
            wWWForm.AddField("total_game_length", Mathf.RoundToInt(SaveLoadManager.main.timePlayedTotal));
            UnityWebRequest unityWebRequest = UnityWebRequest.Post($"{endpointURL}/session-update", wWWForm);
            yield return unityWebRequest.SendWebRequest();
        }

        private IEnumerator SendEvent(TelemetryEventCategory category, string name, string value)
        {
            if (value != null && value.Length > 150)
            {
                Debug.LogException(new ArgumentOutOfRangeException("value", "Backend does not support analytics event data with more than 150 characters."));
                yield break;
            }
            Vector3 vector = Vector3.zero;
            Player main = Player.main;
            if (main != null)
            {
                vector = main.viewModelCamera.GetComponent<Transform>().position;
            }
            WWWForm wWWForm = new WWWForm();
            wWWForm.AddField("product_id", productId);
            wWWForm.AddField("platform", platformName);
            wWWForm.AddField("platform_user_id", userId);
            wWWForm.AddField("cs_id", csId);
            wWWForm.AddField("session_id", sessionId);
            wWWForm.AddField("position_x", vector.x.ToString());
            wWWForm.AddField("position_y", vector.y.ToString());
            wWWForm.AddField("position_z", vector.z.ToString());
            wWWForm.AddField("game_time", GetRealGameTime());
            wWWForm.AddField("event_category", category.ToString());
            wWWForm.AddField("event_name", name);
            wWWForm.AddField("event_value", value);
            UnityWebRequest unityWebRequest = UnityWebRequest.Post($"{endpointURL}/events", wWWForm);
            yield return unityWebRequest.SendWebRequest();
        }

        public static int GetRealGameTime()
        {
            DayNightCycle main = DayNightCycle.main;
            if (!main)
            {
                return 0;
            }
            return (int)Math.Round(main.timePassedSinceOrigin);
        }
    }
}
