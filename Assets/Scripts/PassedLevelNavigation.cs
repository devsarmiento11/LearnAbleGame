using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Shows results first, then returns a passed game to its next preview.</summary>
public static class PassedLevelNavigation
{
    private static string pendingMenu;
    private static string pendingStudent;
    private static int pendingLevel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        pendingMenu = null;
        pendingStudent = null;
        pendingLevel = 0;
    }

    public static string Destination(string passedScene, bool passed = true)
    {
        Reset();
        if (!passed)
            return passedScene;

        var match = Regex.Match(SceneManager.GetActiveScene().name,
            @"^((?:English|Math|Science)Game\d+)Level([1-5])$");
        if (!match.Success || !Application.CanStreamedLevelBeLoaded(match.Groups[1].Value))
            return passedScene;

        pendingMenu = match.Groups[1].Value;
        pendingStudent = LearningDataStore.CurrentUserId;
        pendingLevel = Mathf.Min(int.Parse(match.Groups[2].Value) + 1, 5);
        return passedScene;
    }

    public static string ContinueDestination(string requestedScene)
    {
        // Only replace the existing result Continue action. Settings and retry
        // keep their own routes, and failed attempts never advance a level.
        if (SceneManager.GetActiveScene().name != "ScoreScene" ||
            requestedScene != "ModuleActivitiesScene")
            return requestedScene;

        if (pendingMenu != null && pendingStudent == LearningDataStore.CurrentUserId &&
            Application.CanStreamedLevelBeLoaded(pendingMenu))
            return pendingMenu;

        Reset();
        return requestedScene;
    }

    public static int ConsumePreview(string menuScene, int levelCount)
    {
        int index = 0;
        if (pendingMenu == menuScene && pendingStudent == LearningDataStore.CurrentUserId)
            index = Mathf.Clamp(pendingLevel - 1, 0, Mathf.Max(0, levelCount - 1));
        Reset();
        return index;
    }
}
