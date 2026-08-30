using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StudentScoreRowUI : MonoBehaviour
{
    [Header("ROW UI")]
    public TMP_Text nameText;
    public TMP_Text gradeText;
    public Button viewButton;

    private string studentId;
    private string studentName;
    private string studentGrade;

    public void Setup(
        string id,
        string name,
        string grade,
        TeacherScoreManager manager)
    {
        studentId = id;
        studentName = name;
        studentGrade = grade;

        nameText.text = string.IsNullOrWhiteSpace(name)
            ? "STUDENT"
            : name.Trim().ToUpperInvariant();

        string displayedGrade = string.IsNullOrWhiteSpace(grade)
            ? ""
            : grade.Trim().ToUpperInvariant();

        if (displayedGrade.StartsWith("GRADE"))
            gradeText.text = displayedGrade;
        else if (!string.IsNullOrEmpty(displayedGrade))
            gradeText.text = "GRADE " + displayedGrade;
        else
            gradeText.text = "GRADE —";

        viewButton.onClick.RemoveAllListeners();

        viewButton.onClick.AddListener(() =>
        {
            manager.OpenStudentScores(
                studentId,
                studentName,
                studentGrade
            );
        });
    }
}
