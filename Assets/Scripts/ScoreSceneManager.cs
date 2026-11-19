using TMPro;
using UnityEngine;

public class ScoreSceneManager : MonoBehaviour
{
    public TMP_Text scoreText;

    void Start()
    {
        string scoreColor = ScoreManager.CurrentScore >= 75 ? "#00FF00" : "#FF0000";
        scoreText.richText = true;
        scoreText.text =
            "TOTAL SCORE: <color=" + scoreColor + ">" +
            ScoreManager.CurrentScore +
            "/100</color>";
    }
}
