using UnityEngine;
using UnityEngine.UI;

public class MicToggle : MonoBehaviour
{
    public Toggle toggle;

    public GameObject micOnImage;
    public GameObject micOffImage;

    void Start()
    {
        bool micOn = PlayerPrefs.GetInt("Mic", 1) == 1;

        toggle.SetIsOnWithoutNotify(micOn);

        UpdateMicImage(micOn);

        toggle.onValueChanged.AddListener(ToggleMic);
    }

    void ToggleMic(bool isOn)
    {
        PlayerPrefs.SetInt("Mic", isOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateMicImage(isOn);

        Debug.Log("Mic: " + (isOn ? "ON" : "OFF"));
    }

    void UpdateMicImage(bool isOn)
    {
        micOnImage.SetActive(isOn);
        micOffImage.SetActive(!isOn);
    }
}