using System;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Wires the authored parent controls without altering their artwork or layout.
[DefaultExecutionOrder(1000)]
public sealed class ParentDocumentsPanel : MonoBehaviour
{
    readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    ParentDocumentStore store;
    TeacherPdfPicker picker;
    ParentDocumentPreview preview;
    GameObject panel, popup;
    Button upload, view;
    TMP_Text status, percent;
    Slider progress;
    bool busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= Attach; SceneManager.sceneLoaded += Attach;
    }

    static void Attach(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "ParentsViewingScene") return;
        foreach (var root in scene.GetRootGameObjects())
        {
            var documents = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "DocumentsPanel");
            if (documents == null) continue;
            var canvas = documents.GetComponentInParent<Canvas>(true);
            if (canvas != null && canvas.GetComponent<ParentDocumentsPanel>() == null)
                canvas.gameObject.AddComponent<ParentDocumentsPanel>();
            break;
        }
    }

    T Find<T>(string name) where T : Component
    {
        var target = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        if (target == null)
            throw new InvalidOperationException("Missing parent document control: " + name);
        var component = target.GetComponent<T>();
        // Authored upload artwork is an Image, so add its click behaviour in code.
        if (component == null && typeof(T) == typeof(Button))
        {
            var button = target.gameObject.AddComponent<Button>();
            button.targetGraphic = target.GetComponent<Graphic>();
            button.transition = Selectable.Transition.None;
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
            component = button as T;
        }
        if (component == null)
            throw new InvalidOperationException("Parent document control " + name + " needs " + typeof(T).Name);
        return component;
    }

    void Awake()
    {
        panel = Find<Transform>("DocumentsPanel").gameObject;
        upload = Find<Button>("UploadDocumentsButton"); view = Find<Button>("ViewButton");
        popup = Find<Transform>("ViewPopup").gameObject;
        view.gameObject.SetActive(false); popup.SetActive(false); panel.SetActive(false);
    }

    void Start()
    {
        progress = Find<Slider>("UploadPercentSlider"); progress.interactable = false;
        percent = Find<TMP_Text>("UploadPercent");
        progress.minValue = 0; progress.maxValue = 1; progress.wholeNumbers = false;
        // The display-only progress bar must not intercept clicks on upload artwork.
        foreach (var graphic in progress.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        preview = popup.AddComponent<ParentDocumentPreview>();
        status = panel.transform.Find("Text (TMP)").GetComponent<TMP_Text>(); status.richText = false;
        var receiver = new GameObject("ParentDocumentPicker_" + Guid.NewGuid().ToString("N"));
        receiver.transform.SetParent(transform, false); picker = receiver.AddComponent<TeacherPdfPicker>();
        upload.onClick = new Button.ButtonClickedEvent(); upload.onClick.AddListener(Upload);
        view.onClick = new Button.ButtonClickedEvent(); view.onClick.AddListener(View);
        Find<Button>("CloseBtn").onClick.AddListener(preview.Close);
        var tabs = GetComponentsInChildren<Button>(true);
        foreach (var button in tabs.Where(b => b.name == "ProfileButton" || b.name == "RecentActivityButton" || b.name == "GradesButton"))
            button.onClick.AddListener(() => { panel.SetActive(false); popup.SetActive(false); });
        var documentsTab = tabs.FirstOrDefault(b => b.name == "GradesButton (1)");
        if (documentsTab != null)
        {
            documentsTab.onClick = new Button.ButtonClickedEvent();
            documentsTab.onClick.AddListener(() =>
            {
                var tabsManager = GetComponentInChildren<StudentProfileTabManager>(true);
                if (tabsManager != null)
                {
                    tabsManager.profilePanel.SetActive(false); tabsManager.recentActivityPanel.SetActive(false);
                    tabsManager.gradesPanel.SetActive(false);
                }
                panel.SetActive(true);
                if (!busy) Refresh();
            });
        }
        Refresh();
    }

    void Progress(float value) { progress.SetValueWithoutNotify(Mathf.Clamp01(value)); percent.text = Mathf.RoundToInt(value * 100) + "%"; }
    void Busy(bool value) { busy = value; upload.interactable = !value; view.interactable = !value; }

    async void Refresh()
    {
        if (busy) return;
        Busy(true); view.gameObject.SetActive(false); Progress(0);
        try
        {
            status.text = "Loading documents..."; store = new ParentDocumentStore();
            await store.Load(lifetime.Token);
            if (this == null) return;
            view.gameObject.SetActive(store.HasDocument);
            status.text = store.HasDocument ? store.FileName : "Select Document"; Progress(0);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) status.text = error.GetBaseException().Message; }
        finally { if (this != null) Busy(false); }
    }

    async void Upload()
    {
        if (busy) return; Busy(true);
        try
        {
            string path = await picker.Pick(true);
            lifetime.Token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(path)) return;
            status.text = "Uploading document..."; Progress(0);
            var next = new ParentDocumentStore();
            await next.Upload(path, lifetime.Token, Progress);
            if (this == null) return;
            store = next;
            view.gameObject.SetActive(true); status.text = store.FileName;
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) status.text = error.GetBaseException().Message; }
        finally { if (this != null) Busy(false); }
    }

    async void View()
    {
        if (busy || store == null || !store.HasDocument) return; Busy(true);
        try
        {
            status.text = "Opening document..."; Progress(0);
            string local = await store.Download(lifetime.Token, Progress);
            lifetime.Token.ThrowIfCancellationRequested();
            await preview.Open(local, status, lifetime.Token);
            if (this != null) { status.text = store.FileName; Progress(1); }
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) status.text = error.GetBaseException().Message; }
        finally { if (this != null) Busy(false); }
    }

    void OnDestroy() { lifetime.Cancel(); lifetime.Dispose(); }
}
