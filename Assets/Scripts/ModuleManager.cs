using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    [Header("Module Panels")]
    public GameObject englishPanel;
    public GameObject sciencePanel;
    public GameObject mathPanel;

    private GameObject[] panels;
    private int currentPanel = 0;

    void Start()
    {
        panels = new GameObject[]
        {
            englishPanel,
            sciencePanel,
            mathPanel
        };

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
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].SetActive(i == index);
        }
    }
}