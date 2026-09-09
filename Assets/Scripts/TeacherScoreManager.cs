using System;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;
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

    [Header("SUBJECT FILTER")]
    public Sprite subjectArrow;
    private TMP_Dropdown subjectDropdown;
    private readonly List<DocumentSnapshot> cachedScores = new List<DocumentSnapshot>();
    private string selectedSubject = "All Subjects";
    private bool scoresLoaded;
    private bool scoresFailed;
    private string scoreLoadError;

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
        SetupSubjectFilter();

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

        cachedScores.Clear();
        scoresLoaded = false;
        scoresFailed = false;
        selectedSubject = "All Subjects";
        if (subjectDropdown != null)
        {
            subjectDropdown.Hide();
            subjectDropdown.SetValueWithoutNotify(0);
            subjectDropdown.captionText.text = "All Subjects";
        }
        ClearScoreRows();
        ShowScoreStatus("Loading scores...");
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

        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            scoresFailed = true;
            scoreLoadError = "Sign in with your teacher account to view activity scores.";
            ShowScoreStatus(scoreLoadError);
            return;
        }

        scoreListener = db.Collection(LearningDataStore.ActivityScoresCollection)
            .WhereEqualTo("userId", selectedStudentId)
            .Listen(snapshot =>
            {
                if (requestVersion != scoreRequestVersion)
                    return;

                cachedScores.Clear();
                cachedScores.AddRange(snapshot.Documents);
                scoresLoaded = true;
                scoresFailed = false;
                RebuildScoreRows(cachedScores);
            });
        scoreListener.ListenerTask.ContinueWithOnMainThread(task =>
        {
            if (this == null || requestVersion != scoreRequestVersion || !task.IsFaulted) return;
            scoresFailed = true;
            var error = task.Exception.GetBaseException();
            scoreLoadError = error is FirestoreException firestore && firestore.ErrorCode == FirestoreError.PermissionDenied
                ? "Score access denied. Sign in again with your teacher account."
                : "Unable to connect. Close and reopen to retry.";
            ShowScoreStatus(scoreLoadError);
            Debug.LogWarning("Score listener failed: " + task.Exception.GetBaseException().Message);
        });
    }

    private void SetupSubjectFilter()
    {
        if (studentScorePopup == null) return;
        subjectDropdown = ScoreSubjectDropdown.Create(studentScorePopup.transform,
            TMP_Settings.defaultFontAsset, subjectArrow);
        subjectDropdown.onValueChanged.AddListener(OnSubjectChanged);
        if (noScoresText == null)
        {
            var labelObject = new GameObject("ScoreStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(studentScorePopup.transform, false);
            noScoresText = labelObject.GetComponent<TextMeshProUGUI>();
            noScoresText.font = TMP_Settings.defaultFontAsset;
            noScoresText.fontSize = 19;
            noScoresText.color = new Color(0.35f, 0.16f, 0.04f);
            noScoresText.alignment = TextAlignmentOptions.Center;
            noScoresText.raycastTarget = false;
            var rect = noScoresText.rectTransform;
            rect.anchorMin = new Vector2(0.08f, 0.22f);
            rect.anchorMax = new Vector2(0.92f, 0.48f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }

    private void OnSubjectChanged(int index)
    {
        if (index < 0 || index >= ScoreSubjectFilter.Subjects.Length) return;
        selectedSubject = ScoreSubjectFilter.Subjects[index];
        subjectDropdown.captionText.text = selectedSubject;
        if (scoresLoaded) RebuildScoreRows(cachedScores);
        else ShowScoreStatus(scoresFailed ? scoreLoadError : "Loading scores...");
    }

    private void ShowScoreStatus(string message)
    {
        if (noScoresText == null) return;
        noScoresText.text = message;
        noScoresText.gameObject.SetActive(!string.IsNullOrEmpty(message));
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
            if (!ScoreSubjectFilter.Matches(sceneName, selectedSubject)) continue;
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
        ordered.Sort((a, b) => {
            int recent = b.LatestCompletion.CompareTo(a.LatestCompletion);
            return recent != 0 ? recent : string.Compare(a.SceneName, b.SceneName, StringComparison.OrdinalIgnoreCase);
        });

        foreach (ActivitySummary summary in ordered)
            AddScoreRow(summary);

        ResetScoreScrollToTop();

        ShowScoreStatus(ordered.Count == 0 ? (selectedSubject == "All Subjects" ? "No activity scores yet." : "No " + selectedSubject + " activity scores yet.") : string.Empty);
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
            {
                row.SetActive(false);
                Destroy(row);
            }
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
        if (subjectDropdown != null) subjectDropdown.Hide();

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
        if (subjectDropdown != null) subjectDropdown.onValueChanged.RemoveListener(OnSubjectChanged);
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



/// <summary>Builds the compact subject selector with the scene's existing arrow artwork.</summary>
public static class ScoreSubjectDropdown
{
    public static TMP_Dropdown Create(Transform parent, TMP_FontAsset font, Sprite arrow)
    {
        // Use the bundled display font; the existing Lilita asset has no source font.
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Anton SDF") ?? font;
        RectTransform root = Rect("SubjectDropdown", parent);
        root.anchorMin = new Vector2(0.04f, 0.637f);
        root.anchorMax = new Vector2(0.333f, 0.637f);
        root.sizeDelta = new Vector2(0, 32);
        var background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.93f, 0.28f, 0.015f);
        var dropdown = root.gameObject.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = background;
        var heading = Text("ActivityHeading", root, font);
        Stretch(heading.rectTransform, 9, 151, 0, 0);
        heading.text = "ACTIVITY";
        heading.fontSize = 16;
        heading.fontStyle = FontStyles.Normal;
        heading.color = Color.white;
        heading.faceColor = Color.white;
        heading.outlineColor = new Color32(65, 24, 8, 255);
        heading.outlineWidth = 0.2f;
        var caption = Text("SelectedSubject", root, font);
        Stretch(caption.rectTransform, 100, 38, 0, 0);
        caption.fontSize = 16;
        caption.fontStyle = FontStyles.Normal;
        caption.color = Color.white;
        caption.faceColor = Color.white;
        caption.outlineColor = new Color32(65, 24, 8, 255);
        caption.outlineWidth = 0.2f;
        heading.textWrappingMode = TextWrappingModes.NoWrap;
        caption.textWrappingMode = TextWrappingModes.NoWrap;
        caption.overflowMode = TextOverflowModes.Ellipsis;
        caption.alignment = TextAlignmentOptions.Midline;
        dropdown.captionText = caption;
        var arrowRect = Rect("SubjectArrow", root);
        arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(1, 0.5f);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-2, 0);
        arrowRect.sizeDelta = new Vector2(30, 30);
        var arrowImage = arrowRect.gameObject.AddComponent<Image>();
        arrowImage.sprite = arrow;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;

        RectTransform template = Rect("Template", root);
        template.anchorMin = new Vector2(0, 0);
        template.anchorMax = new Vector2(1, 0);
        template.pivot = new Vector2(0.5f, 1);
        template.anchoredPosition = new Vector2(0, -3);
        template.sizeDelta = new Vector2(0, 158);
        template.gameObject.AddComponent<Image>().color = new Color(1, 0.95f, 0.8f);
        var scroll = template.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        var viewport = Rect("Viewport", template);
        Stretch(viewport, 3, 3, 3, 3);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = new Vector2(0, 38);
        var item = Rect("Item", content);
        item.anchorMin = new Vector2(0, 0.5f);
        item.anchorMax = new Vector2(1, 0.5f);
        item.sizeDelta = new Vector2(0, 38);
        var itemBackground = item.gameObject.AddComponent<Image>();
        itemBackground.color = new Color(1, 0.93f, 0.72f);
        var toggle = item.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = itemBackground;
        var selected = Rect("Selected", item);
        selected.anchorMin = new Vector2(0, 0);
        selected.anchorMax = new Vector2(0, 1);
        selected.pivot = new Vector2(0, 0.5f);
        selected.sizeDelta = new Vector2(5, -8);
        toggle.graphic = selected.gameObject.AddComponent<Image>();
        toggle.graphic.color = new Color(0.9f, 0.3f, 0.03f);
        var label = Text("ItemLabel", item, font);
        Stretch(label.rectTransform, 16, 10, 0, 0);
        label.color = new Color(0.3f, 0.13f, 0.04f);
        label.fontSize = 18;
        dropdown.itemText = label;
        scroll.viewport = viewport;
        scroll.content = content;
        dropdown.template = template;
        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string>(ScoreSubjectFilter.Subjects));
        template.gameObject.SetActive(false);
        dropdown.RefreshShownValue();
        caption.text = "All Subjects";
        return dropdown;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font)
    {
        var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.richText = false;
        return text;
    }
    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}







