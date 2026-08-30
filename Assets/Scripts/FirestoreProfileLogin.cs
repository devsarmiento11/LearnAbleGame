using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Development login for the existing ID-and-username screen.
/// It verifies the entered values against a Firestore user profile before
/// opening the correct menu for the account's role.
/// </summary>
public class FirestoreProfileLogin : MonoBehaviour
{
    private const string IdInputObjectName = "EnterId";
    private const string UsernameInputObjectName = "EnterName";
    private const string LoginLabelObjectName = "LoginTxt";
    private const string StudentSceneName = "CharacterSelect";
    private const string TeacherSceneName = "MainMenuTeacher";

    private TMP_InputField idInput;
    private TMP_InputField usernameInput;
    private TMP_Text loginLabel;
    private bool firebaseReady;

    private void Start()
    {
        idInput = GameObject.Find(IdInputObjectName)?.GetComponent<TMP_InputField>();
        usernameInput = GameObject.Find(UsernameInputObjectName)?.GetComponent<TMP_InputField>();
        loginLabel = GameObject.Find(LoginLabelObjectName)?.GetComponent<TMP_Text>();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(
            task => OnDependenciesChecked(task),
            TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void Login()
    {
        if (!firebaseReady)
        {
            ShowStatus("Connecting...");
            return;
        }

        string enteredId = idInput == null ? string.Empty : idInput.text.Trim();
        string enteredUsername = usernameInput == null ? string.Empty : usernameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(enteredId) || string.IsNullOrWhiteSpace(enteredUsername))
        {
            ShowStatus("Enter ID and username");
            return;
        }

        ShowStatus("Checking...");

        FirebaseFirestore.DefaultInstance
            .Collection(LearningDataStore.UsersCollection)
            .WhereEqualTo("userId", enteredId)
            .Limit(1)
            .GetSnapshotAsync()
            .ContinueWith(
                task => OnProfileLoaded(task, enteredUsername),
                TaskScheduler.FromCurrentSynchronizationContext());
    }

    private void OnDependenciesChecked(Task<DependencyStatus> task)
    {
        firebaseReady = task.Status == TaskStatus.RanToCompletion &&
                        task.Result == DependencyStatus.Available;

        if (!firebaseReady)
        {
            Debug.LogError("Firebase is unavailable: " +
                           (task.IsFaulted ? task.Exception.ToString() : task.Result.ToString()));
            ShowStatus("Firebase unavailable");
        }
    }

    private void OnProfileLoaded(Task<QuerySnapshot> task, string enteredUsername)
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("Unable to retrieve user profile: " + task.Exception);
            ShowStatus("Login failed");
            return;
        }

        if (task.Result.Count != 1)
        {
            ShowStatus("Profile not found");
            return;
        }

        DocumentSnapshot profile = null;
        foreach (DocumentSnapshot document in task.Result.Documents)
        {
            profile = document;
            break;
        }

        if (profile == null)
        {
            ShowStatus("Profile not found");
            return;
        }
        string savedUsername = profile.ContainsField("username")
            ? profile.GetValue<string>("username")
            : string.Empty;

        if (!string.Equals(savedUsername, enteredUsername, StringComparison.OrdinalIgnoreCase))
        {
            ShowStatus("Username does not match");
            return;
        }

        string role = profile.ContainsField("role")
            ? profile.GetValue<string>("role").Trim().ToLowerInvariant()
            : string.Empty;

        string fullName = GetString(profile, "name");
        if (string.IsNullOrWhiteSpace(fullName))
        {
            fullName = string.Join(" ", new[]
            {
                GetString(profile, "firstName"),
                GetString(profile, "middleName"),
                GetString(profile, "lastName")
            }).Trim();
        }

        string firstName = GetString(profile, "firstName");
        LoginSession.Login(profile.Id, fullName, firstName);

        LearningDataStore.SetCurrentUser(profile.Id);

        if (role == "teacher")
        {
            SceneLoader.SetStudentLoggedIn();
            SceneManager.LoadScene(TeacherSceneName);
            return;
        }

        if (role == "student")
        {
            SceneLoader.SetStudentLoggedIn();
            SceneManager.LoadScene(StudentSceneName);
            return;
        }

        LearningDataStore.ClearCurrentUser();
        LoginSession.Logout();
        ShowStatus("This account cannot use the game");
    }

    private static string GetString(DocumentSnapshot profile, string field)
    {
        if (!profile.ContainsField(field))
            return string.Empty;

        string value = profile.GetValue<string>(field);
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private void ShowStatus(string message)
    {
        if (loginLabel != null)
        {
            loginLabel.text = message;
        }
    }
}
