using UnityEngine;

public static class LevelUnlockStorage
{
    private const string Prefix = "LearnAble_Unlocked_";

    // ==========================================
    // CREATE UNIQUE KEY
    // ==========================================

    private static string GetKey(
        string activityId,
        int levelNumber)
    {
        return Prefix +
               activityId +
               "_Level" +
               levelNumber;
    }

    // ==========================================
    // CHECK IF LEVEL IS UNLOCKED
    // ==========================================

    public static bool IsUnlocked(
        string activityId,
        int levelNumber)
    {
        // Level 1 is ALWAYS unlocked
        if (levelNumber <= 1)
            return true;

        string key =
            GetKey(activityId, levelNumber);

        return PlayerPrefs.GetInt(
            key,
            0
        ) == 1;
    }

    // ==========================================
    // UNLOCK LEVEL
    // ==========================================

    public static void UnlockLevel(
        string activityId,
        int levelNumber)
    {
        string key =
            GetKey(activityId, levelNumber);

        PlayerPrefs.SetInt(
            key,
            1
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Unlocked: " +
            activityId +
            " Level " +
            levelNumber
        );
    }

    // ==========================================
    // OPTIONAL - LOCK AGAIN
    // ==========================================

    public static void LockLevel(
        string activityId,
        int levelNumber)
    {
        if (levelNumber <= 1)
            return;

        string key =
            GetKey(activityId, levelNumber);

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();

        Debug.Log(
            "Locked: " +
            activityId +
            " Level " +
            levelNumber
        );
    }
}