public static class ScoreManager
{
    public static int CurrentScore;
    public static int CorrectLines;
    public static int TotalLines = 20;
    private static bool activityRecorded;

    public static void Reset()
    {
        CurrentScore = 0;
        CorrectLines = 0;
        activityRecorded = false;
    }

    public static void BeginActivity()
    {
        Reset();
    }

    public static void RecordActivity(string activityName, int totalItems)
    {
        if (activityRecorded)
            return;

        activityRecorded = true;

        GameTimer timer = UnityEngine.Object.FindAnyObjectByType<GameTimer>();
        int timeUsedSeconds = timer != null ? timer.GetElapsedSeconds() : 0;

        LearningDataStore.RecordActivity(
            activityName,
            CurrentScore,
            CorrectLines,
            totalItems,
            timeUsedSeconds
        );
    }

    public static void RecordSuccessfulActivity(string activityName, int totalItems)
    {
        RecordActivity(activityName, totalItems);
    }
}
