namespace AssemblyCSharp
{
    public interface PlatformServices
    {
        event PlatformServicesUtils.AchievementsChanged OnAchievementsChanged;

        void Shutdown();

        string GetName();

        UserStorage GetUserStorage();

        EconomyItems GetEconomyItems();

        string GetUserMusicPath();

        void UnlockAchievement(GameAchievements.Id id);

        bool GetAchievementUnlocked(GameAchievements.Id id);

        void ResetAchievements();

        bool GetSupportsSharingScreenshots();

        bool ShareScreenshot(string fileName);

        bool IsUserLoggedIn();

        string GetUserId();

        string GetUserName();

        bool GetSupportsDynamicLogOn();

        PlatformServicesUtils.AsyncOperation LogOnUserAsync(int gamepadIndex);

        void LogOffUser();

        void ShowHelp();

        bool ShowVirtualKeyboard(string title, string defaultText, PlatformServicesUtils.VirtualKeyboardFinished callback);

        void SetRichPresence(string presenceKey);

        void ShowUGCRestrictionMessageIfNecessary();

        bool CanAccessUGC();

        string GetRichPresence();

        void Update();

        int GetActiveController();

        bool ReconnectController(int gamepadIndex);
    }
}
