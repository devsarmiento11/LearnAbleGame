using UnityEngine;
using UnityEngine.SceneManagement;

public class EnglishLevel2Manager : MonoBehaviour
{
    [Header("Drawing")]
    public UIDrawing drawing;

    public void Done()
    {
        int completed = drawing.GetCompletedLines();

        ScoreManager.CorrectLines = completed;

        // Level 2 has 16 lines
        ScoreManager.CurrentScore =
            Mathf.RoundToInt((completed / 16f) * 100f);

        Debug.Log("Completed Lines = " + completed);
        Debug.Log("Final Score = " + ScoreManager.CurrentScore);

        PlayerPrefs.SetString("PreviousGameScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        ScoreManager.RecordActivity(SceneManager.GetActiveScene().name, 16);
        SceneManager.LoadScene(ScoreManager.CurrentScore >= 50
            ? PassedLevelNavigation.Destination("ScoreScene") : "ScoreSceneFailed");
    }
}
