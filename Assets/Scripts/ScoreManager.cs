public static class ScoreManager
{
    public static int CurrentScore;
    public static int CorrectLines;
    public static int TotalLines = 20;

    public static void Reset()
    {
        CurrentScore = 0;
        CorrectLines = 0;
    }

    public static void RecordSuccessfulActivity(string activityName, int totalItems)
    {
        LearningDataStore.RecordSuccessfulActivity(
            activityName,
            CurrentScore,
            CorrectLines,
            totalItems
        );
    }
}
