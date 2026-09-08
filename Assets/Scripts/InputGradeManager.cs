using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Firebase.Firestore;
using Firebase.Auth;

public class InputGradeManager : MonoBehaviour
{
    [Header("SELECTED STUDENT")]
    public TMP_Text studentNameText;
    public TMP_Text gradeLevelText;

    [Header("GRADE OPTIONS")]
    public TMP_Dropdown subjectDropdown;
    public TMP_Dropdown quarterDropdown;

    [Header("INPUT")]
    public TMP_InputField gradeInputField;
    public TMP_InputField teacherRemarkInputField;

    [Header("CONFIRMATION")]
    public GameObject confirmationPopup;

    // Student information selected from GradesScene
    private string studentId;
    private string studentName;
    private string studentGrade;

    // Temporarily hold the values while confirmation popup is open
    private string pendingSubject;
    private string pendingQuarter;
    private int pendingGrade;
    private string pendingRemark;
    private bool hasPendingGrade;
    private bool isSaving;
    private TMP_Text statusText;


    // ==========================================
    // START
    // ==========================================

    private void Start()
    {
        LoadSelectedStudent();

        SetupDropdowns();

        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }
    }


    // ==========================================
    // LOAD STUDENT FROM PREVIOUS SCENE
    // ==========================================

    private void LoadSelectedStudent()
    {
        studentId = PlayerPrefs.GetString(
            "SelectedGradeStudentId",
            ""
        );

        studentName = PlayerPrefs.GetString(
            "SelectedGradeStudentName",
            "Unknown Student"
        );

        studentGrade = PlayerPrefs.GetString(
            "SelectedGradeStudentGrade",
            ""
        );

        // Student name
        if (studentNameText != null)
        {
            studentNameText.text =
                studentName.ToUpper();
        }

        // Grade level
        if (gradeLevelText != null)
        {
            string cleanGrade = studentGrade
                .Replace("GRADE", "")
                .Replace("Grade", "")
                .Replace("grade", "")
                .Trim();

            if (string.IsNullOrEmpty(cleanGrade))
            {
                gradeLevelText.text = "NO GRADE";
            }
            else
            {
                gradeLevelText.text =
                    "GRADE " + cleanGrade;
            }
        }

        Debug.Log(
            "Input Grade Student: " +
            studentName +
            " | " +
            studentGrade +
            " | ID: " +
            studentId
        );
    }


    // ==========================================
    // SETUP DROPDOWNS
    // ==========================================

    private void SetupDropdowns()
    {
        // SUBJECT
        if (subjectDropdown != null)
        {
            subjectDropdown.ClearOptions();

            subjectDropdown.AddOptions(
                new System.Collections.Generic.List<string>
                {
                    "SCIENCE",
                    "ENGLISH",
                    "MATH"
                }
            );

            subjectDropdown.value = 0;
            subjectDropdown.RefreshShownValue();
        }


        // QUARTER
        if (quarterDropdown != null)
        {
            quarterDropdown.ClearOptions();

            quarterDropdown.AddOptions(
                new System.Collections.Generic.List<string>
                {
                    "1ST",
                    "2ND",
                    "3RD",
                    "4TH"
                }
            );

            quarterDropdown.value = 0;
            quarterDropdown.RefreshShownValue();
        }
    }


    // ==========================================
    // SAVE GRADE BUTTON
    // ==========================================

    public void OpenConfirmation()
    {
        if (isSaving) return;
        hasPendingGrade = false;
        if (string.IsNullOrWhiteSpace(studentId) || subjectDropdown == null || quarterDropdown == null)
        {
            ShowStatus("Select a student, subject and quarter first.", true);
            return;
        }
        if (gradeInputField == null)
            return;

        // Make sure teacher entered a grade
        if (string.IsNullOrWhiteSpace(
                gradeInputField.text))
        {
            Debug.LogWarning(
                "Please enter a grade."
            );
            ShowStatus("Please enter a grade.", true);

            return;
        }


        int grade;

        // Make sure grade is a number
        if (!int.TryParse(
                gradeInputField.text,
                out grade))
        {
            Debug.LogWarning(
                "Grade must be a number."
            );
            ShowStatus("Enter a whole-number grade from 0 to 100.", true);

            return;
        }


        // Prevent impossible grades
        if (grade < 0 || grade > 100)
        {
            Debug.LogWarning(
                "Grade must be between 0 and 100."
            );
            ShowStatus("Grade must be between 0 and 100.", true);

            return;
        }


        pendingGrade = grade;


        // SUBJECT
        if (subjectDropdown != null)
        {
            pendingSubject =
                subjectDropdown.options[
                    subjectDropdown.value
                ].text;
        }


        // QUARTER
        if (quarterDropdown != null)
        {
            pendingQuarter =
                quarterDropdown.options[
                    quarterDropdown.value
                ].text;
        }


        // REMARK
        if (teacherRemarkInputField != null)
        {
            pendingRemark =
                teacherRemarkInputField.text.Trim();
        }
        else
        {
            pendingRemark = "";
        }


        hasPendingGrade = true;
        ShowStatus(string.Empty, false);
        // Show confirmation
        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(true);
        }
    }


    // ==========================================
    // CONFIRM YES
    // ==========================================

    public async void ConfirmSaveGrade()
    {
        if (isSaving || !hasPendingGrade) return;
        // PlayerPrefs alone is not authentication. Rules verify this profile's
        // authUid against the signed-in Firebase user before accepting grades.
        string teacherId = LearningDataStore.CurrentUserId;
        if (FirebaseAuth.DefaultInstance.CurrentUser == null || string.IsNullOrWhiteSpace(teacherId))
        {
            if (confirmationPopup != null) confirmationPopup.SetActive(false);
            ShowStatus("Sign in with your teacher account before saving grades.", true);
            return;
        }
        isSaving = true;
        SetInputEnabled(false);
        ShowStatus("Saving grade...", false);
        try
        {
            var updates = new Dictionary<string, object>
            {
                { "academicGrades." + AcademicGradeReport.Key(pendingSubject, pendingQuarter), pendingGrade },
                { "gradesUpdatedAt", FieldValue.ServerTimestamp },
                { "gradesUpdatedBy", teacherId }
            };
            // An optional empty remark does not erase a previously saved teacher note.
            if (!string.IsNullOrWhiteSpace(pendingRemark))
                updates["teacherRemark"] = pendingRemark;

            await FirebaseFirestore.DefaultInstance.Collection(LearningDataStore.UsersCollection)
                .Document(studentId).UpdateAsync(updates);
            if (this == null) return;
            hasPendingGrade = false;
            if (confirmationPopup != null) confirmationPopup.SetActive(false);
            ClearInputFields();
            ShowStatus("Grade saved.", false);
        }
        catch (System.Exception exception)
        {
            if (this == null) return;
            Debug.LogError("Unable to save grade: " + exception);
            if (confirmationPopup != null) confirmationPopup.SetActive(false);
            ShowStatus(exception is FirestoreException firestoreException &&
                firestoreException.ErrorCode == FirestoreError.PermissionDenied
                ? "Grade save denied. Sign in as a teacher; the grade-saving Firestore rules must be published."
                : "Grade could not be saved. Please try again.", true);
        }
        finally
        {
            if (this != null)
            {
                isSaving = false;
                SetInputEnabled(true);
            }
        }
    }


    // ==========================================
    // CANCEL CONFIRMATION
    // ==========================================

    public void CancelConfirmation()
    {
        if (isSaving) return;
        hasPendingGrade = false;
        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }
    }


    // ==========================================
    // CLEAR AFTER SAVING
    // ==========================================

    private void ClearInputFields()
    {
        if (gradeInputField != null)
        {
            gradeInputField.text = "";
        }

        if (teacherRemarkInputField != null)
        {
            teacherRemarkInputField.text = "";
        }
    }

    private void SetInputEnabled(bool enabled)
    {
        if (gradeInputField != null) gradeInputField.interactable = enabled;
        if (teacherRemarkInputField != null) teacherRemarkInputField.interactable = enabled;
        if (subjectDropdown != null) subjectDropdown.interactable = enabled;
        if (quarterDropdown != null) quarterDropdown.interactable = enabled;
    }

    private void ShowStatus(string message, bool error)
    {
        if (statusText == null)
        {
            Canvas canvas = gradeInputField == null ? null : gradeInputField.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var statusObject = new GameObject("GradeSaveStatus", typeof(RectTransform));
            statusObject.transform.SetParent(canvas.transform, false);
            statusText = statusObject.AddComponent<TextMeshProUGUI>();
            statusText.font = gradeInputField.textComponent.font;
            statusText.fontSize = 24;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.raycastTarget = false;
            RectTransform rect = statusText.rectTransform;
            rect.anchorMin = new Vector2(0.1f, 0);
            rect.anchorMax = new Vector2(0.9f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 12);
            rect.sizeDelta = new Vector2(0, 60);
        }
        statusText.text = message;
        statusText.color = error ? Color.red : Color.white;
    }
}
