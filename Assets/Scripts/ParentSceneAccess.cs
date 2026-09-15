using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Keeps the signed-in parent separate from the child being viewed.</summary>
public class ParentSceneAccess : MonoBehaviour
{
    private readonly List<GameObject> content = new List<GameObject>();
    private GameObject overlay;
    private TMP_Text message;
    private Button retry;
    private bool loading;
    private bool contentStarted;
    private ListenerRegistration parentListener;

    public static bool IsAllowed(string scene) => scene == "MainMenuParent" ||
        scene == "ParentsViewingScene" || scene == "SettingsScene" || scene == "LoginScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool parentScene = scene.name == "MainMenuParent" || scene.name == "ParentsViewingScene";
        if (parentScene && !LoginSession.IsParent)
        {
            SceneManager.LoadScene("LoginScene");
            return;
        }
        if (LoginSession.IsParent && !IsAllowed(scene.name))
        {
            SceneManager.LoadScene("MainMenuParent");
            return;
        }
        if (!parentScene) return;
        var controller = new GameObject("Parent read-only access").AddComponent<ParentSceneAccess>();
        controller.Prepare(scene);
    }

    private void Prepare(Scene scene)
    {
        // Do this before Start so profile scripts cannot briefly show a previous user.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<Canvas>(true) == null) continue;
            content.Add(root);
            if (scene.name == "MainMenuParent")
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                    if (button.name == "Settings" && button.onClick.GetPersistentEventCount() == 0)
                        button.onClick.AddListener(() => FindAnyObjectByType<SceneLoader>()?.OpenSettings());
            }
            root.SetActive(false);
        }
        CreateOverlay();
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null && LoginSession.VerifiedParentUid == user.UserId &&
            !string.IsNullOrWhiteSpace(LoginSession.ChildrenId))
        {
            ShowContent();
            WatchParent(user.UserId);
            return;
        }
        ValidateChild();
    }

    private async void ValidateChild()
    {
        if (loading) return;
        loading = true;
        retry.interactable = false;
        message.text = "Loading your child's profile...";
        try
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (!LoginSession.IsParent || user == null) throw new InvalidOperationException();
            var reference = FirebaseFirestore.DefaultInstance.Collection("users").Document(LoginSession.StudentId);
            var parent = await WithTimeout(reference.GetSnapshotAsync(Source.Server));
            if (this == null) return;
            if (!ValidParent(parent, user.UserId)) throw new InvalidOperationException();
            string id = Text(parent, "childrenId");
            if (string.IsNullOrWhiteSpace(id) || id.Contains("/")) throw new InvalidOperationException();
            var child = await WithTimeout(FirebaseFirestore.DefaultInstance.Collection("users").Document(id).GetSnapshotAsync(Source.Server));
            if (this == null) return;
            if (!child.Exists || Text(child, "role") != "student") throw new InvalidOperationException();
            string name = string.Join(" ", new[] {Text(child, "firstName"), Text(child, "middleName"), Text(child, "lastName")}.Where(part => part.Length > 0));
            LoginSession.SetParentChild(id, name, user.UserId);
            // Recreate profile listeners after a retry; Start does not rerun on enable.
            if (contentStarted)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }
            ShowContent();
            WatchParent(user.UserId);
        }
        catch (Exception)
        {
            if (this != null) ShowError("Unable to open the linked child. Check your connection or ask your administrator to verify the Children ID.");
        }
        finally
        {
            if (this != null) { loading = false; retry.interactable = true; }
        }
    }

    private static async Task<T> WithTimeout<T>(Task<T> request)
    {
        if (await Task.WhenAny(request, Task.Delay(TimeSpan.FromSeconds(15))) != request)
        {
            _ = request.ContinueWith(task => { var ignored = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            throw new TimeoutException();
        }
        return await request;
    }

    private void ShowContent()
    {
        contentStarted = true;
        overlay.SetActive(false);
        foreach (var root in content) if (root != null) root.SetActive(true);
    }

    private void WatchParent(string uid)
    {
        parentListener?.Stop();
        var reference = FirebaseFirestore.DefaultInstance.Collection("users").Document(LoginSession.StudentId);
        parentListener = reference.Listen(snapshot =>
            {
                if (this == null) return;
                if (!ValidParent(snapshot, uid))
                    ShowError("Account access changed. Please sign in again.");
                else if (Text(snapshot, "childrenId") != LoginSession.ChildrenId)
                {
                    LoginSession.InvalidateParentChild();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            });
            _ = parentListener.ListenerTask.ContinueWithOnMainThread(task =>
            {
                if (this != null && task.IsFaulted) ShowError("Connection lost. Retry to view your child's profile.");
            });
    }

    private static string Text(DocumentSnapshot snapshot, string field) => snapshot.Exists && snapshot.TryGetValue<string>(field, out var value) ? value?.Trim() ?? "" : "";
    private static bool ValidParent(DocumentSnapshot snapshot, string uid) => snapshot.Exists &&
        Text(snapshot, "role") == "parent" && Text(snapshot, "authUid") == uid &&
        string.Equals(Text(snapshot, "status"), "Active", StringComparison.OrdinalIgnoreCase);

    private void ShowError(string text)
    {
        LoginSession.InvalidateParentChild();
        foreach (var root in content) if (root != null) root.SetActive(false);
        overlay.SetActive(true);
        message.text = text;
    }

    private void CreateOverlay()
    {
        overlay = new GameObject("Parent access status", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.97f, 0.95f, 0.91f);
        var label = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(panel.transform, false);
        message = label.GetComponent<TextMeshProUGUI>();
        message.rectTransform.sizeDelta = new Vector2(680, 160);
        message.rectTransform.anchoredPosition = new Vector2(0, 55);
        message.fontSize = 28; message.alignment = TextAlignmentOptions.Center;
        message.color = new Color(0.30f, 0.18f, 0.09f); message.richText = false;
        retry = AddButton(panel.transform, "Retry", -130, ValidateChild);
        AddButton(panel.transform, "Sign out", 130, () =>
        {
            FirebaseAuth.DefaultInstance.SignOut();
            LearningDataStore.ClearCurrentUser(); LoginSession.Logout();
            PlayerPrefs.DeleteKey("StudentLoggedIn"); PlayerPrefs.DeleteKey("SettingsPreviousScene");
            PlayerPrefs.Save(); SceneManager.LoadScene("LoginScene");
        });
    }

    private static Button AddButton(Transform parent, string text, float x, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(230, 60); rect.anchoredPosition = new Vector2(x, -90);
        obj.GetComponent<Image>().color = new Color(0.55f, 0.35f, 0.18f);
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(obj.transform, false);
        var tmp = label.GetComponent<TextMeshProUGUI>(); tmp.rectTransform.sizeDelta = rect.sizeDelta;
        tmp.text = text; tmp.fontSize = 24; tmp.alignment = TextAlignmentOptions.Center;
        var button = obj.GetComponent<Button>(); button.onClick.AddListener(action); return button;
    }

    private void OnDestroy() { parentListener?.Stop(); }
}
