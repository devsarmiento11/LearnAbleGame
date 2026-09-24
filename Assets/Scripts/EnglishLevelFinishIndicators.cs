using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared by English and Science menus; shows each existing badge for a saved score of at least 75.</summary>
public sealed class EnglishLevelFinishIndicators : MonoBehaviour
{
    [Serializable] public sealed class LevelIndicator
    {
        public int levelNumber;
        public GameObject indicator;
    }
    public string gameScene;
    public LevelIndicator[] levels;
    readonly HashSet<int> completed = new HashSet<int>();
    ListenerRegistration listener;
    string studentId, authUid;
    int generation;

    void Awake() { ConfigureIndicators(); }
    public void ConfigureIndicators()
    {
        completed.Clear();
        foreach (var level in levels) {
            if (level.indicator == null) continue;
            // Badges are decoration, never a blocker over the replay buttons.
            foreach (var graphic in level.indicator.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            foreach (var group in level.indicator.GetComponentsInChildren<CanvasGroup>(true)) group.blocksRaycasts = false;
            level.indicator.SetActive(false);
        }
    }
    async void OnEnable()
    {
        int version = ++generation;
        studentId = LearningDataStore.CurrentUserId;
        if (!LoginSession.IsLoggedIn || LoginSession.Role != "student" || string.IsNullOrWhiteSpace(studentId)) return;
        try {
            if (await FirebaseApp.CheckAndFixDependenciesAsync() != DependencyStatus.Available) return;
            if (this == null || version != generation || !isActiveAndEnabled) return;
            authUid = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
            if (authUid == null || !SameStudent()) return;
            listener = FirebaseFirestore.DefaultInstance.Collection(LearningDataStore.ActivityScoresCollection)
                .WhereEqualTo("userId", studentId).Listen(snapshot => {
                    if (this == null || version != generation) return;
                    if (!SameStudent()) { ResetIndicators(); return; }
                    var scores = new Dictionary<string, double>();
                    foreach (var document in snapshot.Documents) {
                        var data = document.ToDictionary();
                        if (!data.TryGetValue("activityName", out var activity) || !(activity is string name) ||
                            !data.TryGetValue("score", out var raw) || raw == null) continue;
                        double score;
                        try { score = Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture); }
                        catch (Exception error) when (error is FormatException || error is InvalidCastException || error is OverflowException) { continue; }
                        if (score < 0 || score > 100 || double.IsNaN(score)) continue;
                        if (!scores.TryGetValue(name, out var best) || score > best) scores[name] = score;
                    }
                    ApplyScores(scores);
                });
            _ = listener.ListenerTask.ContinueWithOnMainThread(task => {
                if (this != null && version == generation && task.IsFaulted)
                    Debug.LogWarning("Unable to refresh finish indicators. Check the student connection.");
            });
        } catch (Exception) {
            if (this != null && version == generation) Debug.LogWarning("Unable to load finish indicators. Check the student connection.");
        }
    }
    bool SameStudent() => LoginSession.IsLoggedIn && LoginSession.Role == "student" &&
        LearningDataStore.CurrentUserId == studentId && FirebaseAuth.DefaultInstance.CurrentUser?.UserId == authUid;
    public bool IsComplete(int levelNumber) => completed.Contains(levelNumber);

    public void ApplyScores(IDictionary<string, double> scores)
    {
        completed.Clear();
        foreach (var level in levels) {
            bool finished = scores.TryGetValue(gameScene + "Level" + level.levelNumber, out var score) && score >= 75 && score <= 100;
            if (finished) completed.Add(level.levelNumber);
            if (level.indicator != null) level.indicator.SetActive(finished);
        }
        foreach (var levelLock in GetComponentsInChildren<LevelLockController>(true)) levelLock.RefreshLockState();
    }
    void ResetIndicators()
    {
        listener?.Stop(); listener = null;
        ApplyScores(new Dictionary<string, double>());
    }
    void Update() { if (authUid != null && !SameStudent()) { authUid = null; ResetIndicators(); } }
    void OnDisable() { ++generation; ResetIndicators(); }
}
