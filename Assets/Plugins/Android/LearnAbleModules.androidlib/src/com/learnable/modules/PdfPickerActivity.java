package com.learnable.modules;

import android.app.Activity;
import android.content.Intent;
import android.database.Cursor;
import android.net.Uri;
import android.os.Bundle;
import android.provider.OpenableColumns;
import android.widget.Toast;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.util.UUID;

public final class PdfPickerActivity extends Activity {
    private String receiver;
    public static void pick(String receiver) {
        Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(() -> {
            Intent intent = new Intent(activity, PdfPickerActivity.class);
            intent.putExtra("receiver", receiver);
            activity.startActivity(intent);
        });
    }
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        receiver = getIntent().getStringExtra("receiver");
        if (state != null) return;
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("application/pdf");
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try { startActivityForResult(intent, 71); }
        catch (Exception error) { complete("ERROR:No document picker is available on this phone."); }
    }
    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != 71) return;
        if (result != RESULT_OK || data == null || data.getData() == null) { complete(""); return; }
        Uri uri = data.getData();
        // Content providers (including cloud drives) are streams, not filesystem paths.
        new Thread(() -> {
            File target = null;
            try {
                String name = "module.pdf";
                try (Cursor cursor = getContentResolver().query(uri, new String[]{OpenableColumns.DISPLAY_NAME}, null, null, null)) {
                    if (cursor != null && cursor.moveToFirst()) name = cursor.getString(0);
                }
                if (name == null || !name.toLowerCase(java.util.Locale.ROOT).endsWith(".pdf"))
                    throw new Exception("Select a PDF file.");
                name = name.replaceAll("[^\\p{L}\\p{N} ._()-]", "_");
                if (name.length() > 150) name = name.substring(0, 146) + ".pdf";
                File directory = new File(getCacheDir(), "module-imports/" + UUID.randomUUID());
                if (!directory.mkdirs()) throw new Exception("Unable to prepare the selected file.");
                target = new File(directory, name);
                try (InputStream input = getContentResolver().openInputStream(uri); FileOutputStream output = new FileOutputStream(target)) {
                    if (input == null) throw new Exception("Unable to read the selected PDF.");
                    byte[] buffer = new byte[65536]; long size = 0; int count;
                    while ((count = input.read(buffer)) != -1) {
                        size += count;
                        if (size > 25L * 1024 * 1024) throw new Exception("Select a PDF no larger than 25 MB.");
                        output.write(buffer, 0, count);
                    }
                }
                complete(target.getAbsolutePath());
            } catch (Exception error) {
                if (target != null) target.delete();
                complete("ERROR:" + (error.getMessage() == null ? "Unable to read the PDF." : error.getMessage()));
            }
        }, "LearnAble PDF import").start();
    }
    private void complete(String result) {
        runOnUiThread(() -> { UnityPlayer.UnitySendMessage(receiver, "OnPdfPicked", result); finish(); });
    }
    public static void view(String path) {
        Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(() -> {
            try {
                File file = new File(path).getCanonicalFile();
                File directory = new File(activity.getCacheDir(), "module-pdfs").getCanonicalFile();
                if (!directory.equals(file.getParentFile())) throw new Exception("Invalid PDF path.");
                Uri uri = new Uri.Builder().scheme("content").authority(activity.getPackageName() + ".modulepdfs").appendPath(file.getName()).build();
                Intent intent = new Intent(Intent.ACTION_VIEW).setDataAndType(uri, "application/pdf");
                intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                activity.startActivity(Intent.createChooser(intent, "Open module PDF"));
            } catch (Exception error) { Toast.makeText(activity, "Install a PDF reader to open this module.", Toast.LENGTH_LONG).show(); }
        });
    }
}
