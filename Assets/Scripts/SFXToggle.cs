using UnityEngine;
using UnityEngine.UI;

public class SFXToggle : MonoBehaviour
{
    public Toggle sfxToggle;

    void Awake()
    {
        sfxToggle.onValueChanged.AddListener(ToggleSFX);
    }

    void Start()
    {
        bool sfxOn = PlayerPrefs.GetInt("SFX", 1) == 1;

        sfxToggle.SetIsOnWithoutNotify(sfxOn);
    }

    public void ToggleSFX(bool isOn)
    {
        PlayerPrefs.SetInt("SFX", isOn ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("SFX: " + (isOn ? "ON" : "OFF"));
    }

    public static bool IsSFXOn()
    {
        return PlayerPrefs.GetInt("SFX", 1) == 1;
    }
}