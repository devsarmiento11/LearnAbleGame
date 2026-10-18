using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Routes a level button to its existing video manager using the saved language.
/// Reuse one component per level, assigning that level's two video managers.
/// </summary>
public sealed class LocalizedVideoPopup : MonoBehaviour
{
    public VideoPopupManager english;
    public VideoPopupManager tagalog;

    private static LocalizedVideoPopup activePopup;
    private VideoPlayer introPlayer;
    private AudioSource introAudio;

    public void OpenVideo()
    {
        // A second click must not start another player or another scene timer.
        if (activePopup != null)
            return;

        VideoPopupManager selected = LanguageManager.EnsureInstance().IsTagalog()
            ? tagalog : english;
        if (selected == null || selected.videoPopupPanel == null ||
            selected.videoPlayer == null || selected.progressBar == null ||
            !selected.isActiveAndEnabled)
        {
            Debug.LogError("LocalizedVideoPopup: Assign both complete video managers for " + name, this);
            return;
        }

        activePopup = this;
        ConfigureIntroAudio(selected.videoPlayer);
        selected.OpenVideo();
    }

    private void ConfigureIntroAudio(VideoPlayer player)
    {
        // Configure before Prepare so decoded audio uses Unity's buffered output.
        // Leave video preparation, rendering, looping and the level timer alone.
        if (player.canSetTimeUpdateMode)
            player.timeUpdateMode = VideoTimeUpdateMode.DSPTime;

        // Only the selected player needs an audio source; do not start ten decoders.
        AudioSource source = player.gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.priority = 0;
        source.volume = 1f;
        // Decode audio during the first pass, but keep that entire pass silent.
        source.mute = true;
        player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        player.controlledAudioTrackCount = 1;
        player.EnableAudioTrack(0, true);
        player.SetTargetAudioSource(0, source);
        introPlayer = player;
        introAudio = source;
        introPlayer.loopPointReached += BeginSecondLoopAudio;
    }

    private void BeginSecondLoopAudio(VideoPlayer player)
    {
        // The first end-of-loop callback marks the second pass. Use the actual
        // loop boundary rather than a delay, since the clips have different lengths.
        if (player != introPlayer || !player.isLooping)
            return;

        player.loopPointReached -= BeginSecondLoopAudio;
        if (introAudio != null)
            introAudio.mute = false;
    }

    private void OnDisable()
    {
        if (introPlayer != null)
            introPlayer.loopPointReached -= BeginSecondLoopAudio;
        if (introAudio != null)
            introAudio.mute = true;
        if (activePopup == this)
            activePopup = null;
    }
}
