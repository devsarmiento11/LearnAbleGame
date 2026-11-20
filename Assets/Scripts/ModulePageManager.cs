using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ModulePageManager : MonoBehaviour
{
    [Header("MODULE PAGES")]
    public GameObject[] pages;

    [Header("BUTTONS")]
    public Button nextButton;
    public Button previousButton;
    [SerializeField] private Button skipButton;

    [Header("PROGRESS BAR")]
    public Slider progressBar;

    [Header("NEXT SCENE")]
    public string nextSceneName;

    private int currentPage = 0;
    private string completionKey;

    private void Awake()
    {
        if (skipButton == null)
            return;

        completionKey = $"LearnAble_ModuleCompleted_{gameObject.scene.name}_{LearningDataStore.CurrentUserId}";
        skipButton.gameObject.SetActive(PlayerPrefs.GetInt(completionKey) == 1);
    }

    void Start()
    {
        // Progress bar setup
        if (progressBar != null)
        {
            progressBar.minValue = 1;
            progressBar.maxValue = pages.Length;
            progressBar.interactable = false;
        }

        ShowPage(SceneReturnHistory.Restore(this, 0));
    }

    // ==========================================
    // SHOW PAGE
    // ==========================================

    private void ShowPage(int pageIndex)
    {
        if (pages == null || pages.Length == 0)
            return;

        currentPage = Mathf.Clamp(
            pageIndex,
            0,
            pages.Length - 1
        );
        SceneReturnHistory.Save(this, currentPage);

        // Hide all pages
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == currentPage);
            }
        }

        // Update progress
        if (progressBar != null)
        {
            progressBar.value = currentPage + 1;
        }

        // Previous button is hidden on Page 1
        if (previousButton != null)
        {
            previousButton.gameObject.SetActive(
                currentPage > 0
            );
        }

        Debug.Log(
            "Module Page: " +
            (currentPage + 1) +
            " / " +
            pages.Length
        );
    }

    // ==========================================
    // NEXT PAGE
    // ==========================================

    public void NextPage()
    {
        if (pages == null || pages.Length == 0)
            return;

        // Still have another page
        if (currentPage < pages.Length - 1)
        {
            ShowPage(currentPage + 1);
        }
        else
        {
            if (skipButton != null && PlayerPrefs.GetInt(completionKey) != 1)
            {
                PlayerPrefs.SetInt(completionKey, 1);
                PlayerPrefs.Save();
            }

            LoadNextScene();
        }
    }

    public void SkipModule()
    {
        if (skipButton != null && PlayerPrefs.GetInt(completionKey) == 1)
            LoadNextScene();
    }

    // ==========================================
    // PREVIOUS PAGE
    // ==========================================

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            ShowPage(currentPage - 1);
        }
    }

    // ==========================================
    // LOAD GAME SCENE
    // ==========================================

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning(
                "ModulePageManager: Next Scene Name is empty!"
            );

            return;
        }

        Debug.Log(
            "Module finished! Loading: " +
            nextSceneName
        );

        SceneManager.LoadScene(nextSceneName);
    }
}
