using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XGamingRuntime;

namespace AssemblyCSharp
{
    public class PlatformServicesWindows : PlatformServices
    {
        private UserStorage userStorage;

        private ulong userId;

        private XStoreContext storeContext;

        private XUserHandle user;

        private XblContextHandle context;

        private const int titleId = 945909358;

        private const int E_SIGN_IN_DISMISSED = -2147467260;

        private const int E_GAMEUSER_RESOLVE_USER_ISSUE_REQUIRED = -1994108670;

        private const int HTTP_E_STATUS_NOT_MODIFIED = -2145844944;

        private const string serviceConfigurationId = "43c90100-1704-438e-b21f-e68a38616e6e";

        private const int maxAchievements = 100;

        private string gamerTag;

        private bool offlineMode;

        private HashSet<string> unlockedAchievements = new HashSet<string>();

        private float richPresenceSendTime;

        private float richPresenceThrottle = 300f;

        private string richPresenceKeyUpdate;

        private string RichPresenceStr;

        private Dictionary<string, string> XDPAchievementIDToGameAchievementID = new Dictionary<string, string>
        {
            { "1", "DiveForTheVeryFirstTime" },
            { "3", "RepairAuroraReactor" },
            { "4", "FindPrecursorGun" },
            { "5", "FindPrecursorLavaCastleFacility" },
            { "6", "FindPrecursorLostRiverFacility" },
            { "7", "FindPrecursorPrisonFacility" },
            { "8", "CureInfection" },
            { "9", "DeployTimeCapsule" },
            { "10", "FindDegasiFloatingIslandsBase" },
            { "11", "FindDegasiJellyshroomCavesBase" },
            { "12", "FindDegasiDeepGrandReefBase" },
            { "13", "BuildBase" },
            { "14", "BuildSeamoth" },
            { "15", "BuildCyclops" },
            { "16", "BuildExosuit" },
            { "17", "LaunchRocket" },
            { "18", "HatchCutefish" }
        };

        public event PlatformServicesUtils.AchievementsChanged OnAchievementsChanged;

        public static bool IsPresent()
        {
            return PlatformServicesUtils.IsRuntimePluginDllPresent("XGamingRuntimeThunks");
        }

        public IEnumerator InitializeAsync()
        {
            offlineMode = false;
            Debug.Log("Setting up userStorage save path...");
            UserStoragePC userStoragePC = new UserStoragePC(Path.Combine(Application.persistentDataPath, "SavedGames"));
            userStoragePC.MigrateSaveData(Path.GetFullPath(Path.Combine(Application.persistentDataPath, "../..", "Unknown Worlds/Subnautica/SavedGames")));
            userStorage = userStoragePC;
            int num = SDK.XGameRuntimeInitialize();
            if (num < 0)
            {
                Debug.LogFormat("XGRuntime Initialization failed with value: 0x{0}", num.ToString("X8"));
            }
            else
            {
                Debug.Log("XGRuntime initialized successfully.");
            }
            Debug.Log("Starting AddUser...");
            AddUser();
            while (userId == 0L && !offlineMode)
            {
                DispatchCallbacks();
                yield return null;
            }
            Debug.Log("Starting XBLive services...");
            InitXBL();
            while (context == null && !offlineMode)
            {
                DispatchCallbacks();
                yield return null;
            }
            Debug.Log("Starting GetAchievements...");
            GetAchievements();
        }

        private void AddUser()
        {
            Debug.Log("Adding default user silently...");
            SDK.XUserAddAsync(XUserAddOptions.AddDefaultUserSilently, SignInSilentlyComplete);
        }

        private void AddUserComplete(int result, XUserHandle userHandle)
        {
            if (result >= 0)
            {
                user = userHandle;
                SDK.XUserGetId(user, out userId);
                SDK.XUserGetGamertag(user, out gamerTag);
                return;
            }
            Debug.LogFormat("Attempt to sign in normally completed with error code: 0x{0}", result.ToString("X8"));
            if (result == -2147467260)
            {
                offlineMode = true;
            }
        }

        private void SignInSilentlyComplete(int result, XUserHandle userHandle)
        {
            Debug.LogFormat("Result returned from XUserAddAsync: 0x{0}", result.ToString("X8"));
            Debug.Log("Sign in silently complete");
            if (result >= 0)
            {
                user = userHandle;
                int num = SDK.XUserGetId(user, out userId);
                SDK.XUserGetGamertag(user, out gamerTag);
                Debug.LogFormat("GetId returned code: 0x{0}", num.ToString("X8"));
                if (num == -1994108670)
                {
                    SDK.XUserResolveIssueWithUiUtf16Async(user, null, ResolveIssueCompleted);
                }
            }
            else
            {
                Debug.LogFormat("Silent Signin failed with code: 0x{0}", result.ToString("X8"));
                Debug.Log("Attempting to sign in with GUI...");
                SDK.XUserAddAsync(XUserAddOptions.AllowGuests, AddUserComplete);
            }
        }

        private void ResolveIssueCompleted(int hresult)
        {
            Debug.LogFormat("Resolve issue completed with code: 0x{0}", hresult.ToString("X8"));
            if (hresult >= 0)
            {
                int num = SDK.XUserGetId(user, out userId);
                SDK.XUserGetGamertag(user, out gamerTag);
                Debug.LogFormat("GetId returned code: 0x{0}", num.ToString("X8"));
            }
            else
            {
                Debug.Log("Attempt to resolve sign in issue failed..");
            }
        }

