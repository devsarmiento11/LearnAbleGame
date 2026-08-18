using UnityEngine;
using UnityEngine.SceneManagement;

public class ScienceMatchingManager : MonoBehaviour
{
    [Header("Matching Settings")]
    public int totalMatches = 4;

    [Header("Score Scenes")]
    public string scoreScene = "ScoreScene";
    public string failedScoreScene = "ScoreSceneFailed";

    public void Done()
    {
        if (MatchManager.Instance == null)
        {
            Debug.LogError("MatchManager.Instance not found!");
            return;
        }

        int correct = MatchManager.Instance.GetCorrectMatches();

        ScoreManager.CorrectLines = correct;

        ScoreManager.CurrentScore =
            Mathf.RoundToInt(
                (correct / (float)totalMatches) * 100f
            );

        Debug.Log("Correct = " + correct);
        Debug.Log("Total Matches = " + totalMatches);
        Debug.Log("Score = " + ScoreManager.CurrentScore);

        // Save the current matching game scene
        PlayerPrefs.SetString(
            "PreviousGameScene",
            SceneManager.GetActiveScene().name
        );

        PlayerPrefs.Save();

        // 50 or higher = PASS
        if (ScoreManager.CurrentScore >= 50)
        {
            ScoreManager.RecordSuccessfulActivity(SceneManager.GetActiveScene().name, totalMatches);
            SceneManager.LoadScene(scoreScene);
        }
        else
        {
            SceneManager.LoadScene(failedScoreScene);
        }
    }
}
