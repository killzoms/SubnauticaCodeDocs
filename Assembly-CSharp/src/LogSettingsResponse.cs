using System;
using Gendarme;
using UnityEngine;

namespace AssemblyCSharp
{
    [Serializable]
    [SuppressMessage("Subnautica.Rules", "AvoidDoubleInitializationRule")]
    public class LogSettingsResponse
    {
        public int session_log_resolution = 300;

        public bool[] category_settings = new bool[5];

        public static LogSettingsResponse CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<LogSettingsResponse>(jsonString);
        }
    }
}
