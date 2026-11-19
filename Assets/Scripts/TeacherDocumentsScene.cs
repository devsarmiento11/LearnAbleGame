using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TeacherDocumentsScene : MonoBehaviour
{
    readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    readonly List<GameObject> rows = new List<GameObject>();
    ListenerRegistration documentsListener, studentsListener;
    QuerySnapshot documents, students;
    ScrollRect scroll;
    RectTransform template;
    ParentDocumentPreview preview;
    TMP_Text style, status;
    Button download;
    StudentPdfDownload exporter;
    Vector2 firstPosition;
    float spacing, rowHeight;
    bool opening;
    string uid, localPath, fileName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= Attach;
        SceneManager.sceneLoaded += Attach;
    }

    static void Attach(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "DocumentsTeacher") return;
        var scroll = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ScrollRect>(true))
            .FirstOrDefault(s => s.name == "Scroll View");
        if (scroll != null) scroll.gameObject.AddComponent<TeacherDocumentsScene>();
    }

    static Button ButtonFor(Transform target)
    {
        var button = target.GetComponent<Button>();
        if (button != null) return button;
        button = target.gameObject.AddComponent<Button>();
        button.targetGraphic = target.GetComponent<Graphic>();
        button.transition = Selectable.Transition.None;
        if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
        return button;
    }

    async void Start()
    {
        try
        {
            scroll = GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            var samples = transform.Cast<Transform>().Where(t => t.name.StartsWith("Data", StringComparison.Ordinal))
                .OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
            template = (RectTransform)samples[0];
            Vector3 origin = scroll.content.InverseTransformPoint(template.position);
            firstPosition = new Vector2(origin.x - scroll.content.rect.xMin, origin.y - scroll.content.rect.yMax);
            spacing = Mathf.Abs(template.anchoredPosition.y - ((RectTransform)samples[1]).anchoredPosition.y);
            rowHeight = template.rect.height;
            style = template.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "NameOfTheChildren");
            foreach (var sample in samples) sample.gameObject.SetActive(false);
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewportGraphic = scroll.viewport.GetComponent<Graphic>();
            if (viewportGraphic != null) viewportGraphic.raycastTarget = true;

            status = Instantiate(style, scroll.viewport, false);
            status.gameObject.SetActive(true);
            status.rectTransform.anchorMin = Vector2.zero;
            status.rectTransform.anchorMax = Vector2.one;
            status.rectTransform.offsetMin = status.rectTransform.offsetMax = Vector2.zero;
            status.alignment = TextAlignmentOptions.Center;
            status.raycastTarget = false;
            status.richText = false;
            status.text = "Loading documents…";

            var popup = GetComponentInParent<Canvas>().GetComponentsInChildren<Transform>(true)
                .First(t => t.name == "ViewPopup");
            preview = popup.gameObject.AddComponent<ParentDocumentPreview>();
            ButtonFor(popup.Find("CloseBtn")).onClick.AddListener(preview.Close);
            download = ButtonFor(popup.Find("DownloadBtn"));
            exporter = gameObject.AddComponent<StudentPdfDownload>();
            download.onClick.AddListener(Download);
            popup.gameObject.SetActive(false);

            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null || LoginSession.Role != "teacher")
                throw new InvalidOperationException("Sign in as a teacher to view documents.");
            uid = user.UserId;
            await StudentModuleStore.Read(user.TokenAsync(true), lifetime.Token);
            if (this == null) return;
            var db = FirebaseFirestore.DefaultInstance;
            documentsListener = db.Collection("parentDocuments").Listen(snapshot => { documents = snapshot; Refresh(); });
            studentsListener = db.Collection("users").WhereEqualTo("role", "student")
                .Listen(snapshot => { students = snapshot; Refresh(); });
            Watch(documentsListener);
            Watch(studentsListener);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) ShowError(error); }
    }

    async void Watch(ListenerRegistration listener)
    {
        try { await listener.ListenerTask; }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) ShowError(error); }
    }

    static string Field(DocumentSnapshot doc, string key) =>
        doc.TryGetValue<string>(key, out var value) ? value?.Trim() ?? "" : "";

    void Refresh()
    {
        if (this == null || documents == null || students == null) return;
        var byId = students.Documents.ToDictionary(student => student.Id);
        var entries = documents.Documents.Where(doc => byId.ContainsKey(Field(doc, "ChildId")))
            .OrderByDescending(doc => doc.TryGetValue<Timestamp>("UploadedAt", out var time) ? time.ToDateTime() : DateTime.MinValue)
            .ThenBy(doc => doc.Id, StringComparer.Ordinal).ToArray();
        foreach (var row in rows) { row.SetActive(false); Destroy(row); }
        rows.Clear();
        float height = Mathf.Max(scroll.viewport.rect.height, -firstPosition.y +
            Mathf.Max(0, entries.Length - 1) * spacing + rowHeight * template.pivot.y);
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        for (int index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var student = byId[Field(entry, "ChildId")];
            var row = Instantiate(template, scroll.content, false);
            row.anchorMin = row.anchorMax = new Vector2(0, 1);
            row.anchoredPosition = firstPosition - new Vector2(0, index * spacing);
            var labels = row.GetComponentsInChildren<TMP_Text>(true);
            var name = string.Join(" ", new[] { "firstName", "middleName", "lastName" }
                .Select(key => Field(student, key)).Where(value => value.Length > 0));
            var nameLabel = labels.First(t => t.name == "NameOfTheChildren");
            nameLabel.richText = false;
            nameLabel.text = name.Length > 0 ? name : "Student";
            var gradeLabel = labels.First(t => t.name == "GradeOfTheChildren");
            gradeLabel.richText = false;
            gradeLabel.text = student.TryGetValue<object>("grade", out var grade) && grade != null
                ? grade.ToString() : "—";
            ButtonFor(row.Find("ViewButtonOfTheDocument")).onClick.AddListener(() => Open(entry));
            row.gameObject.SetActive(true);
            rows.Add(row.gameObject);
        }
        status.text = entries.Length == 0 ? "No parent documents yet." : "";
    }

    async void Open(DocumentSnapshot entry)
    {
        if (opening) return;
        opening = true;
        CancellationToken token = lifetime.Token;
        string pendingPath = null;
        try
        {
            var user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null || user.UserId != uid) throw new InvalidOperationException("Sign in again.");
            string remote = Field(entry, "StoragePath");
            string prefix = "parentDocuments/" + entry.Id + "/" + Field(entry, "ChildId") + "/";
            if (!remote.StartsWith(prefix, StringComparison.Ordinal) || remote.Substring(prefix.Length).Contains("/"))
                throw new InvalidOperationException("Invalid document reference.");
            ParentDocumentFile.ContentType(remote);
            string idToken = await StudentModuleStore.Read(user.TokenAsync(true), token);
            string directory = StudentPdfDownload.DownloadDirectory();
            Directory.CreateDirectory(directory);
            pendingPath = Path.Combine(directory, Guid.NewGuid().ToString("N") + Path.GetExtension(remote));
            string bucket = FirebaseApp.DefaultInstance.Options.StorageBucket;
            string url = "https://firebasestorage.googleapis.com/v0/b/" + Uri.EscapeDataString(bucket)
                + "/o/" + Uri.EscapeDataString(remote) + "?alt=media";
            using (var request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerFile(pendingPath) { removeFileOnAbort = true };
                request.SetRequestHeader("Authorization", "Firebase " + idToken);
                request.timeout = 60;
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (token.IsCancellationRequested) { request.Abort(); token.ThrowIfCancellationRequested(); }
                    await System.Threading.Tasks.Task.Yield();
                }
                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException("Unable to open this document. Refresh the list and try again.");
            }
            token.ThrowIfCancellationRequested();
            ParentDocumentFile.Validate(pendingPath);
            preview.Close();
            if (localPath != null) File.Delete(localPath);
            localPath = pendingPath;
            pendingPath = null;
            fileName = Field(entry, "FileName");
            status.text = "";
            preview.transform.SetAsLastSibling();
            await preview.Open(localPath, style, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null) ShowError(error); }
        finally
        {
            if (pendingPath != null) File.Delete(pendingPath);
            opening = false;
        }
    }

    async void Download()
    {
        if (opening || string.IsNullOrEmpty(localPath)) return;
        opening = true;
        try { await exporter.Save(localPath, fileName); }
        catch (Exception error) { if (this != null) ShowError(error); }
        finally { opening = false; }
    }

    void ShowError(Exception error)
    {
        Debug.LogWarning(error.Message, this);
        if (status != null) status.text = error.Message;
    }

    void OnDestroy()
    {
        documentsListener?.Stop();
        studentsListener?.Stop();
        lifetime.Cancel();
        lifetime.Dispose();
    }
}
