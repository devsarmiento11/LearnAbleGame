using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LeaderboardScene : MonoBehaviour
{
    private sealed class Student
    {
        public string Id, Name;
        public double Total;
    }

    private sealed class Row
    {
        public RectTransform Root;
        public TMP_Text Rank, Name, Total, Average;

        public Row(RectTransform root)
        {
            Root = root;
            Rank = root.Find("Rank of the student").GetComponent<TMP_Text>();
            Name = root.Find("First Name and Last Name of Student").GetComponent<TMP_Text>();
            Total = root.Find("Overall total score of the Student").GetComponent<TMP_Text>();
            Average = root.Find("AverageScoreOfTheStudentupto100%").GetComponent<TMP_Text>();
            Name.richText = false;
        }
    }

    private readonly List<ListenerRegistration> listeners = new List<ListenerRegistration>();
    private readonly List<Row> rows = new List<Row>();
    private readonly HashSet<string> activities = new HashSet<string>();
    private QuerySnapshot profiles, scores, starts;
    private TMP_Dropdown sets;
    private TMP_Text schoolYear, topScore, classAverage;
    private ScrollRect scroll;
    private string year = "", selectedSet = "";
    private float rowSpacing, minimumHeight;
    private bool failed;

    private void Awake()
    {
        var canvas = GetComponentInParent<Canvas>();
        var labels = canvas.GetComponentsInChildren<TMP_Text>(true);
        schoolYear = labels.First(label => label.name == "School year");
        topScore = labels.First(label => label.name == "TopScore");
        classAverage = labels.First(label => label.name == "OverallClassAveragePerSet");
        sets = canvas.GetComponentsInChildren<TMP_Dropdown>(true).First(menu => menu.name == "Set Dropdown");
        scroll = GetComponent<ScrollRect>();

        Canvas.ForceUpdateCanvases();
        var samples = transform.Cast<Transform>().OfType<RectTransform>()
            .Where(child => child.name == "Data" || child.name.StartsWith("Data (", StringComparison.Ordinal))
            .OrderByDescending(child => child.anchoredPosition.y).ToArray();
        rowSpacing = (samples[0].anchoredPosition.y - samples[6].anchoredPosition.y) / 6f;
        // The authored rows sit below the center of the ScrollRect. Align the
        // invisible viewport with those rows before moving them into its mask.
        var scrollBounds = (RectTransform)transform;
        // The template's visible artwork sits above its row root.
        float artworkOffset = samples[0].GetChild(0).localPosition.y;
        Vector2 viewportMin = scroll.viewport.offsetMin;
        Vector2 viewportMax = scroll.viewport.offsetMax;
        viewportMin.y = samples[6].localPosition.y + artworkOffset - rowSpacing * 0.5f - scrollBounds.rect.yMin;
        viewportMax.y = samples[0].localPosition.y + artworkOffset + rowSpacing * 0.5f - scrollBounds.rect.yMax;
        scroll.viewport.offsetMin = viewportMin;
        scroll.viewport.offsetMax = viewportMax;
        // Clip by the viewport rectangle instead of the legacy sprite stencil.
        // Keep the viewport graphic available for pointer and touch input.
        var stencil = scroll.viewport.GetComponent<Mask>();
        if (stencil != null) stencil.enabled = false;
        var viewportGraphic = scroll.viewport.GetComponent<Graphic>();
        if (viewportGraphic != null)
        {
            Color color = viewportGraphic.color;
            color.a = 0f;
            viewportGraphic.color = color;
        }
        if (scroll.viewport.GetComponent<RectMask2D>() == null)
            scroll.viewport.gameObject.AddComponent<RectMask2D>();
        Canvas.ForceUpdateCanvases();
        minimumHeight = scroll.viewport.rect.height;
        scroll.content.anchoredPosition = Vector2.zero;
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minimumHeight);
        float firstRowY = samples[0].anchoredPosition.y;
        foreach (var sample in samples)
        {
            float x = sample.anchoredPosition.x;
            float y = sample.anchoredPosition.y - firstRowY - artworkOffset - rowSpacing * 0.5f;
            sample.SetParent(scroll.content, false);
            sample.anchorMin = sample.anchorMax = new Vector2(0.5f, 1f);
            sample.anchoredPosition = new Vector2(x, y);
            sample.gameObject.SetActive(false);
            rows.Add(new Row(sample));
        }

        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        sets.onValueChanged.AddListener(SelectSet);
        sets.interactable = false;
        sets.captionText.text = "—";
        schoolYear.text = topScore.text = classAverage.text = "—";
    }

    private async void Start()
    {
        try
        {
#if UNITY_EDITOR
            var paths = UnityEditor.EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path);
#else
            var paths = Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
                .Select(SceneUtility.GetScenePathByBuildIndex);
#endif
            foreach (string path in paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string key = LeaderboardScoring.ActivityKey(name);
                if (key != null) activities.Add(key);
            }

            var teacher = FirebaseAuth.DefaultInstance.CurrentUser;
            if (teacher == null || LoginSession.Role != "teacher")
                throw new InvalidOperationException("A teacher session is required.");
            await teacher.TokenAsync(true);
            if (this == null) return;
            var db = FirebaseFirestore.DefaultInstance;
            Watch(db.Collection("adminSettings").Document("schoolYear").Listen(MetadataChanges.Include, snapshot =>
            {
                if (failed || this == null || snapshot.Metadata.IsFromCache) return;
                string current = snapshot.Exists ? ReadText(snapshot.ToDictionary(), "currentSchoolYear") : "";
                bool changed = current != year;
                year = current;
                schoolYear.text = year.Length == 0 ? "—" : year;
                Refresh(changed);
            }));
            Watch(db.Collection("users").WhereEqualTo("role", "student")
                .Listen(MetadataChanges.Include, snapshot => Accept(snapshot, ref profiles)));
            Watch(db.Collection(LearningDataStore.ActivityScoresCollection)
                .Listen(MetadataChanges.Include, snapshot => Accept(snapshot, ref scores)));
            Watch(db.Collection(LearningDataStore.LeaderboardStartsCollection)
                .Listen(MetadataChanges.Include, snapshot => Accept(snapshot, ref starts)));
        }
        catch (Exception error)
        {
            if (this != null) ShowError(error);
        }
    }

    private void Watch(ListenerRegistration listener)
    {
        listeners.Add(listener);
        listener.ListenerTask.ContinueWithOnMainThread(task =>
        {
            if (this != null && task.IsFaulted) ShowError(task.Exception);
        });
    }

    private void Accept(QuerySnapshot snapshot, ref QuerySnapshot target)
    {
        if (this == null || failed || snapshot.Metadata.IsFromCache) return;
        target = snapshot;
        Refresh(false);
    }

    private void ShowError(Exception error)
    {
        failed = true;
        schoolYear.text = "Unavailable";
        sets.interactable = false;
        sets.captionText.text = "—";
        Render(new List<Student>(), true);
        Debug.LogError("Unable to load the leaderboard: " + error, this);
    }

    private static string ReadText(IDictionary<string, object> fields, string name)
    {
        return fields.TryGetValue(name, out object value) && value is string text ? text.Trim() : "";
    }

    private static string CreationYear(IDictionary<string, object> fields)
    {
        // Promotions replace schoolYear and preserve the year the student came from.
        if (fields.TryGetValue("promotions", out object raw) && raw is IDictionary<string, object> promotions)
        {
            foreach (var promotion in promotions.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                if (promotion.Value is IDictionary<string, object> details)
                {
                    string source = ReadText(details, "sourceYear");
                    if (source.Length > 0) return source;
                }
        }
        return ReadText(fields, "schoolYear");
    }

    private void SelectSet(int index)
    {
        if (index < 0 || index >= sets.options.Count) return;
        selectedSet = sets.options[index].text;
        Refresh(true);
    }

    private void Refresh(bool resetScroll)
    {
        if (failed || profiles == null) return;
        var roster = profiles.Documents
            .Select(doc => (Id: doc.Id, Fields: doc.ToDictionary()))
            .Where(student => year.Length > 0 && CreationYear(student.Fields) == year &&
                !string.Equals(ReadText(student.Fields, "status"), "Archived", StringComparison.OrdinalIgnoreCase))
            .Select(student => (student.Id, Set: ReadText(student.Fields, "set"),
                AuthUid: ReadText(student.Fields, "authUid"),
                Name: (ReadText(student.Fields, "firstName") + " " + ReadText(student.Fields, "lastName")).Trim()))
            .Where(student => student.Set.Length > 0)
            .ToArray();

        var options = roster.Select(student => student.Set).Distinct().OrderBy(set => set, StringComparer.Ordinal).ToList();
        if (!sets.options.Select(option => option.text).SequenceEqual(options))
        {
            sets.Hide();
            sets.ClearOptions();
            sets.AddOptions(options);
        }
        if (!options.Contains(selectedSet))
        {
            selectedSet = options.FirstOrDefault() ?? "";
            resetScroll = true;
        }
        sets.interactable = options.Count > 0;
        if (options.Count > 0)
        {
            sets.SetValueWithoutNotify(options.IndexOf(selectedSet));
            sets.RefreshShownValue();
        }
        else sets.captionText.text = "—";

        if (scores == null || starts == null)
        {
            Render(new List<Student>(), resetScroll);
            return;
        }

        var eligible = roster.Where(student => student.Set == selectedSet)
            .ToDictionary(student => student.Id);
        var best = new Dictionary<string, Dictionary<string, double>>();
        foreach (var start in starts.Documents)
            if (eligible.TryGetValue(start.Id, out var student) && student.AuthUid.Length > 0 &&
                ReadText(start.ToDictionary(), "authUid") == student.AuthUid)
                best[start.Id] = new Dictionary<string, double>();
        foreach (var doc in scores.Documents)
        {
            var fields = doc.ToDictionary();
            string id = ReadText(fields, "userId");
            if (!eligible.ContainsKey(id)) continue;
            string key = LeaderboardScoring.ActivityKey(ReadText(fields, "activityName"));
            if (key == null || !activities.Contains(key) || !fields.TryGetValue("score", out object raw) ||
                !LeaderboardScoring.TryPoints(raw, out double points)) continue;
            if (!best.TryGetValue(id, out var results))
                best[id] = results = new Dictionary<string, double>();
            if (!results.TryGetValue(key, out double previous) || points > previous)
                results[key] = points;
        }

        var ranked = best.Select(entry => new Student
            { Id = entry.Key, Name = eligible[entry.Key].Name, Total = entry.Value.Values.Sum() })
            .OrderByDescending(student => student.Total)
            .ThenBy(student => student.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(student => student.Id, StringComparer.Ordinal).ToList();
        Render(ranked, resetScroll);
    }

    private void Render(List<Student> ranked, bool resetScroll)
    {
        float position = scroll.verticalNormalizedPosition;
        while (rows.Count < ranked.Count)
        {
            var clone = Instantiate(rows[6].Root, scroll.content, false);
            clone.name = "Data (" + rows.Count + ")";
            clone.anchoredPosition = rows[6].Root.anchoredPosition -
                Vector2.up * (rowSpacing * (rows.Count - 6));
            rows.Add(new Row(clone));
        }

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            row.Root.gameObject.SetActive(i < ranked.Count);
            if (i >= ranked.Count) continue;
            var student = ranked[i];
            row.Rank.text = (i + 1).ToString(CultureInfo.InvariantCulture);
            row.Name.text = student.Name;
            row.Total.text = Format(student.Total);
            row.Average.text = Format(activities.Count == 0 ? 0 : student.Total / activities.Count) + "%";
        }

        topScore.text = ranked.Count == 0 ? "—" : Format(ranked[0].Total);
        classAverage.text = ranked.Count == 0 ? "—" : Format(ranked.Average(student => student.Total));
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            minimumHeight + Mathf.Max(0, ranked.Count - 7) * rowSpacing);
        Canvas.ForceUpdateCanvases();
        if (resetScroll) scroll.StopMovement();
        scroll.verticalNormalizedPosition = resetScroll || ranked.Count <= 7 ? 1f : Mathf.Clamp01(position);
    }

    private static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private void OnDestroy()
    {
        if (sets != null) sets.onValueChanged.RemoveListener(SelectSet);
        foreach (var listener in listeners) listener.Stop();
    }
}
