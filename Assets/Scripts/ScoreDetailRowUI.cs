using TMPro;
using UnityEngine;

public class ScoreDetailRowUI : MonoBehaviour
{
    [Header("SCORE ROW UI")]
    public TMP_Text activityText;
    public TMP_Text levelText;
    public TMP_Text lowestScoreText;
    public TMP_Text highestScoreText;
    public TMP_Text timeUsedText;
    public TMP_Text attemptsText;
    public TMP_Text dateText;

    public void Setup(
        string activity,
        string level,
        string lowestScore,
        string highestScore,
        string timeUsed,
        string attempts,
        string date)
    {
        activityText.text = activity;
        levelText.text = level;
        lowestScoreText.text = lowestScore;
        highestScoreText.text = highestScore;
        timeUsedText.text = timeUsed;
        attemptsText.text = attempts;
        dateText.text = date;
    }
}