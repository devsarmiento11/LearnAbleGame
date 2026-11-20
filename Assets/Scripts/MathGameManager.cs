using UnityEngine;

public class MathGameManager : MonoBehaviour
{
    public GameObject[] levels;

    private int currentLevel = 0;

    void Start()
    {
        currentLevel = PassedLevelNavigation.ConsumePreview(
            gameObject.scene.name, levels.Length, SceneReturnHistory.Restore(this, 0));
        ShowLevel(currentLevel);
    }

    public void NextLevel()
    {
        if (currentLevel < levels.Length - 1)
        {
            currentLevel++;
            ShowLevel(currentLevel);
        }
        else
        {
            Debug.Log("Last level reached!");
        }
    }

    public void PreviousLevel()
    {
        if (currentLevel > 0)
        {
            currentLevel--;
            ShowLevel(currentLevel);
        }
        else
        {
            Debug.Log("Already at first level!");
        }
    }

    private void ShowLevel(int index)
    {
        SceneReturnHistory.Save(this, index);
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null)
                levels[i].SetActive(i == index);
        }
    }
}
