using Steamworks;
using UnityEngine;

namespace AssemblyCSharp
{
    public class DebugGUI : MonoBehaviour
    {
        private string SteamID = "No SteamID";

        private void Start()
        {
            Debug.LogWarning("Steamworks: Debug code is active in this build! Remove DebugGUI from Steamworks object in menu scene before shipping to customers!");
            InvokeRepeating("getSteamID", 1f, 2f);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(Screen.width - 250, Screen.height - 50, 240f, 40f), "Steamworks Debug");
            GUI.Label(new Rect(Screen.width - 190, Screen.height - 30, 180f, 40f), "SteamID: " + SteamID);
        }

        private void getSteamID()
        {
            SteamID = SteamUser.GetSteamID().ToString();
            if (SteamID.Equals(null))
            {
                SteamID = "Unable to get ID";
            }
        }
    }
}
