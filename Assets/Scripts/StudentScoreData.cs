using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>Pure score parsing shared by the student score popup and its checks.</summary>
public static class StudentScoreData
{
    private static readonly Regex ScenePattern = new Regex(
        @"^(English|Science|Math)[\s_-]*Game[\s_-]*(\d+)(?:[\s_-]*Level[\s_-]*(\d+))?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string NormalizeActivity(string name)
    {
        name = (name ?? string.Empty).Trim();
        Match match = ScenePattern.Match(name);
        if (!match.Success) return name;
        string subject = match.Groups[1].Value.ToLowerInvariant();
        subject = char.ToUpperInvariant(subject[0]) + subject.Substring(1);
        return subject + "Game" + NormalizeNumber(match.Groups[2].Value)
            + (match.Groups[3].Success ? "Level" + NormalizeNumber(match.Groups[3].Value) : "");
    }

    private static string NormalizeNumber(string number)
    {
        string trimmed = number.TrimStart('0');
        return trimmed.Length == 0 ? "0" : trimmed;
    }

    public static int CompareActivities(string left, string right)
    {
        Match a = ScenePattern.Match(left);
        Match b = ScenePattern.Match(right);
        int subject = SubjectOrder(left).CompareTo(SubjectOrder(right));
        if (subject != 0) return subject;
        if (a.Success && b.Success)
        {
            int game = CompareNumber(a.Groups[2].Value, b.Groups[2].Value);
            if (game != 0) return game;
            int level = CompareNumber(a.Groups[3].Value, b.Groups[3].Value);
            if (level != 0) return level;
        }
        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareNumber(string left, string right)
    {
        left = NormalizeNumber(left);
        right = NormalizeNumber(right);
        int length = left.Length.CompareTo(right.Length);
        return length != 0 ? length : string.CompareOrdinal(left, right);
    }

    private static int SubjectOrder(string activity)
    {
        for (int i = 1; i < ScoreSubjectFilter.Subjects.Length; i++)
            if (ScoreSubjectFilter.Matches(activity, ScoreSubjectFilter.Subjects[i])) return i;
        return ScoreSubjectFilter.Subjects.Length;
    }

    public static bool TryNumber(Dictionary<string, object> data, string field, out double number)
    {
        object value;
        number = 0;
        return data.TryGetValue(field, out value) && value != null && !(value is bool)
            && double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            && !double.IsNaN(number) && !double.IsInfinity(number);
    }

    public static int? ReadPercentage(Dictionary<string, object> data)
    {
        double score, correct, total;
        if (!TryNumber(data, "score", out score))
        {
            if (!TryNumber(data, "correctAnswers", out correct)
                || !TryNumber(data, "totalItems", out total) || total <= 0 || correct < 0 || correct > total)
                return null;
            score = correct / total * 100d;
        }
        return (int)Math.Round(Math.Max(0d, Math.Min(100d, score)), MidpointRounding.AwayFromZero);
    }

    public static long ReadSeconds(Dictionary<string, object> data)
    {
        double seconds;
        if (!TryNumber(data, "timeUsedSeconds", out seconds) || seconds <= 0) return 0;
        return (long)Math.Min(int.MaxValue, Math.Round(seconds, MidpointRounding.AwayFromZero));
    }
}
