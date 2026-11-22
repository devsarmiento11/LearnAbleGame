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
    private static string forwardDestination;
    private static SceneHandle currentSceneHandle;

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
        forwardDestination = null;
        currentSceneHandle = default;
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
            int index = history.IndexOf(returning);
            if (index >= 0)
                history.RemoveRange(index, history.Count - index);
            else
                history.Clear();
        }
        else if (current == null || current.SceneName != scene.name)
        {
            // Returning through a workflow action (for example, saving an edit)
            // reuses the existing visit and drops the finished branch.
            int index = forwardDestination == scene.name
                ? -1
                : history.FindLastIndex(visit => visit.SceneName == scene.name);
            if (index >= 0)
            {
                current = history[index];
                history.RemoveRange(index, history.Count - index);
            }
            else
            {
                if (current != null && (scene.name == "SettingsScene" || !IsTransient(current.SceneName)))
                    history.Add(current);
                current = new Visit(scene.name);
            }
        }

        returning = null;
        forwardDestination = null;
        currentSceneHandle = scene.handle;
    }

    private static bool IsTransient(string sceneName) =>
        sceneName == "LoginScene" || sceneName == "ScoreScene" ||
        sceneName == "ScoreSceneFailed" || sceneName == "SettingsScene";

    public static string BackDestination(string fallback, bool requireDestination = false)
    {
        for (int index = history.Count - 1; index >= 0; index--)
        {
            Visit previous = history[index];
            if ((requireDestination && previous.SceneName != fallback) || !CanReturnTo(previous.SceneName))
                continue;

            returning = previous;
            return previous.SceneName;
        }

        if (!CanReturnTo(fallback))
            return null;

        // An explicit return without a matching visit starts a fresh path.
        if (requireDestination)
            returning = new Visit(fallback);
        return fallback;
    }

    private static bool CanReturnTo(string sceneName) =>
        !string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName) &&
        (!LoginSession.IsParent || ParentSceneAccess.IsAllowed(sceneName));

    public static void BeginForward(string sceneName)
    {
        if (current != null && !IsTransient(current.SceneName))
            forwardDestination = sceneName;
    }

    public static void Save<T>(Component owner, T state)
    {
        if (current != null && currentSceneHandle == owner.gameObject.scene.handle)
            current.State[owner.GetType()] = state;
    }

    public static T Restore<T>(Component owner, T fallback)
    {
        if (current != null && currentSceneHandle == owner.gameObject.scene.handle &&
            current.State.TryGetValue(owner.GetType(), out object stored) && stored is T value)
            return value;

        return fallback;
    }

    public static void GoBack(string fallback)
    {
        string destination = BackDestination(fallback);
        if (!string.IsNullOrEmpty(destination))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(destination);
        }
    }
}
