using UnityEngine;
using UnityEngine.SceneManagement;

public class EnglishLevel1Manager : MonoBehaviour
{
    [Header("Drawing")]
    public UIDrawing drawing;

    public void Done()
{
    int completed = drawing.GetCompletedLines();

    ScoreManager.CorrectLines = completed;

    // Score out of 100
    ScoreManager.CurrentScore =
        Mathf.RoundToInt((completed / 18f) * 100f);

    Debug.Log("Completed Lines = " + completed);
    Debug.Log("Final Score = " + ScoreManager.CurrentScore);

    PlayerPrefs.SetString("PreviousGameScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
        ScoreManager.RecordActivity(SceneManager.GetActiveScene().name, 18);
    SceneManager.LoadScene(ScoreManager.CurrentScore >= 50
            ? PassedLevelNavigation.Destination("ScoreScene") : "ScoreSceneFailed");
}
}
