using System;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

public class GradesSceneManager : MonoBehaviour
{
    [Header("STUDENT LIST")]
    public Transform content;
    public GameObject studentRowPrefab;

    [Header("SCENE LOADER")]
    public SceneLoader sceneLoader;

    [Header("NEXT SCENE")]
    public string studentGradesSceneName;

    private FirebaseFirestore db;
    private ListenerRegistration studentListener;

    private readonly Dictionary<string, GameObject> studentRows =
        new Dictionary<string, GameObject>();


    private void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

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
                List<StudentProfile> students =
                    new List<StudentProfile>();

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

                students.Sort(
                    (a, b) =>
                        string.Compare(
                            a.Name,
                            b.Name,
                            StringComparison.OrdinalIgnoreCase
                        )
                );

                RebuildRows(students);
            });
    }


    // ==========================================
    // REBUILD STUDENT LIST
    // ==========================================

    private void RebuildRows(
        List<StudentProfile> students)
    {
        foreach (GameObject row
                 in studentRows.Values)
        {
            if (row != null)
                Destroy(row);
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
}
