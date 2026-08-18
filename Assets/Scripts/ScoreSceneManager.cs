using TMPro;
using UnityEngine;

public class ScoreSceneManager : MonoBehaviour
{
    public TMP_Text scoreText;

    void Start()
    {
        scoreText.text =
            "TOTAL SCORE: " +
            ScoreManager.CurrentScore +
            "/100";
    }
}