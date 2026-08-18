using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ButtonClickSound : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayClick()
    {
        audioSource.Play();
    }
}