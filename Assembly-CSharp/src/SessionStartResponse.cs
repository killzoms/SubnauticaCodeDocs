using System;
using UnityEngine;

namespace AssemblyCSharp
{
    [Serializable]
    public class SessionStartResponse
    {
        public int session_id;

        public static SessionStartResponse CreateFromJSON(string jsonString)
        {
            return JsonUtility.FromJson<SessionStartResponse>(jsonString);
        }
    }
}
