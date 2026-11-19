using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class TeacherPdfPicker : MonoBehaviour
{
    TaskCompletionSource<string> pending;
    public Task<string> Pick(bool documents = false)
    {
        if (pending != null) throw new InvalidOperationException("The file picker is already open.");
#if UNITY_EDITOR
        return Task.FromResult(UnityEditor.EditorUtility.OpenFilePanelWithFilters("Select a module", "",
            new[] { documents ? "Documents, videos and images" : "PDF, videos and images", documents ? ParentDocumentFile.Extensions : ModuleFile.Extensions }));
#elif UNITY_ANDROID
        pending = new TaskCompletionSource<string>();
        try
        {
            using (var bridge = new AndroidJavaClass("com.learnable.modules.PdfPickerActivity"))
                bridge.CallStatic(documents ? "pickDocuments" : "pick", gameObject.name);
        }
        catch (Exception e) { var failed = pending; pending = null; failed.SetException(e); return failed.Task; }
        return pending.Task;
#else
        throw new PlatformNotSupportedException("Module selection is supported in the Unity Editor and on Android phones.");
#endif
    }

    // Called by the Android activity on Unity's main thread, including cancellation.
    [UnityEngine.Scripting.Preserve]
    public void OnPdfPicked(string result)
    {
        var task = pending; pending = null;
        if (result.StartsWith("ERROR:")) task?.TrySetException(new InvalidOperationException(result.Substring(6)));
        else task?.TrySetResult(result);
    }

    public static void Open(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var bridge = new AndroidJavaClass("com.learnable.modules.PdfPickerActivity"))
            bridge.CallStatic("view", path);
#else
        Application.OpenURL(new Uri(path).AbsoluteUri);
#endif
    }

    void OnDestroy() { pending?.TrySetCanceled(); pending = null; }
}
