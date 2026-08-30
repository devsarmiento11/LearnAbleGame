#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class AutoFocusGameViewOnPlay
{
    static AutoFocusGameViewOnPlay()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        EditorApplication.delayCall += FocusGameView;
    }

    private static void FocusGameView()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
}
#endif
