using System.IO;
using UnityEngine;

public static class QuickLaunchHelper
{
    private const string quickLaunchObjName = "QUICK_LAUNCHER";

    private const string quickLaunchFileName = "QUICK_LAUNCH.tmp";

    public static void StartQuickLaunch(string startScenePath, bool forceNewGame)
    {
    }

    private static QuickLaunchMarker FindQuickLaunchMarker()
    {
        return null;
    }

    public static bool IsQuickLaunching()
    {
        return FindQuickLaunchMarker() != null;
    }

    public static bool ForceNewGame()
    {
        QuickLaunchMarker quickLaunchMarker = FindQuickLaunchMarker();
        if (quickLaunchMarker != null && quickLaunchMarker.forceNewGame)
        {
            return true;
        }
        return false;
    }

    public static void SetupEditorReturnAfterPlayMode(QuickLaunchMarker marker)
    {
    }

    public static void TryReturnToOriginalScene()
    {
    }

    private static string GetTempFilePath()
    {
        return Path.Combine(Application.temporaryCachePath, "QUICK_LAUNCH.tmp");
    }

    private static void MakeTempFile(string contents)
    {
        DeleteTempFile();
        using StreamWriter streamWriter = FileUtils.CreateTextFile(GetTempFilePath());
        streamWriter.Write(contents);
    }

    private static bool HasTempFile()
    {
        return File.Exists(GetTempFilePath());
    }

    private static void DeleteTempFile()
    {
        string tempFilePath = GetTempFilePath();
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }

    private static string ReadOriginalSceneNameFromTempFile()
    {
        return File.ReadAllText(GetTempFilePath());
    }

    private static bool WantsReturnToOriginalScene()
    {
        if (HasTempFile())
        {
            return true;
        }
        return false;
    }

    private static void ReturnToOriginalScene()
    {
    }
}
