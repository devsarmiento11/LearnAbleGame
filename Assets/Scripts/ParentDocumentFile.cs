using System;
using System.IO;
using System.IO.Compression;

public static class ParentDocumentFile
{
    public const string Extensions = ModuleFile.Extensions + ",doc,docx";
    public static string ContentType(string path)
    {
        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".doc": return "application/msword";
            case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            default: return ModuleFile.ContentType(path);
        }
    }

    public static void Validate(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension != ".doc" && extension != ".docx") { ModuleFile.Validate(path); return; }
        var file = new FileInfo(path);
        if (!file.Exists || file.Length == 0 || file.Length > ModuleFile.MaxBytes)
            throw new InvalidOperationException("Select a document no larger than 25 MB.");
        if (extension == ".docx")
        {
            using (var archive = ZipFile.OpenRead(path))
                if (archive.GetEntry("[Content_Types].xml") == null || archive.GetEntry("word/document.xml") == null)
                    throw new InvalidOperationException("Select a valid Word document.");
        }
        else
        {
            byte[] expected = { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 };
            using (var stream = File.OpenRead(path))
                foreach (byte value in expected)
                    if (stream.ReadByte() != value) throw new InvalidOperationException("Select a valid Word document.");
        }
    }
}
