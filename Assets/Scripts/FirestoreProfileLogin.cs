using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Signs in a managed account using its school ID and Firebase password.</summary>
public class FirestoreProfileLogin : MonoBehaviour
{
    [SerializeField] private TMP_InputField idInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text loginLabel;
    [SerializeField] private Button loginButton;
    private TMP_Text statusLabel;
    private Button passwordButton;
    private LoginPasswordEye eye;
    private FirebaseAuth auth;
    private bool firebaseReady;
    private bool initializing;
    private bool loginInProgress;
    private bool passwordVisible;

    private void Start()
    {
        if (idInput == null) idInput = GameObject.Find("EnterId")?.GetComponent<TMP_InputField>();
        if (passwordInput == null) passwordInput = GameObject.Find("EnterName")?.GetComponent<TMP_InputField>();
        if (loginLabel == null) loginLabel = GameObject.Find("LoginTxt")?.GetComponent<TMP_Text>();
        if (loginButton == null) loginButton = GameObject.Find("ButtonPlay")?.GetComponent<Button>();
        if (idInput == null || passwordInput == null || loginButton == null)
        {
            Debug.LogError("LoginScene is missing a required input field or login button.");
            enabled = false;
            return;
        }
        if (loginLabel != null) loginLabel.text = "Login";
        ConfigureInput(idInput, "Enter your ID number", false);
        ConfigureInput(passwordInput, "Enter your password", true);
        idInput.onSubmit.AddListener(FocusPassword);
        passwordInput.onSubmit.AddListener(SubmitPassword);
        CreatePasswordButton();
        CreateStatusLabel();
        InitializeFirebase();
    }

    private static void ConfigureInput(TMP_InputField input, string hint, bool password)
    {
        input.contentType = password ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.richText = false;
        input.textComponent.richText = false;
        input.text = string.Empty;
        if (input.placeholder is TMP_Text placeholder) placeholder.text = hint;
        input.ForceLabelUpdate();
    }

    private async void InitializeFirebase()
    {
        if (initializing) return;
        initializing = true;
        SetBusy(true);
        ShowStatus("Connecting...", false);
        try
        {
            var result = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (this == null) return;
            firebaseReady = result == DependencyStatus.Available;
            if (firebaseReady)
            {
                auth = FirebaseAuth.DefaultInstance;
                // A new login must not inherit a previous user's school profile.
                auth.SignOut();
                LearningDataStore.ClearCurrentUser();
                LoginSession.Logout();
                ShowStatus(string.Empty, false);
            }
            else ShowStatus("Connection unavailable. Press Play to retry.", true);
        }
        catch (Exception)
        {
            if (this != null) ShowStatus("Connection unavailable. Press Play to retry.", true);
        }
        finally
        {
            if (this != null)
            {
                initializing = false;
                SetBusy(false);
            }
        }
    }

    public async void Login()
    {
        if (loginInProgress || initializing) return;
        if (!firebaseReady) { InitializeFirebase(); return; }
        string schoolId = idInput.text.Trim();
        string password = passwordInput.text; // Passwords must never be trimmed.
        if (string.IsNullOrWhiteSpace(schoolId) || schoolId.Contains("/") || schoolId == "." || schoolId == "..")
        {
            ShowStatus("Enter your ID number.", true);
            idInput.ActivateInputField();
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            ShowStatus("Enter your password.", true);
            passwordInput.ActivateInputField();
            return;
        }
        loginInProgress = true;
        SetPasswordVisible(false);
        SetBusy(true);
        ShowStatus("Signing in...", false);
        bool signedIn = false;
        bool completed = false;
        try
        {
            // The admin app stores users/{schoolId} and derives the Auth email from username.
            // Resolve that mapping without changing existing accounts or passwords.
            var reference = FirebaseFirestore.DefaultInstance.Collection(LearningDataStore.UsersCollection).Document(schoolId);
            var profile = await reference.GetSnapshotAsync(Source.Server);
            if (this == null) return;
            string username = GetString(profile, "username");
            string expectedUid = GetString(profile, "authUid");
            if (!profile.Exists || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(expectedUid))
            {
                ShowStatus("ID number or password is incorrect.", true);
                return;
            }
            var result = await auth.SignInWithEmailAndPasswordAsync(BuildManagedEmail(username), password);
            signedIn = true;
            if (this == null) { auth.SignOut(); return; }
            // Fetch again after authentication so a changed/deleted profile cannot create a stale session.
            profile = await reference.GetSnapshotAsync(Source.Server);
            if (this == null) { auth.SignOut(); return; }
            if (result.User == null || !profile.Exists || GetString(profile, "authUid") != result.User.UserId ||
                expectedUid != result.User.UserId)
            {
                ShowStatus("ID number or password is incorrect.", true);
                return;
            }
            string role = GetString(profile, "role").ToLowerInvariant();
            string destination = role == "teacher" ? "MainMenuTeacher" : role == "student" ? "CharacterSelect" : null;
            if (destination == null)
            {
                ShowStatus("This account cannot use the game.", true);
                return;
            }
            if (!Application.CanStreamedLevelBeLoaded(destination))
            {
                ShowStatus("The next screen is unavailable. Please contact your administrator.", true);
                return;
            }
            string fullName = GetString(profile, "name");
            if (string.IsNullOrWhiteSpace(fullName))
                fullName = string.Join(" ", new[] { GetString(profile, "firstName"), GetString(profile, "middleName"), GetString(profile, "lastName") }.Where(part => part.Length > 0));
            LoginSession.Login(profile.Id, fullName, GetString(profile, "firstName"));
            LearningDataStore.SetCurrentUser(profile.Id);
            SceneLoader.SetStudentLoggedIn();
            passwordInput.text = string.Empty;
            UnityEngine.SceneManagement.SceneManager.LoadScene(destination);
            completed = true;
        }
        catch (Exception exception)
        {
            if (this != null) ShowStatus(ErrorMessage(exception), true);
        }
        finally
        {
            if (!completed && signedIn)
            {
                auth.SignOut();
                LearningDataStore.ClearCurrentUser();
                LoginSession.Logout();
            }
            if (this != null && !completed)
            {
                loginInProgress = false;
                SetBusy(false);
            }
        }
    }

