using UnityEngine;
using UnityEngine.UI;

public class MusicVolume : MonoBehaviour
{
    public Slider slider;

    void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        if (slider == null)
        {
            Debug.LogWarning("MusicVolume: Slider component is not assigned.");
            enabled = false;
            return;
        }

        float volume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        slider.SetValueWithoutNotify(volume);

        ApplyToMusicManager(volume);

        slider.onValueChanged.AddListener(ChangeVolume);
    }

    void ChangeVolume(float value)
    {
        ApplyToMusicManager(value);

        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    private static void ApplyToMusicManager(float value)
    {
        if (MusicManager.Instance != null &&
            MusicManager.Instance.musicSource != null)
        {
            MusicManager.Instance.musicSource.volume = value;
        }
    }

    private void OnDestroy()
    {
        if (slider != null)
            slider.onValueChanged.RemoveListener(ChangeVolume);
    }
}
