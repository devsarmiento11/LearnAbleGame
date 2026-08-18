using UnityEngine;

public class EnglishGameManager : MonoBehaviour
{
    public GameObject[] levels;

    private int currentLevel = 0;

    void Start()
    {
        currentLevel = 0;
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
            Debug.Log("Already at the first level!");
        }
    }

    void ShowLevel(int index)
    {
        foreach (GameObject level in levels)
        {
            level.SetActive(false);
        }

        levels[index].SetActive(true);

        Debug.Log("Current Level: " + (index + 1));
    }
}