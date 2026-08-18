using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ScienceSelectionManager : MonoBehaviour
{
    public static ScienceSelectionManager Instance;

    [Header("Choices")]
    public GlowOnClick[] choices;

    [Header("Selection Limit")]
    [Tooltip("Maximum number of answers the player can select.")]
    public int maxSelections = 2;

    [Header("Correct Answers")]
    [Tooltip("Number of correct answers in this activity.")]
    public int totalCorrectAnswers = 2;

    [Header("Scoring")]
    [Tooltip("Points deducted for each wrong selected answer.")]
    public int wrongAnswerPenalty = 25;

    [Header("Score Scenes")]
    public string scoreSceneName = "ScoreScene";
    public string failedScoreSceneName = "ScoreSceneFailed";

    private List<GlowOnClick> selectedChoices = new List<GlowOnClick>();

    void Awake()
    {
        Instance = this;
    }

    public bool CanSelect()
    {
        return selectedChoices.Count < maxSelections;
    }

    public void AddSelection(GlowOnClick choice)
    {
        if (!selectedChoices.Contains(choice))
        {
            selectedChoices.Add(choice);
        }

        Debug.Log(
            "Selected: " +
            selectedChoices.Count +
            "/" +
            maxSelections
        );
    }

    public void RemoveSelection(GlowOnClick choice)
    {
        if (selectedChoices.Contains(choice))
        {
            selectedChoices.Remove(choice);
        }
    }

    public void Done()
    {
        int correct = 0;
        int wrong = 0;

        foreach (GlowOnClick item in selectedChoices)
        {
            if (item == null)
                continue;

            if (item.IsCorrect)
            {
                correct++;
            }
            else
            {
                wrong++;
            }
        }

        // Calculate score
        int scoreFromCorrect =
            Mathf.RoundToInt(
                (correct / (float)totalCorrectAnswers) * 100f
            );

        // Deduct points for wrong answers
        int finalScore =
            scoreFromCorrect - (wrong * wrongAnswerPenalty);

        // Don't allow score below 0
        finalScore = Mathf.Clamp(finalScore, 0, 100);

        ScoreManager.CorrectLines = correct;
        ScoreManager.CurrentScore = finalScore;

        Debug.Log("Correct Answers = " + correct);
        Debug.Log("Wrong Answers = " + wrong);
        Debug.Log("Score Before Penalty = " + scoreFromCorrect);
        Debug.Log("Wrong Answer Penalty = " + (wrong * wrongAnswerPenalty));
        Debug.Log("FINAL SCORE = " + finalScore);

        // Save current game scene for Try Again
        PlayerPrefs.SetString(
            "PreviousGameScene",
            SceneManager.GetActiveScene().name
        );

        PlayerPrefs.Save();

        // 50 or higher = pass
        if (finalScore >= 50)
        {
            ScoreManager.RecordSuccessfulActivity(SceneManager.GetActiveScene().name, totalCorrectAnswers);
            SceneManager.LoadScene(scoreSceneName);
        }
        else
        {
            SceneManager.LoadScene(failedScoreSceneName);
        }
    }
}
