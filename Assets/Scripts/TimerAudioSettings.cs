using UnityEngine;

// Shared by every GameTimer, including scenes opened directly in the editor.
public class TimerAudioSettings : ScriptableObject
{
    public AudioClip normalCountdown;
    public AudioClip lastTenSeconds;
}
