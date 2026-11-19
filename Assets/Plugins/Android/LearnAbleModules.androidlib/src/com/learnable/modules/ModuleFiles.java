package com.learnable.modules;

import java.util.Locale;

// Keep in sync with ModuleFile.cs. Existing PDF activity names remain compatible.
final class ModuleFiles {
    static String extension(String name) throws Exception {
        int dot = name == null ? -1 : name.lastIndexOf('.');
        String ext = dot < 0 ? "" : name.substring(dot).toLowerCase(Locale.ROOT);
        mimeForExtension(ext);
        return ext;
    }
    static String mime(String name) throws Exception { return mimeForExtension(extension(name)); }
    private static String mimeForExtension(String ext) throws Exception {
        switch (ext) {
            case ".pdf": return "application/pdf";
            case ".doc": return "application/msword";
            case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            case ".mp4": return "video/mp4";
            case ".m4v": return "video/x-m4v";
            case ".mov": return "video/quicktime";
            case ".webm": return "video/webm";
            case ".png": return "image/png";
            case ".jpg": case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".webp": return "image/webp";
            default: throw new Exception("Select a supported PDF, video, or image.");
        }
    }
    static String cleanName(String name) throws Exception {
        String ext = extension(name);
        String clean = name.replaceAll("[^\\p{L}\\p{N} ._()-]", "_");
        if (clean.length() > 150) clean = clean.substring(0, 150 - ext.length()) + ext;
        return clean;
    }
}
