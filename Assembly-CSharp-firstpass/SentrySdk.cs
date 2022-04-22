using System;
using System.Collections.Generic;
using System.Text;
using Sentry;
using UnityEngine;
using UnityEngine.Networking;

public class SentrySdk : MonoBehaviour
{
    private float _timeLastError;

    private const float MinTime = 1f;

    private Breadcrumb[] _breadcrumbs;

    private int _lastBreadcrumbPos;

    private int _noBreadcrumbs;

    [Header("DSN of your sentry instance")]
    public string Dsn;

    [Header("Send PII like User and Computer names")]
    public bool SendDefaultPii = true;

    [Header("Enable SDK debug messages")]
    public bool Debug = true;

    [Header("Override game version")]
    public string Version = "";

    private string _lastErrorMessage = "";

    private Dsn _dsn;

    private bool _initialized;

    private static SentrySdk _instance;

    public void Start()
    {
        if (Dsn == string.Empty)
        {
            UnityEngine.Debug.LogWarning("No DSN defined. The Sentry SDK will be disabled.");
        }
        else if (_instance == null)
        {
            try
            {
                _dsn = new Dsn(Dsn);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error parsing DSN: {ex.Message}");
                return;
            }
            Version = SNUtils.GetPlasticChangeSetOfBuild();
            _breadcrumbs = new Breadcrumb[100];
            UnityEngine.Object.DontDestroyOnLoad(this);
            _instance = this;
            _initialized = true;
        }
        else
        {
            UnityEngine.Object.Destroy(this);
        }
    }

    public static void AddBreadcrumb(string message)
    {
        if (!(_instance == null))
        {
            _instance.DoAddBreadcrumb(message);
        }
    }

    public static void CaptureMessage(string message)
    {
        if (!(_instance == null))
        {
            _instance.DoCaptureMessage(message);
        }
    }

    public static void CaptureEvent(SentryEvent @event)
    {
        if (!(_instance == null))
        {
            _instance.DoCaptureEvent(@event);
        }
    }

    private void DoCaptureMessage(string message)
    {
        if (Debug)
        {
            UnityEngine.Debug.Log("sending message to sentry.");
        }
        SentryEvent @event = new SentryEvent(message, GetBreadcrumbs())
        {
            level = "info"
        };
        DoCaptureEvent(@event);
    }

    private void DoCaptureEvent(SentryEvent @event)
    {
        if (Debug)
        {
            UnityEngine.Debug.Log("sending event to sentry.");
        }
        StartCoroutine(ContinueSendingEvent(@event));
    }

    private void DoAddBreadcrumb(string message)
    {
        if (!_initialized)
        {
            UnityEngine.Debug.LogError("Cannot AddBreadcrumb if we are not initialized");
            return;
        }
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
        _breadcrumbs[_lastBreadcrumbPos] = new Breadcrumb(timestamp, message);
        _lastBreadcrumbPos++;
        _lastBreadcrumbPos %= 100;
        if (_noBreadcrumbs < 100)
        {
            _noBreadcrumbs++;
        }
    }

    private List<Breadcrumb> GetBreadcrumbs()
    {
        return Breadcrumb.CombineBreadcrumbs(_breadcrumbs, _lastBreadcrumbPos, _noBreadcrumbs);
    }

    public void OnEnable()
    {
        Application.logMessageReceived += OnLogMessageReceived;
    }

    public void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    public void DrawGUI()
    {
        if (_lastErrorMessage != "")
        {
            GUILayout.TextArea(_lastErrorMessage);
            if (GUILayout.Button("Clear"))
            {
                _lastErrorMessage = "";
            }
        }
    }

    public void ScheduleException(string excType, string excValue, string stackTrace)
    {
        if (Debug)
        {
            UnityEngine.Debug.Log("sending exception to sentry.");
        }
        List<StackTraceSpec> list = new List<StackTraceSpec>();
        foreach (StackTraceSpec stackTrace2 in GetStackTraces(stackTrace))
        {
            list.Add(stackTrace2);
        }
        SentryExceptionEvent @event = new SentryExceptionEvent(excType, excValue, GetBreadcrumbs(), list);
        StartCoroutine(ContinueSendingEvent(@event));
    }

