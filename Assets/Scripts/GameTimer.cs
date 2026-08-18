using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float startingTime = 60f;

    [Header("Timer UI")]
    public TMP_Text timerText;

    [Header("Timer Warning")]
    public float warningTime = 10f;

    [Header("Failed Scene")]
    public string failedSceneName = "ScoreSceneFailed";

    private float currentTime;
    private bool timerRunning = true;

    // Colors
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;

    void Start()
    {
        currentTime = startingTime;

        // Start with normal color
        if (timerText != null)
        {
            timerText.color = normalColor;
        }

        UpdateTimerDisplay();
    }

    void Update()
    {
        if (!timerRunning)
            return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;

            UpdateTimerDisplay();

            timerRunning = false;

            TimeUp();

            return;
        }

        UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(currentTime);

        timerText.text = seconds.ToString();

        // Turn RED when 10 seconds or less remain
        if (currentTime <= warningTime)
        {
            timerText.color = warningColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }

    void TimeUp()
    {
        Debug.Log("TIME'S UP!");

        PlayerPrefs.SetString(
            "PreviousGameScene",
            SceneManager.GetActiveScene().name
        );

        PlayerPrefs.Save();

        SceneManager.LoadScene(failedSceneName);
    }
}