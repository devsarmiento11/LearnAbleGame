using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine.Networking;

public sealed class ParentDocumentStore
{
    readonly string uid, parentId, childId, bucket;
    DocumentReference Record => FirebaseFirestore.DefaultInstance.Collection("parentDocuments").Document(uid);
    public string FileName { get; private set; }
    public bool HasDocument { get; private set; }

    public ParentDocumentStore()
    {
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (!LoginSession.IsParent || user == null || LoginSession.VerifiedParentUid != user.UserId)
            throw new InvalidOperationException("Sign in with your active parent account first.");
        uid = user.UserId; parentId = LoginSession.StudentId; childId = LoginSession.ChildrenId;
        bucket = FirebaseApp.DefaultInstance.Options.StorageBucket;
        CheckSession();
    }

    void CheckSession()
    {
        if (!LoginSession.IsParent || FirebaseAuth.DefaultInstance.CurrentUser?.UserId != uid ||
            LoginSession.VerifiedParentUid != uid || LoginSession.StudentId != parentId || LoginSession.ChildrenId != childId)
            throw new InvalidOperationException("Your account changed. Sign in again.");
        if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(childId) || childId.Contains("/"))
            throw new InvalidOperationException("Document storage or your linked child is unavailable.");
    }

    async Task Verify(CancellationToken cancellation)
    {
        CheckSession();
        // Rules look up the managed parent profile using the Auth name claim.
        // Refresh it before Firestore reads after login/profile updates.
        await StudentModuleStore.Read(FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(true), cancellation);
        CheckSession();
        var profile = await StudentModuleStore.Read(FirebaseFirestore.DefaultInstance.Collection("users")
            .Document(parentId).GetSnapshotAsync(Source.Server), cancellation);
        CheckSession();
        if (!profile.Exists || profile.GetValue<string>("role") != "parent" ||
            profile.GetValue<string>("authUid") != uid || profile.GetValue<string>("status") != "Active" ||
            profile.GetValue<string>("childrenId") != childId)
            throw new InvalidOperationException("Your parent account or linked child changed. Sign in again.");
    }

    public async Task Load(CancellationToken cancellation)
    {
        HasDocument = false; FileName = null;
        await Verify(cancellation);
        var saved = await StudentModuleStore.Read(Record.GetSnapshotAsync(Source.Server), cancellation);
        CheckSession();
        if (saved.Exists && saved.GetValue<string>("ChildId") == childId)
        {
            ValidatePath(saved.GetValue<string>("StoragePath"));
            FileName = saved.GetValue<string>("FileName"); HasDocument = true;
        }
    }

    string ValidatePath(string path)
    {
        string prefix = "parentDocuments/" + uid + "/" + childId + "/";
        if (string.IsNullOrEmpty(path) || !path.StartsWith(prefix, StringComparison.Ordinal) ||
            path.Substring(prefix.Length).Contains("/") || path.Contains(".."))
            throw new InvalidOperationException("The saved document reference is invalid.");
        ParentDocumentFile.ContentType(path);
        return path;
    }

    string Url(string path) => "https://firebasestorage.googleapis.com/v0/b/" + Uri.EscapeDataString(bucket)
        + "/o/" + Uri.EscapeDataString(path);

    async Task Transfer(UnityWebRequest request, CancellationToken cancellation, Action<float> progress, bool uploading)
    {
        CheckSession();
        string token = await StudentModuleStore.Read(FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(true), cancellation);
        CheckSession();
        request.SetRequestHeader("Authorization", "Firebase " + token); request.timeout = 120;
        try
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                cancellation.ThrowIfCancellationRequested(); CheckSession();
                if (!uploading && request.downloadedBytes > (ulong)ModuleFile.MaxBytes)
                    throw new InvalidOperationException("This document exceeds the 25 MB limit.");
                progress?.Invoke(Math.Max(0, uploading ? request.uploadProgress * .95f : request.downloadProgress));
                await Task.Yield();
            }
            cancellation.ThrowIfCancellationRequested(); CheckSession();
            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(request.responseCode == 401 || request.responseCode == 403
                    ? "Document access denied. Sign in again or ask your administrator to check permissions."
                    : request.responseCode == 404 ? "This document is no longer available. Upload it again."
                    : "Document transfer failed. Check your connection and try again.");
        }
        finally { if (!request.isDone) request.Abort(); }
    }

    public async Task Upload(string path, CancellationToken cancellation, Action<float> progress)
    {
        ParentDocumentFile.Validate(path);
        await Verify(cancellation);
        string extension = Path.GetExtension(path).ToLowerInvariant();
        string destination = "parentDocuments/" + uid + "/" + childId + "/" + Guid.NewGuid().ToString("N") + extension;
        string fileName = Path.GetFileName(path);
        if (fileName.Length > 255) throw new InvalidOperationException("Choose a file with a shorter name.");
        using (var request = new UnityWebRequest("https://firebasestorage.googleapis.com/v0/b/" + Uri.EscapeDataString(bucket)
            + "/o?name=" + Uri.EscapeDataString(destination), "POST"))
        {
            request.uploadHandler = new UploadHandlerFile(path); request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", ParentDocumentFile.ContentType(path));
            request.SetRequestHeader("X-Goog-Upload-Protocol", "raw");
            await Transfer(request, cancellation, progress, true);
        }
        await Verify(cancellation);
        await StudentModuleStore.Read(Record.SetAsync(new Dictionary<string, object>
        {
            { "ParentId", parentId }, { "ChildId", childId }, { "FileName", fileName },
            { "StoragePath", destination }, { "UploadedAt", FieldValue.ServerTimestamp }
        }).ContinueWith(t => { t.GetAwaiter().GetResult(); return true; }), cancellation);
        CheckSession(); HasDocument = true; FileName = fileName; progress?.Invoke(1);
    }

    public async Task<string> Download(CancellationToken cancellation, Action<float> progress)
    {
        await Verify(cancellation);
        var saved = await StudentModuleStore.Read(Record.GetSnapshotAsync(Source.Server), cancellation);
        CheckSession();
        if (!saved.Exists || saved.GetValue<string>("ChildId") != childId)
            throw new InvalidOperationException("Upload a document first.");
        string remote = ValidatePath(saved.GetValue<string>("StoragePath"));
        string directory = StudentPdfDownload.DownloadDirectory(); Directory.CreateDirectory(directory);
        string local = Path.Combine(directory, Guid.NewGuid().ToString("N") + Path.GetExtension(remote));
        try
        {
            using (var request = UnityWebRequest.Get(Url(remote) + "?alt=media"))
            {
                request.downloadHandler = new DownloadHandlerFile(local) { removeFileOnAbort = true };
                await Transfer(request, cancellation, progress, false);
            }
            ParentDocumentFile.Validate(local); return local;
        }
        catch { if (File.Exists(local)) File.Delete(local); throw; }
    }
}
