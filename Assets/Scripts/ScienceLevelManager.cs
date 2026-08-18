using UnityEngine;
using UnityEngine.SceneManagement;

public class ScienceLevelManager : MonoBehaviour
{
    [Header("Drop Zones")]
    public DropZone[] dropZones;

    [Header("Settings")]
    public int totalQuestions = 4;

    [Header("Score Scenes")]
    public string scoreSceneName = "ScoreScene";
    public string failedScoreSceneName = "ScoreSceneFailed";

    public void Done()
    {
        int correct = 0;

        foreach (DropZone zone in dropZones)
        {
            if (zone != null && zone.IsCorrect())
            {
                correct++;
            }
        }

        ScoreManager.CorrectLines = correct;

        ScoreManager.CurrentScore =
            Mathf.RoundToInt((correct / (float)totalQuestions) * 100f);

        Debug.Log("Correct = " + correct);
        Debug.Log("Total Questions = " + totalQuestions);
        Debug.Log("Score = " + ScoreManager.CurrentScore);

        // Save the current game scene for the Try Again button
        PlayerPrefs.SetString(
            "PreviousGameScene",
            SceneManager.GetActiveScene().name
        );

        PlayerPrefs.Save();

        // 50 or higher = passed
        if (ScoreManager.CurrentScore >= 50)
        {
            ScoreManager.RecordSuccessfulActivity(SceneManager.GetActiveScene().name, totalQuestions);
            SceneManager.LoadScene(scoreSceneName);
        }
        else
        {
            SceneManager.LoadScene(failedScoreSceneName);
        }
    }
}
