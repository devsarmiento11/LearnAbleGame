using System.Collections.Generic;
using System;
using System.Text;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class StudentGradesView : MonoBehaviour
{
    public Transform textRoot;
    public bool useSelectedStudent = true;
    private ListenerRegistration listener;
    private Button promoteButton;
    private DocumentReference studentDocument;
    private bool promoting;
    private int eligibleGrade;
    private const string PromotionYearField = "academicPromotionSchoolYear";
    private readonly Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();
    private readonly Dictionary<string, Color> originalColors = new Dictionary<string, Color>();
    private static readonly string[] QuarterLabels = { "First", "Second", "Third" };
    [Serializable] private sealed class PromotionRequest { public PromotionData data; }
    [Serializable] private sealed class PromotionData { public string studentId; public int expectedGrade; }
    [Serializable] private sealed class PromotionResponse { public PromotionResult result; public PromotionError error; }
    [Serializable] private sealed class PromotionResult { public string studentId; }
    [Serializable] private sealed class PromotionError { public string message; }

    private void Awake()
    {
        if (LoginSession.IsParent) useSelectedStudent = false;
        if (textRoot == null) textRoot = transform;
        foreach (Button button in textRoot.GetComponentsInChildren<Button>(true))
        {
            if (button.name != "PromoteButton") continue;
            promoteButton = button;
            promoteButton.gameObject.SetActive(false);
            promoteButton.onClick.AddListener(PromoteStudent);
            break;
        }
        foreach (TMP_Text label in textRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            string key = label.name.Trim();
            labels[key] = label;
            originalColors[key] = label.color;
        }
        Render(new Dictionary<string, object>());
    }

    private void Start()
    {
        string studentId = useSelectedStudent
            ? PlayerPrefs.GetString("SelectedGradeStudentId", string.Empty)
            : LoginSession.ProfileStudentId;
        if (string.IsNullOrWhiteSpace(studentId)) return;

        studentDocument = FirebaseFirestore.DefaultInstance.Collection(LearningDataStore.UsersCollection)
            .Document(studentId);
        listener = studentDocument.Listen(MetadataChanges.Include, snapshot =>
            {
                if (this == null) return;
                var data = snapshot.Exists ? snapshot.ToDictionary() : new Dictionary<string, object>();
                Render(data);
                if (promoteButton != null)
                {
                    eligibleGrade = CanPromote() && !snapshot.Metadata.IsFromCache &&
                        !snapshot.Metadata.HasPendingWrites ? EligibleGrade(data) : 0;
                    promoteButton.gameObject.SetActive(eligibleGrade > 0);
                }
            });
        listener.ListenerTask.ContinueWithOnMainThread(task =>
        {
            if (this != null && task.IsFaulted)
                Debug.LogError("Unable to load student grades: " + task.Exception);
        });
    }

    private void Render(Dictionary<string, object> data)
    {
        var report = new AcademicGradeReport(data);
        string fallback = useSelectedStudent
            ? PlayerPrefs.GetString("SelectedGradeStudentName", string.Empty) : LoginSession.ProfileStudentName;
        Set("StudentNameText", AcademicGradeReport.StudentName(data, fallback).ToUpperInvariant());
        string grade = AcademicGradeReport.Text(data, "grade");
        if (grade.Length > 0 && useSelectedStudent && studentDocument != null &&
            PlayerPrefs.GetString("SelectedGradeStudentId", string.Empty) == studentDocument.Id &&
            PlayerPrefs.GetString("SelectedGradeStudentGrade", string.Empty) != grade)
        {
            PlayerPrefs.SetString("SelectedGradeStudentGrade", grade);
            PlayerPrefs.Save();
        }
        if (grade.Length == 0 && useSelectedStudent)
            grade = PlayerPrefs.GetString("SelectedGradeStudentGrade", string.Empty);
        Set("GradeLevelText", grade.Length == 0 ? string.Empty
            : "GRADE " + grade.ToUpperInvariant().Replace("GRADE", string.Empty).Trim());
        string studentSet = AcademicGradeReport.Text(data, "set");
        Set("SetText", studentSet);
        // The parent scene uses SectionText for the same existing label.
        Set("SectionText", studentSet.Length > 0 ? studentSet : AcademicGradeReport.Text(data, "section"));
        Set("SchoolYearText", AcademicGradeReport.Text(data, "schoolYear"));

        foreach (string subject in AcademicGradeReport.Subjects)
        {
            for (int q = 0; q < QuarterLabels.Length; q++)
                Set(subject + QuarterLabels[q] + "Text",
                    AcademicGradeReport.Format(report.Grade(subject, AcademicGradeReport.Quarters[q])));
            decimal? average = report.SubjectAverage(subject);
            Set(subject + "FinalText", AcademicGradeReport.Format(average));
            string remarkKey = subject + "RemarksText";
            Set(remarkKey, AcademicGradeReport.Remark(average));
            if (labels.TryGetValue(remarkKey, out TMP_Text remark))
                remark.color = average.HasValue && average.Value < 75 ? Color.red : originalColors[remarkKey];
        }
        string general = AcademicGradeReport.Format(report.GeneralAverage());
        Set("GeneralAverageText", general);
        Set("GeneralAverage", general);
        Set("TeacherRemarkText", report.TeacherRemark);
        if (labels.TryGetValue("TeacherRemarkText", out TMP_Text teacherRemark))
            teacherRemark.richText = false;
    }

    private bool CanPromote()
    {
        return useSelectedStudent && LoginSession.IsLoggedIn && LoginSession.Role == "teacher" &&
            FirebaseAuth.DefaultInstance.CurrentUser != null;
    }

    private static int EligibleGrade(Dictionary<string, object> data)
    {
        if (AcademicGradeReport.Text(data, "role") != "student" ||
            AcademicGradeReport.Text(data, "status") != "Active")
            return 0;

        string grade = StudentEnrollmentData.NormalizeGrade(AcademicGradeReport.Text(data, "grade"));
        if (!grade.StartsWith("Grade ", System.StringComparison.Ordinal) ||
            !int.TryParse(grade.Substring(6), out int level) || level < 1 || level > 6)
            return 0;
        if (level < 6 && data.ContainsKey(PromotionYearField) &&
            AcademicGradeReport.Text(data, PromotionYearField) == AcademicGradeReport.Text(data, "schoolYear"))
            return 0;

        var report = new AcademicGradeReport(data);
        decimal total = 0;
        foreach (string subject in AcademicGradeReport.Subjects)
        {
            foreach (string quarter in AcademicGradeReport.Quarters)
            {
                decimal? value = report.Grade(subject, quarter);
                if (!value.HasValue || decimal.Truncate(value.Value) != value.Value) return 0;
                total += value.Value;
            }
        }
        return total >= 75 * AcademicGradeReport.Subjects.Length * AcademicGradeReport.Quarters.Length ? level : 0;
    }

    private async void PromoteStudent()
    {
        int currentGrade = eligibleGrade;
        if (promoting || currentGrade < 1 || currentGrade >= 6 || !CanPromote()) return;

        promoting = true;
        promoteButton.interactable = false;
        try
        {
            string studentId = studentDocument.Id;
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            string token = await user.TokenAsync(true);
            if (this == null || FirebaseAuth.DefaultInstance.CurrentUser?.UserId != user.UserId) return;
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Could not refresh your Firebase session. Sign out and sign back in as a teacher.");
            var payload = new PromotionRequest
            {
                data = new PromotionData { studentId = studentId, expectedGrade = currentGrade }
            };
            string url = "https://us-central1-" + FirebaseApp.DefaultInstance.Options.ProjectId +
                ".cloudfunctions.net/promoteStudentAndReset";
            using var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload)));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 125;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            PromotionResponse response = null;
            try { response = JsonUtility.FromJson<PromotionResponse>(request.downloadHandler.text); }
            catch (ArgumentException) { }
            if (!string.IsNullOrWhiteSpace(response?.error?.message))
                throw new InvalidOperationException(response.error.message);
            if (request.responseCode == 404)
                throw new InvalidOperationException(
                    "The promoteStudentAndReset Cloud Function was not found. Check its deployment in " +
                    FirebaseApp.DefaultInstance.Options.ProjectId + " (us-central1).");
            if (request.responseCode == 401 || request.responseCode == 403)
                throw new InvalidOperationException(
                    $"The promotion endpoint rejected the request before returning a Firebase callable response (HTTP {request.responseCode}). " +
                    "Check the Cloud Run invocation permissions for promoteStudentAndReset.");
            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"Promotion request failed (HTTP {request.responseCode}): {request.error}");
            if (response?.result?.studentId != studentId)
                throw new InvalidOperationException("The promotion service returned an unexpected response. Reopen the student's grades to check their current grade.");
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Unable to promote student: " + exception);
        }
        finally
        {
            if (this != null)
            {
                promoting = false;
                promoteButton.interactable = true;
            }
        }
    }

    private void Set(string key, string value)
    {
        if (labels.TryGetValue(key, out TMP_Text label)) label.text = value;
    }

    private void OnDestroy()
    {
        listener?.Stop();
    }
}
