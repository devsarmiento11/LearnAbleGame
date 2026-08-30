using UnityEngine;

public class CharacterSelectionManager : MonoBehaviour
{
    private const string SelectedCharacterKey = "SelectedCharacter";

    // ==========================================
    // SELECT JIMBEI
    // ==========================================

    public void SelectJimbei()
    {
        SaveCharacter("Jimbei");
    }

    // ==========================================
    // SELECT BOA
    // ==========================================

    public void SelectBoa()
    {
        SaveCharacter("Boa");
    }

    // ==========================================
    // SELECT SHANKS
    // ==========================================

    public void SelectShanks()
    {
        SaveCharacter("Shanks");
    }

    // ==========================================
    // SAVE CHARACTER
    // ==========================================

    private void SaveCharacter(string characterName)
    {
        PlayerPrefs.SetString(
            SelectedCharacterKey,
            characterName
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Selected Character: " +
            characterName
        );
    }

    // ==========================================
    // GET SELECTED CHARACTER
    // ==========================================

    public static string GetSelectedCharacter()
    {
        return PlayerPrefs.GetString(
            SelectedCharacterKey,
            "Jimbei"
        );
    }
}
