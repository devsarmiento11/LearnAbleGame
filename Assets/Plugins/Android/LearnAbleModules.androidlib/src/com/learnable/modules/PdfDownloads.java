package com.learnable.modules;

import android.app.Activity;
import android.content.ContentValues;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.provider.MediaStore;
import android.widget.Toast;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.util.UUID;

public final class PdfDownloads {
    private PdfDownloads() { }

    public static void save(String path, String name, String receiver) throws Exception {
        Activity activity = UnityPlayer.currentActivity;
        File source = new File(path).getCanonicalFile();
        File allowed = new File(activity.getCacheDir(), "module-pdfs").getCanonicalFile();
        if (!allowed.equals(source.getParentFile()) || !source.isFile() || source.length() > 25L * 1024 * 1024)
            throw new Exception("Invalid module download.");
        String clean = name.replaceAll("[^\\p{L}\\p{N} ._()-]", "_");
        if (clean.length() > 150) clean = clean.substring(0, 146) + ".pdf";
        final String fileName = clean;
        File directory = new File(activity.getCacheDir(), "module-exports");
        if (!directory.isDirectory() && !directory.mkdirs()) throw new Exception("Unable to save the PDF.");
        File staged = new File(directory, UUID.randomUUID() + ".pdf");
        // Own an independent copy before returning to Unity, so leaving the scene
        // cannot delete the source while the Android worker or picker uses it.
        try (OutputStream output = new FileOutputStream(staged)) { copy(source, output); }
        catch (Exception error) { staged.delete(); throw error; }
        if (Build.VERSION.SDK_INT < 29) {
            activity.runOnUiThread(() -> {
                try {
                    Intent intent = new Intent(activity, PdfSaveActivity.class);
                    intent.putExtra("path", staged.getAbsolutePath());
                    intent.putExtra("name", fileName);
                    intent.putExtra("receiver", receiver);
                    activity.startActivity(intent);
                } catch (Exception error) {
                    staged.delete(); complete(activity, receiver, "ERROR:Unable to open the Save dialog.");
                }
            });
            return;
        }
        new Thread(() -> {
            Uri uri = null;
            try {
                ContentValues values = new ContentValues();
                values.put(MediaStore.MediaColumns.DISPLAY_NAME, fileName);
                values.put(MediaStore.MediaColumns.MIME_TYPE, "application/pdf");
                values.put(MediaStore.MediaColumns.RELATIVE_PATH, Environment.DIRECTORY_DOWNLOADS + "/LearnAble");
                values.put(MediaStore.MediaColumns.IS_PENDING, 1);
                uri = activity.getContentResolver().insert(MediaStore.Downloads.EXTERNAL_CONTENT_URI, values);
                if (uri == null) throw new Exception("Unable to create a download.");
                try (OutputStream output = activity.getContentResolver().openOutputStream(uri)) { copy(staged, output); }
                values.clear(); values.put(MediaStore.MediaColumns.IS_PENDING, 0);
                if (activity.getContentResolver().update(uri, values, null, null) != 1)
                    throw new Exception("Unable to finish the download.");
                complete(activity, receiver, "Downloads/LearnAble/" + fileName);
            } catch (Exception error) {
                if (uri != null) try { activity.getContentResolver().delete(uri, null, null); } catch (Exception ignored) { }
                complete(activity, receiver, "ERROR:Unable to save the PDF. Check available phone storage and retry.");
            } finally { staged.delete(); }
        }, "LearnAble PDF download").start();
    }

    static void copy(File source, OutputStream output) throws Exception {
        if (output == null) throw new Exception("Unable to open the destination.");
        try (FileInputStream input = new FileInputStream(source)) {
            byte[] buffer = new byte[65536]; int count;
            while ((count = input.read(buffer)) != -1) output.write(buffer, 0, count);
            output.flush();
        }
    }

    static void complete(Activity activity, String receiver, String result) {
        activity.runOnUiThread(() -> {
            UnityPlayer.UnitySendMessage(receiver, "OnPdfSaved", result);
            if (!result.isEmpty()) Toast.makeText(activity,
                result.startsWith("ERROR:") ? result.substring(6) : "PDF saved to " + result, Toast.LENGTH_LONG).show();
        });
    }
}
