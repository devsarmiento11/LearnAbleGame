using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EditStudentSceneController : MonoBehaviour
{
    readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    Button edit;
    Button[] sectionButtons;
    ColorBlock[] sectionColors;
    StudentInfoSection selected = StudentInfoSection.None;
    bool ready;
    void Start() { ConfigureUI(); LoadStudent(); }
    public void ConfigureUI()
    {
        edit = StudentSceneUI.Bind(transform, "Buttons/Edit", () => {
            if (!ready || selected == StudentInfoSection.None) return;
            StudentEnrollmentSelection.Section = selected; StudentEnrollmentSelection.Editing = true;
            SceneManager.LoadScene("EnrollStudentScene");
        }); edit.interactable = false;
        sectionButtons = new[] {
            StudentSceneUI.Bind(transform, "Options/PersonalInfo", () => Select(StudentInfoSection.Personal)),
            StudentSceneUI.Bind(transform, "Options/FamilyInfo", () => Select(StudentInfoSection.Family)),
            StudentSceneUI.Bind(transform, "Options/AccountInfo", () => Select(StudentInfoSection.Account))
        };
        sectionColors = new ColorBlock[sectionButtons.Length];
        for (int i = 0; i < sectionButtons.Length; i++) sectionColors[i] = sectionButtons[i].colors;
        foreach (var label in transform.Find("Options").GetComponentsInChildren<TMPro.TMP_Text>(true)) { label.richText = false; label.text = "—"; }
    }
    public void Select(StudentInfoSection section)
    {
        selected = section; edit.interactable = ready && section != StudentInfoSection.None;
        for (int i = 0; i < sectionButtons.Length; i++) {
            var colors = sectionColors[i];
            if (i == (int)section) {
                // Keep the chosen panel highlighted even after pointer exit or focus moves to Edit.
                var highlight = new Color(1f, .88f, .48f, colors.normalColor.a);
                colors.normalColor = highlight;
                colors.highlightedColor = highlight;
                colors.selectedColor = highlight;
                colors.pressedColor = new Color(.95f, .79f, .35f, highlight.a);
            }
            sectionButtons[i].colors = colors;
        }
    }
    async void LoadStudent()
    {
        try {
            var store = new StudentEnrollmentStore(); await store.Open(lifetime.Token);
            var student = await store.Student(StudentEnrollmentSelection.StudentId, lifetime.Token);
            if (this != null) Display(student);
        } catch (OperationCanceledException) { }
        catch (Exception error) {
            if (this != null) StudentSceneUI.Text(transform, "Options/PersonalInfo/FirstName", error is InvalidOperationException ? error.Message : "Unable to load. Return to the list and retry.");
        }
    }
    public void Display(StudentEnrollmentData data)
    {
        const string p = "Options/PersonalInfo/", f = "Options/FamilyInfo/", a = "Options/AccountInfo/";
        StudentSceneUI.Text(transform, p + "FirstName", data.firstName); StudentSceneUI.Text(transform, p + "MiddleName", data.middleName);
        StudentSceneUI.Text(transform, p + "LastName", data.lastName); StudentSceneUI.Text(transform, p + "GradeLevel", data.grade);
        StudentSceneUI.Text(transform, p + "Gender", data.gender); StudentSceneUI.Text(transform, p + "DateOfBirth", data.birthday);
        StudentSceneUI.Text(transform, p + "Age", string.IsNullOrWhiteSpace(data.birthday) ? "—" : data.age.ToString());
        StudentSceneUI.Text(transform, f + "MotherName", data.motherName); StudentSceneUI.Text(transform, f + "FatherName", data.fatherName);
        StudentSceneUI.Text(transform, f + "GuardianName", data.guardianName); StudentSceneUI.Text(transform, f + "ContactNumber", data.contactNumber);
        StudentSceneUI.Text(transform, f + "Address", data.address); StudentSceneUI.Text(transform, f + "Relationship", data.relationship);
        StudentSceneUI.Text(transform, a + "StudentIdorLrn", data.id);
        // Firebase Auth passwords cannot be read back; never pretend sample text is the password.
        StudentSceneUI.Text(transform, a + "Password", "••••••••"); StudentSceneUI.Text(transform, a + "ConfirmPassword", "••••••••");
        ready = true; edit.interactable = selected != StudentInfoSection.None;
    }
    void OnDestroy() { lifetime.Cancel(); lifetime.Dispose(); }
}
