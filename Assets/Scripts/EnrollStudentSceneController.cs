using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EnrollStudentSceneController : MonoBehaviour
{
    readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    readonly Dictionary<string, TMP_InputField> inputs = new Dictionary<string, TMP_InputField>();
    readonly Dictionary<string, TMP_Dropdown> dropdowns = new Dictionary<string, TMP_Dropdown>();
    readonly Dictionary<TMP_Text, string> labels = new Dictionary<TMP_Text, string>();
    readonly List<EnrollmentPasswordToggle> passwordToggles = new List<EnrollmentPasswordToggle>();
    readonly string[] boards = { "BoardOne", "BoardTwo", "BoardThree" };
    readonly StudentEnrollmentStore store = new StudentEnrollmentStore();
    string requestId = Guid.NewGuid().ToString("N");
    StudentEnrollmentData record;
    bool editing, busy, ready;
    int board;

    void Start() { ConfigureUI(); Initialize(); }
    public void ConfigureUI()
    {
        editing = StudentEnrollmentSelection.Editing && !string.IsNullOrEmpty(StudentEnrollmentSelection.StudentId);
        foreach (string name in boards) {
            var root = transform.Find(name);
            foreach (var input in root.GetComponentsInChildren<TMP_InputField>(true)) {
                inputs.Add(input.name, input); input.text = ""; input.richText = false; input.textComponent.richText = false;
                input.onValueChanged.AddListener(_ => { ClearMessage(); RefreshButtons(); });
            }
            foreach (var dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true)) {
                dropdowns.Add(dropdown.name, dropdown);
                var choices = dropdown.options.Select(o => o.text).ToList(); choices.Insert(0, "Select an option");
                dropdown.ClearOptions(); dropdown.AddOptions(choices); dropdown.SetValueWithoutNotify(0);
                dropdown.onValueChanged.AddListener(_ => { ClearMessage(); RefreshButtons(); });
            }
            foreach (var label in root.Find("Texts").GetComponentsInChildren<TMP_Text>(true)) labels[label] = label.text;
            StudentSceneUI.Bind(transform, name + "/CancelBtn", Cancel);
            StudentSceneUI.Bind(transform, name + "/ConfirmBtn", Save);
        }
        StudentSceneUI.Bind(transform, "BoardOne/NextBtn", () => Next(0));
        StudentSceneUI.Bind(transform, "BoardTwo/NextBtn", () => Next(1));
        StudentSceneUI.Bind(transform, "BoardThree/CreateAccountBtn", Save);
        StudentSceneUI.Bind(transform, "Buttons/Button", Cancel);
        // Do not leave during a save, including through the existing Settings button.
        var settings = StudentSceneUI.At<Button>(transform, "Buttons/Button (1)");
        settings.onClick = new Button.ButtonClickedEvent();
        settings.onClick.AddListener(() => { if (!busy) FindAnyObjectByType<SceneLoader>().OpenSettings(); });
        inputs["PasswordField"].contentType = TMP_InputField.ContentType.Password;
        inputs["ConfirmPasswordField"].contentType = TMP_InputField.ContentType.Password;
        passwordToggles.Add(EnrollmentPasswordToggle.Attach(inputs["PasswordField"]));
        passwordToggles.Add(EnrollmentPasswordToggle.Attach(inputs["ConfirmPasswordField"]));
        inputs["StudentIdField"].readOnly = editing; // Stable key used by grades, scores, and parent links.
        inputs["Age"].readOnly = true;
        inputs["DateOfBirth"].onValueChanged.AddListener(UpdateAgeFromBirthday);
        inputs["DateOfBirth"].onEndEdit.AddListener(UpdateAgeFromBirthday);
        if (inputs["DateOfBirth"].placeholder is TMP_Text dateHint) dateHint.text = "MM/DD/YYYY";
        if (editing && inputs["PasswordField"].placeholder is TMP_Text hint) hint.text = "New password (leave blank to keep)";
        if (inputs["StudentIdField"].placeholder is TMP_Text idHint) idHint.text = "Enter Student ID or LRN";
        ShowBoard(editing ? Mathf.Clamp((int)StudentEnrollmentSelection.Section, 0, 2) : 0);
    }
    async void Initialize()
    {
        busy = true; RefreshButtons();
        try {
            await store.Open(lifetime.Token);
            if (editing) record = await store.Student(StudentEnrollmentSelection.StudentId, lifetime.Token);
            else record = new StudentEnrollmentData();
            if (this == null) return;
            if (editing) Fill(record);
            ready = true;
        } catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) Message(error is InvalidOperationException ? error.Message : "Connection failed. Cancel and try again."); }
        finally { if (this != null) { busy = false; RefreshButtons(); } }
    }
    public void ShowBoard(int index)
    {
        board = index;
        for (int i = 0; i < boards.Length; i++) {
            var root = transform.Find(boards[i]); root.gameObject.SetActive(i == index);
            root.Find("ConfirmBtn").gameObject.SetActive(editing);
            var next = root.Find(i < 2 ? "NextBtn" : "CreateAccountBtn"); next.gameObject.SetActive(!editing);
        }
        RefreshButtons();
    }
    string Value(string name) => inputs[name].text.Trim();
    string Choice(string name) => dropdowns[name].value > 0 ? dropdowns[name].options[dropdowns[name].value].text : "";
    public bool ValidateBoard(int index, out string message)
    {
        message = "";
        if (index == 0) {
            if (new[] { "FirstNameField", "MiddleNameField", "LastNameField", "DateOfBirth", "Age" }.Any(n => Value(n).Length == 0) ||
                new[] { "GradeLevelDropdown", "GenderDropdown", "LearningConditionDropdown" }.Any(n => Choice(n).Length == 0))
                message = "Complete every personal information field.";
            else if (!StudentEnrollmentData.TryBirthday(Value("DateOfBirth"), out _)) message = "Enter a valid birthday as MM/DD/YYYY.";
        } else if (index == 1) {
            if (new[] { "MotherNameField", "FatherNameField", "GuardianNameField", "AddressField", "ContactNumberField" }.Any(n => Value(n).Length == 0) || Choice("RelationDropdown").Length == 0)
                message = "Complete every family information field.";
            else if (!Regex.IsMatch(Value("ContactNumberField"), @"^\+?[0-9 ()-]{7,20}$")) message = "Enter a valid contact number.";
        } else {
            if (!Regex.IsMatch(Value("StudentIdField"), @"^[A-Za-z0-9][A-Za-z0-9_-]{0,63}$")) message = "Enter a valid Student ID or LRN.";
            else if ((!editing || inputs["PasswordField"].text.Length > 0 || inputs["ConfirmPasswordField"].text.Length > 0) && inputs["PasswordField"].text.Length < 6)
                message = "Password must have at least 6 characters.";
            else if (inputs["PasswordField"].text != inputs["ConfirmPasswordField"].text) message = "Passwords do not match.";
        }
        return message.Length == 0;
    }
    void RefreshButtons()
    {
        bool valid = ValidateBoard(board, out _);
        for (int i = 0; i < boards.Length; i++) {
            StudentSceneUI.At<Button>(transform, boards[i] + "/ConfirmBtn").interactable = ready && !busy && valid;
            StudentSceneUI.At<Button>(transform, boards[i] + (i < 2 ? "/NextBtn" : "/CreateAccountBtn")).interactable = ready && !busy && valid;
            StudentSceneUI.At<Button>(transform, boards[i] + "/CancelBtn").interactable = !busy;
        }
        foreach (var input in inputs.Values) input.interactable = !busy;
        foreach (var toggle in passwordToggles) toggle.SetInteractable(!busy);
        foreach (var dropdown in dropdowns.Values) dropdown.interactable = !busy;
        StudentSceneUI.At<Button>(transform, "Buttons/Button").interactable = !busy;
        StudentSceneUI.At<Button>(transform, "Buttons/Button (1)").interactable = !busy;
    }
    void Next(int current)
    {
        if (!ready || busy || editing) return;
        if (!ValidateBoard(current, out var message)) { Message(message); return; }
        ShowBoard(current + 1);
    }
    void Fill(StudentEnrollmentData value)
    {
        void Put(string name, string text) => inputs[name].SetTextWithoutNotify(text ?? "");
        Put("FirstNameField", value.firstName); Put("MiddleNameField", value.middleName); Put("LastNameField", value.lastName);
        Put("DateOfBirth", value.birthday); UpdateAgeFromBirthday(value.birthday);
        Put("MotherNameField", value.motherName); Put("FatherNameField", value.fatherName); Put("GuardianNameField", value.guardianName);
        Put("AddressField", value.address); Put("ContactNumberField", value.contactNumber); Put("StudentIdField", value.id);
        SetChoice("GradeLevelDropdown", value.grade); SetChoice("GenderDropdown", value.gender);
        SetChoice("LearningConditionDropdown", value.condition); SetChoice("RelationDropdown", value.relationship);
    }
    void UpdateAgeFromBirthday(string birthday)
    {
        // Keep the displayed age derived from the date, including when loading an edit.
        inputs["Age"].SetTextWithoutNotify(StudentEnrollmentData.TryBirthday(birthday, out var date)
            ? StudentEnrollmentData.Age(date).ToString() : "");
        RefreshButtons();
    }
    void SetChoice(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var dropdown = dropdowns[name]; int index = dropdown.options.FindIndex(o => string.Equals(o.text, value, StringComparison.OrdinalIgnoreCase));
        if (index < 0) { dropdown.AddOptions(new List<string> { value }); index = dropdown.options.Count - 1; }
        dropdown.SetValueWithoutNotify(index);
    }
    StudentEnrollmentData Read()
    {
        var value = record ?? new StudentEnrollmentData();
        if (!editing || board == 0) {
            value.firstName = Value("FirstNameField"); value.middleName = Value("MiddleNameField"); value.lastName = Value("LastNameField");
            StudentEnrollmentData.TryBirthday(Value("DateOfBirth"), out var date); value.birthday = date.ToString("yyyy-MM-dd"); value.age = StudentEnrollmentData.Age(date);
            value.grade = Choice("GradeLevelDropdown"); value.condition = Choice("LearningConditionDropdown"); value.gender = Choice("GenderDropdown");
        }
        if (!editing || board == 1) {
            value.motherName = Value("MotherNameField"); value.fatherName = Value("FatherNameField"); value.guardianName = Value("GuardianNameField");
            value.address = Value("AddressField"); value.contactNumber = Value("ContactNumberField"); value.relationship = Choice("RelationDropdown");
        }
        value.id = editing ? StudentEnrollmentSelection.StudentId : Value("StudentIdField").ToUpperInvariant(); return value;
    }
    async void Save()
    {
        if (!ready || busy) return;
        foreach (int index in editing ? new[] { board } : new[] { 0, 1, 2 })
            if (!ValidateBoard(index, out var message)) { ShowBoard(index); Message(message); return; }
        busy = true; ClearMessage(); RefreshButtons();
        try {
            var data = Read();
            await store.Save(data, (StudentInfoSection)board, editing, (!editing || board == 2) ? inputs["PasswordField"].text : "", requestId, lifetime.Token);
            if (this == null) return;
            inputs["PasswordField"].text = ""; inputs["ConfirmPasswordField"].text = "";
            StudentEnrollmentSelection.StudentId = data.id; StudentEnrollmentSelection.Editing = false; StudentEnrollmentSelection.Section = StudentInfoSection.None;
            SceneManager.LoadScene("EditStudentScene");
        } catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) Message(error is InvalidOperationException ? error.Message : "Unable to save. Please try again."); }
        finally { if (this != null) { busy = false; RefreshButtons(); } }
    }
    void Cancel()
    {
        if (busy) return;
        if (!editing && board > 0) { ShowBoard(board - 1); return; }
        StudentEnrollmentSelection.Editing = false;
        SceneManager.LoadScene(editing ? "EditStudentScene" : "StudentListScene");
    }
    void ClearMessage() { foreach (var entry in labels) entry.Key.text = entry.Value; }
    void Message(string message)
    {
        // Reuse a label already in the board; no new panels or artwork.
        string[] names = { "Firstname", "Father Full Name", "StudentId" };
        var label = StudentSceneUI.At<TMP_Text>(transform, boards[board] + "/Texts/" + names[board]);
        label.richText = false; label.text = message; Debug.LogWarning("Student enrollment: " + message);
    }
    void OnDestroy()
    {
        lifetime.Cancel(); lifetime.Dispose();
        foreach (string name in new[] { "PasswordField", "ConfirmPasswordField" }) if (inputs.TryGetValue(name, out var input) && input != null) input.SetTextWithoutNotify("");
    }
}
