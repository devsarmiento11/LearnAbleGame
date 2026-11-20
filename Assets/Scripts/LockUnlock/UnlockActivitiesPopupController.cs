using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Controls only the existing subject popups; their artwork and layout stay authored in the scene.
public sealed class UnlockActivitiesPopupController : MonoBehaviour
{
    [SerializeField] private GameObject englishPopup;
    [SerializeField] private GameObject sciencePopup;
    [SerializeField] private GameObject mathPopup;
    [SerializeField] private GameObject confirmationPopup;

    private readonly List<Selectable> blockedControls = new List<Selectable>();
    private GameObject returnSelection;
    private int openSubject;

    private void Awake() { ClosePopups(); }

    private void Start()
    {
        int subject = SceneReturnHistory.Restore(this, 0);
        if (subject == 1) OpenEnglishPopup();
        if (subject == 2) OpenSciencePopup();
        if (subject == 3) OpenMathPopup();
    }

    public void OpenEnglishPopup() { OpenPopup(englishPopup); }
    public void OpenSciencePopup() { OpenPopup(sciencePopup); }
    public void OpenMathPopup() { OpenPopup(mathPopup); }

    private void OpenPopup(GameObject popup)
    {
        if (popup == null) return;
        ClosePopups();
        returnSelection = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;
        popup.transform.SetAsLastSibling();
        popup.SetActive(true);
        openSubject = popup == englishPopup ? 1 : popup == sciencePopup ? 2 : 3;
        SceneReturnHistory.Save(this, openSubject);

        Canvas canvas = popup.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            foreach (Selectable control in canvas.GetComponentsInChildren<Selectable>(true))
            {
                if (confirmationPopup != null && control.transform.IsChildOf(confirmationPopup.transform))
                    continue;

                if (control.interactable && !control.transform.IsChildOf(popup.transform))
                {
                    blockedControls.Add(control);
                    control.interactable = false;
                }
            }
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            foreach (Selectable control in popup.GetComponentsInChildren<Selectable>())
            {
                if (!control.IsInteractable()) continue;
                EventSystem.current.SetSelectedGameObject(control.gameObject);
                break;
            }
        }
    }

    public void ClosePopups()
    {
        if (englishPopup != null) englishPopup.SetActive(false);
        if (sciencePopup != null) sciencePopup.SetActive(false);
        if (mathPopup != null) mathPopup.SetActive(false);
        foreach (Selectable control in blockedControls)
            if (control != null) control.interactable = true;
        blockedControls.Clear();

        if (EventSystem.current != null && returnSelection != null && returnSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(returnSelection);
        returnSelection = null;
        openSubject = 0;
        SceneReturnHistory.Save(this, openSubject);
    }

    private void OnDisable()
    {
        int subject = openSubject;
        ClosePopups();
        SceneReturnHistory.Save(this, subject);
    }
}
