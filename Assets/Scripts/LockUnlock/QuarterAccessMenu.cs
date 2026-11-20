using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuarterAccessMenu : MonoBehaviour
{
    [Serializable]
    private class QuarterButton
    {
        public string subject;
        public int quarter;
        public string destination;
        public GameObject lockOverlay;
        public Image image;
        public Sprite lockedSprite;
        public Sprite unlockedSprite;

        public string StorageId => "Quarter_" + subject;
        public bool IsUnlocked => LevelUnlockStorage.IsUnlocked(StorageId, quarter);
    }

    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private QuarterButton[] quarters;
    [SerializeField] private GameObject confirmationPopup;

    private readonly List<Selectable> blockedControls = new List<Selectable>();
    private QuarterButton pendingQuarter;

    private bool CanUnlock => LoginSession.IsLoggedIn && LoginSession.Role == "teacher";

    private void Awake() => CancelUnlock();
    private void OnEnable() => RefreshButtons();
    private void OnDisable() => CancelUnlock();

    public void SelectQuarter(string destination)
    {
        if (pendingQuarter != null || (confirmationPopup != null && !CanUnlock))
            return;

        QuarterButton quarter = Array.Find(quarters, entry => entry.destination == destination);
        if (quarter == null)
            return;

        if (quarter.IsUnlocked)
        {
            sceneLoader.LoadScene(quarter.destination);
            return;
        }

        if (confirmationPopup == null)
            return;

        pendingQuarter = quarter;
        confirmationPopup.transform.SetAsLastSibling();
        confirmationPopup.SetActive(true);

        Canvas canvas = confirmationPopup.GetComponentInParent<Canvas>();
        foreach (Selectable control in canvas.GetComponentsInChildren<Selectable>())
        {
            if (!control.interactable || control.transform.IsChildOf(confirmationPopup.transform))
                continue;

            blockedControls.Add(control);
            control.interactable = false;
        }
    }

    public void ConfirmUnlock()
    {
        if (pendingQuarter != null && CanUnlock)
            LevelUnlockStorage.UnlockLevel(pendingQuarter.StorageId, pendingQuarter.quarter);

        CancelUnlock();
        RefreshButtons();
    }

    public void LockSecondAndThirdQuarters(string subject)
    {
        if (confirmationPopup == null || !CanUnlock || pendingQuarter != null)
            return;

        foreach (QuarterButton quarter in quarters)
            if (quarter.subject == subject && (quarter.quarter == 2 || quarter.quarter == 3))
                LevelUnlockStorage.LockLevel(quarter.StorageId, quarter.quarter);

        RefreshButtons();
    }

    public void CancelUnlock()
    {
        pendingQuarter = null;
        if (confirmationPopup != null)
            confirmationPopup.SetActive(false);

        foreach (Selectable control in blockedControls)
            if (control != null)
                control.interactable = true;

        blockedControls.Clear();
    }

    private void RefreshButtons()
    {
        foreach (QuarterButton quarter in quarters)
        {
            bool unlocked = quarter.IsUnlocked;
            if (quarter.lockOverlay != null)
                quarter.lockOverlay.SetActive(!unlocked);
            else if (quarter.image != null)
                quarter.image.sprite = unlocked ? quarter.unlockedSprite : quarter.lockedSprite;
        }
    }
}
