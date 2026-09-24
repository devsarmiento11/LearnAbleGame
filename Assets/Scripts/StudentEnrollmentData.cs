using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Firebase.Firestore;

public enum StudentInfoSection { None = -1, Personal = 0, Family = 1, Account = 2 }

// Selection is session-only; it never replaces the signed-in teacher's identity.
public static class StudentEnrollmentSelection
{
    public static string StudentId;
    public static StudentInfoSection Section = StudentInfoSection.None;
    public static bool Editing;
    public static void Clear() { StudentId = null; Section = StudentInfoSection.None; Editing = false; }
}

[Serializable]
public sealed class StudentEnrollmentData
{
    public string id, firstName, middleName, lastName, grade, condition, birthday, gender;
    public string address, motherName, fatherName, guardianName, contactNumber, relationship;
    public int age;

    public string FullName => string.Join(" ", new[] { firstName, middleName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public static string Text(IDictionary<string, object> data, string key) =>
        data.TryGetValue(key, out var value) && value != null ? value.ToString().Trim() : "";

    public static StudentEnrollmentData FromDocument(DocumentSnapshot document) => FromMap(document.Id, document.ToDictionary());
    public static StudentEnrollmentData FromMap(string id, IDictionary<string, object> data)
    {
        var result = new StudentEnrollmentData {
            id = id, firstName = Text(data, "firstName"), middleName = Text(data, "middleName"), lastName = Text(data, "lastName"),
            grade = NormalizeGrade(Text(data, "grade")), condition = Text(data, "condition"), gender = Text(data, "gender"),
            address = Text(data, "address"), motherName = FamilyName(data, "mother"), fatherName = FamilyName(data, "father"),
            guardianName = FamilyName(data, "guardian"), contactNumber = Text(data, "contactNumber"), relationship = Text(data, "relationship")
        };
        if (data.TryGetValue("birthday", out var value)) {
            DateTime date;
            if (value is Timestamp timestamp) date = timestamp.ToDateTime();
            else if (!DateTime.TryParse(value?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) date = DateTime.MinValue;
            if (date != DateTime.MinValue) { result.birthday = date.ToString("yyyy-MM-dd"); result.age = Age(date); }
        }
        return result;
    }
    static string FamilyName(IDictionary<string, object> data, string prefix)
    {
        // The admin app stores split names. Prefer these so subsequent admin edits stay accurate.
        string split = string.Join(" ", new[] { Text(data, prefix + "FirstName"), Text(data, prefix + "LastName") }.Where(s => s.Length > 0));
        return split.Length > 0 ? split : Text(data, prefix + "Name");
    }
    public static string NormalizeGrade(string value)
    {
        var match = Regex.Match(value ?? "", @"^(?:Grade\s*)?([1-6])$", RegexOptions.IgnoreCase);
        return match.Success ? "Grade " + match.Groups[1].Value : value ?? "";
    }
    public static bool TryBirthday(string text, out DateTime birthday) => DateTime.TryParseExact(
        text?.Trim(), new[] { "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out birthday)
        && birthday.Date <= DateTime.Today && birthday.Year >= 1900;
    public static int Age(DateTime birthday)
    {
        int years = DateTime.Today.Year - birthday.Year;
        return birthday.Date > DateTime.Today.AddYears(-years) ? years - 1 : years;
    }
}
