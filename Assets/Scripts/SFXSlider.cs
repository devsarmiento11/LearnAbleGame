using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SFXSlider : MonoBehaviour
{
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        slider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 1f)));
        slider.onValueChanged.AddListener(ChangeVolume);
    }

    private void ChangeVolume(float value)
    {
        // Keep volume independent of the toggle so unmuting restores this level.
        PlayerPrefs.SetFloat("SFXVolume", Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(ChangeVolume);
    }
}
