using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PlatformServicesEpic : PlatformServices
    {
        private UserStorage userStorage;

        private string userId;

        private string savePath;

        public event PlatformServicesUtils.AchievementsChanged OnAchievementsChanged;

        public PlatformServicesEpic()
        {
            savePath = Path.Combine(Application.persistentDataPath, "Subnautica/SavedGames");
        }

        public static bool IsPresent()
        {
            return Array.IndexOf(Environment.GetCommandLineArgs(), "-EpicPortal") != -1;
        }

        public bool Initialize()
        {
            Match match = Regex.Match(Environment.CommandLine, "-epicuserid\\s*=\\s*(\\S+)");
            if (match.Success)
            {
                userId = match.Groups[1].Value;
                Debug.LogFormat("User id = {0}", userId);
            }
            UserStoragePC userStoragePC = new UserStoragePC(savePath);
            userStoragePC.MigrateSaveData(Path.GetFullPath(Path.Combine(Application.persistentDataPath, "../..", "Unknown Worlds/Subnautica/SavedGames")));
            userStorage = userStoragePC;
            return true;
        }

        public void Shutdown()
        {
        }

        public string GetName()
        {
            return "Epic";
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
            Debug.LogFormat("Achievement Unlocked: {0}", id);
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
            return userId;
        }

        public string GetUserName()
        {
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
