using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class StudentListSceneController : MonoBehaviour
{
    readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    readonly List<GameObject> rows = new List<GameObject>();
    List<StudentEnrollmentData> students = new List<StudentEnrollmentData>();
    ScrollRect scroll;
    RectTransform template;
    TMP_Dropdown grades;
    Vector2 firstPosition;
    bool loading;
    const float RowSpacing = 74;

    void Start() { ConfigureUI(); Reload(); }
    public void ConfigureUI()
    {
        scroll = StudentSceneUI.At<ScrollRect>(transform, "ListOfStudentsScrollView");
        template = StudentSceneUI.At<RectTransform>(scroll.transform, "Row");
        Canvas.ForceUpdateCanvases();
        // The original four row graphics keep their exact positions and dimensions.
        // Extend only the invisible clipping area to contain all four authored rows.
        scroll.viewport.offsetMin = new Vector2(0, scroll.viewport.rect.height - (4 * RowSpacing + 4) + scroll.viewport.offsetMin.y);
        scroll.viewport.offsetMax = new Vector2(0, scroll.viewport.offsetMax.y);
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35; scroll.inertia = true;
        scroll.content.anchorMin = new Vector2(0, 1); scroll.content.anchorMax = new Vector2(1, 1);
        scroll.content.pivot = new Vector2(.5f, 1); scroll.content.anchoredPosition = Vector2.zero;
        Canvas.ForceUpdateCanvases();
        var local = scroll.content.InverseTransformPoint(template.position);
        firstPosition = new Vector2(local.x, local.y);
        foreach (Transform child in scroll.transform)
            if (child.name == "Row" || child.name.StartsWith("Row (", StringComparison.Ordinal)) child.gameObject.SetActive(false);
        grades = StudentSceneUI.At<TMP_Dropdown>(transform, "SortingGradeDropDown");
        grades.ClearOptions(); grades.AddOptions(new List<string> { "All Grades", "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6" });
        grades.SetValueWithoutNotify(0); grades.onValueChanged.AddListener(_ => Render());
        StudentSceneUI.Bind(transform, "Buttons/Button", () => {
            if (loading) return;
            StudentEnrollmentSelection.Clear(); SceneManager.LoadScene("EnrollStudentScene");
        });
    }
    async void Reload()
    {
        if (loading) return; loading = true; grades.interactable = false;
        ShowMessage("Loading students…", false);
        try {
            var store = new StudentEnrollmentStore(); await store.Open(lifetime.Token);
            students = await store.Students(lifetime.Token);
            if (this != null) Render();
        } catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) ShowMessage(error is InvalidOperationException ? error.Message : "Unable to load students. Press the arrow to retry.", true); }
        finally { if (this != null) { loading = false; grades.interactable = true; } }
    }
    public void SetStudents(List<StudentEnrollmentData> data) { students = data; Render(); }
    void ClearRows()
    {
        foreach (var row in rows) { row.SetActive(false); Destroy(row); }
        rows.Clear();
    }
    RectTransform AddRow(int index)
    {
        var row = Instantiate(template, scroll.content, false); row.name = "StudentRow";
        row.anchorMin = row.anchorMax = new Vector2(.5f, 1);
        row.anchoredPosition = firstPosition - new Vector2(0, RowSpacing * index);
        row.gameObject.SetActive(true); rows.Add(row.gameObject); return row;
    }
    void Render()
    {
        ClearRows();
        string filter = grades.options[grades.value].text;
        foreach (var student in students.Where(s => grades.value == 0 || StudentEnrollmentData.NormalizeGrade(s.grade) == filter)) {
            var row = AddRow(rows.Count);
            StudentSceneUI.Text(row, "NameOftheStudent", student.FullName.Length == 0 ? student.id : student.FullName);
            StudentSceneUI.Text(row, "CurrentGradeOfStudent", student.grade);
            StudentSceneUI.Bind(row, "Button", () => {
                StudentEnrollmentSelection.StudentId = student.id;
                StudentEnrollmentSelection.Section = StudentInfoSection.None; StudentEnrollmentSelection.Editing = false;
                SceneManager.LoadScene("EditStudentScene");
            });
        }
        if (rows.Count == 0) { ShowMessage("No students found.", false); return; }
        ResizeContent();
    }
    void ShowMessage(string text, bool retry)
    {
        ClearRows(); var row = AddRow(0);
        StudentSceneUI.Text(row, "NameOftheStudent", text);
        StudentSceneUI.Text(row, "CurrentGradeOfStudent", " ");
        var button = StudentSceneUI.Bind(row, "Button", Reload); button.gameObject.SetActive(retry);
        ResizeContent();
    }
    void ResizeContent()
    {
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(scroll.viewport.rect.height, rows.Count * RowSpacing + 4));
        Canvas.ForceUpdateCanvases(); scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
    }
    void OnDestroy() { lifetime.Cancel(); lifetime.Dispose(); }
}
