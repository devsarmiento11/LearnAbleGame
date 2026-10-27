using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ModuleManager : MonoBehaviour
{
    [Header("Module Panels")]
    public GameObject[] panels;

    [Header("Subject Popups")]
    public GameObject englishPopup;
    public GameObject sciencePopup;
    public GameObject mathPopup;

    private readonly List<Button> blockedButtons = new List<Button>();

    private int currentPanel = 0;

    private void Awake()
    {
        ClosePopups();
    }

    public void OpenEnglishPopup() { OpenPopup(englishPopup); }
    public void OpenSciencePopup() { OpenPopup(sciencePopup); }
    public void OpenMathPopup() { OpenPopup(mathPopup); }

    private void OpenPopup(GameObject popup)
    {
        if (popup == null)
            return;

        ClosePopups();
        popup.transform.SetAsLastSibling();
        popup.SetActive(true);

        // Keep clicks and keyboard navigation inside the active popup.
        Canvas canvas = popup.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            if (button.interactable && !button.transform.IsChildOf(popup.transform))
            {
                blockedButtons.Add(button);
                button.interactable = false;
            }
        }
    }

    public void ClosePopups()
    {
        if (englishPopup != null) englishPopup.SetActive(false);
        if (sciencePopup != null) sciencePopup.SetActive(false);
        if (mathPopup != null) mathPopup.SetActive(false);

        foreach (Button button in blockedButtons)
        {
            if (button != null) button.interactable = true;
        }
        blockedButtons.Clear();
    }

    private void OnDisable()
    {
        ClosePopups();
    }

    void Start()
    {
        currentPanel = 0;
        ShowPanel(currentPanel);
    }

    public void NextPanel()
    {
        if (currentPanel < panels.Length - 1)
        {
            currentPanel++;
            ShowPanel(currentPanel);
        }
        else
        {
            Debug.Log("Already at the last module!");
        }
    }

    public void PreviousPanel()
    {
        if (currentPanel > 0)
        {
            currentPanel--;
            ShowPanel(currentPanel);
        }
        else
        {
            Debug.Log("Already at the first module!");
        }
    }

    private void ShowPanel(int index)
    {
        ClosePopups();
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
            {
                panels[i].SetActive(i == index);
            }
        }
    }
}
