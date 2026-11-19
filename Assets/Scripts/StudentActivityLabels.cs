using System.Text.RegularExpressions;

// Display labels only: saved activity identities remain distinct for scores and filters.
public static class StudentActivityLabels
{
    private static readonly string[] EnglishTitles =
    {
        "Let's Trace Vertical Lines",
        "Connecting Vertical Dots",
        "Dividing the Pizza in Half",
        "Missing Parts",
        "Drawing Shapes",
        "Let's Trace Horizontal Lines",
        "Connecting Horizontal Dots",
        "Dividing the Pizza in Half",
        "Missing Parts",
        "Color Shapes",
        "Let's Trace Curved Lines",
        "Trace the Egg",
        "Let's Trace Curved Lines"
    };

    private static readonly string[] ScienceTitles =
    {
        "Drag The Correct Answer",
        "What Does Body Need?",
        "Math It!",
        "Food or Not Food",
        "Good,Bad For Health?",
        "Do You Do These?",
        "My Morning Routine",
        "Caring For Environment",
        "Help The Environment"
    };

    private static readonly string[] MathTitles =
    {
        "Draw Above the Cake",
        "Find The Missing Number",
        "Let's Count!",
        "Comparing Numbers",
        "Let's Compare Numbers!",
        "Match The Coins",
        "Match The Money",
        "How Much?"
    };

    public static string MathTitle(string activityName)
    {
        return SubjectTitle(activityName, "Math", MathTitles);
    }

    public static string EnglishTitle(string activityName)
    {
        return SubjectTitle(activityName, "English", EnglishTitles);
    }

    public static string ScienceTitle(string activityName)
    {
        return SubjectTitle(activityName, "Science", ScienceTitles);
    }

    private static string SubjectTitle(string activityName, string subject, string[] titles)
    {
        Match match = Regex.Match(activityName ?? string.Empty,
            @"^\s*" + subject + @"[\s_-]*Game[\s_-]*0*(\d+)(?:[\s_-]*Level[\s_-]*\d+)?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int game)
            || game < 1 || game > titles.Length) return null;
        return titles[game - 1];
    }

    public static string Level(string activityName)
    {
        Match match = Regex.Match(activityName ?? string.Empty,
            @"Level[\s_-]*0*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : "-";
    }
}
