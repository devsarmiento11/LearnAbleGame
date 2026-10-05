using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

// Android 10+: publish a completed PDF to Downloads through MediaStore. Older
// Android: system Save dialog; neither path needs broad storage permission.
public sealed class StudentPdfDownload : MonoBehaviour
{
    TaskCompletionSource<string> pending;

    // Unity's temporary cache can be external on Android. The native exporter
    // validates internal-cache files, so ask it for the exact staging directory.
    public static string DownloadDirectory()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var bridge = new AndroidJavaClass("com.learnable.modules.PdfDownloads"))
            return bridge.CallStatic<string>("downloadDirectory");
#else
        return Path.Combine(Application.temporaryCachePath, "module-pdfs");
#endif
    }

    public Task<string> Save(string path, string fileName)
    {
        TeacherModuleStore.ValidatePdf(path);
        fileName = Path.GetFileName((fileName ?? "module.pdf").Replace('\\', '/'));
        foreach (char invalid in Path.GetInvalidFileNameChars()) fileName = fileName.Replace(invalid, '_');
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "module.pdf";
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) fileName += ".pdf";
#if UNITY_ANDROID && !UNITY_EDITOR
        if (pending != null) throw new InvalidOperationException("A PDF is already being saved.");
        pending = new TaskCompletionSource<string>();
        var result = pending.Task;
        try
        {
            using (var bridge = new AndroidJavaClass("com.learnable.modules.PdfDownloads"))
                bridge.CallStatic("save", path, fileName, gameObject.name);
        }
        catch (Exception error) { pending.TrySetException(error); pending = null; }
        return result;
#else
        string directory = Path.Combine(Application.persistentDataPath, "Downloads");
        Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, fileName);
        for (int index = 1; File.Exists(target); index++)
            target = Path.Combine(directory, Path.GetFileNameWithoutExtension(fileName) + " (" + index + ").pdf");
        File.Copy(path, target, false);
        return Task.FromResult(target);
#endif
    }

    [UnityEngine.Scripting.Preserve]
    public void OnPdfSaved(string result)
    {
        var completion = pending; pending = null;
        result = result ?? "ERROR:Unable to save the PDF.";
        if (result.StartsWith("ERROR:", StringComparison.Ordinal)) completion?.TrySetException(new IOException(result.Substring(6)));
        else completion?.TrySetResult(result);
    }

    // The Android worker owns a separate staged copy, so cancelling this scene's
    // wait cannot remove the file being saved. Native completion cleans that copy.
    void OnDestroy() { pending?.TrySetCanceled(); pending = null; }
}
