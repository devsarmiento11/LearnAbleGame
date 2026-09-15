using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attached only to the two teacher module scenes. Existing artwork is reused.
public sealed class TeacherModuleScene : MonoBehaviour
{
    public Sprite viewIcon, editIcon, archiveIcon, deleteIcon;
    public TeacherFolderPopup folderPopup;
    public TMP_Text uploadStatus;
    public Button createFolderButton, uploadModuleButton, backButton, settingsButton;
    public GameObject folderEntryPrefab;
    TeacherModuleStore store;
    TeacherPdfPicker picker;
    readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    readonly List<GameObject> generated = new List<GameObject>();
    List<TeacherModuleFolder> folders = new List<TeacherModuleFolder>();
    List<TeacherPdfModule> modules = new List<TeacherPdfModule>();
    [SerializeField] TMP_InputField title, folderName;
    TMP_InputField search, editTitle;
    [SerializeField] TMP_Dropdown folderOptions, grade;
    TMP_Dropdown filter, editFolder, editGrade;
    TMP_Text status, fileName;
    [SerializeField] TMP_Text percent;
    [SerializeField] Slider progress;
    GameObject createPopup, editPopup, archivePopup, deletePopup, template;
    [SerializeField] ScrollRect scroll;
    [SerializeField] RectTransform content;
    Vector2[] folderPositions;
    TeacherPdfModule selected;
    string replacement, currentFolder;
    bool uploadScene, busy, showingArchive;
    float statusFontSize;
    Task uploadInitialization;

