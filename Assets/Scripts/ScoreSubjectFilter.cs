using System;

public static class ScoreSubjectFilter
{
    public static readonly string[] Subjects = { "All Subjects", "English", "Science", "Math" };

    public static bool Matches(string activityName, string subject)
    {
        if (subject == "All Subjects") return true;
        if (string.IsNullOrWhiteSpace(activityName) || string.IsNullOrWhiteSpace(subject)) return false;
        string name = activityName.Trim();
        if (!name.StartsWith(subject, StringComparison.OrdinalIgnoreCase)) return false;
        // Saved activity names are scene names, e.g. EnglishGame1Level5.
        string suffix = name.Substring(subject.Length);
        return suffix.Length == 0 || char.IsWhiteSpace(suffix[0]) || suffix[0] == '_' || suffix[0] == '-' ||
            suffix.StartsWith("Game", StringComparison.OrdinalIgnoreCase) ||
            suffix.StartsWith("Level", StringComparison.OrdinalIgnoreCase);
    }
}

