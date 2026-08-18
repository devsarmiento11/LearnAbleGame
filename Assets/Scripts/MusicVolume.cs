using UnityEngine;
using UnityEngine.UI;

public class MusicVolume : MonoBehaviour
{
    public Slider slider;

    void Start()
    {
        if (MusicManager.Instance == null)
        {
            Debug.LogError("MusicManager not found!");
            return;
        }

        float volume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        slider.value = volume;

        MusicManager.Instance.musicSource.volume = volume;

        slider.onValueChanged.AddListener(ChangeVolume);
    }

    void ChangeVolume(float value)
    {
        MusicManager.Instance.musicSource.volume = value;

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }
}