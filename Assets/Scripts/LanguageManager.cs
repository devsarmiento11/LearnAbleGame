using System;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public enum Language
    {
        English,
        Tagalog
    }

    public static LanguageManager Instance;

    public static event Action OnLanguageChanged;

    private const string LanguageKey = "LearnAbleLanguage";

    public Language CurrentLanguage { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateManagerBeforeFirstScene()
    {
        EnsureInstance();
    }

    public static LanguageManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        LanguageManager existing = FindAnyObjectByType<LanguageManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject("LanguageManager");
        return managerObject.AddComponent<LanguageManager>();
    }


    private void Awake()
    {
        // Prevent duplicate LanguageManagers
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Keep this manager when changing scenes
        DontDestroyOnLoad(gameObject);

        LoadSavedLanguage();
    }


    // ==========================================
    // LOAD SAVED LANGUAGE
    // ==========================================

    private void LoadSavedLanguage()
    {
        // Migrate values written by the older LanguageToggle implementation.
        if (!PlayerPrefs.HasKey(LanguageKey) && PlayerPrefs.HasKey("Language"))
        {
            bool legacyTagalog =
                PlayerPrefs.GetString("Language", "English") == "Tagalog";

            PlayerPrefs.SetInt(LanguageKey, legacyTagalog ? 1 : 0);
            PlayerPrefs.DeleteKey("Language");
            PlayerPrefs.Save();
        }

        int savedLanguage =
            PlayerPrefs.GetInt(LanguageKey, 0);

        if (savedLanguage == 1)
        {
            CurrentLanguage = Language.Tagalog;
        }
        else
        {
            CurrentLanguage = Language.English;
        }
    }


    // ==========================================
    // ENGLISH
    // ==========================================

    public void SetEnglish()
    {
        SetLanguage(Language.English);
    }


    // ==========================================
    // TAGALOG
    // ==========================================

    public void SetTagalog()
    {
        SetLanguage(Language.Tagalog);
    }


    // ==========================================
    // SWITCH LANGUAGE
    // ==========================================

    public void ToggleLanguage()
    {
        if (CurrentLanguage == Language.English)
        {
            SetLanguage(Language.Tagalog);
        }
        else
        {
            SetLanguage(Language.English);
        }
    }


    // ==========================================
    // SAVE LANGUAGE
    // ==========================================

    private void SetLanguage(Language language)
    {
        CurrentLanguage = language;

        if (language == Language.Tagalog)
        {
            PlayerPrefs.SetInt(LanguageKey, 1);
        }
        else
        {
            PlayerPrefs.SetInt(LanguageKey, 0);
        }

        PlayerPrefs.Save();

        Debug.Log(
            "Language changed to: " +
            CurrentLanguage
        );

        // Tell all text objects to update
        OnLanguageChanged?.Invoke();
    }


    // ==========================================
    // CHECK LANGUAGE
    // ==========================================

    public bool IsEnglish()
    {
        return CurrentLanguage == Language.English;
    }

    public bool IsTagalog()
    {
        return CurrentLanguage == Language.Tagalog;
    }


    // ==========================================
    // STATIC CHECK
    // Works even before SettingsScene is opened
    // ==========================================

    public static bool SavedLanguageIsTagalog()
    {
        return PlayerPrefs.GetInt(
            LanguageKey,
            0
        ) == 1;
    }
}
