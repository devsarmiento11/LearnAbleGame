using System;
using System.Collections.Generic;
using System.Globalization;

// Shared calculations for the teacher's report and the student's GradesPanel.
public sealed class AcademicGradeReport
{
    public static readonly string[] Subjects = { "English", "Math", "Science" };
    public static readonly string[] Quarters = { "1ST", "2ND", "3RD", "4TH" };
    private readonly Dictionary<string, object> grades;
    public readonly string TeacherRemark;

    public AcademicGradeReport(Dictionary<string, object> profile)
    {
        grades = profile.TryGetValue("academicGrades", out object value)
            && value is Dictionary<string, object> saved
            ? saved : new Dictionary<string, object>();
        TeacherRemark = Text(profile, "teacherRemark");
    }

    public static string Key(string subject, string quarter)
    {
        return subject.ToUpperInvariant() + "_" + quarter.ToUpperInvariant();
    }

    public decimal? Grade(string subject, string quarter)
    {
        if (grades.TryGetValue(Key(subject, quarter), out object value) && value != null
            && decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                NumberStyles.Number, CultureInfo.InvariantCulture, out decimal grade)
            && grade >= 0 && grade <= 100)
            return grade;
        return null;
    }

    // Missing quarters are not zeroes. Show a running average of entered quarters.
    public decimal? SubjectAverage(string subject)
    {
        decimal sum = 0;
        int count = 0;
        foreach (string quarter in Quarters)
        {
            decimal? grade = Grade(subject, quarter);
            if (!grade.HasValue) continue;
            sum += grade.Value;
            count++;
        }
        return count == 0 ? (decimal?)null : sum / count;
    }

    public decimal? GeneralAverage()
    {
        decimal sum = 0;
        int count = 0;
        foreach (string subject in Subjects)
        {
            decimal? average = SubjectAverage(subject);
            if (!average.HasValue) continue;
            sum += average.Value;
            count++;
        }
        return count == 0 ? (decimal?)null : sum / count;
    }

    public static string Remark(decimal? average)
    {
        if (!average.HasValue) return string.Empty;
        if (average.Value < 75) return "Failed";
        if (average.Value < 85) return "Good";
        if (average.Value < 90) return "Very Good";
        return "Excellent";
    }

    public static string Format(decimal? value)
    {
        return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
    }

    public static string StudentName(Dictionary<string, object> profile, string fallback)
    {
        string first = Text(profile, "firstName");
        string last = Text(profile, "lastName");
        if (first.Length > 0 || last.Length > 0)
            return (first + " " + last).Trim();
        string name = Text(profile, "name");
        if (name.Length == 0) name = fallback ?? string.Empty;
        string middle = Text(profile, "middleName");
        if (middle.Length > 0)
            name = (" " + name + " ").Replace(" " + middle + " ", " ").Trim();
        return name;
    }

    public static string Text(Dictionary<string, object> data, string field)
    {
        return data.TryGetValue(field, out object value) && value != null
            ? value.ToString().Trim() : string.Empty;
    }
}
