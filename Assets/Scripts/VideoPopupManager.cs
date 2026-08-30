using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoPopupManager : MonoBehaviour
{
    public GameObject videoPopupPanel;
    public VideoPlayer videoPlayer;
    public Slider progressBar;

    public float totalTime = 15f;

    public string nextSceneName;

    private bool alreadyPlaying = false;

    void Start()
    {
        // Hide video when the scene starts
        videoPopupPanel.SetActive(false);

        // Prepare progress bar
        progressBar.minValue = 0;
        progressBar.maxValue = totalTime;
        progressBar.value = 0;

        // User cannot drag the bar
        progressBar.interactable = false;

        // Make video loop
        videoPlayer.isLooping = true;
        videoPlayer.playOnAwake = false;
    }

    public void OpenVideo()
    {
        if (!alreadyPlaying)
        {
            StartCoroutine(PlayVideo());
        }
    }

    IEnumerator PlayVideo()
    {
        alreadyPlaying = true;

        // Show popup
        videoPopupPanel.SetActive(true);

        progressBar.value = 0;

        // Prepare video
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        // Start video
        videoPlayer.Play();

        float timer = 0;

        while (timer < totalTime)
        {
            timer += Time.unscaledDeltaTime;

            progressBar.value = timer;

            yield return null;
        }

        // Finish bar
        progressBar.value = totalTime;

        // Stop video
        videoPlayer.Stop();

        // Go to next scene
        SceneManager.LoadScene(nextSceneName);
    }
}