using System;
using System.IO;
using System.Text;
using Steamworks;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PlatformServicesSteam : PlatformServices
    {
        public sealed class CallbackHost : MonoBehaviour
        {
            private void Update()
            {
                SteamAPI.RunCallbacks();
            }
        }

        private readonly AppId_t appId = new AppId_t(264710u);

        private UserStorage userStorage;

        private EconomyItems economyItems;

        private GameObject callbackHost;

        private CGameID gameID;

        private SteamAPIWarningMessageHook_t warningMessageHook;

        private Callback<UserStatsReceived_t> userStatsReceived;

        private Callback<UserStatsStored_t> userStatsStored;

        private Callback<UserAchievementStored_t> userAchievementStored;

        public string RichPresenceStr = string.Empty;

        public event PlatformServicesUtils.AchievementsChanged OnAchievementsChanged;

        public static bool IsPresent()
        {
            return PlatformServicesUtils.IsRuntimePluginDllPresent("CSteamworks");
        }

        public bool Initialize()
        {
            if (!Packsize.Test())
            {
                Debug.LogWarning("The wrong version of Steamworks.NET is being run in this platform.");
                return false;
            }
            if (!DllCheck.Test())
            {
                Debug.LogWarning("One or more of the Steamworks binaries seems to be the wrong version.");
                return false;
            }
            if (!Application.isEditor)
            {
                RestartInSteam();
            }
            if (!SteamAPI.Init())
            {
                Debug.LogWarning("Couldn't initialize Steamworks");
                return false;
            }
            warningMessageHook = DebugTextHook;
            SteamClient.SetWarningMessageHook(warningMessageHook);
            gameID = new CGameID(SteamUtils.GetAppID());
            userStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            userAchievementStored = Callback<UserAchievementStored_t>.Create(OnAchievementStored);
            SteamUserStats.RequestCurrentStats();
            StartCallbacks();
            string fullName = Directory.GetParent(Application.dataPath).FullName;
            UserStoragePC userStoragePC = new UserStoragePC(Path.Combine(fullName, "SNAppData/SavedGames"));
            userStoragePC.MigrateSaveData(Path.GetFullPath(Path.Combine(fullName, "../..", "Subnautica/SNAppData/SavedGames")));
            userStorage = userStoragePC;
            economyItems = new EconomyItemsSteam(GetUserId());
            return true;
        }

        public void Shutdown()
        {
            StopCallbacks();
            SteamAPI.Shutdown();
        }

        public string GetName()
        {
            return "Steam";
        }

        public UserStorage GetUserStorage()
        {
            return userStorage;
        }

        public EconomyItems GetEconomyItems()
        {
            return economyItems;
        }

        public string GetUserMusicPath()
        {
            return PlatformServicesUtils.GetDesktopUserMusicPath();
        }

        private void StartCallbacks()
        {
            callbackHost = new GameObject();
            callbackHost.hideFlags = HideFlags.HideInHierarchy;
            global::UnityEngine.Object.DontDestroyOnLoad(callbackHost);
            callbackHost.AddComponent<CallbackHost>();
            callbackHost.AddComponent<SceneCleanerPreserve>();
        }

        private void StopCallbacks()
        {
            global::UnityEngine.Object.Destroy(callbackHost);
        }

        private void RestartInSteam()
        {
            try
            {
                if (SteamAPI.RestartAppIfNecessary(appId))
                {
                    Debug.LogWarning("Missing Steam DRM");
                    Application.Quit();
                }
            }
            catch (DllNotFoundException)
            {
                Debug.LogFormat("Could not load [lib]steam_api.dll/so/dylib");
                Application.Quit();
            }
        }

        public void UnlockAchievement(GameAchievements.Id id)
        {
            string platformId = GameAchievements.GetPlatformId(id);
            if (platformId != null)
            {
                SteamUserStats.SetAchievement(platformId);
                SteamUserStats.StoreStats();
            }
        }

        public bool GetAchievementUnlocked(GameAchievements.Id id)
        {
            string platformId = GameAchievements.GetPlatformId(id);
            bool pbAchieved = false;
            if (platformId != null)
            {
                SteamUserStats.GetAchievement(platformId, out pbAchieved);
            }
            return pbAchieved;
        }

        public void ResetAchievements()
        {
            SteamUserStats.ResetAllStats(bAchievementsToo: true);
            SteamUserStats.RequestCurrentStats();
        }

        public bool GetSupportsSharingScreenshots()
        {
            return true;
        }

        public bool ShareScreenshot(string fileName)
        {
            if (File.Exists(fileName))
            {
                SteamScreenshots.AddScreenshotToLibrary(fileName, "", Screen.width, Screen.height);
                return true;
            }
            return false;
        }

        public bool IsUserLoggedIn()
        {
            return true;
        }

        public string GetUserId()
        {
            if (!SteamUser.BLoggedOn())
            {
                return "0";
            }
            if (!SteamUser.GetSteamID().IsValid())
            {
                return "0";
            }
            return SteamUser.GetSteamID().ToString();
        }

        public string GetUserName()
        {
            if (!SteamUser.BLoggedOn())
            {
                return "UnityEditorPlayer";
            }
            if (!SteamUser.GetSteamID().IsValid())
            {
                return "UnityEditorPlayer";
            }
            return SteamFriends.GetPersonaName();
        }

        private void OnUserStatsReceived(UserStatsReceived_t pCallback)
        {
            if ((ulong)gameID == pCallback.m_nGameID)
            {
                if (EResult.k_EResultOK == pCallback.m_eResult)
                {
                    this.OnAchievementsChanged();
                }
                else
                {
                    Debug.Log("Steamworks: RequestStats - failed, " + pCallback.m_eResult);
                }
            }
        }

        private void OnAchievementStored(UserAchievementStored_t pCallback)
        {
            if ((ulong)gameID == pCallback.m_nGameID)
            {
                if (pCallback.m_nMaxProgress == 0)
                {
                    Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' unlocked!");
                    return;
                }
                Debug.Log("Achievement '" + pCallback.m_rgchAchievementName + "' progress callback, (" + pCallback.m_nCurProgress + "," + pCallback.m_nMaxProgress + ")");
            }
        }

        private static void DebugTextHook(int nSeverity, StringBuilder pchDebugText)
        {
            Debug.LogWarning(pchDebugText);
        }

        public bool GetSupportsDynamicLogOn()
        {
            return false;
        }

        public PlatformServicesUtils.AsyncOperation LogOnUserAsync(int gamepadIndex)
        {
            return null;
        }

        public void LogOffUser()
        {
        }

        public void ShowHelp()
        {
        }

        public bool ShowVirtualKeyboard(string title, string defaultText, PlatformServicesUtils.VirtualKeyboardFinished callback)
        {
            return false;
        }

        public void SetRichPresence(string presenceKey)
        {
            RichPresenceStr = Language.main.GetFormat(presenceKey);
        }

        public void ShowUGCRestrictionMessageIfNecessary()
        {
        }

        public bool CanAccessUGC()
        {
            return true;
        }

        public string GetRichPresence()
        {
            return RichPresenceStr;
        }

        public void Update()
        {
        }

        public int GetActiveController()
        {
            return -1;
        }

        public bool ReconnectController(int gamepadIndex)
        {
            return true;
        }
    }
}
