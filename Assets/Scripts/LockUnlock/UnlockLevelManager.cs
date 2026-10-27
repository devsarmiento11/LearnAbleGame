using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private readonly List<Selectable> blockedControls = new List<Selectable>();
    private GameObject returnSelection;

    private void Awake() { CancelUnlock(); }
    private void OnEnable() { RefreshLevelButtons(); }
    private void OnDisable() { CancelUnlock(); }

    public void RequestUnlock(int levelNumber)
    {
        // Reject invalid requests and keep the selected level fixed while confirming.
        if (pendingLevel != -1 || confirmationPopup == null ||
            string.IsNullOrWhiteSpace(activityId) || !HasLevel(levelNumber) ||
            LevelUnlockStorage.IsUnlocked(activityId, levelNumber)) return;

        pendingLevel = levelNumber;
        returnSelection = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;
        confirmationPopup.transform.SetAsLastSibling();
        confirmationPopup.SetActive(true);

        Canvas canvas = confirmationPopup.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            foreach (Selectable control in canvas.GetComponentsInChildren<Selectable>(true))
            {
                if (control.interactable && !control.transform.IsChildOf(confirmationPopup.transform))
                {
                    blockedControls.Add(control);
                    control.interactable = false;
                }
            }
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            // Prefer Cancel so an accidental keyboard submit cannot approve an unlock.
            Selectable cancel = null;
            foreach (Selectable control in confirmationPopup.GetComponentsInChildren<Selectable>())
                if (control.IsInteractable() && control.name.IndexOf("Cancel", StringComparison.OrdinalIgnoreCase) >= 0)
                { cancel = control; break; }
            if (cancel != null) EventSystem.current.SetSelectedGameObject(cancel.gameObject);
        }
    }

    public void ConfirmUnlock()
    {
        if (pendingLevel < 2 || !HasLevel(pendingLevel)) return;
        LevelUnlockStorage.UnlockLevel(activityId, pendingLevel);
        CancelUnlock();
        RefreshLevelButtons();
    }

    public void CancelUnlock()
    {
        pendingLevel = -1;
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        foreach (Selectable control in blockedControls)
            if (control != null) control.interactable = true;
        blockedControls.Clear();
        if (EventSystem.current != null && returnSelection != null && returnSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(returnSelection);
        returnSelection = null;
    }

    public void LockLevels2To5()
    {
        CancelUnlock();
        if (string.IsNullOrWhiteSpace(activityId)) return;
        for (int level = 2; level <= 5; level++)
            LevelUnlockStorage.LockLevel(activityId, level);
        RefreshLevelButtons();
    }

    private bool HasLevel(int levelNumber)
    {
        if (levelNumber < 2 || levelNumber > 5 || levelButtons == null) return false;
        foreach (LevelButtonData level in levelButtons)
            if (level != null && level.levelNumber == levelNumber) return true;
        return false;
    }

    public void RefreshLevelButtons()
    {
        if (levelButtons == null) return;
        foreach (LevelButtonData level in levelButtons)
        {
            if (level == null || level.buttonImage == null) continue;
            Sprite sprite = LevelUnlockStorage.IsUnlocked(activityId, level.levelNumber)
                ? level.unlockedSprite : level.lockedSprite;
            if (sprite != null) level.buttonImage.sprite = sprite;
        }
    }
}