using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;

public class StudentProfileTabManager : MonoBehaviour
{
    [Header("PANELS")]
    public GameObject profilePanel;
    public GameObject recentActivityPanel;
    public GameObject gradesPanel;

    [Header("RECENT ACTIVITY")]
    [Tooltip("Row prefab containing ActivityText and LevelText.")]
    public GameObject activityRowPrefab;

    [Header("PROFILE SUBJECT FILTER")]
    public TMP_Dropdown subjectDropdown;

    [Header("RECENT ACTIVITY SUBJECT FILTER")]
    public TMP_Dropdown recentSubjectDropdown;

    private ListenerRegistration activityListener;
    private GameObject rowTemplate;
    private Transform profileContent;
    private Transform recentContent;
    private readonly List<ActivitySummary> profileSummaries = new List<ActivitySummary>();
    private string selectedSubject = "All Subjects";
    private readonly List<ActivityRecord> recentRecords = new List<ActivityRecord>();
    private string selectedRecentSubject = "All Subjects";

    private void Start()
    {
        ShowProfile();
        PrepareRows();
        subjectDropdown = PrepareSubjectDropdown(profilePanel, subjectDropdown, 105);
        subjectDropdown.onValueChanged.AddListener(OnSubjectChanged);
        recentSubjectDropdown = PrepareSubjectDropdown(recentActivityPanel, recentSubjectDropdown, 136);
        recentSubjectDropdown.onValueChanged.AddListener(OnRecentSubjectChanged);
        LoadStudentData();
    }

    private void OnDestroy()
    {
        activityListener?.Stop();
        if (subjectDropdown != null)
            subjectDropdown.onValueChanged.RemoveListener(OnSubjectChanged);
        if (recentSubjectDropdown != null)
            recentSubjectDropdown.onValueChanged.RemoveListener(OnRecentSubjectChanged);
    }

    public void ShowProfile()
    {
        profilePanel.SetActive(true);
        recentActivityPanel.SetActive(false);
        gradesPanel.SetActive(false);
    }

    public void ShowRecentActivity()
    {
        profilePanel.SetActive(false);
        recentActivityPanel.SetActive(true);
        gradesPanel.SetActive(false);
    }

    public void ShowGrades()
    {
        profilePanel.SetActive(false);
        recentActivityPanel.SetActive(false);
        gradesPanel.SetActive(true);
    }

    private void PrepareRows()
    {
        profileContent = Find(profilePanel.transform, "Content");
        recentContent = Find(recentActivityPanel.transform, "Content");
        rowTemplate = Find(profilePanel.transform, "ProfileRow")?.gameObject;

        GameObject sampleRecentRow = Find(recentActivityPanel.transform, "RecentActivityRow")?.gameObject;
        if (sampleRecentRow != null)
            sampleRecentRow.SetActive(false);
        if (rowTemplate != null)
            rowTemplate.SetActive(false);
    }

    private TMP_Dropdown PrepareSubjectDropdown(GameObject panel, TMP_Dropdown dropdown, float y)
    {
        if (dropdown == null)
            dropdown = panel.GetComponentInChildren<TMP_Dropdown>(true);

        if (dropdown == null)
        {
            dropdown = ScoreSubjectDropdown.Create(panel.transform,
                TMP_Settings.defaultFontAsset, null);
            var rect = (RectTransform)dropdown.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(200, 28);
            Find(rect, "ActivityHeading").gameObject.SetActive(false);
            dropdown.captionText.rectTransform.offsetMin = new Vector2(10, 0);

            Transform arrow = Find(rect, "SubjectArrow");
            arrow.GetComponent<UnityEngine.UI.Image>().enabled = false;
            var arrowText = arrow.gameObject.AddComponent<TextMeshProUGUI>();
            arrowText.font = dropdown.captionText.font;
            arrowText.text = "v";
            arrowText.fontSize = 18;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.raycastTarget = false;
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>(ScoreSubjectFilter.Subjects));
        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private void OnSubjectChanged(int index)
    {
        if (index < 0 || index >= ScoreSubjectFilter.Subjects.Length) return;
        selectedSubject = ScoreSubjectFilter.Subjects[index];
        BuildProfileRows(profileSummaries);
    }

    private void OnRecentSubjectChanged(int index)
    {
        if (index < 0 || index >= ScoreSubjectFilter.Subjects.Length) return;
        selectedRecentSubject = ScoreSubjectFilter.Subjects[index];
        BuildRecentRows(recentRecords);
    }

    private void LoadStudentData()
    {
        string userId = LearningDataStore.CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            SetStudentName(LoginSession.StudentName);
            Debug.LogWarning("No logged-in student ID is available for StudentProfileScene.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection(LearningDataStore.UsersCollection).Document(userId).GetSnapshotAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion && task.Result.Exists)
                    SetStudentName(GetStudentName(task.Result));
                else
                {
                    SetStudentName(LoginSession.StudentName);
                    Debug.LogError("Unable to load the logged-in student profile: " + task.Exception);
                }
            });

        activityListener = db.Collection(LearningDataStore.ActivityScoresCollection)
            .WhereEqualTo("userId", userId)
            .Listen(snapshot => RebuildPanels(snapshot.Documents));
    }

