using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Networking;

[FirestoreData]
public sealed class TeacherModuleFolder
{
    [FirestoreDocumentId] public string Id { get; set; }
    [FirestoreProperty] public string Name { get; set; }
}

[FirestoreData]
public sealed class TeacherPdfModule
{
    [FirestoreDocumentId] public string Id { get; set; }
    [FirestoreProperty] public string Title { get; set; }
    [FirestoreProperty] public string FolderId { get; set; }
    [FirestoreProperty] public int Grade { get; set; }
    [FirestoreProperty] public string FileName { get; set; }
    [FirestoreProperty] public string StoragePath { get; set; }
    [FirestoreProperty] public bool Archived { get; set; }
    [FirestoreProperty] public int Revision { get; set; }
}

// These collections are deliberately separate from profiles, grades and scores.
public sealed class TeacherModuleStore
{
    public const long MaxPdfBytes = 25 * 1024 * 1024;
    public static string SelectedFolderId;
    public static string SelectedOwner;
    readonly string uid;
    readonly DocumentReference library;
    readonly string bucket;
    public string Owner => uid;

    TeacherModuleStore(string verifiedUid)
    {
        uid = verifiedUid;
        library = FirebaseFirestore.DefaultInstance.Collection("teacherLibraries").Document(uid);
        bucket = FirebaseApp.DefaultInstance.Options.StorageBucket;
    }

