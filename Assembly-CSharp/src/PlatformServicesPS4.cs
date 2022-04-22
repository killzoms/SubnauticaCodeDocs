using UnityEngine;

namespace AssemblyCSharp
{
    public class PlatformServicesPS4 : PlatformServices
    {
        private UserStoragePS4 userStorage;

        private bool canUseUGC;

        private bool needsToShowUGCMessage;

        private float richPresenceLastSetTime = -1f;

        private int activeController = -1;

        private const float kRichPresenceSetCooldown = 300f;

        private string RichPresenceStr = string.Empty;

        public event PlatformServicesUtils.AchievementsChanged OnAchievementsChanged;

        public PlatformServicesPS4()
        {
            userStorage = new UserStoragePS4();
        }

        private void RegisterTrophyPack()
        {
        }

        private void RequestUnlockedTrophies()
        {
        }

        private void RequestParentalControlInfo()
        {
        }

        public void Shutdown()
        {
        }

        public string GetName()
        {
            return "PS4";
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
            return null;
        }

        public void ShowUGCRestrictionMessageIfNecessary()
        {
        }

        public void UnlockAchievement(GameAchievements.Id id)
        {
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

        public bool CanAccessUGC()
        {
            return canUseUGC;
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
            float realtimeSinceStartup = Time.realtimeSinceStartup;
            if (richPresenceLastSetTime < 0f || realtimeSinceStartup - richPresenceLastSetTime >= 300f)
            {
                RichPresenceStr = Language.main.GetFormat(presenceKey);
                richPresenceLastSetTime = realtimeSinceStartup;
            }
        }

        private void UpdateActiveController()
        {
        }

        public int GetActiveController()
        {
            return activeController;
        }

        public void Update()
        {
        }

        public string GetRichPresence()
        {
            return RichPresenceStr;
        }

        public bool ReconnectController(int gamepadIndex)
        {
            return true;
        }
    }
}