    T At<T>(string path) where T : Component
    {
        var found = transform.Find(path);
        if (found == null || !found.TryGetComponent<T>(out var result))
            throw new InvalidOperationException("Missing scene control: " + path);
        return result;
    }
    GameObject Obj(string path) => At<Transform>(path).gameObject;
    void Bind(string path, UnityAction action)
    {
        Bind(At<Button>(path), action);
    }
    void Bind(Button button, UnityAction action)
    {
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => { if (!busy) action(); });
    }
    Button UploadButton(Button assigned, string currentName, string previousName)
    {
        if (assigned != null) return assigned;
        var found = transform.Find("Buttons/" + currentName) ?? transform.Find("Buttons/" + previousName);
        if (found != null && found.TryGetComponent<Button>(out var button)) return button;
        throw new InvalidOperationException("Assign the " + currentName + " button in the UploadModuleScene controller.");
    }
    void Run(Func<Task> operation) { if (!busy) Execute(operation); }
    async void Execute(Func<Task> operation)
    {
        busy = true; SetInteractive(false);
        try { await operation(); }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (this != null)
            {
                var root = error.GetBaseException();
                Message(root is Firebase.Firestore.FirestoreException firestore && firestore.ErrorCode == Firebase.Firestore.FirestoreError.PermissionDenied
                    ? "Folder access is blocked by Firebase rules. The teacher-library rules must be published."
                    : root is InvalidOperationException ? root.Message : "Unable to complete this action. Check your connection and Firebase module permissions, then try again.");
                if (folderPopup != null && folderPopup.gameObject.activeSelf) folderPopup.ShowMessage(status.text);
                if (editPopup != null && editPopup.activeSelf && fileName != null)
                {
                    fileName.enableAutoSizing = true; fileName.fontSizeMin = 10;
                    fileName.text = status.text;
                }
                Debug.LogWarning("Teacher modules: " + root.Message);
            }
        }
        finally { if (this != null) { busy = false; SetInteractive(true); } }
    }
    void SetInteractive(bool enabled)
    {
        foreach (var control in GetComponentsInChildren<Selectable>(true)) control.interactable = enabled;
        if (progress != null) progress.interactable = false;
        if (uploadScene && folderOptions != null) folderOptions.interactable = enabled && folders.Count > 0;
    }
    void Message(string message)
    {
        if (status != null)
        {
            if (statusFontSize <= 0) statusFontSize = status.fontSize;
            status.richText = false; status.text = message;
            status.enableAutoSizing = true; status.fontSizeMin = 10; status.fontSizeMax = statusFontSize;
        }
    }
    void Navigate(string scene)
    {
        if (scene == "SettingsScene") PlayerPrefs.SetString("SettingsPreviousScene", SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(scene);
    }
    void Awake()
    {
        // A disabled controller must not start network work in the UI-only scene.
        if (!enabled) return;
        uploadScene = SceneManager.GetActiveScene().name == "UploadModuleScene";
        picker = new GameObject("TeacherModulePdfPicker").AddComponent<TeacherPdfPicker>();
        picker.transform.SetParent(transform, false);
        try
        {
            if (uploadScene) SetupUpload(); else SetupFolder();
            if (uploadScene)
            {
                RefreshFolders(); SetInteractive(true);
                uploadInitialization = InitializeUpload();
                ObserveUploadInitialization();
                return;
            }
            Run(async () =>
            {
                Message("Loading folders...");
                var dependencies = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
                lifetime.Token.ThrowIfCancellationRequested();
                if (dependencies != Firebase.DependencyStatus.Available) throw new InvalidOperationException("Firebase is unavailable on this device.");
                store = await TeacherModuleStore.OpenAsync(lifetime.Token);
                folders = await store.Folders();
                lifetime.Token.ThrowIfCancellationRequested();
                if (uploadScene) { RefreshFolders(); Message(folders.Count == 0 ? "Create a folder to begin." : "Select Module"); }
                else
                {
                    currentFolder = TeacherModuleStore.SelectedOwner == store.Owner ? TeacherModuleStore.SelectedFolderId : null;
                    if (!folders.Any(f => f.Id == currentFolder))
                    { Navigate("UploadModuleScene"); return; }
                    await ReloadModules();
                }
            });
        }
        catch (Exception error) { Debug.LogError(error); Message(error.Message); }
    }

    async Task InitializeUpload()
    {
        Message("Loading folders...");
        var dependencies = await ReadWithTimeout(Firebase.FirebaseApp.CheckAndFixDependenciesAsync());
        if (dependencies != Firebase.DependencyStatus.Available) throw new InvalidOperationException("Firebase is unavailable on this device.");
        store = await ReadWithTimeout(TeacherModuleStore.OpenAsync(lifetime.Token));
        folders = await ReadWithTimeout(store.Folders());
        lifetime.Token.ThrowIfCancellationRequested();
        RefreshFolders();
        if (!busy) { SetInteractive(true); Message(folders.Count == 0 ? "Create a folder to begin." : "Select Module"); }
    }

    async void ObserveUploadInitialization()
    {
        try { await uploadInitialization; }
        catch (Exception error)
        {
            if (this == null || busy) return;
            Message(error.GetBaseException() is InvalidOperationException ? error.GetBaseException().Message : "Unable to load folders. Check your connection and try Create Folder again.");
            Debug.LogWarning("Teacher folders: " + error.GetBaseException().Message);
        }
    }

    async Task<T> ReadWithTimeout<T>(Task<T> read)
    {
        var timeout = Task.Delay(15000, lifetime.Token);
        if (await Task.WhenAny(read, timeout) != read)
        {
            lifetime.Token.ThrowIfCancellationRequested();
            // Observe a late failure without allowing the abandoned read to update UI.
            _ = read.ContinueWith(t => { var observed = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            throw new InvalidOperationException("Connection timed out. Check your internet and try again.");
        }
        lifetime.Token.ThrowIfCancellationRequested();
        return await read;
    }

    public void OpenCreateFolder()
    {
        if (!busy) folderPopup.Show();
    }

    public void ConfirmCreateFolder() => Run(CreateFolderFromPopup);
    public void CancelCreateFolder() { if (!busy) createPopup.SetActive(false); }
    public void UploadModule() => Run(Upload);

    async Task CreateFolderFromPopup()
    {
        string name = TeacherModuleStore.ValidName(folderName.text);
        folderPopup.ShowMessage("Saving folder...");
        if (uploadInitialization != null)
        {
            try { await uploadInitialization; }
            catch { /* Retry account initialization below; never keep controls locked. */ }
        }
        lifetime.Token.ThrowIfCancellationRequested();
        if (store == null) store = await ReadWithTimeout(TeacherModuleStore.OpenAsync(lifetime.Token));
        string id = await store.CreateFolder(name);
        lifetime.Token.ThrowIfCancellationRequested();
        folders.RemoveAll(f => f.Id == id);
        folders.Add(new TeacherModuleFolder { Id = id, Name = name });
        folders = folders.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
        createPopup.SetActive(false); folderName.text = "";
        RefreshFolders(id); Message("Folder created. Select your module.");
    }

    void SetupUpload()
    {
        if (title == null) title = At<TMP_InputField>("InputField (TMP)"); title.characterLimit = 100;
        if (folderOptions == null) folderOptions = At<TMP_Dropdown>("Dropdown/Select Folder");
        if (grade == null) grade = At<TMP_Dropdown>("Dropdown/Select Grade Level"); SetGrades(grade, false);
        // Find the existing Select Module label, independent of sibling order.
        status = uploadStatus != null ? uploadStatus : At<TMP_Text>("Texts/Text (TMP) (3)");
        if (progress == null) progress = At<Slider>("DownloadProgress");
        if (percent == null) percent = At<TMP_Text>("DownloadProgress/UploadPercent");
        progress.minValue = 0; progress.maxValue = 1; progress.wholeNumbers = false;
        progress.gameObject.SetActive(false);
        createPopup = folderPopup != null ? folderPopup.gameObject : Obj("ConfirmationPopup"); createPopup.SetActive(false);
        if (folderName == null) folderName = At<TMP_InputField>("ConfirmationPopup/InputField (TMP) (1)"); folderName.characterLimit = 100;
        if (folderPopup == null) folderPopup = createPopup.GetComponent<TeacherFolderPopup>();
        if (folderPopup == null) folderPopup = createPopup.AddComponent<TeacherFolderPopup>();
        folderPopup.nameInput = folderName;
        folderPopup.confirmButton = At<Button>("ConfirmationPopup/Button (1)");
        folderPopup.cancelButton = At<Button>("ConfirmationPopup/Button");
        createFolderButton = UploadButton(createFolderButton, "CreateFolderBtn", "Button");
        uploadModuleButton = UploadButton(uploadModuleButton, "UploadModuleBtn", "Button (1)");
        backButton = UploadButton(backButton, "BackBtn", "Button (2)");
        settingsButton = UploadButton(settingsButton, "Settings", "Button (3)");
        Bind(createFolderButton, OpenCreateFolder);
        Bind("ConfirmationPopup/Button", CancelCreateFolder);
        Bind("ConfirmationPopup/Button (1)", ConfirmCreateFolder);
        Bind(uploadModuleButton, UploadModule);
        Bind(backButton, () => Navigate("MainMenuTeacher"));
        Bind(settingsButton, () => Navigate("SettingsScene"));
        if (scroll == null) scroll = At<ScrollRect>("FoldersScrollView");
        Canvas.ForceUpdateCanvases();
        var originals = At<Transform>("FoldersScrollView/Folders").Cast<Transform>().ToArray();
        var viewport = At<RectTransform>("FoldersScrollView/Viewport");
        folderPositions = originals.Select(t => (Vector2)viewport.InverseTransformPoint(t.position) - new Vector2(viewport.rect.xMin, viewport.rect.yMax)).ToArray();
        template = folderEntryPrefab != null ? folderEntryPrefab : originals[0].gameObject;
        foreach (var item in originals) item.gameObject.SetActive(false);
        if (content == null) content = At<RectTransform>("FoldersScrollView/Viewport/Content");
        ConfigureScroll(viewport);
    }
    void ConfigureScroll(RectTransform viewport)
    {
        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
        foreach (var group in content.GetComponents<LayoutGroup>()) group.enabled = false;
        var fitter = content.GetComponent<ContentSizeFitter>(); if (fitter != null) fitter.enabled = false;
        content.anchorMin = content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 1);
        content.anchoredPosition = Vector2.zero;
        // Transparent raycast area lets dragging start between items.
        var background = viewport.GetComponent<Image>(); if (background != null) background.raycastTarget = true;
    }
    void ClearRows()
    {
        foreach (var row in generated) { row.SetActive(false); Destroy(row); }
        generated.Clear();
    }
    void RefreshFolders(string selectedId = null)
    {
        ClearRows();
        folderOptions.ClearOptions();
        folderOptions.AddOptions(folders.Count == 0 ? new List<string> { "Create a folder first" } : folders.Select(f => f.Name).ToList());
        folderOptions.SetValueWithoutNotify(Math.Max(0, folders.FindIndex(f => f.Id == selectedId)));
        folderOptions.RefreshShownValue();
        float rowHeight = scroll.viewport.rect.height;
        content.sizeDelta = new Vector2(scroll.viewport.rect.width, Math.Max(1, (folders.Count + 3) / 4) * rowHeight);
        for (int i = 0; i < folders.Count; i++)
        {
            var folder = folders[i];
            var row = Instantiate(template, content); row.name = "Folder_" + folder.Id; row.SetActive(true);
            var rect = (RectTransform)row.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = folderPositions[i % 4] - new Vector2(0, i / 4 * rowHeight);
            var entry = row.GetComponent<TeacherFolderEntry>();
            if (entry == null) entry = row.AddComponent<TeacherFolderEntry>();
            entry.Setup(folder.Name, () =>
            {
                if (busy) return;
                TeacherModuleStore.SelectedFolderId = folder.Id; TeacherModuleStore.SelectedOwner = store.Owner;
                Navigate("FolderScene");
            });
            generated.Add(row);
        }
        scroll.verticalNormalizedPosition = 1;
    }
    async Task Upload()
    {
        string moduleTitle = TeacherModuleStore.ValidName(title.text);
        if (folders.Count == 0) throw new InvalidOperationException("Create a folder before uploading a module.");
        var module = new TeacherPdfModule { Title = moduleTitle, FolderId = folders[folderOptions.value].Id, Grade = grade.value + 1 };
        string path = await picker.Pick(); lifetime.Token.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(path)) { Message("Selection cancelled."); return; }
        TeacherModuleStore.ValidatePdf(path);
        progress.gameObject.SetActive(true); Progress(0); Message("Uploading PDF...");
        try
        {
            await store.Save(module, path, lifetime.Token, Progress);
            lifetime.Token.ThrowIfCancellationRequested(); title.text = ""; Message("Module uploaded successfully.");
        }
        catch { progress.gameObject.SetActive(false); throw; }
    }
    void Progress(float value)
    {
        if (this == null) return;
        if (progress != null) { progress.value = value; percent.text = Mathf.FloorToInt(value * 100) + "%"; }
        else if (fileName != null) fileName.text = "Uploading PDF... " + Mathf.FloorToInt(value * 100) + "%";
    }
    static void SetGrades(TMP_Dropdown dropdown, bool all)
    {
        dropdown.ClearOptions(); var labels = Enumerable.Range(1, 6).Select(g => "Grade " + g).ToList();
        if (all) labels.Insert(0, "All Grade Levels");
        dropdown.AddOptions(labels); dropdown.SetValueWithoutNotify(0); dropdown.RefreshShownValue();
    }

    void SetupFolder()
    {
        status = At<TMP_Text>("Title Of Folder");
        search = At<TMP_InputField>("InputField (TMP)");
        filter = At<TMP_Dropdown>("Dropdown"); SetGrades(filter, true);
        editPopup = Obj("EditPopUp"); archivePopup = Obj("ArchivePopUp"); deletePopup = Obj("DeletePopUp");
        editPopup.SetActive(false); archivePopup.SetActive(false); deletePopup.SetActive(false);
        editTitle = At<TMP_InputField>("EditPopUp/ExistingModuleName"); editTitle.characterLimit = 100;
        editFolder = At<TMP_Dropdown>("EditPopUp/Folder Options ");
        editGrade = At<TMP_Dropdown>("EditPopUp/CurrentLevelPutted"); SetGrades(editGrade, false);
        fileName = editPopup.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Current File Name");
        Bind("Buttons/Button", () => Navigate("UploadModuleScene"));
        Bind("Buttons/Button (1)", () => Navigate("SettingsScene"));
        Bind("Buttons/Button (2)", () => { showingArchive = !showingArchive; RenderModules(); });
        Bind("EditPopUp/Button", ClosePopups); Bind("EditPopUp/CancelBtn", ClosePopups);
        Bind("ArchivePopUp/CloseBtn", ClosePopups); Bind("ArchivePopUp/CancelBtn", ClosePopups);
        Bind("DeletePopUp/CloseButton", ClosePopups); Bind("DeletePopUp/CancelBtn", ClosePopups);
        Bind("EditPopUp/ReplaceFileBtn", () => Run(async () =>
        {
            string path = await picker.Pick(); lifetime.Token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(path)) return;
            TeacherModuleStore.ValidatePdf(path); replacement = path;
            fileName.text = System.IO.Path.GetFileName(path);
        }));
        Bind("EditPopUp/UploadBtn", () => Run(async () =>
        {
            var change = Copy(selected);
            change.Title = editTitle.text; change.FolderId = folders[editFolder.value].Id; change.Grade = editGrade.value + 1;
            await store.Save(change, replacement, lifetime.Token, Progress);
            lifetime.Token.ThrowIfCancellationRequested(); ClosePopups(); await ReloadModules();
        }));
        Bind("ArchivePopUp/ArchiveBtn", () => Run(async () =>
        {
            var change = Copy(selected); change.Archived = !change.Archived;
            await store.Save(change, null, lifetime.Token, null);
            lifetime.Token.ThrowIfCancellationRequested(); ClosePopups(); await ReloadModules();
        }));
        Bind("DeletePopUp/DeleteBtn", () => Run(async () =>
        {
            await store.Delete(selected); lifetime.Token.ThrowIfCancellationRequested(); ClosePopups(); await ReloadModules();
        }));
        search.onValueChanged.AddListener(_ => { if (!busy) RenderModules(); });
        filter.onValueChanged.AddListener(_ => { if (!busy) RenderModules(); });
        scroll = At<ScrollRect>("Scroll View"); content = At<RectTransform>("Scroll View/Viewport/Content");
        template = Obj("Scroll View/Viewport/Content/Modules"); template.SetActive(false);
        ConfigureScroll(At<RectTransform>("Scroll View/Viewport"));
    }
    static TeacherPdfModule Copy(TeacherPdfModule item) => new TeacherPdfModule
    {
        Id = item.Id, Title = item.Title, FolderId = item.FolderId, Grade = item.Grade,
        FileName = item.FileName, StoragePath = item.StoragePath, Archived = item.Archived, Revision = item.Revision
    };
    void ClosePopups()
    {
        editPopup.SetActive(false); archivePopup.SetActive(false); deletePopup.SetActive(false); replacement = null;
    }
    async Task ReloadModules()
    {
        modules = await store.Modules(currentFolder); lifetime.Token.ThrowIfCancellationRequested(); RenderModules();
    }
    void RenderModules()
    {
        if (currentFolder == null) return;
        ClearRows();
        var items = modules.Where(m => m.Archived == showingArchive && (filter.value == 0 || m.Grade == filter.value)
            && m.Title.IndexOf(search.text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        Message(folders.First(f => f.Id == currentFolder).Name + (showingArchive ? " — Archived" : "") + (items.Count == 0 ? " — No modules" : ""));
        float width = scroll.viewport.rect.width;
        content.sizeDelta = new Vector2(width, Math.Max(scroll.viewport.rect.height, items.Count * 100));
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i]; var row = Instantiate(template, content); row.SetActive(true); row.name = "Module_" + item.Id;
            var rect = (RectTransform)row.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(0, -i * 100); rect.sizeDelta = new Vector2(width, 90);
            var textObject = new GameObject("Module title and grade", typeof(RectTransform)); textObject.transform.SetParent(rect, false);
            var label = textObject.AddComponent<TextMeshProUGUI>(); label.font = status.font; label.fontSize = 22;
            label.color = new Color(0.25f, 0.12f, 0.04f); label.richText = false; label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = 14;
            label.text = item.Title + "\nGrade " + item.Grade; label.overflowMode = TextOverflowModes.Ellipsis; label.raycastTarget = false;
            var labelRect = label.rectTransform; labelRect.anchorMin = new Vector2(0, 0); labelRect.anchorMax = new Vector2(1, 1);
            // BlankField's supplied sprite includes transparent padding. Keep
            // content inside its visible field without modifying the sprite.
            labelRect.offsetMin = new Vector2(40, 15); labelRect.offsetMax = new Vector2(-200, -38);
            RowButton(rect, viewIcon, -164, () => Run(async () =>
            { Message("Opening PDF..."); var path = await store.Download(item, lifetime.Token); lifetime.Token.ThrowIfCancellationRequested(); TeacherPdfPicker.Open(path); RenderModules(); }));
            RowButton(rect, editIcon, -126, () => OpenEdit(item));
            RowButton(rect, archiveIcon, -88, () =>
            {
                if (item.Archived)
                {
                    Run(async () =>
                    {
                        var restored = Copy(item); restored.Archived = false;
                        await store.Save(restored, null, lifetime.Token, null);
                        lifetime.Token.ThrowIfCancellationRequested(); await ReloadModules();
                    });
                    return;
                }
                selected = item; archivePopup.SetActive(true);
                At<TMP_Text>("ArchivePopUp/Text (TMP)").text = item.Archived ? "Restore this item?" : "Archive this item?";
                At<TMP_Text>("ArchivePopUp/Text (TMP) (1)").text = item.Archived ? "Restore this module to its folder?" : "Are you sure you want to archive this item?";
                At<TMP_Text>("ArchivePopUp/Text (TMP) (2)").text = item.Archived ? "It will appear in your active modules." : "You can restore it later.";
            });
            RowButton(rect, deleteIcon, -50, () => { selected = item; deletePopup.SetActive(true); });
            generated.Add(row);
        }
        scroll.verticalNormalizedPosition = 1;
    }
    void RowButton(RectTransform parent, Sprite sprite, float x, UnityAction action)
    {
        var go = new GameObject("Module action", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(1, .5f); rect.sizeDelta = new Vector2(32, 32); rect.anchoredPosition = new Vector2(x, -12);
        go.GetComponent<Image>().sprite = sprite; go.GetComponent<Image>().preserveAspect = true;
        go.GetComponent<Button>().onClick.AddListener(() => { if (!busy) action(); });
    }
    void OpenEdit(TeacherPdfModule item)
    {
        selected = item; replacement = null; editPopup.SetActive(true); editTitle.text = item.Title;
        editFolder.ClearOptions(); editFolder.AddOptions(folders.Select(f => f.Name).ToList());
        editFolder.SetValueWithoutNotify(Math.Max(0, folders.FindIndex(f => f.Id == item.FolderId))); editFolder.RefreshShownValue();
        editGrade.SetValueWithoutNotify(item.Grade - 1); editGrade.RefreshShownValue(); fileName.richText = false; fileName.text = item.FileName;
    }
    void OnDestroy() { lifetime.Cancel(); lifetime.Dispose(); }
}
