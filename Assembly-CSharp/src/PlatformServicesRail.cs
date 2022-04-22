using System;
using System.IO;
using rail;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PlatformServicesRail : PlatformServices
    {
        private sealed class CallbackHost : MonoBehaviour
        {
            private void Update()
            {
                rail_api.RailFireEvents();
            }
        }

        private readonly ulong railId = 2000174uL;

        private UserStorage userStorage;

        private string userMusicPath;

        private GameObject callbackHost;

        private bool initialized;

        private IRailPlayer player;

        private IRailAchievementHelper achievementHelper;

        private IRailPlayerAchievement playerAchievement;

        private string RichPresenceStr;

        public event PlatformServicesUtils.AchievementsChanged OnAchievementsChanged;

        public static bool IsPresent()
        {
            return PlatformServicesUtils.IsRuntimePluginDllPresent("rail_sdk64");
        }

        public bool Initialize()
        {
            try
            {
                RailGameID railGameID = new RailGameID();
                railGameID.id_ = railId;
                string[] array = new string[1] { string.Empty };
                if (rail_api.RailNeedRestartAppForCheckingEnvironment(railGameID, array.Length, array))
                {
                    Debug.Log("CheckingEnvironment failed, please run in TGP");
                    Application.Quit();
                    return false;
                }
                initialized = rail_api.RailInitialize();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            if (initialized)
            {
                string savePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "SNAppData/SavedGames");
                userStorage = new UserStoragePC(savePath);
                StartCallbacks();
                Debug.Log("RailInitialize success");
            }
            else
            {
                Debug.Log("RailInitialize failed");
            }
            if (initialized)
            {
                IRailFactory railFactory = rail_api.RailFactory();
                if (railFactory != null)
                {
                    player = railFactory.RailPlayer();
                }
            }
            if (player != null)
            {
                Debug.Log("Initializing achievements");
                InitializeAchievements(player.GetRailID());
            }
            return initialized;
        }

        private void InitializeAchievements(RailID playerId)
        {
            IRailFactory railFactory = rail_api.RailFactory();
            if (railFactory != null)
            {
                achievementHelper = railFactory.RailAchievementHelper();
            }
            if (achievementHelper != null)
            {
                playerAchievement = achievementHelper.CreatePlayerAchievement(playerId);
                if (playerAchievement != null)
                {
                    playerAchievement.AsyncRequestAchievement(string.Empty);
                }
            }
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

        public void Shutdown()
        {
            if (initialized)
            {
                StopCallbacks();
                rail_api.CSharpRailUnRegisterAllEvent();
                rail_api.RailFinalize();
            }
        }

        public string GetName()
        {
            return "Rail";
        }

        public UserStorage GetUserStorage()
        {
            return userStorage;
        }

        public EconomyItems GetEconomyItems()
        {
            return null;
        }

        public string GetUserMusicPath()
        {
            return PlatformServicesUtils.GetDesktopUserMusicPath();
        }

        public void UnlockAchievement(GameAchievements.Id id)
        {
            if (playerAchievement == null)
            {
                return;
            }
            string platformId = GameAchievements.GetPlatformId(id);
            if (platformId == null)
            {
                return;
            }
            RailResult railResult = playerAchievement.MakeAchievement(platformId);
            if (railResult != 0)
            {
                Debug.LogErrorFormat("Error unlocking achievement {0}", railResult);
                return;
            }
            railResult = playerAchievement.AsyncStoreAchievement(string.Empty);
            if (railResult != 0)
            {
                Debug.LogErrorFormat("Error storing achievement {0}", railResult);
            }
        }

        public bool GetAchievementUnlocked(GameAchievements.Id id)
        {
            return false;
        }

        public void ResetAchievements()
        {
        }

        public bool GetSupportsSharingScreenshots()
        {
            return false;
        }

        public bool ShareScreenshot(string fileName)
        {
            return false;
        }

        public bool IsUserLoggedIn()
        {
            return true;
        }

        public string GetUserId()
        {
            if (player != null)
            {
                return player.GetRailID().id_.ToString();
            }
            return null;
        }

        public string GetUserName()
        {
            if (player != null && player.GetPlayerName(out var name) == RailResult.kSuccess)
            {
                return name;
            }
            return null;
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
