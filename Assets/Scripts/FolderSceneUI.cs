using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation-only scene preview. No Firebase requests or module mutations.
public sealed class FolderSceneUI : MonoBehaviour
{
    [Tooltip("Design preview rows only. These are not uploaded modules.")]
    public FolderModuleRowUI[] previewRows;
    public ScrollRect moduleScroll;
    public TMP_InputField search;
    public TMP_Dropdown gradeFilter;
    public GameObject editPopup, deletePopup, archivePopup, modalBlocker;
    public TMP_InputField editTitle;
    public TMP_Dropdown editGrade;

    void Awake()
    {
        ClosePopups();
        foreach (var row in previewRows)
        {
            var current = row;
            row.editButton.onClick.AddListener(() => {
                editTitle.SetTextWithoutNotify(current.moduleTitle.text);
                int.TryParse(current.gradeLabel.text.Replace("Grade ", ""), out int value);
                editGrade.SetValueWithoutNotify(Mathf.Max(0, value - 1));
                editGrade.RefreshShownValue();
                Show(editPopup);
            });
            row.deleteButton.onClick.AddListener(() => Show(deletePopup));
            row.archiveButton.onClick.AddListener(() => Show(archivePopup));
        }
        search.onValueChanged.AddListener(_ => FilterRows());
        gradeFilter.onValueChanged.AddListener(_ => FilterRows());
        FilterRows();
    }

    public void Show(GameObject popup)
    {
        ClosePopups();
        modalBlocker.SetActive(true); modalBlocker.transform.SetAsLastSibling();
        popup.SetActive(true); popup.transform.SetAsLastSibling();
    }

    public void ClosePopups()
    {
        editPopup.SetActive(false); deletePopup.SetActive(false); archivePopup.SetActive(false);
        modalBlocker.SetActive(false);
    }

    public void FilterRows()
    {
        foreach (var row in previewRows)
            row.gameObject.SetActive(row.moduleTitle.text.IndexOf(search.text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0
                && (gradeFilter.value == 0 || row.gradeLabel.text == "Grade " + gradeFilter.value));
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(moduleScroll.content);
        moduleScroll.StopMovement(); moduleScroll.verticalNormalizedPosition = 1;
    }
}
