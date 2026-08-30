using UnityEngine;
using UnityEngine.UI;

public class SFXToggle : MonoBehaviour
{
    public Toggle sfxToggle;

    void Awake()
    {
        if (sfxToggle == null)
            sfxToggle = GetComponent<Toggle>();

        if (sfxToggle == null)
        {
            // Some older scenes contain a manager-only SFXToggle component.
            // It has no UI Toggle and should not run.
            enabled = false;
            return;
        }

        sfxToggle.onValueChanged.AddListener(ToggleSFX);
    }

    void Start()
    {
        if (sfxToggle == null)
            return;

        bool sfxOn = PlayerPrefs.GetInt("SFX", 1) == 1;

        sfxToggle.SetIsOnWithoutNotify(sfxOn);
    }

    private void OnDestroy()
    {
        if (sfxToggle != null)
            sfxToggle.onValueChanged.RemoveListener(ToggleSFX);
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
