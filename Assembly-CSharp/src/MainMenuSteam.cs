using Steamworks;
using UnityEngine;

namespace AssemblyCSharp
{
    public class MainMenuSteam : MonoBehaviour
    {
        public void openOverlay(string url)
        {
            SteamFriends.ActivateGameOverlayToWebPage(url);
        }

        public void openInventory()
        {
            SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/profiles/" + SteamUser.GetSteamID().m_SteamID + "/inventory/#264710");
        }
    }
}
