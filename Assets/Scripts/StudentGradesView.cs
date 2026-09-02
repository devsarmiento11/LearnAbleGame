using System.Collections.Generic;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;

public class StudentGradesView : MonoBehaviour
{
    public Transform textRoot;
    public bool useSelectedStudent = true;
    private ListenerRegistration listener;
    private readonly Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();
    private readonly Dictionary<string, Color> originalColors = new Dictionary<string, Color>();
    private static readonly string[] QuarterLabels = { "First", "Second", "Third", "Fourth" };

    private void Awake()
    {
        if (textRoot == null) textRoot = transform;
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
            : LearningDataStore.CurrentUserId;
        if (string.IsNullOrWhiteSpace(studentId)) return;

        listener = FirebaseFirestore.DefaultInstance.Collection(LearningDataStore.UsersCollection)
            .Document(studentId).Listen(snapshot =>
            {
                if (this == null) return;
                Render(snapshot.Exists ? snapshot.ToDictionary() : new Dictionary<string, object>());
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
            ? PlayerPrefs.GetString("SelectedGradeStudentName", string.Empty) : LoginSession.StudentName;
        Set("StudentNameText", AcademicGradeReport.StudentName(data, fallback).ToUpperInvariant());
        string grade = AcademicGradeReport.Text(data, "grade");
        if (grade.Length == 0 && useSelectedStudent)
            grade = PlayerPrefs.GetString("SelectedGradeStudentGrade", string.Empty);
        Set("GradeLevelText", grade.Length == 0 ? string.Empty
            : "GRADE " + grade.ToUpperInvariant().Replace("GRADE", string.Empty).Trim());
        Set("SectionText", AcademicGradeReport.Text(data, "section"));
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

    private void Set(string key, string value)
    {
        if (labels.TryGetValue(key, out TMP_Text label)) label.text = value;
    }

    private void OnDestroy()
    {
        listener?.Stop();
    }
}
