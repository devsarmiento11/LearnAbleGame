using UnityEngine;
using UnityEngine.SceneManagement;

public class TryAgain : MonoBehaviour
{
    public void TryAgainLevel()
    {
        string previousScene =
            PlayerPrefs.GetString("PreviousGameScene", "");

        if (!string.IsNullOrEmpty(previousScene))
        {
            Debug.Log("Trying again: " + previousScene);

            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.LogWarning("No previous game scene saved!");
        }
    }
}