using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keep the actual level alive so returning from Settings does not run Start again.
public class GameplaySettingsSession : MonoBehaviour
{
    private static GameplaySettingsSession instance;
    private Scene gameScene;
    private GameObject[] gameRoots;
    private bool[] activeRoots;
    private bool changingScene;
    private float previousTimeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        instance = null;
    }

    public static bool TryOpen()
    {
        if (instance != null)
            return true;

        Scene scene = SceneManager.GetActiveScene();
        bool hasTimer = false;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<GameTimer>() != null)
            {
                hasTimer = true;
                break;
            }
        }
        if (!hasTimer || LoginSession.IsParent)
            return false;

        GameObject host = new GameObject("Gameplay Settings Session");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<GameplaySettingsSession>();
        instance.gameScene = scene;
        instance.StartCoroutine(instance.Open());
        return true;
    }

    private IEnumerator Open()
    {
        changingScene = true;
        previousTimeScale = Time.timeScale;
        gameRoots = gameScene.GetRootGameObjects();
        activeRoots = new bool[gameRoots.Length];
        for (int i = 0; i < gameRoots.Length; i++)
        {
            activeRoots[i] = gameRoots[i].activeSelf;
            gameRoots[i].SetActive(false);
        }

        // Settings UI runs normally; the inactive game does not advance.
        Time.timeScale = 1f;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AsyncOperation load = SceneManager.LoadSceneAsync("SettingsScene", LoadSceneMode.Additive);
        if (load == null)
        {
            RestoreGame();
            Destroy(gameObject);
            yield break;
        }
        yield return load;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("SettingsScene"));
        changingScene = false;
    }

    public static bool TryResume()
    {
        if (instance == null || !instance.gameScene.IsValid() || !instance.gameScene.isLoaded)
            return false;
        if (!instance.changingScene)
            instance.StartCoroutine(instance.Resume());
        return true;
    }

    private IEnumerator Resume()
    {
        changingScene = true;
        SceneManager.SetActiveScene(gameScene);
        AsyncOperation unload = SceneManager.UnloadSceneAsync("SettingsScene");
        if (unload != null)
            yield return unload;
        RestoreGame();
        Destroy(gameObject);
    }

    private void RestoreGame()
    {
        Time.timeScale = previousTimeScale;
        for (int i = 0; i < gameRoots.Length; i++)
        {
            if (gameRoots[i] != null && activeRoots[i])
                gameRoots[i].SetActive(true);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Logout or another normal navigation discards the suspended game.
        if (mode == LoadSceneMode.Single)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
            instance = null;
    }
}
