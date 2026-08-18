using UnityEngine;
using UnityEngine.SceneManagement;

public class TracingLevelManager : MonoBehaviour
{
    [Header("Drawing")]
    public UIDrawing drawing;

    [Header("Level Settings")]
    [Tooltip("Total number of LineEnd objects in this level.")]
    public int totalLines = 18;

    [Header("Score Scenes")]
    public string scoreSceneName = "ScoreScene";
    public string failedScoreSceneName = "ScoreSceneFailed";

    public void Done()
    {
        if (drawing == null)
        {
            Debug.LogError("UIDrawing is not assigned!");
            return;
        }

        int completed = drawing.GetCompletedLines();

        ScoreManager.CorrectLines = completed;

        ScoreManager.CurrentScore =
            Mathf.RoundToInt((completed / (float)totalLines) * 100f);

        Debug.Log("Completed Lines: " + completed);
        Debug.Log("Total Lines: " + totalLines);
        Debug.Log("Final Score: " + ScoreManager.CurrentScore);

        // Save the game scene we came from
        PlayerPrefs.SetString(
            "PreviousGameScene",
            SceneManager.GetActiveScene().name
        );

        PlayerPrefs.Save();

        // 50 or higher = passed
        if (ScoreManager.CurrentScore >= 50)
        {
            ScoreManager.RecordSuccessfulActivity(SceneManager.GetActiveScene().name, totalLines);
            SceneManager.LoadScene(scoreSceneName);
        }
        else
        {
            SceneManager.LoadScene(failedScoreSceneName);
        }
    }
}
