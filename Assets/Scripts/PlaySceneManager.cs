using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaySceneManager : MonoBehaviour
{
    [Header("GREETING - OPTIONAL")]
    public TMP_Text greetingText;

    [Header("CHARACTER DISPLAY")]
    public Image characterImage;

    [Header("CHARACTER SPRITES")]
    public Sprite jimbeiSprite;
    public Sprite boaSprite;
    public Sprite shanksSprite;

    private const string SelectedCharacterKey = "SelectedCharacter";

    void Start()
    {
        UpdateScene();
    }

    // ==========================================
    // UPDATE EVERYTHING
    // ==========================================

    private void UpdateScene()
    {
        string firstName = LoginSession.GetFirstName();

        string characterName = PlayerPrefs.GetString(
            SelectedCharacterKey,
            "Jimbei"
        );

        // If this scene has a greeting text,
        // update it.
        if (greetingText != null)
        {
            UpdateGreeting(
                firstName,
                characterName
            );
        }

        // If this scene has a character image,
        // update it.
        if (characterImage != null)
        {
            UpdateCharacterImage(
                characterName
            );
        }
    }

    // ==========================================
    // GREETING
    // ==========================================

    private void UpdateGreeting(
        string firstName,
        string characterName)
    {
        greetingText.text =
            "Kumusta, " + firstName + "!\n" +
            "Ako si " + characterName + ".\n" +
            "Maligayang pagdating sa LearnAble!";
    }

    // ==========================================
    // CHARACTER IMAGE
    // ==========================================

    private void UpdateCharacterImage(
        string characterName)
    {
        switch (characterName.ToLower())
        {
            case "jimbei":
                characterImage.sprite =
                    jimbeiSprite;
                break;

            case "boa":
                characterImage.sprite =
                    boaSprite;
                break;

            case "shanks":
                characterImage.sprite =
                    shanksSprite;
                break;

            default:
                Debug.LogWarning(
                    "Unknown character: " +
                    characterName +
                    ". Using Jimbei."
                );

                characterImage.sprite =
                    jimbeiSprite;
                break;
        }

        characterImage.preserveAspect = true;
    }

    // ==========================================
    // OPTIONAL MANUAL REFRESH
    // ==========================================

    public void RefreshScene()
    {
        UpdateScene();
    }
}