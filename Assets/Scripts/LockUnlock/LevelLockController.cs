using UnityEngine;
using UnityEngine.UI;

public class LevelLockController : MonoBehaviour
{
    [Header("ACTIVITY")]
    public string activityId =
        "English_TraceVerticalLines";

    [Header("LEVEL")]
    public int levelNumber = 2;

    [Header("LOCK IMAGE")]
    public GameObject lockObject;

    [Header("BUTTONS THAT MUST BE DISABLED WHEN LOCKED")]
    public Button[] buttonsToLock;


    // ==========================================
    // START
    // ==========================================

    void Start()
    {
        RefreshLockState();
    }


    // Also refresh if object becomes enabled again
    void OnEnable()
    {
        RefreshLockState();
    }


    // ==========================================
    // CHECK LOCK
    // ==========================================

    public void RefreshLockState()
    {
        bool unlocked =
            LevelUnlockStorage.IsUnlocked(
                activityId,
                levelNumber
            );

        // Lock image
        if (lockObject != null)
        {
            lockObject.SetActive(!unlocked);
        }


        // Actual buttons
        if (buttonsToLock != null)
        {
            foreach (
                Button button
                in buttonsToLock)
            {
                if (button != null)
                {
                    button.interactable =
                        unlocked;
                }
            }
        }


        Debug.Log(
            activityId +
            " Level " +
            levelNumber +
            " | Unlocked: " +
            unlocked
        );
    }
}