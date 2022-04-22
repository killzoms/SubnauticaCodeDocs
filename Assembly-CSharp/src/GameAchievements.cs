using UnityEngine;

namespace AssemblyCSharp
{
    public static class GameAchievements
    {
        public enum Id
        {
            None,
            DiveForTheVeryFirstTime,
            RepairAuroraReactor,
            FindPrecursorGun,
            FindPrecursorLavaCastleFacility,
            FindPrecursorLostRiverFacility,
            FindPrecursorPrisonFacility,
            CureInfection,
            DeployTimeCapsule,
            FindDegasiFloatingIslandsBase,
            FindDegasiJellyshroomCavesBase,
            FindDegasiDeepGrandReefBase,
            BuildBase,
            BuildSeamoth,
            BuildCyclops,
            BuildExosuit,
            LaunchRocket,
            HatchCutefish
        }

        public static string GetPlatformId(Id achievementId)
        {
            if (achievementId == Id.None)
            {
                return null;
            }
            return achievementId.ToString();
        }

        public static void Unlock(Id id)
        {
            if (PlatformUtils.isConsolePlatform)
            {
                if (PlatformUtils.isShippingRelease && (!GameModeUtils.AllowsAchievements() || DevConsole.HasUsedConsole()))
                {
                    return;
                }
            }
            else if (!Application.isEditor && (!GameModeUtils.AllowsAchievements() || DevConsole.HasUsedConsole()))
            {
                return;
            }
            PlatformUtils.main.GetServices().UnlockAchievement(id);
        }
    }
}
