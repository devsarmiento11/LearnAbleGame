using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private const string PreviousSceneKey = "SettingsPreviousScene";
    private const string LoggedInKey = "StudentLoggedIn";

    [Header("SETTINGS - OPTIONAL")]
    [Tooltip("Only assign this in SettingsScene.")]
    public GameObject logoutButton;

    [Header("LOGIN SCENE")]
    public string loginSceneName = "LoginScene";

    [Header("SETTINGS FALLBACK")]
    [Tooltip("Used when SettingsScene is tested directly and has no saved return scene.")]
    public string settingsFallbackSceneName = "MainMenu";
    private bool isChangingScene;


    // ==========================================
    // START
    // ==========================================

    private void Start()
    {
        // A previous gameplay pause must never make Settings or the newly
        // loaded scene unresponsive.
        Time.timeScale = 1f;

        // Only control the Logout button
        // if one was assigned in the Inspector.
        if (logoutButton != null)
        {
            bool isLoggedIn =
                PlayerPrefs.GetInt(LoggedInKey, 0) == 1;

            logoutButton.SetActive(isLoggedIn);

            Debug.Log(
                "Logout Button Visible: " +
                isLoggedIn
            );
        }
    }


    // ==========================================
    // NORMAL SCENE LOADING
    // ==========================================

    public void LoadScene(string sceneName)
    {
        if (isChangingScene || string.IsNullOrWhiteSpace(sceneName))
            return;

        // If this button is loading SettingsScene,
        // automatically save the current scene first.
        if (sceneName == "SettingsScene")
        {
            SaveCurrentScene();
        }

        StartCoroutine(LoadSceneSafely(sceneName));
    }


    // ==========================================
    // OPEN SETTINGS
    // ==========================================

    public void OpenSettings()
    {
        if (isChangingScene)
            return;

        SaveCurrentScene();
        StartCoroutine(LoadSceneSafely("SettingsScene"));
    }

    private System.Collections.IEnumerator LoadSceneSafely(string sceneName)
    {
        if (LoginSession.IsParent && !ParentSceneAccess.IsAllowed(sceneName))
            sceneName = "MainMenuParent";
        isChangingScene = true;
        Time.timeScale = 1f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            isChangingScene = false;
            Debug.LogError("Unable to load scene: " + sceneName);
            yield break;
        }

        while (!operation.isDone)
            yield return null;
    }


    // ==========================================
    // SAVE CURRENT SCENE
    // ==========================================

    private void SaveCurrentScene()
    {
        string currentScene =
            SceneManager.GetActiveScene().name;

        // Never save SettingsScene
        // as the previous scene.
        if (currentScene == "SettingsScene")
            return;

        PlayerPrefs.SetString(
            PreviousSceneKey,
            currentScene
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Saved Settings Previous Scene: " +
            currentScene
        );
    }


    // ==========================================
    // BACK FROM SETTINGS
    // ==========================================

    public void BackFromSettings()
    {
        if (isChangingScene)
            return;

        string previousScene =
            PlayerPrefs.GetString(
                PreviousSceneKey,
                ""
            );

        Debug.Log(
            "Previous Scene = [" +
            previousScene +
            "]"
        );

        if (string.IsNullOrEmpty(previousScene))
        {
            Debug.LogWarning(
                "No previous Settings scene was saved. Returning to " +
                settingsFallbackSceneName + "."
            );

            previousScene = settingsFallbackSceneName;
        }

        if (string.IsNullOrWhiteSpace(previousScene))
        {
            Debug.LogError("Settings fallback scene is not configured.");
            return;
        }

        // Clear it so an old scene
        // cannot be reused.
        PlayerPrefs.DeleteKey(
            PreviousSceneKey
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Returning to: " +
            previousScene
        );

        StartCoroutine(LoadSceneSafely(previousScene));
    }


    // ==========================================
    // STUDENT LOGGED IN
    // ==========================================

    public static void SetStudentLoggedIn()
    {
        PlayerPrefs.SetInt(
            LoggedInKey,
            1
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Student login status saved."
        );
    }


    // ==========================================
    // CHECK LOGIN STATUS
    // ==========================================

    public static bool IsStudentLoggedIn()
    {
        return PlayerPrefs.GetInt(
            LoggedInKey,
            0
        ) == 1;
    }


    // ==========================================
    // LOGOUT
    // ==========================================

    public void Logout()
    {
        Debug.Log(
            "Logging student out..."
        );

        // Remove login status.
        PlayerPrefs.DeleteKey(
            LoggedInKey
        );

        // Remove previous Settings scene
        // so it cannot return to an old
        // student screen after logout.
        PlayerPrefs.DeleteKey(
            PreviousSceneKey
        );

        // Do not leave the previous Firestore profile active after logout.
        LearningDataStore.ClearCurrentUser();
        LoginSession.Logout();
        FirebaseAuth.DefaultInstance.SignOut();

        PlayerPrefs.Save();

        // Go back to login.
        SceneManager.LoadScene(
            loginSceneName
        );
    }
}
