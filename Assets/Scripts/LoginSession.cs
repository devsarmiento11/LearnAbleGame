using System;
using UnityEngine;

public static class LoginSession
{
    public const string StudentNameKey = "StudentName";
    public const string StudentFirstNameKey = "StudentFirstName";

    public static bool IsLoggedIn = false;

    public static string StudentId = "";
    public static string StudentName = "";
    public static string Role { get; private set; } = "";
    public static string ChildrenId { get; private set; } = "";
    public static string ChildName { get; private set; } = "";
    public static string VerifiedParentUid { get; private set; } = "";
    public static bool IsParent => IsLoggedIn && Role == "parent";
    public static string ProfileStudentId => IsParent ? ChildrenId : LearningDataStore.CurrentUserId;
    public static string ProfileStudentName => IsParent ? ChildName : StudentName;

    public static void Login(string studentId, string studentName, string firstName, string role = "student", string childrenId = "")
    {
        IsLoggedIn = true;
        StudentId = studentId;
        StudentName = studentName;
        Role = role;
        ChildrenId = role == "parent" ? childrenId : "";
        ChildName = "";
        VerifiedParentUid = "";

        string resolvedFirstName = ResolveFirstName(firstName, studentName);
        PlayerPrefs.SetString(StudentNameKey, studentName ?? string.Empty);
        PlayerPrefs.SetString(StudentFirstNameKey, resolvedFirstName);
        PlayerPrefs.Save();
    }

    public static void SetParentChild(string id, string name, string parentUid)
    {
        if (!IsParent) throw new InvalidOperationException("A parent session is required.");
        ChildrenId = id;
        ChildName = name;
        VerifiedParentUid = parentUid;
    }

    public static void InvalidateParentChild() => VerifiedParentUid = "";

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
        Role = "";
        ChildrenId = "";
        ChildName = "";
        VerifiedParentUid = "";
        PlayerPrefs.DeleteKey("SelectedGradeStudentId");
        PlayerPrefs.DeleteKey("SelectedGradeStudentName");
        PlayerPrefs.DeleteKey("SelectedGradeStudentGrade");
        PlayerPrefs.DeleteKey(StudentNameKey);
        PlayerPrefs.DeleteKey(StudentFirstNameKey);
        PlayerPrefs.Save();
    }
}
