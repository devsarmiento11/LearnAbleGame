using System;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Persists user profiles and completed learning activities in Cloud Firestore.
/// Call SetCurrentUser after a successful login before recording scores.
/// </summary>
public static class LearningDataStore
{
    public const string UsersCollection = "users";
    public const string ActivityScoresCollection = "activityScores";

    private const string CurrentUserIdKey = "CurrentUserId";

    public static string CurrentUserId => PlayerPrefs.GetString(CurrentUserIdKey, string.Empty);

    public static void SetCurrentUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            Debug.LogError("A user ID is required to start a session.");
            return;
        }

        PlayerPrefs.SetString(CurrentUserIdKey, userId.Trim());
        PlayerPrefs.Save();
    }

    public static void ClearCurrentUser()
    {
        PlayerPrefs.DeleteKey(CurrentUserIdKey);
        PlayerPrefs.Save();
    }

    public static void CreateOrUpdateUser(string userId, string username, string schoolId, string name, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("User ID and username are required when saving a user.");
            return;
        }

        var user = new Dictionary<string, object>
        {
            { "userId", userId.Trim() },
            { "username", username.Trim() },
            { "schoolId", schoolId?.Trim() ?? string.Empty },
            { "name", name?.Trim() ?? string.Empty },
            { "role", role.ToString().ToLowerInvariant() },
            { "updatedAt", FieldValue.ServerTimestamp }
        };

        FirebaseFirestore.DefaultInstance
            .Collection(UsersCollection)
            .Document(userId.Trim())
            .SetAsync(user, SetOptions.MergeAll)
            .ContinueWith(task => LogResult(task, "save user profile"));
    }

    public static void RecordSuccessfulActivity(string activityName, int score, int correctAnswers, int totalItems)
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
        {
            Debug.LogWarning("Score was not saved because no logged-in user is set.");
            return;
        }

        var activity = new Dictionary<string, object>
        {
            { "userId", CurrentUserId },
            { "activityName", activityName },
            { "score", Mathf.Clamp(score, 0, 100) },
            { "correctAnswers", Mathf.Max(0, correctAnswers) },
            { "totalItems", Mathf.Max(0, totalItems) },
            { "completedAt", FieldValue.ServerTimestamp }
        };

        FirebaseFirestore.DefaultInstance
            .Collection(ActivityScoresCollection)
            .Document()
            .SetAsync(activity)
            .ContinueWith(task => LogResult(task, "save activity score"));
    }

    private static void LogResult(System.Threading.Tasks.Task task, string operation)
    {
        if (task.IsFaulted)
        {
            Debug.LogError("Unable to " + operation + ": " + task.Exception);
        }
    }
}

public enum UserRole
{
    Student,
    Parent,
    Teacher,
    Admin
}
