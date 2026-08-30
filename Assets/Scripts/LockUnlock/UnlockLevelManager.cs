using System;
using UnityEngine;
using UnityEngine.UI;

public class UnlockLevelManager : MonoBehaviour
{
    [Serializable]
    public class LevelButtonData
    {
        [Header("LEVEL")]
        public int levelNumber = 1;

        [Header("BUTTON IMAGE")]
        public Image buttonImage;

        [Header("SPRITES")]
        public Sprite lockedSprite;
        public Sprite unlockedSprite;
    }


    [Header("ACTIVITY ID")]
    [Tooltip("Must be exactly the same ID used in the game scene.")]
    public string activityId = "English_TraceVerticalLines";


    [Header("CONFIRMATION POPUP")]
    public GameObject confirmationPopup;


    [Header("LEVEL BUTTONS")]
    public LevelButtonData[] levelButtons;


    private int pendingLevel = -1;


    // ==========================================
    // START
    // ==========================================

    void Start()
    {
        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }

        RefreshLevelButtons();
    }


    // ==========================================
    // CLICK LOCKED LEVEL
    // ==========================================

    public void RequestUnlock(int levelNumber)
    {
        // Already unlocked
        if (LevelUnlockStorage.IsUnlocked(
                activityId,
                levelNumber))
        {
            Debug.Log(
                "Level " +
                levelNumber +
                " is already unlocked."
            );

            return;
        }

        pendingLevel = levelNumber;

        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(true);
        }

        Debug.Log(
            "Unlock requested for Level " +
            levelNumber
        );
    }


    // ==========================================
    // YES BUTTON
    // ==========================================

    public void ConfirmUnlock()
    {
        if (pendingLevel < 1)
            return;

        LevelUnlockStorage.UnlockLevel(
            activityId,
            pendingLevel
        );

        RefreshLevelButtons();

        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }

        pendingLevel = -1;
    }


    // ==========================================
    // CANCEL BUTTON
    // ==========================================

    public void CancelUnlock()
    {
        pendingLevel = -1;

        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }
    }


    // ==========================================
    // LOCK LEVELS 2 TO 5
    // ==========================================

    public void LockLevels2To5()
    {
        LevelUnlockStorage.LockLevel(
            activityId,
            2
        );

        LevelUnlockStorage.LockLevel(
            activityId,
            3
        );

        LevelUnlockStorage.LockLevel(
            activityId,
            4
        );

        LevelUnlockStorage.LockLevel(
            activityId,
            5
        );

        // Update the images immediately
        RefreshLevelButtons();

        // Close confirmation popup if open
        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }

        pendingLevel = -1;

        Debug.Log(
            activityId +
            ": Levels 2, 3, 4 and 5 have been locked."
        );
    }


    // ==========================================
    // UPDATE LEVEL BUTTON IMAGES
    // ==========================================

    public void RefreshLevelButtons()
    {
        foreach (
            LevelButtonData levelData
            in levelButtons)
        {
            if (levelData == null)
                continue;

            if (levelData.buttonImage == null)
                continue;

            bool unlocked =
                LevelUnlockStorage.IsUnlocked(
                    activityId,
                    levelData.levelNumber
                );

            if (unlocked)
            {
                if (levelData.unlockedSprite != null)
                {
                    levelData.buttonImage.sprite =
                        levelData.unlockedSprite;
                }
            }
            else
            {
                if (levelData.lockedSprite != null)
                {
                    levelData.buttonImage.sprite =
                        levelData.lockedSprite;
                }
            }
        }
    }
}