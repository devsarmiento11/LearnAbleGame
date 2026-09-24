using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Both student scenes reuse their authored icons, labels, buttons and backgrounds.
public sealed class StudentModuleScene : MonoBehaviour
{
    readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    readonly List<GameObject> generated = new List<GameObject>();
    StudentModuleStore store;
    StudentModuleFolder folder;
    StudentPdfDownload exporter;
    ScrollRect scroll;
    RectTransform content;
    GameObject template;
    TMP_Text status;
    Vector2[] slots;
    float pageHeight;
    bool folderScene, busy, ready;

    void Start()
    {
        if (!enabled) return;
        try { Setup(); Reload(); }
        catch (Exception error) { Debug.LogException(error); Message("Unable to prepare module controls."); }
    }

    void Setup()
    {
        folderScene = gameObject.scene.name == "OtherModuleFolderScene";
        scroll = transform.Find(folderScene ? "Module ScrollView" : "Existing Folder ScrollView").GetComponent<ScrollRect>();
        var viewport = (RectTransform)scroll.transform.Find("Viewport");
        content = (RectTransform)viewport.Find("Content");
        var container = folderScene ? transform.Find("Modules") : scroll.transform.Find("Folders");
        Canvas.ForceUpdateCanvases();
        var originals = container.Cast<Transform>().OrderByDescending(t => Mathf.Round(t.position.y * 10))
            .ThenBy(t => t.position.x).ToArray();
        template = originals[0].gameObject;
        slots = originals.Select(t => (Vector2)viewport.InverseTransformPoint(t.position)
            - new Vector2(viewport.rect.xMin, viewport.rect.yMax)).ToArray();
        var label = template.GetComponentInChildren<TMP_Text>(true);

        // The six folder labels extend just below the authored mask. Expand only
        // its invisible lower edge so all six original positions fit on page one.
        float bottom = slots.Min(p => p.y) + label.rectTransform.anchoredPosition.y - label.rectTransform.rect.height / 2;
        float required = Mathf.Max(viewport.rect.height, -bottom + 8);
        viewport.offsetMin -= new Vector2(0, required - viewport.rect.height);
        Canvas.ForceUpdateCanvases();
        pageHeight = viewport.rect.height;
        foreach (var original in originals) original.gameObject.SetActive(false);
        foreach (Transform child in content) child.gameObject.SetActive(false);
        foreach (var layout in content.GetComponents<LayoutGroup>()) layout.enabled = false;
        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(viewport.rect.width, pageHeight);
        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true; scroll.scrollSensitivity = 35;
        var hitArea = viewport.GetComponent<Image>();
        if (hitArea == null) hitArea = viewport.gameObject.AddComponent<Image>();
        var mask = viewport.GetComponent<Mask>();
        // A stencil mask needs an opaque graphic even when that graphic is hidden.
        hitArea.color = mask != null ? Color.white : Color.clear;
        hitArea.raycastTarget = true;
        if (mask != null) mask.showMaskGraphic = false;
        if (mask == null && viewport.GetComponent<RectMask2D>() == null)
            viewport.gameObject.AddComponent<RectMask2D>();
        // Decorative graphics must not cover the ScrollView's touch surface.
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            if (graphic.transform != viewport && graphic.GetComponentInParent<Selectable>(true) == null)
                graphic.raycastTarget = false;

        // Transient messages use the existing label's font and style, with no new
        // permanent controls or artwork. Tapping an error/empty state retries.
        status = Instantiate(label, viewport);
        status.name = "ModuleStatus"; status.gameObject.SetActive(true);
        status.rectTransform.anchorMin = status.rectTransform.anchorMax = new Vector2(.5f, .5f);
        status.rectTransform.anchoredPosition = Vector2.zero;
        status.rectTransform.sizeDelta = new Vector2(viewport.rect.width - 40, 100);
        status.richText = false; status.enableAutoSizing = true;
        status.fontSizeMin = 14; status.fontSizeMax = label.fontSize;
        status.raycastTarget = true;
        var retry = status.gameObject.AddComponent<Button>();
        retry.targetGraphic = status; retry.transition = Selectable.Transition.None;
        retry.onClick.AddListener(Reload);
        var exportObject = new GameObject("StudentPdfDownload_" + Guid.NewGuid().ToString("N"));
        exportObject.transform.SetParent(transform, false);
        exporter = exportObject.AddComponent<StudentPdfDownload>();
        ready = true;
    }

    void Message(string message)
    {
        if (status == null) return;
        status.text = message;
        status.gameObject.SetActive(!string.IsNullOrEmpty(message));
        status.transform.SetAsLastSibling();
    }

