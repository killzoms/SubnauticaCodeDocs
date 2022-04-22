using System.Collections;
using System.IO;
using Discord;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PlatformServicesDiscord : PlatformServices
    {
        public sealed class CallbackHost : MonoBehaviour
        {
            private global::Discord.Discord discord;

            public void Initialize(global::Discord.Discord _discord)
            {
                discord = _discord;
            }

            private void Update()
            {
                discord.RunCallbacks();
            }
        }

        private readonly long applicationId = 363413811518242816L;

        private global::Discord.Discord discord;

        private GameObject callbackHostObject;

        private ActivityManager activityManager;

        private UserManager userManager;

        private UserStorage userStorage;

        private string userId;

        private string userName;

        public event PlatformServicesUtils.AchievementsChanged OnAchievementsChanged;

        public static bool IsPresent()
        {
            return PlatformServicesUtils.IsRuntimePluginDllPresent("discord_game_sdk");
        }

        public IEnumerator InitializeAsync()
        {
            discord = new global::Discord.Discord(applicationId, 0uL);
            discord.SetLogHook(LogLevel.Debug, delegate(LogLevel level, string message)
            {
                Debug.LogFormat("Discord: [{0}] {1}", level, message);
            });
            ApplicationManager applicationManager = discord.GetApplicationManager();
            activityManager = discord.GetActivityManager();
            userManager = discord.GetUserManager();
            userManager.OnCurrentUserUpdate += OnCurrentUserUpdate;
            UserStoragePC userStoragePC = new UserStoragePC(Path.Combine(Application.persistentDataPath, "Subnautica/SavedGames"));
            userStoragePC.MigrateSaveData(Path.GetFullPath(Path.Combine(Application.persistentDataPath, "../..", "Unknown Worlds/Subnautica/SavedGames")));
            userStorage = userStoragePC;
            StartCallbacks();
            while (userId == null)
            {
                yield return null;
            }
            Debug.LogFormat("Discord Locale: {0}", applicationManager.GetCurrentLocale());
            Debug.LogFormat("Discord Branch: {0}", applicationManager.GetCurrentBranch());
            Debug.LogFormat("Discord User: {0} ({1})", GetUserName(), GetUserId());
        }

        private void OnCurrentUserUpdate()
        {
            try
            {
                Discord.User currentUser = userManager.GetCurrentUser();
                userId = currentUser.Id.ToString();
                userName = currentUser.Username;
            }
            catch
            {
                userId = "0";
                userName = "DiscordUser";
            }
        }

        public string GetName()
        {
            return "Discord";
        }

        public bool GetSupportsDynamicLogOn()
        {
            return false;
        }

        public bool GetSupportsSharingScreenshots()
        {
            return false;
        }

        public string GetUserId()
        {
            return userId;
        }

        public string GetUserName()
        {
            return userName;
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

        public bool IsUserLoggedIn()
        {
            return true;
        }

        public void LogOffUser()
        {
        }

        public PlatformServicesUtils.AsyncOperation LogOnUserAsync(int gamepadIndex)
        {
            return null;
        }

        public void ResetAchievements()
        {
        }

        private static void OnActivityUpdated(Result result)
        {
        }

        public void SetRichPresence(string presenceKey)
        {
            string format = Language.main.GetFormat(presenceKey);
            Activity activity = default(Activity);
            activity.State = "Playing";
            activity.Details = format;
            activityManager.UpdateActivity(activity, OnActivityUpdated);
        }

        public bool ShareScreenshot(string fileName)
        {
            return false;
        }

        public void ShowHelp()
        {
        }

        public bool ShowVirtualKeyboard(string title, string defaultText, PlatformServicesUtils.VirtualKeyboardFinished callback)
        {
            return false;
        }

        private void StartCallbacks()
        {
            callbackHostObject = new GameObject();
            callbackHostObject.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(callbackHostObject);
            callbackHostObject.AddComponent<SceneCleanerPreserve>();
            callbackHostObject.AddComponent<CallbackHost>().Initialize(discord);
        }

        private void StopCallbacks()
        {
            Object.Destroy(callbackHostObject);
        }

        public void Shutdown()
        {
            discord.Dispose();
        }

        public void UnlockAchievement(GameAchievements.Id id)
        {
        }

        public bool GetAchievementUnlocked(GameAchievements.Id id)
        {
            return false;
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
            return string.Empty;
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
