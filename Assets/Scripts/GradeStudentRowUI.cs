using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GradeStudentRowUI : MonoBehaviour
{
    [Header("ROW UI")]
    public TMP_Text nameText;
    public TMP_Text gradeText;
    public Button viewGradeButton;

    private string studentId;
    private string studentName;
    private string studentGrade;

    public void Setup(
        string id,
        string name,
        string grade,
        GradesSceneManager manager)
    {
        studentId = id;
        studentName = name;
        studentGrade = grade;

        if (nameText != null)
        {
            nameText.text = studentName.ToUpper();
        }

        if (gradeText != null)
        {
            string cleanGrade = grade
                .Replace("GRADE", "")
                .Replace("Grade", "")
                .Replace("grade", "")
                .Trim();

            gradeText.text = "GRADE " + cleanGrade;
        }

        if (viewGradeButton != null)
        {
            viewGradeButton.onClick.RemoveAllListeners();

            viewGradeButton.onClick.AddListener(() =>
            {
                manager.OpenStudentGrades(
                    studentId,
                    studentName,
                    studentGrade
                );
            });
        }
    }
}