    private static IEnumerable<StackTraceSpec> GetStackTraces(string stackTrace)
    {
        string[] stackList = stackTrace.Split('\n');
        for (int i = stackList.Length - 1; i >= 0; i--)
        {
            string text = stackList[i];
            if (text == string.Empty)
            {
                continue;
            }
            int num = text.IndexOf(')');
            if (num == -1)
            {
                continue;
            }
            string text2;
            string text3;
            int lineNo;
            try
            {
                text2 = text.Substring(0, num + 1);
                if (text.Length < num + 6)
                {
                    text3 = string.Empty;
                    lineNo = -1;
                }
                else if (text.Substring(num + 1, 5) != " (at ")
                {
                    UnityEngine.Debug.Log("failed parsing " + text);
                    text2 = text;
                    lineNo = -1;
                    text3 = string.Empty;
                }
                else
                {
                    int num2 = text.LastIndexOf(':', text.Length - 1, text.Length - num);
                    if (num == text.Length - 1)
                    {
                        text3 = string.Empty;
                        lineNo = -1;
                    }
                    else if (num2 == -1)
                    {
                        text3 = text.Substring(num + 6, text.Length - num - 7);
                        lineNo = -1;
                    }
                    else
                    {
                        text3 = text.Substring(num + 6, num2 - num - 6);
                        lineNo = Convert.ToInt32(text.Substring(num2 + 1, text.Length - 2 - num2));
                    }
                }
            }
            catch
            {
                continue;
            }
            bool inApp;
            if (text3 == string.Empty || (text3[0] == '<' && text3[text3.Length - 1] == '>'))
            {
                text3 = string.Empty;
                inApp = true;
                if (text2.Contains("UnityEngine."))
                {
                    inApp = false;
                }
            }
            else
            {
                inApp = text3.Contains("Assets/");
            }
            yield return new StackTraceSpec(text3, text2, lineNo, inApp);
        }
    }

    public void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!_initialized)
        {
            return;
        }
        _lastErrorMessage = condition;
        if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !(Time.time - _timeLastError <= 1f))
        {
            _timeLastError = Time.time;
            if (type == LogType.Exception)
            {
                string[] array = condition.Split(new char[1] { ':' }, 2);
                string excType = array[0];
                string excValue = array[1].Substring(1);
                ScheduleException(excType, excValue, stackTrace);
            }
            else
            {
                ScheduleException(type.ToString(), condition, stackTrace);
            }
        }
    }

    private void PrepareEvent(SentryEvent @event)
    {
        if (Version != "")
        {
            @event.release = Version;
        }
        if (SendDefaultPii)
        {
            @event.contexts.device.name = SystemInfo.deviceName;
        }
        @event.tags.deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
        @event.extra.unityVersion = Application.unityVersion;
        @event.extra.screenOrientation = Screen.orientation.ToString();
        @event.user.id = SystemInfo.deviceUniqueIdentifier;
        if (Application.isEditor)
        {
            @event.level = "warning";
        }
    }

    private IEnumerator<UnityWebRequestAsyncOperation> ContinueSendingEvent<T>(T @event) where T : SentryEvent
    {
        PrepareEvent(@event);
        string s = JsonUtility.ToJson(@event);
        string publicKey = _dsn.publicKey;
        string secretKey = _dsn.secretKey;
        string arg = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
        string value = $"Sentry sentry_version=5,sentry_client=Unity0.1,sentry_timestamp={arg},sentry_key={publicKey},sentry_secret={secretKey}";
        UnityWebRequest www = new UnityWebRequest(_dsn.callUri.ToString());
        www.method = "POST";
        www.SetRequestHeader("X-Sentry-Auth", value);
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(s));
        www.downloadHandler = new DownloadHandlerBuffer();
        yield return www.SendWebRequest();
        while (!www.isDone)
        {
            yield return null;
        }
        if (www.isNetworkError || www.isHttpError || www.responseCode != 200)
        {
            UnityEngine.Debug.LogWarning("error sending request to sentry: " + www.error);
        }
        else if (Debug)
        {
            UnityEngine.Debug.Log("Sentry sent back: " + www.downloadHandler.text);
        }
    }
}
