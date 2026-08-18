using UnityEngine;

public class AdminDashboardManager : MonoBehaviour
{
    public GameObject dashboardPanel;
    public GameObject addStudentPanel;
    public GameObject approvalPanel;

    void Start()
    {
        ShowDashboard();
    }

    public void ShowDashboard()
    {
        dashboardPanel.SetActive(true);
        addStudentPanel.SetActive(false);
        approvalPanel.SetActive(false);
    }

    public void ShowAddStudent()
    {
        dashboardPanel.SetActive(false);
        addStudentPanel.SetActive(true);
        approvalPanel.SetActive(false);
    }

    public void ShowApproval()
    {
        dashboardPanel.SetActive(false);
        addStudentPanel.SetActive(false);
        approvalPanel.SetActive(true);
    }
}