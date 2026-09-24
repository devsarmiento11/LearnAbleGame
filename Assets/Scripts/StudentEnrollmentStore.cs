using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.Networking;

public sealed class StudentEnrollmentStore
{
    string owner;
    public async Task Open(CancellationToken cancellation)
    {
        if (await FirebaseApp.CheckAndFixDependenciesAsync() != DependencyStatus.Available)
            throw new InvalidOperationException("Unable to connect to student accounts. Try again.");
        cancellation.ThrowIfCancellationRequested();
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user == null || string.IsNullOrWhiteSpace(user.DisplayName) || user.DisplayName.Contains("/"))
            throw new InvalidOperationException("Sign in with your teacher account first.");
        owner = user.UserId;
        var profile = await FirebaseFirestore.DefaultInstance.Collection("users").Document(user.DisplayName).GetSnapshotAsync(Source.Server);
        cancellation.ThrowIfCancellationRequested(); CheckSession();
        if (!profile.Exists || !profile.TryGetValue<string>("authUid", out var uid) || uid != owner ||
            !profile.TryGetValue<string>("role", out var role) || role != "teacher")
            throw new InvalidOperationException("A verified teacher account is required.");
    }
    void CheckSession()
    {
        if (owner == null || FirebaseAuth.DefaultInstance.CurrentUser?.UserId != owner)
            throw new InvalidOperationException("Your session changed. Sign in again.");
    }
    public async Task<List<StudentEnrollmentData>> Students(CancellationToken cancellation)
    {
        CheckSession();
        var snapshot = await FirebaseFirestore.DefaultInstance.Collection("users").WhereEqualTo("role", "student").GetSnapshotAsync(Source.Server);
        cancellation.ThrowIfCancellationRequested(); CheckSession();
        return snapshot.Documents.Select(StudentEnrollmentData.FromDocument).OrderBy(s => s.FullName, StringComparer.OrdinalIgnoreCase).ThenBy(s => s.id).ToList();
    }
    public async Task<StudentEnrollmentData> Student(string id, CancellationToken cancellation)
    {
        CheckSession();
        if (string.IsNullOrWhiteSpace(id) || id.Contains("/")) throw new InvalidOperationException("Select a student from the student list.");
        var document = await FirebaseFirestore.DefaultInstance.Collection("users").Document(id).GetSnapshotAsync(Source.Server);
        cancellation.ThrowIfCancellationRequested(); CheckSession();
        if (!document.Exists || !document.TryGetValue<string>("role", out var role) || role != "student")
            throw new InvalidOperationException("This student account is no longer available.");
        return StudentEnrollmentData.FromDocument(document);
    }
    [Serializable] sealed class Request { public Payload data; }
    [Serializable] sealed class Payload { public string id, section, password, requestId; public StudentEnrollmentData profile; }
    [Serializable] sealed class Response { public Result result; public Failure error; }
    [Serializable] sealed class Result { public string id; }
    [Serializable] sealed class Failure { public string message; }

    public async Task Save(StudentEnrollmentData data, StudentInfoSection section, bool editing, string password, string requestId, CancellationToken cancellation)
    {
        CheckSession();
        string token = await FirebaseAuth.DefaultInstance.CurrentUser.TokenAsync(false);
        cancellation.ThrowIfCancellationRequested(); CheckSession();
        var payload = new Payload { id = data.id, profile = data, section = section.ToString().ToLowerInvariant(), password = password, requestId = requestId };
        string url = "https://us-central1-" + FirebaseApp.DefaultInstance.Options.ProjectId + ".cloudfunctions.net/" +
            (editing ? "updateStudentEnrollment" : "enrollStudent");
        using (var request = new UnityWebRequest(url, "POST")) {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Request { data = payload })));
            request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 60;
            request.SetRequestHeader("Content-Type", "application/json"); request.SetRequestHeader("Authorization", "Bearer " + token);
            var operation = request.SendWebRequest();
            while (!operation.isDone) { cancellation.ThrowIfCancellationRequested(); await Task.Yield(); }
            cancellation.ThrowIfCancellationRequested(); CheckSession();
            Response response = null;
            try { response = JsonUtility.FromJson<Response>(request.downloadHandler.text); } catch (ArgumentException) { }
            if (response?.error != null && !string.IsNullOrEmpty(response.error.message)) throw new InvalidOperationException(response.error.message);
            if (request.result != UnityWebRequest.Result.Success || response?.result?.id != data.id)
                throw new InvalidOperationException("Unable to save. Check your connection and try again.");
        }
    }
}
