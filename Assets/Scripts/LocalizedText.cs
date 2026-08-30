using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    [Header("TEXT COMPONENT")]
    public TMP_Text textComponent;

    [Header("ENGLISH")]

    [TextArea(2, 6)]
    public string englishText;

    [Header("TAGALOG")]

    [TextArea(2, 6)]
    public string tagalogText;


    private void Awake()
    {
        if (textComponent == null)
        {
            textComponent =
                GetComponent<TMP_Text>();
        }
    }


    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged +=
            UpdateLanguage;

        UpdateLanguage();
    }


    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -=
            UpdateLanguage;
    }


    public void UpdateLanguage()
    {
        if (textComponent == null)
            return;

        // If the manager already exists
        if (LanguageManager.Instance != null)
        {
            if (LanguageManager.Instance.IsTagalog())
            {
                textComponent.text = tagalogText;
            }
            else
            {
                textComponent.text = englishText;
            }

            return;
        }

        // Manager does not exist yet.
        // Read saved language directly.
        if (LanguageManager.SavedLanguageIsTagalog())
        {
            textComponent.text = tagalogText;
        }
        else
        {
            textComponent.text = englishText;
        }
    }
}