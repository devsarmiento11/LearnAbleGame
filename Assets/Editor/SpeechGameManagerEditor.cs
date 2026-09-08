using UnityEditor;
using UnityEngine;

// The installed plugin emits "Hello world" in Editor instead of recognizing audio.
// Supply a transcript here to test the same answer/reveal/scoring flow as Android.
[CustomEditor(typeof(SpeechGameManager))]
public class SpeechGameManagerEditor : Editor
{
    private string transcript = "six";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Android uses microphone recognition. The plugin only simulates speech in Editor. In Play Mode, enter a transcript below to test an answer.", MessageType.Info);
        transcript = EditorGUILayout.TextField("Test transcript", transcript);
        var manager = (SpeechGameManager)target;
        using (new EditorGUI.DisabledScope(!Application.isPlaying || !manager.isActiveAndEnabled || SpeechToText.IsBusy()))
        {
            if (GUILayout.Button("Test spoken answer"))
                manager.OnResultReceived(transcript, null);
        }
    }
}