    public static async Task<TeacherModuleStore> OpenAsync(CancellationToken cancellation)
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null)
            throw new InvalidOperationException("Sign in through LoginScene with your teacher account, then open Upload Modules.");
        if (LoginSession.IsLoggedIn && LoginSession.Role != "teacher")
            throw new InvalidOperationException("Only teacher accounts can manage modules.");

        // Firebase Auth persists across Editor Play sessions, unlike LoginSession's
        // static fields. Never trust saved PlayerPrefs or the display name alone.
        string profileId = user.DisplayName;
        if (string.IsNullOrWhiteSpace(profileId) || profileId.Contains("/"))
            throw new InvalidOperationException("Sign in again through LoginScene to verify your teacher account.");
        string authUid = user.UserId;
        var profile = await FirebaseFirestore.DefaultInstance.Collection("users").Document(profileId).GetSnapshotAsync(Source.Server);
        cancellation.ThrowIfCancellationRequested();
        if (FirebaseAuth.DefaultInstance.CurrentUser?.UserId != authUid)
            throw new InvalidOperationException("Your session changed. Please sign in again.");
        if (!profile.Exists || !profile.TryGetValue<string>("authUid", out var owner) || owner != authUid ||
            !profile.TryGetValue<string>("role", out var role) || role != "teacher")
            throw new InvalidOperationException("This signed-in account is not a verified teacher.");
        return new TeacherModuleStore(authUid);
    }

    void CheckSession()
    {
        if ((LoginSession.IsLoggedIn && LoginSession.Role != "teacher") || FirebaseAuth.DefaultInstance.CurrentUser?.UserId != uid)
            throw new InvalidOperationException("Your session changed. Sign in again before continuing.");
    }

    public static string ValidName(string value)
    {
        value = (value ?? "").Trim();
        if (value.Length == 0 || value.Length > 100 || value.Any(char.IsControl))
            throw new InvalidOperationException("Enter a name between 1 and 100 characters.");
        return value;
    }

    public async Task<List<TeacherModuleFolder>> Folders()
    {
        CheckSession();
        var result = await library.Collection("folders").GetSnapshotAsync(Source.Server);
        CheckSession();
        return result.Documents.Select(d => d.ConvertTo<TeacherModuleFolder>()).OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<string> CreateFolder(string name)
    {
        CheckSession(); name = ValidName(name);
        string id;
        using (var hash = SHA256.Create())
            id = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(name.Normalize().ToUpperInvariant()))).Replace("-", "").ToLowerInvariant();
        var reference = library.Collection("folders").Document(id);
        await FirebaseFirestore.DefaultInstance.RunTransactionAsync(async transaction =>
        {
            var existing = await transaction.GetSnapshotAsync(reference);
            if (existing.Exists) throw new InvalidOperationException("A folder with this name already exists.");
            transaction.Set(reference, new TeacherModuleFolder { Name = name });
        });
        CheckSession(); return id;
    }

    public async Task<List<TeacherPdfModule>> Modules(string folder)
    {
        CheckSession();
        var result = await library.Collection("modules").WhereEqualTo("FolderId", folder).GetSnapshotAsync(Source.Server);
        CheckSession();
        return result.Documents.Select(d => d.ConvertTo<TeacherPdfModule>()).OrderBy(d => d.Title, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static void ValidatePdf(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException("The selected file is no longer available.");
        var info = new FileInfo(path);
        if (!string.Equals(info.Extension, ".pdf", StringComparison.OrdinalIgnoreCase) || info.Length < 5 || info.Length > MaxPdfBytes)
            throw new InvalidOperationException("Select a PDF file no larger than 25 MB.");
        using (var file = File.OpenRead(path))
        {
            byte[] header = new byte[5];
            if (file.Read(header, 0, 5) != 5 || Encoding.ASCII.GetString(header) != "%PDF-")
                throw new InvalidOperationException("This file is not a valid PDF.");
        }
    }

    string ObjectUrl(string path) => "https://firebasestorage.googleapis.com/v0/b/" + Uri.EscapeDataString(bucket) + "/o/" + Uri.EscapeDataString(path);

    internal static string TransferError(long status, bool uploading, bool connectionError)
    {
        if (connectionError) return "Cannot reach PDF storage. Check your internet connection and try again.";
        if (status == 401) return "Your sign-in expired. Sign in again before uploading a module.";
        if (status == 403) return "PDF storage access was denied. Ask the administrator to check teacher permissions and Storage setup.";
        if (status == 404) return uploading
            ? "PDF storage has not been set up. Ask the administrator to enable Firebase Storage."
            : "This PDF is no longer available in storage. Replace the module's PDF and try again.";
        if (status == 412) return "PDF storage is unavailable. Ask the administrator to check Firebase Storage and billing.";
        if (status == 413) return "The PDF is too large. Select a PDF no larger than 25 MB.";
        if (status == 429 || status >= 500) return "PDF storage is temporarily busy. Please try again shortly.";
        return "The PDF transfer failed (" + status + "). Please try again or contact the administrator.";
    }

    async Task Send(UnityWebRequest request, CancellationToken cancellation, Action<float> progress = null)
    {
        CheckSession();
        string token = await FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(false);
        CheckSession(); cancellation.ThrowIfCancellationRequested();
        request.SetRequestHeader("Authorization", "Firebase " + token);
        request.timeout = 120;
        try
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                cancellation.ThrowIfCancellationRequested(); CheckSession();
                progress?.Invoke(Mathf.Clamp01(request.uploadProgress) * 0.95f);
                await Task.Yield();
            }
            cancellation.ThrowIfCancellationRequested();
            if (request.result != UnityWebRequest.Result.Success)
            {
                // Log transport status without exposing authorization headers or PDF contents.
                Debug.LogWarning("Module PDF transfer: HTTP " + request.responseCode + ", " + request.error);
                throw new InvalidOperationException(TransferError(request.responseCode, request.method == "POST",
                    request.result == UnityWebRequest.Result.ConnectionError));
            }
        }
        finally { if (!request.isDone) request.Abort(); }
    }

    public async Task Save(TeacherPdfModule module, string replacement, CancellationToken cancellation, Action<float> progress)
    {
        CheckSession(); module.Title = ValidName(module.Title);
        if (!string.IsNullOrEmpty(replacement) && string.IsNullOrEmpty(bucket))
            throw new InvalidOperationException("PDF Storage is not configured yet. You can still create folders.");
        if (module.Grade < 1 || module.Grade > 6) throw new InvalidOperationException("Select a grade from 1 to 6.");
        if (string.IsNullOrEmpty(module.FolderId)) throw new InvalidOperationException("Create and select a folder first.");
        if (!(await library.Collection("folders").Document(module.FolderId).GetSnapshotAsync(Source.Server)).Exists)
            throw new InvalidOperationException("The selected folder no longer exists.");
        string oldPath = module.StoragePath;
        string newPath = null;
        var reference = string.IsNullOrEmpty(module.Id) ? library.Collection("modules").Document() : library.Collection("modules").Document(module.Id);
        int expectedRevision = module.Revision;
        try
        {
            if (!string.IsNullOrEmpty(replacement))
            {
                ValidatePdf(replacement);
                newPath = "teacherModules/" + uid + "/" + reference.Id + "/" + Guid.NewGuid().ToString("N") + ".pdf";
                using (var request = new UnityWebRequest("https://firebasestorage.googleapis.com/v0/b/" + Uri.EscapeDataString(bucket) + "/o?name=" + Uri.EscapeDataString(newPath), "POST"))
                {
                    request.uploadHandler = new UploadHandlerFile(replacement);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/pdf");
                    request.SetRequestHeader("X-Goog-Upload-Protocol", "raw");
                    await Send(request, cancellation, progress);
                }
                module.StoragePath = newPath;
                module.FileName = Path.GetFileName(replacement);
            }
            if (string.IsNullOrEmpty(module.StoragePath)) throw new InvalidOperationException("Select a PDF first.");
            cancellation.ThrowIfCancellationRequested(); CheckSession();
            module.Revision = expectedRevision + 1;
            await FirebaseFirestore.DefaultInstance.RunTransactionAsync(async transaction =>
            {
                var existing = await transaction.GetSnapshotAsync(reference);
                if ((expectedRevision == 0 && existing.Exists) || (expectedRevision > 0 && (!existing.Exists || existing.ConvertTo<TeacherPdfModule>().Revision != expectedRevision)))
                    throw new InvalidOperationException("This module changed on another device. Reload the folder and try again.");
                transaction.Set(reference, module);
            });
            module.Id = reference.Id;
        }
        catch
        {
            // A failed metadata acknowledgement can still have committed. Keep the
            // uploaded object rather than risking removal of a referenced PDF.
            module.StoragePath = oldPath; module.Revision = expectedRevision;
            throw;
        }
        progress?.Invoke(1f);
        // Old versions are retained: concurrent readers and uncertain commits must
        // never lose files. A server retention job can reclaim unreferenced versions.
    }

    public async Task Delete(TeacherPdfModule module)
    {
        CheckSession();
        var reference = library.Collection("modules").Document(module.Id);
        await FirebaseFirestore.DefaultInstance.RunTransactionAsync(async transaction =>
        {
            var existing = await transaction.GetSnapshotAsync(reference);
            if (existing.Exists && existing.ConvertTo<TeacherPdfModule>().Revision != module.Revision)
                throw new InvalidOperationException("This module changed. Reload the folder before deleting it.");
            transaction.Delete(reference);
        });
        // Retain the blob for administrator recovery; it is no longer listed.
    }

    public async Task<string> Download(TeacherPdfModule module, CancellationToken cancellation)
    {
        CheckSession();
        if (string.IsNullOrEmpty(bucket)) throw new InvalidOperationException("PDF Storage is not configured yet.");
        string directory = Path.Combine(Application.temporaryCachePath, "module-pdfs");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            using (var request = UnityWebRequest.Get(ObjectUrl(module.StoragePath) + "?alt=media"))
            {
                request.downloadHandler = new DownloadHandlerFile(path) { removeFileOnAbort = true };
                await Send(request, cancellation);
            }
            ValidatePdf(path); return path;
        }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
    }
}
