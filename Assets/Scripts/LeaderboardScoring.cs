using System;
using System.Globalization;
using System.Text.RegularExpressions;

public static class LeaderboardScoring
{
    private static readonly Regex ActivityName = new Regex(
        @"^\s*(English|Science|Math(?:ematics)?)[\s_-]*Game[\s_-]*0*([1-9]\d*)(?:[\s_-]*Level[\s_-]*0*[1-9]\d*)?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string ActivityKey(string name)
    {
        Match match = ActivityName.Match(name ?? "");
        if (!match.Success) return null;
        string subject = match.Groups[1].Value.ToLowerInvariant();
        if (subject == "mathematics") subject = "math";
        return subject + ":" + match.Groups[2].Value;
    }

    public static bool TryPoints(object value, out double points)
    {
        points = 0;
        if (!(value is long) && !(value is int) && !(value is double) && !(value is float))
            return false;
        double score = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (double.IsNaN(score) || double.IsInfinity(score)) return false;
        points = Math.Max(0, Math.Min(100, score));
        return true;
    }
}