        private void InitXBL()
        {
            int num = SDK.XBL.XblInitialize("43c90100-1704-438e-b21f-e68a38616e6e");
            int num2 = SDK.XBL.XblContextCreateHandle(user, out context);
            if (num < 0)
            {
                Debug.LogFormat("XBLive failed to initialize with value: 0x{0}", num.ToString("x8"));
            }
            if (num2 < 0)
            {
                Debug.LogFormat("XBLive failed to create context handler with value: 0x{0}", num2.ToString("X8"));
                offlineMode = true;
            }
        }

        public void Shutdown()
        {
        }

        public string GetName()
        {
            return "Windows Store";
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

        private void GetAchievements()
        {
            Debug.Log("Getting Unlocked Achievements");
            SDK.XBL.XblAchievementsGetAchievementsForTitleIdAsync(context, userId, 945909358u, XblAchievementType.All, unlockedOnly: true, XblAchievementOrderBy.DefaultOrder, 0u, 100u, ExtractAchievements);
        }

        private void ExtractAchievements(int hresult, XblAchievementsResultHandle result)
        {
            Debug.LogFormat("Get achievements completed with result: 0x{0}", hresult.ToString("X8"));
            if (hresult < 0)
            {
                return;
            }
            XblAchievement[] achievements;
            int num = SDK.XBL.XblAchievementsResultGetAchievements(result, out achievements);
            Debug.LogFormat("Extracted achievements with result: 0x{0}", num.ToString("X8"));
            if (num < 0)
            {
                return;
            }
            Debug.LogFormat("    {0} achievements retrieved", achievements.Length);
            XblAchievement[] array = achievements;
            foreach (XblAchievement xblAchievement in array)
            {
                if (!XDPAchievementIDToGameAchievementID.ContainsKey(xblAchievement.Id))
                {
                    Debug.LogErrorFormat("Achievement ID [{0}] does not map with any GameAchievemendID. Please update XDPAchievementIDToGameAchievementID in PlatformServicesWindows.cs");
                }
                else
                {
                    unlockedAchievements.Add(XDPAchievementIDToGameAchievementID[xblAchievement.Id]);
                }
            }
            if (this.OnAchievementsChanged != null)
            {
                Debug.Log("OnAchievementsChanged event not referencing null...");
                this.OnAchievementsChanged();
            }
            else
            {
                Debug.Log("OnAchievementsChanged event is null...");
            }
        }

        public void UnlockAchievement(GameAchievements.Id id)
        {
            string platformId = GameAchievements.GetPlatformId(id);
            if (platformId != null && !unlockedAchievements.Contains(platformId))
            {
                Debug.LogFormat("Achievement Unlocked: {0}", id);
                if (!Application.isEditor)
                {
                    Debug.LogFormat("User {0}", userId);
                    SendAchievementUnlocked(userId, platformId);
                }
                unlockedAchievements.Add(platformId);
            }
        }

        private void SendAchievementUnlocked(ulong user, string achievementId)
        {
            Debug.LogFormat("Sending achievement unlocked event for: {0}", achievementId);
            int num = SDK.XBL.XblEventsWriteInGameEvent(context, "AchievementUnlocked", $"{{\"UnlockId\": \"{achievementId}\"}}", "{}");
            Debug.LogFormat("Unlock event response: 0x{0}", num.ToString("X8"));
        }

        public bool GetAchievementUnlocked(GameAchievements.Id id)
        {
            string platformId = GameAchievements.GetPlatformId(id);
            if (platformId != null)
            {
                return unlockedAchievements.Contains(platformId);
            }
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
            if (userId != 0L)
            {
                return userId.ToString();
            }
            return null;
        }

        public string GetUserName()
        {
            if (Application.isEditor)
            {
                return "UnityEditorPlayer";
            }
            return gamerTag;
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
            if (Time.time - richPresenceSendTime < richPresenceThrottle)
            {
                richPresenceKeyUpdate = presenceKey;
                return;
            }
            Debug.Log("Setting Presence...");
            XblPresenceRichPresenceIds richPresenceIds;
            int num = XblPresenceRichPresenceIds.Create("43c90100-1704-438e-b21f-e68a38616e6e", presenceKey, null, out richPresenceIds);
            Debug.LogFormat("Create XblPresenceRichPresenceIds completed with code: 0x{0}", num.ToString("X8"));
            if (num >= 0)
            {
                Debug.LogFormat("SetRichPresence - presenceData - configId:{0} presenceKey:{1}", "43c90100-1704-438e-b21f-e68a38616e6e", presenceKey);
                SDK.XBL.XblPresenceSetPresenceAsync(context, isUserActiveInTitle: true, richPresenceIds, delegate(int hresult)
                {
                    Debug.LogFormat("Set presence completed with result: 0x{0}", hresult.ToString("X8"));
                });
            }
            richPresenceSendTime = Time.time;
            richPresenceKeyUpdate = null;
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

        private void DispatchCallbacks()
        {
            SDK.XTaskQueueDispatch();
        }

        public void Update()
        {
            DispatchCallbacks();
            if (richPresenceKeyUpdate != null && Time.time - richPresenceSendTime > richPresenceThrottle)
            {
                Debug.Log("Throttled RichPresenceKeyUpdate - presenceKey: " + richPresenceKeyUpdate);
                SetRichPresence(richPresenceKeyUpdate);
            }
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
