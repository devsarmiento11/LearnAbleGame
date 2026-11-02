using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class ModuleImageDecoder
{
    public static Task<byte[]> Read(string path, CancellationToken cancellation)
    {
        // Capture Unity's directory on the main thread before decoding in a worker.
        string tools = Path.Combine(Application.streamingAssetsPath, "ModulePreviewWindows");
        return Task.Run(() =>
        {
            cancellation.ThrowIfCancellationRequested();
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJNI.AttachCurrentThread();
            try
            {
                using (var bridge = new AndroidJavaClass("com.learnable.modules.ModulePreview"))
                    return bridge.CallStatic<byte[]>("image", path);
            }
            finally { AndroidJNI.DetachCurrentThread(); }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".webp") return DecodeWebP(path, tools, cancellation);
            if (extension == ".gif") return DecodeGif(path);
            return File.ReadAllBytes(path);
#else
            return File.ReadAllBytes(path);
#endif
        }, cancellation);
    }

    public static Texture2D Texture(byte[] data)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(data, true))
        {
            UnityEngine.Object.Destroy(texture);
            throw new InvalidOperationException("Unable to display this image.");
        }
        return texture;
    }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private static byte[] DecodeGif(string path)
    {
        // Mono's Windows image decoder supplies the first frame for GIF documents.
        var assembly = System.Reflection.Assembly.Load("System.Drawing");
        Type imageType = assembly.GetType("System.Drawing.Image", true);
        Type formatType = assembly.GetType("System.Drawing.Imaging.ImageFormat", true);
        object image = imageType.GetMethod("FromFile", new[] { typeof(string) }).Invoke(null, new object[] { path });
        try
        {
            using (var bytes = new MemoryStream())
            {
                object png = formatType.GetProperty("Png").GetValue(null, null);
                imageType.GetMethod("Save", new[] { typeof(Stream), formatType }).Invoke(image, new[] { (object)bytes, png });
                return bytes.ToArray();
            }
        }
        finally { ((IDisposable)image).Dispose(); }
    }

    private static byte[] DecodeWebP(string path, string tools, CancellationToken cancellation)
    {
        string target = Path.Combine(Path.GetDirectoryName(path), Guid.NewGuid().ToString("N") + ".png");
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Path.Combine(tools, "dwebp.exe"),
                Arguments = "-quiet \"" + path + "\" -o \"" + target + "\"",
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardError = true
            };
            using (var process = System.Diagnostics.Process.Start(start))
            {
                if (process == null) throw new InvalidOperationException("Unable to start the image decoder.");
                if (!process.WaitForExit(15000)) { process.Kill(); throw new InvalidOperationException("Image preview timed out."); }
                if (process.ExitCode != 0) throw new InvalidOperationException("Unable to display this WebP image. You can still download the original file.");
            }
            cancellation.ThrowIfCancellationRequested();
            return File.ReadAllBytes(target);
        }
        finally { if (File.Exists(target)) File.Delete(target); }
    }
#endif
}
