using UnityEngine;
using UnityEngine.UI;

public class SFXVolume : MonoBehaviour
{
    public Toggle sfxToggle;
    public AudioSource sfxSource;

    void Start()
    {
        float volume = PlayerPrefs.GetFloat("SFXVolume", 1f);

        bool sfxOn = volume > 0f;

        sfxToggle.SetIsOnWithoutNotify(sfxOn);
        sfxSource.volume = volume;

        sfxToggle.onValueChanged.AddListener(ChangeVolume);
    }

    void ChangeVolume(bool isOn)
    {
        float volume = isOn ? 1f : 0f;

        sfxSource.volume = volume;

        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();

        Debug.Log("SFX: " + (isOn ? "ON" : "OFF"));
    }
}