    private void SetStudentName(string value)
    {
        TMP_Text text = Find(profilePanel.transform, "StudentNameText")?.GetComponent<TMP_Text>();
        if (text != null)
            text.text = (string.IsNullOrWhiteSpace(value) ? "STUDENT" : value.Trim()).ToUpperInvariant();
    }

    private void RebuildPanels(IEnumerable<DocumentSnapshot> documents)
    {
        var records = new List<ActivityRecord>();
        var summaries = new Dictionary<string, ActivitySummary>(StringComparer.OrdinalIgnoreCase);

        foreach (DocumentSnapshot document in documents)
        {
            Dictionary<string, object> data = document.ToDictionary();
            var record = new ActivityRecord
            {
                Name = GetText(data, "activityName", "Unknown Activity"),
                Score = GetInt(data, "score"),
                Seconds = GetInt(data, "timeUsedSeconds"),
                CompletedAt = GetDate(data, "completedAt")
            };
            records.Add(record);

            if (!summaries.TryGetValue(record.Name, out ActivitySummary summary))
            {
                summary = new ActivitySummary(record.Name);
                summaries.Add(record.Name, summary);
            }
            summary.Add(record);
        }

        records.Sort((a, b) => b.CompletedAt.CompareTo(a.CompletedAt));
        profileSummaries.Clear();
        profileSummaries.AddRange(summaries.Values);
        BuildProfileRows(profileSummaries);
        recentRecords.Clear();
        recentRecords.AddRange(records);
        BuildRecentRows(recentRecords);
    }

    private void BuildProfileRows(IEnumerable<ActivitySummary> summaries)
    {
        ClearRows(profileContent);
        var ordered = new List<ActivitySummary>(summaries);
        ordered.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        foreach (ActivitySummary summary in ordered)
        {
            if (!ScoreSubjectFilter.Matches(summary.Name, selectedSubject)) continue;
            GameObject row = CreateRow(profileContent);
            if (row == null) return;
            FillRow(row, summary.Name, summary.Attempts.ToString(),
                FormatTime(summary.TotalSeconds), summary.Highest, summary.Lowest);
        }

        ResetScrollPosition(profileContent);
    }

