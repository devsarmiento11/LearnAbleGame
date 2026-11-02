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
    private float elapsedTime;
    private bool timerRunning = true;
    private const float AudioWarningTime = 10f;
    private AudioSource timerAudioSource;
    private TimerAudioSettings timerAudioSettings;
    private bool audioPaused;

    public float ElapsedTime => elapsedTime;
    // Optional activity-specific scoring on timeout; legacy scenes retain their behavior.
    public System.Action Expired;
    public void StopTimer()
    {
        timerRunning = false;
        if (timerAudioSource != null)
            timerAudioSource.Stop();
    }

    // Colors
    public Color normalColor = Color.white;
    public Color warningColor = Color.red;

    void Start()
    {
        ScoreManager.BeginActivity();
        currentTime = startingTime;
        elapsedTime = 0f;
        InitializeTimerAudio();

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

        float frameTime = Mathf.Min(Time.deltaTime, currentTime);
        elapsedTime += frameTime;
        currentTime -= frameTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;

            UpdateTimerDisplay();

            StopTimer();

            TimeUp();

            return;
        }

        UpdateTimerDisplay();
        UpdateTimerAudio();
    }

    private void InitializeTimerAudio()
    {
        timerAudioSettings = Resources.Load<TimerAudioSettings>("TimerAudioSettings");
        if (timerAudioSettings == null)
        {
            Debug.LogWarning("TimerAudioSettings is missing from Resources.", this);
            return;
        }

        // A dedicated source keeps timer loops separate from other game sounds.
        timerAudioSource = gameObject.AddComponent<AudioSource>();
        timerAudioSource.playOnAwake = false;
        timerAudioSource.loop = true;
        timerAudioSource.spatialBlend = 0f;
        UpdateTimerAudio();
    }

    private void UpdateTimerAudio()
    {
        if (timerAudioSource == null || !timerRunning || currentTime <= 0f)
            return;

        timerAudioSource.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f));
        timerAudioSource.mute = !SFXToggle.IsSFXOn();

        if (Time.timeScale == 0f)
        {
            if (!audioPaused)
            {
                timerAudioSource.Pause();
                audioPaused = true;
            }
            return;
        }

        AudioClip clip = currentTime <= AudioWarningTime
            ? timerAudioSettings.lastTenSeconds
            : timerAudioSettings.normalCountdown;
        if (timerAudioSource.clip != clip)
        {
            timerAudioSource.Stop();
            timerAudioSource.clip = clip;
            audioPaused = false;
        }

        if (audioPaused)
        {
            timerAudioSource.UnPause();
            audioPaused = false;
        }
        if (clip != null && !timerAudioSource.isPlaying)
            timerAudioSource.Play();
    }

    private void OnEnable()
    {
        UpdateTimerAudio();
    }

    private void OnDisable()
    {
        if (timerAudioSource != null)
            timerAudioSource.Stop();
        audioPaused = false;
    }

    public int GetElapsedSeconds()
    {
        return Mathf.Max(0, Mathf.CeilToInt(elapsedTime));
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
        if (Expired != null)
        {
            Expired.Invoke();
            return;
        }
        Debug.Log("TIME'S UP!");

        ScoreManager.CurrentScore = 0;
        ScoreManager.CorrectLines = 0;
        ScoreManager.RecordActivity(SceneManager.GetActiveScene().name, 0);

        PlayerPrefs.SetString(
            "PreviousGameScene",
            SceneManager.GetActiveScene().name
        );

        PlayerPrefs.Save();

        SceneManager.LoadScene(failedSceneName);
    }
}
