using UnityEngine;
using UnityEngine.UI;

public class MusicToggle : MonoBehaviour
{
    public Toggle musicToggle;

    void Awake()
    {
        if (musicToggle == null)
        {
            musicToggle = GetComponent<Toggle>();
        }

        if (musicToggle != null)
        {
            musicToggle.onValueChanged.AddListener(ToggleMusic);
        }
        else
        {
            Debug.LogError("MusicToggle: No Toggle component found!");
        }
    }

    void Start()
    {
        bool musicOn = PlayerPrefs.GetInt("Music", 1) == 1;

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(musicOn);
        }

        AudioListener.volume = musicOn ? 1f : 0f;
    }

    public void ToggleMusic(bool isOn)
    {
        Debug.Log("Toggle Changed: " + isOn);

        AudioListener.volume = isOn ? 1f : 0f;

        PlayerPrefs.SetInt("Music", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        if (musicToggle != null)
        {
            musicToggle.onValueChanged.RemoveListener(ToggleMusic);
        }
    }
}