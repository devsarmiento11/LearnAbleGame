using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneReturnHistory
{
    private sealed class Visit
    {
        public readonly string SceneName;
        public readonly Dictionary<System.Type, object> State = new Dictionary<System.Type, object>();

        public Visit(string sceneName) => SceneName = sceneName;
    }

    private static readonly List<Visit> history = new List<Visit>();
    private static Visit current;
    private static Visit returning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Clear();
    }

    public static void Clear()
    {
        history.Clear();
        current = null;
        returning = null;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        if (scene.name == "LoginScene")
            Clear();

        if (returning != null && returning.SceneName == scene.name)
        {
            current = returning;
        }
        else if (current == null || current.SceneName != scene.name)
        {
            // Returning through a workflow action (for example, saving an edit)
            // reuses the existing visit and drops the finished branch.
            int index = history.FindLastIndex(visit => visit.SceneName == scene.name);
            if (index >= 0)
            {
                current = history[index];
                history.RemoveRange(index, history.Count - index);
            }
            else
            {
                if (current != null && !IsTransient(current.SceneName))
                    history.Add(current);
                current = new Visit(scene.name);
            }
        }

        returning = null;
    }

    private static bool IsTransient(string sceneName) =>
        sceneName == "LoginScene" || sceneName == "ScoreScene" ||
        sceneName == "ScoreSceneFailed" || sceneName == "SettingsScene";

    public static string BackDestination(string fallback)
    {
        if (history.Count == 0)
            return fallback;

        int index = history.Count - 1;
        Visit previous = history[index];
        if (!Application.CanStreamedLevelBeLoaded(previous.SceneName) ||
            (LoginSession.IsParent && !ParentSceneAccess.IsAllowed(previous.SceneName)))
            return fallback;

        history.RemoveAt(index);
        returning = previous;
        return previous.SceneName;
    }

    public static void Save<T>(Component owner, T state)
    {
        if (current != null && current.SceneName == owner.gameObject.scene.name)
            current.State[owner.GetType()] = state;
    }

    public static T Restore<T>(Component owner, T fallback)
    {
        if (current != null && current.SceneName == owner.gameObject.scene.name &&
            current.State.TryGetValue(owner.GetType(), out object stored) && stored is T value)
            return value;

        return fallback;
    }

    public static void GoBack(string fallback)
    {
        string destination = BackDestination(fallback);
        if (!string.IsNullOrEmpty(destination))
            SceneManager.LoadScene(destination);
    }
}
