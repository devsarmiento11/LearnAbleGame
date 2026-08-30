using UnityEngine;
using UnityEngine.UI;

public class LanguageToggle : MonoBehaviour
{
    public Toggle languageToggle;

    private LanguageManager languageManager;

    void Start()
    {
        if (languageToggle == null)
            languageToggle = GetComponent<Toggle>();

        if (languageToggle == null)
        {
            Debug.LogError("LanguageToggle: Toggle component is not assigned.");
            enabled = false;
            return;
        }

        languageManager = LanguageManager.EnsureInstance();

        // OFF = English
        // ON = Tagalog
        languageToggle.SetIsOnWithoutNotify(languageManager.IsTagalog());

        languageToggle.onValueChanged.AddListener(ChangeLanguage);
    }

    private void OnDestroy()
    {
        if (languageToggle != null)
            languageToggle.onValueChanged.RemoveListener(ChangeLanguage);
    }

    public void ChangeLanguage(bool isTagalog)
    {
        if (languageManager == null)
            languageManager = LanguageManager.EnsureInstance();

        if (isTagalog)
            languageManager.SetTagalog();
        else
            languageManager.SetEnglish();
    }

    public static bool IsTagalog()
    {
        return LanguageManager.SavedLanguageIsTagalog();
    }

    public static bool IsEnglish()
    {
        return !IsTagalog();
    }
}
