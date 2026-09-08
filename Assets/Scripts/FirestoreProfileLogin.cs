using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Authenticates Flutter-managed accounts, then loads the matching Firestore
/// profile while preserving its school ID as the active game user ID.
/// </summary>
public class FirestoreProfileLogin : MonoBehaviour
{
    private const string PasswordInputObjectName = "EnterId";
    private const string UsernameInputObjectName = "EnterName";
    private const string LoginLabelObjectName = "LoginTxt";
    private const string StudentSceneName = "CharacterSelect";
    private const string TeacherSceneName = "MainMenuTeacher";

    private const string ManagedEmailDomain = "users.learnable.app";

    private TMP_InputField passwordInput;
    private TMP_InputField usernameInput;
    private TMP_Text loginLabel;
    private FirebaseAuth auth;
    private bool firebaseReady;
    private bool loginInProgress;

    private void Start()
    {
        passwordInput = GameObject.Find(PasswordInputObjectName)?.GetComponent<TMP_InputField>();
        usernameInput = GameObject.Find(UsernameInputObjectName)?.GetComponent<TMP_InputField>();
        loginLabel = GameObject.Find(LoginLabelObjectName)?.GetComponent<TMP_Text>();

        if (passwordInput != null)
        {
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();

            TMP_Text placeholder = passwordInput.placeholder as TMP_Text;
            if (placeholder != null)
                placeholder.text = "Password";
        }

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

        string enteredUsername = usernameInput == null ? string.Empty : usernameInput.text.Trim();
        string enteredPassword = passwordInput == null ? string.Empty : passwordInput.text;

        if (string.IsNullOrWhiteSpace(enteredUsername) || string.IsNullOrEmpty(enteredPassword))
        {
            ShowStatus("Enter username and password");
            return;
        }

        if (loginInProgress)
            return;

        loginInProgress = true;
        ShowStatus("Signing in...");

        string email = BuildManagedEmail(enteredUsername);
        auth.SignInWithEmailAndPasswordAsync(email, enteredPassword)
            .ContinueWithOnMainThread(OnAuthenticationCompleted);
    }

    private void OnDependenciesChecked(Task<DependencyStatus> task)
    {
        firebaseReady = task.Status == TaskStatus.RanToCompletion &&
                        task.Result == DependencyStatus.Available;

        if (firebaseReady)
            auth = FirebaseAuth.DefaultInstance;

        if (!firebaseReady)
        {
            Debug.LogError("Firebase is unavailable: " +
                           (task.IsFaulted ? task.Exception.ToString() : task.Result.ToString()));
            ShowStatus("Firebase unavailable");
        }
    }

    private void OnAuthenticationCompleted(Task<AuthResult> task)
    {
        if (task.IsFaulted || task.IsCanceled || task.Result == null || task.Result.User == null)
        {
            loginInProgress = false;
            Debug.LogWarning("Firebase Authentication sign-in failed: " + task.Exception);
            ShowStatus("Invalid username or password");
            return;
        }

        string authUid = task.Result.User.UserId;
        ShowStatus("Loading profile...");

        FirebaseFirestore.DefaultInstance
            .Collection(LearningDataStore.UsersCollection)
            .WhereEqualTo("authUid", authUid)
            .Limit(1)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread(OnProfileLoaded);
    }

    private void OnProfileLoaded(Task<QuerySnapshot> task)
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            loginInProgress = false;
            auth.SignOut();
            Debug.LogError("Unable to retrieve user profile: " + task.Exception);
            ShowStatus("Login failed");
            return;
        }

        if (task.Result.Count != 1)
        {
            loginInProgress = false;
            auth.SignOut();
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
            loginInProgress = false;
            auth.SignOut();
            ShowStatus("Profile not found");
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

        // The document ID is the existing school ID. authUid remains a
        // separate Firestore field and never replaces this value.
        LearningDataStore.SetCurrentUser(profile.Id);
        loginInProgress = false;

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
        auth.SignOut();
        ShowStatus("This account cannot use the game");
    }

    private static string BuildManagedEmail(string username)
    {
        string normalized = username.Trim().ToLowerInvariant();
        if (normalized.Contains("@"))
            return normalized;

        char[] safe = normalized.Select(character =>
            (character >= 'a' && character <= 'z') ||
            (character >= '0' && character <= '9') ||
            character == '.' || character == '_' || character == '-'
                ? character
                : '-').ToArray();

        return new string(safe) + "@" + ManagedEmailDomain;
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
