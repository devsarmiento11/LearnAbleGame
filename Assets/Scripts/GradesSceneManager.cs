using System;
using System.Collections.Generic;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GradesSceneManager : MonoBehaviour
{
    [Header("STUDENT LIST")]
    public Transform content;
    public GameObject studentRowPrefab;

    [Header("SCENE LOADER")]
    public SceneLoader sceneLoader;

    [Header("NEXT SCENE")]
    public string studentGradesSceneName;

    [SerializeField] private TMP_Dropdown nameSortingDropdown;
    [SerializeField] private TMP_Dropdown gradeSortingDropdown;

    private readonly List<StudentProfile> students = new List<StudentProfile>();
    private ScrollRect studentScrollView;
    private FirebaseFirestore db;
    private ListenerRegistration studentListener;

    private readonly Dictionary<string, GameObject> studentRows =
        new Dictionary<string, GameObject>();


    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        studentScrollView = content != null ? content.GetComponentInParent<ScrollRect>() : null;
        nameSortingDropdown?.onValueChanged.AddListener(OnSortingChanged);
        gradeSortingDropdown?.onValueChanged.AddListener(OnSortingChanged);

        ListenForStudents();
    }


    // ==========================================
    // LOAD STUDENTS
    // ==========================================

    private void ListenForStudents()
    {
        studentListener = db
            .Collection(LearningDataStore.UsersCollection)
            .WhereEqualTo("role", "student")
            .Listen(snapshot =>
            {
                students.Clear();

                foreach (DocumentSnapshot document
                         in snapshot.Documents)
                {
                    if (!document.Exists)
                        continue;

                    Dictionary<string, object> data =
                        document.ToDictionary();

                    // Login also uses the document ID; userId may be a school/login ID.
                    string studentId = document.Id;

                    string studentName =
                        GetStudentName(
                            data,
                            studentId
                        );

                    string grade =
                        GetText(
                            data,
                            "grade",
                            ""
                        );

                    students.Add(
                        new StudentProfile(
                            studentId,
                            studentName,
                            grade
                        )
                    );
                }

                RebuildRows(students);
                ApplyStudentSorting();
            });
    }


    // ==========================================
    // REBUILD STUDENT LIST
    // ==========================================

    private void OnSortingChanged(int _)
    {
        ApplyStudentSorting();

        if (studentScrollView != null)
        {
            studentScrollView.StopMovement();
            LayoutRebuilder.ForceRebuildLayoutImmediate(studentScrollView.content);
            studentScrollView.verticalNormalizedPosition = 1f;
        }
    }

    private void ApplyStudentSorting()
    {
        // Grade options 0–5 select one grade. The last option shows all grades.
        int selectedGrade = gradeSortingDropdown != null ? gradeSortingDropdown.value + 1 : 7;
        bool descendingNames = nameSortingDropdown != null && nameSortingDropdown.value == 1;

        students.Sort((a, b) =>
        {
            int gradeOrder = a.GradeNumber.CompareTo(b.GradeNumber);
            if (gradeOrder != 0)
                return gradeOrder;

            int nameOrder = descendingNames
                ? StringComparer.OrdinalIgnoreCase.Compare(b.Name, a.Name)
                : StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
            return nameOrder != 0 ? nameOrder : StringComparer.Ordinal.Compare(a.Id, b.Id);
        });

        foreach (StudentProfile student in students)
        {
            if (!studentRows.TryGetValue(student.Id, out GameObject row))
                continue;

            bool visible = selectedGrade > 6 || student.GradeNumber == selectedGrade;
            row.SetActive(visible);
            if (visible)
                row.transform.SetAsLastSibling();
        }
    }

    private void RebuildRows(
        List<StudentProfile> students)
    {
        foreach (GameObject row
                 in studentRows.Values)
        {
            if (row != null)
            {
                row.SetActive(false);
                Destroy(row);
            }
        }

        studentRows.Clear();

        foreach (StudentProfile student
                 in students)
        {
            AddStudent(
                student.Id,
                student.Name,
                student.Grade
            );
        }
    }


    // ==========================================
    // CREATE ROW
    // ==========================================

    private void AddStudent(
        string studentId,
        string studentName,
        string grade)
    {
        if (studentRowPrefab == null ||
            content == null)
        {
            Debug.LogError(
                "GradesSceneManager: Content or prefab is missing!"
            );

            return;
        }

        GameObject newRow =
            Instantiate(
                studentRowPrefab,
                content
            );

        newRow.SetActive(true);

        GradeStudentRowUI row =
            newRow.GetComponent<GradeStudentRowUI>();

        if (row != null)
        {
            row.Setup(
                studentId,
                studentName,
                grade,
                this
            );
        }

        studentRows[studentId] = newRow;
    }


    // ==========================================
    // YELLOW BUTTON
    // ==========================================

    public void OpenStudentGrades(
        string studentId,
        string studentName,
        string grade)
    {
        Debug.Log(
            "Opening grades for: " +
            studentName +
            " | " +
            studentId
        );

        // Save which student was selected.
        // The next scene can read these values.
        PlayerPrefs.SetString(
            "SelectedGradeStudentId",
            studentId
        );

        PlayerPrefs.SetString(
            "SelectedGradeStudentName",
            studentName
        );

        PlayerPrefs.SetString(
            "SelectedGradeStudentGrade",
            grade
        );

        PlayerPrefs.Save();

        // Use your existing SceneLoader
        if (sceneLoader != null &&
            !string.IsNullOrEmpty(studentGradesSceneName))
        {
            sceneLoader.LoadScene(
                studentGradesSceneName
            );
        }
    }


    // ==========================================
    // STUDENT NAME
    // ==========================================

    private static string GetStudentName(
        Dictionary<string, object> data,
        string fallback)
    {
        return AcademicGradeReport.StudentName(data, fallback);
    }


    private static string GetText(
        Dictionary<string, object> data,
        string field,
        string fallback)
    {
        object value;

        return data.TryGetValue(
                   field,
                   out value
               ) &&
               value != null
            ? value.ToString().Trim()
            : fallback;
    }


    private void OnDestroy()
    {
        nameSortingDropdown?.onValueChanged.RemoveListener(OnSortingChanged);
        gradeSortingDropdown?.onValueChanged.RemoveListener(OnSortingChanged);

        if (studentListener != null)
        {
            studentListener.Stop();
        }
    }


    private class StudentProfile
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Grade;
        public readonly int GradeNumber;

        public StudentProfile(
            string id,
            string name,
            string grade)
        {
            Id = id;
            Name = name;
            Grade = grade;
            string gradeValue = grade.StartsWith("Grade ", StringComparison.OrdinalIgnoreCase)
                ? grade.Substring("Grade ".Length)
                : grade;
            GradeNumber = int.TryParse(gradeValue, out int number) && number >= 1 && number <= 6
                ? number
                : int.MaxValue;
        }
    }
}