    private static void ResetScrollPosition(Transform content)
    {
        if (content is RectTransform contentRect)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            var scroll = content.GetComponentInParent<UnityEngine.UI.ScrollRect>(true);
            if (scroll != null)
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1;
            }
        }
    }

    private void BuildRecentRows(IEnumerable<ActivityRecord> records)
    {
        ClearRows(recentContent);
        foreach (ActivityRecord record in records)
        {
            if (!ScoreSubjectFilter.Matches(record.Name, selectedRecentSubject)) continue;
            GameObject row = CreateRecentActivityRow();
            if (row == null) return;
            SetText(row, "ActivityText", GetActivityName(record.Name));
            SetText(row, "LevelText", GetLevel(record.Name));
        }
        ResetScrollPosition(recentContent);
    }

    private GameObject CreateRecentActivityRow()
    {
        if (activityRowPrefab == null || recentContent == null)
        {
            Debug.LogError("StudentProfileTabManager: Activity Row prefab is not assigned.");
            return null;
        }

        GameObject row = Instantiate(activityRowPrefab, recentContent);
        row.name = "StudentActivityRow_Generated";
        row.SetActive(true);
        return row;
    }

    private GameObject CreateRow(Transform parent)
    {
        if (rowTemplate == null || parent == null) return null;
        GameObject row = Instantiate(rowTemplate, parent);
        row.name = "StudentActivityRow_Generated";
        row.SetActive(true);
        return row;
    }

    private static void FillRow(GameObject row, string activityName, string attempts,
        string time, int highest, int lowest)
    {
        SetText(row, "ActivityText", FormatActivityName(activityName));
        SetText(row, "LevelText", GetLevel(activityName));
        SetText(row, "AttemptsText", attempts);
        SetText(row, "TimeUsedText", time);
        SetText(row, "HighestScoreText", highest.ToString());
        SetText(row, "LowestScoreText", lowest.ToString());
    }

    private static void ClearRows(Transform content)
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            if (content.GetChild(i).name == "StudentActivityRow_Generated")
            {
                content.GetChild(i).gameObject.SetActive(false);
                Destroy(content.GetChild(i).gameObject);
            }
    }

    private static void SetText(GameObject row, string name, string value)
    {
        TMP_Text text = Find(row.transform, name)?.GetComponent<TMP_Text>();
        if (text != null) text.text = value;
    }

    private static string GetStudentName(DocumentSnapshot profile)
    {
        string name = GetDocumentText(profile, "name");
        if (!string.IsNullOrWhiteSpace(name)) return name;
        return string.Join(" ", new[] { GetDocumentText(profile, "firstName"),
            GetDocumentText(profile, "middleName"), GetDocumentText(profile, "lastName") }).Trim();
    }

    private static string GetDocumentText(DocumentSnapshot document, string field)
    {
        if (!document.ContainsField(field)) return string.Empty;
        return document.GetValue<object>(field)?.ToString().Trim() ?? string.Empty;
    }

    private static string GetText(Dictionary<string, object> data, string field, string fallback)
    {
        if (!data.TryGetValue(field, out object value) || value == null) return fallback;
        string text = value.ToString().Trim();
        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private static int GetInt(Dictionary<string, object> data, string field)
    {
        return data.TryGetValue(field, out object value) && value != null &&
            int.TryParse(value.ToString(), out int result) ? result : 0;
    }

    private static DateTime GetDate(Dictionary<string, object> data, string field)
    {
        return data.TryGetValue(field, out object value) && value is Timestamp timestamp
            ? timestamp.ToDateTime() : DateTime.MinValue;
    }

    private static string FormatTime(int value)
    {
        int seconds = Mathf.Max(0, value);
        return seconds >= 60 ? seconds / 60 + "m " + seconds % 60 + "s" : seconds + "s";
    }

    private static string GetLevel(string name)
    {
        int index = name.Length - 1;
        while (index >= 0 && char.IsDigit(name[index])) index--;
        return index == name.Length - 1 ? "-" : name.Substring(index + 1);
    }

    private static string GetActivityName(string name)
    {
        int levelMarker = name.LastIndexOf("Level", StringComparison.OrdinalIgnoreCase);
        string activity = levelMarker < 0 ? name : name.Substring(0, levelMarker);
        return FormatActivityName(activity.Replace("Game", " Game ").Trim());
    }

    private static string FormatActivityName(string name)
    {
        var text = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && char.IsLower(name[i - 1])) text.Append(' ');
            text.Append(name[i]);
        }
        return text.ToString().Trim();
    }

    private static Transform Find(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = Find(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private sealed class ActivityRecord
    {
        public string Name;
        public int Score;
        public int Seconds;
        public DateTime CompletedAt;
    }

    private sealed class ActivitySummary
    {
        public readonly string Name;
        public int Attempts { get; private set; }
        public int TotalSeconds { get; private set; }
        public int Highest { get; private set; }
        public int Lowest { get; private set; } = 100;

        public ActivitySummary(string name) { Name = name; }

        public void Add(ActivityRecord record)
        {
            Attempts++;
            TotalSeconds += Mathf.Max(0, record.Seconds);
            Highest = Mathf.Max(Highest, record.Score);
            Lowest = Mathf.Min(Lowest, record.Score);
        }
    }
}