    async void Reload()
    {
        if (busy || !ready) return;
        busy = true;
        try
        {
            Message("Loading modules...");
            store = await StudentModuleStore.Open(lifetime.Token);
            if (folderScene)
            {
                folder = StudentModuleStore.SelectedBy == store.UserId ? StudentModuleStore.SelectedFolder : null;
                if (folder == null) { SceneManager.LoadScene("OtherModulesScene"); return; }
                var modules = await store.Modules(folder, lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested(); RenderModules(modules);
            }
            else
            {
                var folders = await store.Folders(lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested(); RenderFolders(folders);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (this != null)
            {
                Clear();
                Message(ErrorMessage(error) + "\nTap here to retry.");
                Debug.LogWarning("Student modules: " + error.GetBaseException().Message);
            }
        }
        finally { if (this != null) busy = false; }
    }

    static string ErrorMessage(Exception error)
    {
        var root = error.GetBaseException();
        if (root is InvalidOperationException) return root.Message;
        if (root is Firebase.Firestore.FirestoreException firestore && firestore.ErrorCode == Firebase.Firestore.FirestoreError.PermissionDenied)
            return "Module access is unavailable. Ask your teacher to check your account and module permissions.";
        return "Unable to load modules. Check your internet connection.";
    }

    void Clear()
    {
        foreach (var row in generated) { row.SetActive(false); Destroy(row); }
        generated.Clear();
    }

    void BeginRows(int count)
    {
        Clear();
        // Six folders (two rows) or three PDFs (one row) per visible page.
        int capacity = folderScene ? 3 : 6;
        content.sizeDelta = new Vector2(scroll.viewport.rect.width, Mathf.Max(1, Mathf.CeilToInt(count / (float)capacity)) * pageHeight);
        scroll.StopMovement(); content.anchoredPosition = Vector2.zero;
        scroll.verticalNormalizedPosition = 1;
        Message("");
    }

    Button AddRow(int index, string name, string caption)
    {
        var row = Instantiate(template, content); row.name = name;
        var rect = (RectTransform)row.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.anchoredPosition = slots[index % slots.Length] - new Vector2(0, index / slots.Length * pageHeight);
        var label = row.GetComponentInChildren<TMP_Text>(true);
        label.richText = false; label.text = caption;
        label.enableAutoSizing = true; label.fontSizeMin = 12;
        label.overflowMode = TextOverflowModes.Ellipsis;
        // Module labels were wider than the space between icons. Constrain text
        // to its existing column to keep long teacher titles from overlapping.
        if (folderScene) label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 220);
        else
        {
            // Match the authored folder captions: auto-sizing otherwise enlarges
            // short names towards the template's 72-point maximum independently.
            label.enableAutoSizing = false;
            label.fontSize = template.GetComponentInChildren<TMP_Text>(true).fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.anchoredPosition = new Vector2(0, label.rectTransform.anchoredPosition.y);
        }
        var button = row.GetComponent<Button>() ?? row.AddComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.targetGraphic = row.GetComponent<Image>();
        foreach (var graphic in row.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = true;
        row.SetActive(true); generated.Add(row);
        return button;
    }

    void RenderFolders(List<StudentModuleFolder> folders)
    {
        BeginRows(folders.Count);
        for (int i = 0; i < folders.Count; i++)
        {
            var item = folders[i];
            AddRow(i, "Folder_" + item.Owner + "_" + item.Id, item.Name).onClick.AddListener(() =>
            {
                if (busy) return;
                StudentModuleStore.SelectedFolder = item; StudentModuleStore.SelectedBy = store.UserId;
                SceneManager.LoadScene("OtherModuleFolderScene");
            });
        }
        if (folders.Count == 0) Message("No folders have been uploaded yet.\nTap to refresh.");
    }

    void RenderModules(List<TeacherPdfModule> modules)
    {
        // Defence in depth: never render wrong-grade, archived or foreign-folder rows.
        var items = modules.Where(m => m.Grade == store.Grade && !m.Archived && m.FolderId == folder.Id).ToList();
        BeginRows(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var button = AddRow(i, "Module_" + item.Id, item.Title);
            var label = button.GetComponentInChildren<TMP_Text>(true);
            button.onClick.AddListener(() => Download(item, label));
        }
        if (items.Count == 0) Message("No Grade " + store.Grade + " modules in this folder yet.\nTap to refresh.");
    }

    async void Download(TeacherPdfModule module, TMP_Text label)
    {
        if (busy) return;
        busy = true;
        string path = null;
        try
        {
            label.text = "Downloading...";
            path = await store.Download(folder, module.Id, lifetime.Token,
                value => { if (label != null) label.text = "Downloading " + Mathf.FloorToInt(value * 100) + "%"; });
            lifetime.Token.ThrowIfCancellationRequested();
            label.text = "Saving PDF...";
            string saved = await exporter.Save(path, module.FileName);
            lifetime.Token.ThrowIfCancellationRequested();
            label.text = string.IsNullOrEmpty(saved) ? module.Title + "\nSave cancelled" : module.Title + "\nDownloaded";
            if (!string.IsNullOrEmpty(saved)) Debug.Log("Module saved: " + saved);
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (label != null) label.text = module.Title + "\nDownload failed — tap to retry";
            Debug.LogWarning("Module download: " + error.GetBaseException().Message);
        }
        finally
        {
            if (path != null && File.Exists(path)) File.Delete(path);
            if (this != null) busy = false;
        }
    }

    void OnApplicationFocus(bool focused) { if (focused && ready && !busy) Reload(); }
    void OnDestroy() { lifetime.Cancel(); lifetime.Dispose(); }
}
