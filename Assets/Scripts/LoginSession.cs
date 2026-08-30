using System;
using UnityEngine;

public static class LoginSession
{
    public const string StudentNameKey = "StudentName";
    public const string StudentFirstNameKey = "StudentFirstName";

    public static bool IsLoggedIn = false;

    public static string StudentId = "";
    public static string StudentName = "";

    public static void Login(string studentId, string studentName, string firstName)
    {
        IsLoggedIn = true;
        StudentId = studentId;
        StudentName = studentName;

        string resolvedFirstName = ResolveFirstName(firstName, studentName);
        PlayerPrefs.SetString(StudentNameKey, studentName ?? string.Empty);
        PlayerPrefs.SetString(StudentFirstNameKey, resolvedFirstName);
        PlayerPrefs.Save();
    }

    public static string GetFirstName()
    {
        string savedFirstName = PlayerPrefs.GetString(StudentFirstNameKey, string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(savedFirstName))
            return savedFirstName;

        string savedFullName = PlayerPrefs.GetString(StudentNameKey, StudentName);
        return ResolveFirstName(string.Empty, savedFullName);
    }

    private static string ResolveFirstName(string firstName, string fullName)
    {
        if (!string.IsNullOrWhiteSpace(firstName))
            return firstName.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
            return "Student";

        string[] parts = fullName.Trim().Split(
            new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries
        );

        return parts.Length > 0 ? parts[0] : "Student";
    }

    public static void Logout()
    {
        IsLoggedIn = false;
        StudentId = "";
        StudentName = "";
        PlayerPrefs.DeleteKey(StudentNameKey);
        PlayerPrefs.DeleteKey(StudentFirstNameKey);
        PlayerPrefs.Save();
    }
}
