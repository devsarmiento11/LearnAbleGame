using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sound Effects")]
    public AudioSource sfxSource;
    public AudioClip buttonClick;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            sfxSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void PlayButtonClick()
    {
        if (buttonClick != null)
            sfxSource.PlayOneShot(buttonClick);
    }

    public void LoadSceneWithClick(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    IEnumerator LoadSceneRoutine(string sceneName)
    {
        PlayButtonClick();

        yield return new WaitForSeconds(0.15f);

        SceneManager.LoadScene(sceneName);
    }
}