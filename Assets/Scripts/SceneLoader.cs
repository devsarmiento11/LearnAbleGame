using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private const string PreviousSceneKey = "SettingsPreviousScene";

    // ==========================================
    // NORMAL SCENE LOADING
    // ==========================================

    public void LoadScene(string sceneName)
    {
        // If this button is loading SettingsScene,
        // automatically save the current scene first.
        if (sceneName == "SettingsScene")
        {
            SaveCurrentScene();
        }

        SceneManager.LoadScene(sceneName);
    }

    // ==========================================
    // OPEN SETTINGS
    // ==========================================

    public void OpenSettings()
    {
        SaveCurrentScene();

        SceneManager.LoadScene("SettingsScene");
    }

    // ==========================================
    // SAVE CURRENT SCENE
    // ==========================================

    private void SaveCurrentScene()
    {
        string currentScene =
            SceneManager.GetActiveScene().name;

        // Never save SettingsScene as the previous scene
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
                "No previous scene saved!"
            );

            return;
        }

        // Clear it so an old scene cannot be reused
        PlayerPrefs.DeleteKey(PreviousSceneKey);
        PlayerPrefs.Save();

        Debug.Log(
            "Returning to: " +
            previousScene
        );

        SceneManager.LoadScene(previousScene);
    }
}