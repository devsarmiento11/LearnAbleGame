using System;
using System.Collections.Generic;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeacherScoreManager : MonoBehaviour
{
    [Header("STUDENT LIST")]
    public Transform content;
    public GameObject studentRowPrefab;

    [Header("STUDENT SCORE POPUP")]
    public GameObject studentScorePopup;
    public TMP_Text popupStudentNameText;
    public TMP_Text popupStudentGradeText;
    public Transform scoreContent;
    public GameObject scoreDetailRowPrefab;
    public TMP_Text noScoresText;

    private FirebaseFirestore db;
    private ListenerRegistration studentListener;
    private ListenerRegistration scoreListener;
    private int scoreRequestVersion;
    private readonly List<GameObject> scoreRows = new List<GameObject>();

    private Dictionary<string, GameObject> studentRows =
        new Dictionary<string, GameObject>();

    // Remembers which student is currently selected
    private string selectedStudentId = "";
    private string selectedStudentName = "";
    private string selectedStudentGrade = "";

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        // Hide popup when this scene starts
        if (studentScorePopup != null)
        {
            studentScorePopup.SetActive(false);
        }

        EnsureScoreContent();

        ListenForStudents();
    }

    private void EnsureScoreContent()
    {
        if (scoreContent != null || studentScorePopup == null)
            return;

        GameObject container = new GameObject("ScoreContent", typeof(RectTransform));
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.SetParent(studentScorePopup.transform, false);
        rect.anchorMin = new Vector2(0.04f, 0.08f);
        rect.anchorMax = new Vector2(0.96f, 0.72f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 4f;

        scoreContent = rect;
    }

    void ListenForStudents()
    {
        studentListener = db.Collection(LearningDataStore.UsersCollection)
            .WhereEqualTo("role", "student")
            .Listen(snapshot =>
            {
                var students = new List<StudentProfile>();

                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    if (!document.Exists)
                        continue;

                    Dictionary<string, object> data = document.ToDictionary();

                    // Scores are saved with the authenticated profile document ID.
                    // Always query with that same canonical ID; the userId field may
                    // contain a different school/login identifier.
                    string studentId = document.Id;

                    string studentName =
                        GetStudentName(data, studentId);

                    string grade =
                        GetText(data, "grade", string.Empty);

                    students.Add(
                        new StudentProfile(
                            studentId,
                            studentName,
                            grade
                        )
                    );
                }

                students.Sort((left, right) =>
                    string.Compare(
                        left.Name,
                        right.Name,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

                RebuildStudentRows(students);
            });
    }

    private void RebuildStudentRows(
        List<StudentProfile> students)
    {
        foreach (GameObject existingRow in studentRows.Values)
        {
            if (existingRow != null)
                Destroy(existingRow);
        }

        studentRows.Clear();

        foreach (StudentProfile student in students)
        {
            AddStudent(
                student.Id,
                student.Name,
                student.Grade
            );
        }
    }

    private void AddStudent(
        string studentId,
        string studentName,
        string grade)
    {
        if (studentRowPrefab == null)
        {
            Debug.LogError(
                "TeacherScoreManager: Student Row Prefab is not assigned!"
            );
            return;
        }

        if (content == null)
        {
            Debug.LogError(
                "TeacherScoreManager: Content is not assigned!"
            );
            return;
        }

        GameObject newRow = Instantiate(
            studentRowPrefab,
            content
        );

        newRow.SetActive(true);

        StudentScoreRowUI row =
            newRow.GetComponent<StudentScoreRowUI>();

        if (row != null)
        {
            row.Setup(
                studentId,
                studentName,
                grade,
                this
            );
        }
        else
        {
            Debug.LogWarning(
                "StudentRow prefab does not have StudentScoreRowUI."
            );
        }

        if (!studentRows.ContainsKey(studentId))
        {
            studentRows.Add(studentId, newRow);
        }
    }

    private static string GetStudentName(
        Dictionary<string, object> data,
        string fallback)
    {
        string fullName =
            GetText(data, "name", string.Empty);

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        string firstName =
            GetText(data, "firstName", string.Empty);

        string middleName =
            GetText(data, "middleName", string.Empty);

        string lastName =
            GetText(data, "lastName", string.Empty);

        string combinedName =
            (firstName + " " + middleName + " " + lastName)
            .Trim();

        while (combinedName.Contains("  "))
        {
            combinedName =
                combinedName.Replace("  ", " ");
        }

        return string.IsNullOrWhiteSpace(combinedName)
            ? fallback
            : combinedName;
    }

    private static string GetText(
        Dictionary<string, object> data,
        string field,
        string fallback)
    {
        object value;

        return data.TryGetValue(field, out value)
            && value != null
            ? value.ToString().Trim()
            : fallback;
    }

    // Called automatically by ViewScoreButton
    public void OpenStudentScores(
        string studentId,
        string studentName,
        string grade)
    {
        selectedStudentId = studentId;
        selectedStudentName = studentName;
        selectedStudentGrade = grade;

        Debug.Log(
            "Opening scores for " +
            studentName +
            " | " +
            studentId
        );

        if (studentScorePopup == null)
        {
            Debug.LogError(
                "TeacherScoreManager: Student Score Popup is not assigned!"
            );
            return;
        }

        // Put student's name in popup
        if (popupStudentNameText != null)
        {
            popupStudentNameText.text =
                studentName.ToUpper();
        }

        // Clean grade so "GRADE 2" becomes just "2"
        if (popupStudentGradeText != null)
        {
            string cleanGrade = grade;

            if (!string.IsNullOrEmpty(cleanGrade))
            {
                cleanGrade = cleanGrade
                    .Replace("GRADE", "")
                    .Replace("Grade", "")
                    .Replace("grade", "")
                    .Trim();
            }

            popupStudentGradeText.text =
                cleanGrade;
        }

        // Show popup
        studentScorePopup.SetActive(true);
        ListenForSelectedStudentScores();
    }

    private void ListenForSelectedStudentScores()
    {
        int requestVersion = ++scoreRequestVersion;

        if (scoreListener != null)
        {
            scoreListener.Stop();
            scoreListener = null;
        }

        if (string.IsNullOrWhiteSpace(selectedStudentId))
            return;

        scoreListener = db.Collection(LearningDataStore.ActivityScoresCollection)
            .WhereEqualTo("userId", selectedStudentId)
            .Listen(snapshot =>
            {
                if (requestVersion != scoreRequestVersion)
                    return;

                RebuildScoreRows(snapshot.Documents);
            });
    }

    private void RebuildScoreRows(IEnumerable<DocumentSnapshot> documents)
    {
        ClearScoreRows();

        var summaries = new Dictionary<string, ActivitySummary>(StringComparer.OrdinalIgnoreCase);

        foreach (DocumentSnapshot document in documents)
        {
            if (!document.Exists)
                continue;

            Dictionary<string, object> data = document.ToDictionary();
            string sceneName = GetText(data, "activityName", "Unknown Activity");
            int score = GetInt(data, "score");
            int timeUsed = GetInt(data, "timeUsedSeconds");
            DateTime completedAt = GetDateTime(data, "completedAt");

            ActivitySummary summary;
            if (!summaries.TryGetValue(sceneName, out summary))
            {
                summary = new ActivitySummary(sceneName);
                summaries.Add(sceneName, summary);
            }

            summary.AddAttempt(score, timeUsed, completedAt);
        }

        var ordered = new List<ActivitySummary>(summaries.Values);
        ordered.Sort((a, b) => string.Compare(a.SceneName, b.SceneName, StringComparison.OrdinalIgnoreCase));

        foreach (ActivitySummary summary in ordered)
            AddScoreRow(summary);

        ResetScoreScrollToTop();

        if (noScoresText != null)
            noScoresText.gameObject.SetActive(ordered.Count == 0);
    }

    private void ResetScoreScrollToTop()
    {
        RectTransform contentRect = scoreContent as RectTransform;
        if (contentRect == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        ScrollRect scrollRect = scoreContent.GetComponentInParent<ScrollRect>();
        if (scrollRect == null)
            return;

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void AddScoreRow(ActivitySummary summary)
    {
        if (scoreContent == null || scoreDetailRowPrefab == null)
            return;

        GameObject newRow = Instantiate(scoreDetailRowPrefab, scoreContent);
        newRow.SetActive(true);
        scoreRows.Add(newRow);

        ScoreDetailRowUI row = newRow.GetComponent<ScoreDetailRowUI>();
        if (row == null)
            return;

        string activity;
        string level;
        SplitActivityAndLevel(summary.SceneName, out activity, out level);

        row.Setup(
            activity,
            level,
            summary.LowestScore + "%",
            summary.HighestScore + "%",
            FormatDuration(summary.TotalTimeUsedSeconds),
            summary.Attempts.ToString(),
            summary.LatestCompletion == DateTime.MinValue ? "—" : summary.LatestCompletion.ToLocalTime().ToString("MM/dd/yy")
        );
    }

    private void ClearScoreRows()
    {
        foreach (GameObject row in scoreRows)
        {
            if (row != null)
                Destroy(row);
        }
        scoreRows.Clear();
    }

    private static int GetInt(Dictionary<string, object> data, string field)
    {
        object value;
        if (!data.TryGetValue(field, out value) || value == null)
            return 0;

        try { return Convert.ToInt32(value); }
        catch (Exception) { return 0; }
    }

    private static DateTime GetDateTime(Dictionary<string, object> data, string field)
    {
        object value;
        if (!data.TryGetValue(field, out value) || value == null)
            return DateTime.MinValue;

        if (value is Timestamp timestamp)
            return timestamp.ToDateTime();

        return DateTime.MinValue;
    }

    private static string FormatDuration(int totalSeconds)
    {
        TimeSpan duration = TimeSpan.FromSeconds(Mathf.Max(0, totalSeconds));
        return duration.TotalHours >= 1d
            ? string.Format("{0}:{1:00}:{2:00}", (int)duration.TotalHours, duration.Minutes, duration.Seconds)
            : string.Format("{0}:{1:00}", duration.Minutes, duration.Seconds);
    }

    private static void SplitActivityAndLevel(string sceneName, out string activity, out string level)
    {
        int marker = sceneName.LastIndexOf("Level", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            activity = sceneName;
            level = "—";
            return;
        }

        activity = sceneName.Substring(0, marker).Replace("Game", " Game ").Trim();
        level = sceneName.Substring(marker + "Level".Length).Trim();
        if (string.IsNullOrEmpty(level))
            level = "—";
    }

    // Connect this to your Close / X button
    public void CloseStudentScores()
    {
        scoreRequestVersion++;

        if (scoreListener != null)
        {
            scoreListener.Stop();
            scoreListener = null;
        }

        if (studentScorePopup != null)
        {
            studentScorePopup.SetActive(false);
        }
    }

    // We will use this later when loading scores
    public string GetSelectedStudentId()
    {
        return selectedStudentId;
    }

    public string GetSelectedStudentName()
    {
        return selectedStudentName;
    }

    public string GetSelectedStudentGrade()
    {
        return selectedStudentGrade;
    }

    void OnDestroy()
    {
        if (studentListener != null)
        {
            studentListener.Stop();
        }

        if (scoreListener != null)
        {
            scoreListener.Stop();
        }
    }

    private class StudentProfile
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Grade;

        public StudentProfile(
            string id,
            string name,
            string grade)
        {
            Id = id;
            Name = name;
            Grade = grade;
        }
    }

    private class ActivitySummary
    {
        public readonly string SceneName;
        public int Attempts { get; private set; }
        public int LowestScore { get; private set; }
        public int HighestScore { get; private set; }
        public int TotalTimeUsedSeconds { get; private set; }
        public DateTime LatestCompletion { get; private set; }

        public ActivitySummary(string sceneName)
        {
            SceneName = sceneName;
            LowestScore = 100;
            HighestScore = 0;
            LatestCompletion = DateTime.MinValue;
        }

        public void AddAttempt(int score, int timeUsedSeconds, DateTime completedAt)
        {
            score = Mathf.Clamp(score, 0, 100);
            Attempts++;
            LowestScore = Mathf.Min(LowestScore, score);
            HighestScore = Mathf.Max(HighestScore, score);
            TotalTimeUsedSeconds += Mathf.Max(0, timeUsedSeconds);
            if (completedAt > LatestCompletion)
                LatestCompletion = completedAt;
        }
    }
}