    private static string ErrorMessage(Exception exception)
    {
        if (exception is FirestoreException)
            return "Unable to connect. Check your internet and try again.";
        if (exception is FirebaseException firebase)
        {
            if (firebase.ErrorCode == (int)AuthError.NetworkRequestFailed)
                return "Check your internet connection and try again.";
            if (firebase.ErrorCode == (int)AuthError.TooManyRequests)
                return "Too many attempts. Please wait before trying again.";
        }
        return "Unable to sign in. Check your ID number and password.";
    }

    private static string BuildManagedEmail(string username)
    {
        // Keep identical to managedEmail() in the admin account service, including '@' sanitization.
        string normalized = username.Trim().ToLowerInvariant();
        return new string(normalized.Select(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
            c == '.' || c == '_' || c == '-' ? c : '-').ToArray()) + "@users.learnable.app";
    }

    private static string GetString(DocumentSnapshot profile, string field)
    {
        return profile.Exists && profile.TryGetValue<string>(field, out var value) ? (value ?? string.Empty).Trim() : string.Empty;
    }

    private void SetBusy(bool busy)
    {
        idInput.interactable = !busy;
        passwordInput.interactable = !busy;
        loginButton.interactable = !busy;
        if (passwordButton != null) passwordButton.interactable = !busy;
    }

    private void FocusPassword(string _) { if (!loginInProgress && !initializing) passwordInput.ActivateInputField(); }
    private void SubmitPassword(string _) { Login(); }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame || loginInProgress || initializing) return;
        bool reverse = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        var selected = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
        Selectable next;
        if (selected == idInput.gameObject) next = reverse ? (Selectable)loginButton : passwordInput;
        else if (selected == passwordInput.gameObject) next = reverse ? (Selectable)idInput : passwordButton;
        else if (selected == passwordButton.gameObject) next = reverse ? (Selectable)passwordInput : loginButton;
        else next = reverse ? (Selectable)passwordButton : idInput;
        next.Select();
        if (next is TMP_InputField input) input.ActivateInputField();
#endif
    }

    private void CreatePasswordButton()
    {
        var buttonObject = new GameObject("Show password", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(passwordInput.transform, false);
        var rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(1, 0.5f);
        rect.sizeDelta = new Vector2(44, 0);
        rect.anchoredPosition = Vector2.zero;
        var background = buttonObject.GetComponent<Image>();
        background.color = new Color(1, 1, 1, 0.08f);
        passwordButton = buttonObject.GetComponent<Button>();
        passwordButton.targetGraphic = background;
        passwordButton.onClick.AddListener(TogglePasswordVisibility);
        var iconObject = new GameObject("Eye", typeof(RectTransform), typeof(CanvasRenderer), typeof(LoginPasswordEye));
        iconObject.transform.SetParent(buttonObject.transform, false);
        eye = iconObject.GetComponent<LoginPasswordEye>();
        eye.rectTransform.sizeDelta = new Vector2(24, 24);
        eye.color = new Color(1f, 0.94f, 0.8f);
        eye.raycastTarget = false;
        // Reserve the button's width so long passwords never render underneath it.
        var viewport = passwordInput.textViewport;
        viewport.offsetMax = new Vector2(-48, viewport.offsetMax.y);
        SetPasswordVisible(false);
    }

    public void TogglePasswordVisibility()
    {
        if (loginInProgress || initializing) return;
        SetPasswordVisible(!passwordVisible);
    }

    private void SetPasswordVisible(bool visible)
    {
        passwordVisible = visible;
        passwordInput.inputType = visible ? TMP_InputField.InputType.Standard : TMP_InputField.InputType.Password;
        passwordInput.ForceLabelUpdate();
        if (eye != null) eye.SetVisible(visible);
        if (passwordButton != null) passwordButton.gameObject.name = visible ? "Hide password" : "Show password";
    }

    private void OnApplicationFocus(bool focused) { if (!focused && passwordInput != null) SetPasswordVisible(false); }
    private void OnApplicationPause(bool paused) { if (paused && passwordInput != null) SetPasswordVisible(false); }

    private void CreateStatusLabel()
    {
        var statusObject = new GameObject("LoginStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        statusObject.transform.SetParent(idInput.transform.parent, false);
        statusLabel = statusObject.GetComponent<TextMeshProUGUI>();
        statusLabel.font = idInput.textComponent.font;
        statusLabel.fontSize = 15;
        statusLabel.alignment = TextAlignmentOptions.Center;
        statusLabel.raycastTarget = false;
        statusLabel.richText = false;
        var rect = statusLabel.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -111);
        rect.sizeDelta = new Vector2(340, 32);
    }

    private void ShowStatus(string message, bool error)
    {
        if (statusLabel == null) return;
        statusLabel.text = message;
        statusLabel.color = error ? new Color(1f, 0.83f, 0.55f) : Color.white;
    }

    private void OnDestroy()
    {
        if (idInput != null) idInput.onSubmit.RemoveListener(FocusPassword);
        if (passwordInput != null)
        {
            passwordInput.onSubmit.RemoveListener(SubmitPassword);
            passwordInput.text = string.Empty;
        }
        if (passwordButton != null) passwordButton.onClick.RemoveListener(TogglePasswordVisibility);
    }
}

