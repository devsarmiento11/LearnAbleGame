using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Networking;

public sealed class StudentModuleFolder
{
    public string Owner, Id, Name;
}

// Reads the same library as UploadModuleScene; no copied catalogue can go stale.
public sealed class StudentModuleStore
{
    public static StudentModuleFolder SelectedFolder;
    public static string SelectedBy;
    readonly string uid, profileId;
    public string UserId => uid;
    public int Grade { get; private set; }
    FirebaseFirestore Database => FirebaseFirestore.DefaultInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSelection() { SelectedFolder = null; SelectedBy = null; }

    StudentModuleStore(string user, string profile) { uid = user; profileId = profile; }

    public static int ParseGrade(object value)
    {
        string text = Convert.ToString(value)?.Trim() ?? "";
        if (text.StartsWith("Grade ", StringComparison.Ordinal)) text = text.Substring(6);
        return int.TryParse(text, out int grade) && grade >= 1 && grade <= 6 ? grade : 0;
    }

    public static async Task<StudentModuleStore> Open(CancellationToken cancellation)
    {
        if (await Read(FirebaseApp.CheckAndFixDependenciesAsync(), cancellation) != DependencyStatus.Available)
            throw new InvalidOperationException("Firebase is unavailable on this device.");
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null || string.IsNullOrWhiteSpace(user.DisplayName) || user.DisplayName.Contains("/"))
            throw new InvalidOperationException("Sign in with your student account to see modules.");
        var store = new StudentModuleStore(user.UserId, user.DisplayName);
        await store.VerifyProfile(cancellation);
        return store;
    }

    void CheckSession()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser?.UserId != uid ||
            (LoginSession.IsLoggedIn && (LoginSession.Role != "student" || LoginSession.StudentId != profileId)))
            throw new InvalidOperationException("Your session changed. Sign in again to see modules.");
    }

    async Task VerifyProfile(CancellationToken cancellation)
    {
        CheckSession();
        var profile = await Read(Database.Collection("users").Document(profileId).GetSnapshotAsync(Source.Server), cancellation);
        CheckSession();
        if (!profile.Exists || !profile.TryGetValue<string>("authUid", out var owner) || owner != uid ||
            !profile.TryGetValue<string>("role", out var role) || role != "student" ||
            !profile.TryGetValue<string>("status", out var status) || status != "Active")
            throw new InvalidOperationException("An active student account is required to see modules.");
        Grade = profile.TryGetValue<object>("grade", out var grade) ? ParseGrade(grade) : 0;
        if (Grade == 0) throw new InvalidOperationException("Your grade level is missing. Ask your teacher to update your profile.");
    }

    public async Task<List<StudentModuleFolder>> Folders(CancellationToken cancellation)
    {
        await VerifyProfile(cancellation);
        // Library parent documents need not exist: teachers originally created only
        // folders/modules subcollections. Discover owners from the teacher profiles.
        var teachers = await Read(Database.Collection("users").WhereEqualTo("role", "teacher").GetSnapshotAsync(Source.Server), cancellation);
        var owners = teachers.Documents.Select(d => d.TryGetValue<string>("authUid", out var id) ? id : null)
            .Where(id => !string.IsNullOrEmpty(id) && !id.Contains("/")).Distinct();
        var folders = new List<StudentModuleFolder>();
        foreach (string owner in owners)
        {
            var result = await Read(Database.Collection("teacherLibraries").Document(owner).Collection("folders").GetSnapshotAsync(Source.Server), cancellation);
            CheckSession();
            folders.AddRange(result.Documents.Select(d => new StudentModuleFolder
            { Owner = owner, Id = d.Id, Name = d.GetValue<string>("Name") }));
        }
        return folders.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ThenBy(f => f.Owner).ThenBy(f => f.Id).ToList();
    }

    DocumentReference Library(StudentModuleFolder folder)
    {
        CheckSession();
        if (folder == null || string.IsNullOrEmpty(folder.Owner) || string.IsNullOrEmpty(folder.Id) ||
            folder.Owner.Contains("/") || folder.Id.Contains("/"))
            throw new InvalidOperationException("Go back and select a module folder.");
        return Database.Collection("teacherLibraries").Document(folder.Owner);
    }

    public async Task<List<TeacherPdfModule>> Modules(StudentModuleFolder folder, CancellationToken cancellation)
    {
        await VerifyProfile(cancellation);
        var library = Library(folder);
        var savedFolder = await Read(library.Collection("folders").Document(folder.Id).GetSnapshotAsync(Source.Server), cancellation);
        if (!savedFolder.Exists) throw new InvalidOperationException("This folder is no longer available. Go back and choose another folder.");
        folder.Name = savedFolder.GetValue<string>("Name");
        // Only equality filters: existing single-field indexes suffice. Firestore
        // rules enforce both constraints, including for modified clients.
        var snapshot = await Read(library.Collection("modules").WhereEqualTo("Grade", Grade)
            .WhereEqualTo("Archived", false).GetSnapshotAsync(Source.Server), cancellation);
        CheckSession();
        return snapshot.Documents.Select(d => d.ConvertTo<TeacherPdfModule>())
            .Where(m => m.FolderId == folder.Id && m.Grade == Grade && !m.Archived)
            .OrderBy(m => m.Title, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Id).ToList();
    }

    public async Task<string> Download(StudentModuleFolder folder, string moduleId, CancellationToken cancellation, Action<float> progress)
    {
        await VerifyProfile(cancellation);
        var snapshot = await Read(Library(folder).Collection("modules").Document(moduleId).GetSnapshotAsync(Source.Server), cancellation);
        CheckSession();
        if (!snapshot.Exists) throw new InvalidOperationException("This module is no longer available.");
        var module = snapshot.ConvertTo<TeacherPdfModule>();
        if (module.Archived || module.Grade != Grade || module.FolderId != folder.Id)
            throw new InvalidOperationException("This module is no longer available for your grade.");
        string bucket = FirebaseApp.DefaultInstance.Options.StorageBucket;
        if (string.IsNullOrEmpty(bucket)) throw new InvalidOperationException("Module storage is not configured.");
        if (string.IsNullOrEmpty(module.StoragePath) || !module.StoragePath.StartsWith("teacherModules/" + folder.Owner + "/" + module.Id + "/", StringComparison.Ordinal))
            throw new InvalidOperationException("The module has an invalid file reference. Ask your teacher to upload it again.");
        string token = await Read(FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(false), cancellation);
        CheckSession();
        string directory = Path.Combine(Application.temporaryCachePath, "module-pdfs");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            using (var request = UnityWebRequest.Get("https://firebasestorage.googleapis.com/v0/b/" + Uri.EscapeDataString(bucket)
                + "/o/" + Uri.EscapeDataString(module.StoragePath) + "?alt=media"))
            {
                request.SetRequestHeader("Authorization", "Firebase " + token);
                request.downloadHandler = new DownloadHandlerFile(path) { removeFileOnAbort = true };
                request.timeout = 120;
                try
                {
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        cancellation.ThrowIfCancellationRequested(); CheckSession();
                        if (request.downloadedBytes > (ulong)TeacherModuleStore.MaxPdfBytes)
                            throw new InvalidOperationException("This PDF exceeds the 25 MB module limit.");
                        progress?.Invoke(Mathf.Clamp01(request.downloadProgress));
                        await Task.Yield();
                    }
                    cancellation.ThrowIfCancellationRequested(); CheckSession();
                    if (request.result != UnityWebRequest.Result.Success)
                        throw new InvalidOperationException(request.responseCode == 403 || request.responseCode == 401
                            ? "Download access denied. Sign in again or ask your teacher to check module permissions."
                            : request.responseCode == 404 ? "This PDF is no longer available. Ask your teacher to upload it again."
                            : "Download failed. Check your internet connection and tap the module to retry.");
                }
                finally { if (!request.isDone) request.Abort(); }
            }
            TeacherModuleStore.ValidatePdf(path);
            return path;
        }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
    }

    internal static async Task<T> Read<T>(Task<T> task, CancellationToken cancellation)
    {
        if (await Task.WhenAny(task, Task.Delay(15000, cancellation)) != task)
        {
            _ = task.ContinueWith(t => { var observed = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            cancellation.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Connection timed out. Check your internet and tap to retry.");
        }
        cancellation.ThrowIfCancellationRequested();
        return await task;
    }
}
