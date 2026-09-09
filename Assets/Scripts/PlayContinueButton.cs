using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Remembers whether each school account has started on this device.</summary>
public class PlayContinueButton : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string destinationScene = "ModuleActivitiesScene";
    private bool opening;

    private void OnEnable()
    {
        opening = false;
        RefreshLabel();
    }

    public void RefreshLabel()
    {
        if (label != null)
            label.text = AccountStartState.HasStarted(LearningDataStore.CurrentUserId) ? "Continue" : "Start";
    }

    public void StartOrContinue()
    {
        if (opening || sceneLoader == null) return;
        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError("Play destination is unavailable: " + destinationScene);
            return;
        }
        // Save only on the Start/Continue action, not when visiting Profile or Settings.
        AccountStartState.MarkStarted(LearningDataStore.CurrentUserId);
        opening = true;
        RefreshLabel();
        sceneLoader.LoadScene(destinationScene);
    }
}
