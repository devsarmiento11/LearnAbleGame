using System;
using System.IO;
using System.Text;

// One format policy for upload, replacement, viewing and student downloads.
public static class ModuleFile
{
    public const long MaxBytes = 25L * 1024 * 1024;
    public const string Extensions = "pdf,mp4,m4v,mov,webm,png,jpg,jpeg,gif,webp";

    public static string Extension(string path)
    {
        string extension = Path.GetExtension(path ?? "").ToLowerInvariant();
        ContentType(extension);
        return extension;
    }

    public static string ContentType(string path)
    {
        switch (Path.GetExtension(path ?? "").ToLowerInvariant())
        {
            case ".pdf": return "application/pdf";
            case ".mp4": return "video/mp4";
            case ".m4v": return "video/x-m4v";
            case ".mov": return "video/quicktime";
            case ".webm": return "video/webm";
            case ".png": return "image/png";
            case ".jpg": case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".webp": return "image/webp";
            default: throw new InvalidOperationException("Select a PDF, video (MP4, M4V, MOV, WebM), or image (PNG, JPG, GIF, WebP).");
        }
    }

    public static void Validate(string path)
    {
        if (!File.Exists(path)) throw new InvalidOperationException("The selected file is no longer available.");
        string extension = Extension(path);
        var info = new FileInfo(path);
        if (info.Length == 0 || info.Length > MaxBytes)
            throw new InvalidOperationException("Select a module file no larger than 25 MB.");
        byte[] header = new byte[16];
        int count;
        using (var stream = File.OpenRead(path)) count = stream.Read(header, 0, header.Length);
        bool valid;
        switch (extension)
        {
            case ".pdf": valid = Starts(header, count, 0, "%PDF-"); break;
            case ".png": valid = count >= 8 && header[0] == 137 && Starts(header, count, 1, "PNG\r\n\u001a\n"); break;
            case ".jpg": case ".jpeg": valid = count >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255; break;
            case ".gif": valid = Starts(header, count, 0, "GIF87a") || Starts(header, count, 0, "GIF89a"); break;
            case ".webp": valid = Starts(header, count, 0, "RIFF") && Starts(header, count, 8, "WEBP"); break;
            case ".webm": valid = count >= 4 && header[0] == 26 && header[1] == 69 && header[2] == 223 && header[3] == 163; break;
            default: valid = Starts(header, count, 4, "ftyp") ||
                (extension == ".mov" && (Starts(header, count, 4, "moov") || Starts(header, count, 4, "mdat") || Starts(header, count, 4, "wide"))); break;
        }
        if (!valid) throw new InvalidOperationException("The file contents do not match its PDF, video, or image extension.");
    }

    private static bool Starts(byte[] bytes, int count, int offset, string value)
    {
        return count >= offset + value.Length && Encoding.ASCII.GetString(bytes, offset, value.Length) == value;
    }
}
