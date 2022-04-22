using System.IO;
using UnityEngine;

namespace AssemblyCSharp
{
    public class PlatformServicesNull : PlatformServices
    {
        private string richPresence = string.Empty;

        private UserStorage userStorage;

        public static string DefaultSavePath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "SNAppData/SavedGames");

        public event PlatformServicesUtils.AchievementsChanged OnAchievementsChanged;

        public PlatformServicesNull(string savePath)
        {
            userStorage = new UserStoragePC(savePath);
        }

        public void Shutdown()
        {
        }

        public virtual string GetName()
        {
            return "Null";
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
            Debug.LogFormat("Achievement Unlocked: {0}", id.ToString());
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
            return null;
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
            richPresence = Language.main.GetFormat(presenceKey);
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
            return richPresence;
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
