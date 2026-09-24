package com.learnable.modules;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import java.io.File;
import java.io.OutputStream;

// Permission-free destination picker for Android versions before MediaStore Downloads.
public final class PdfSaveActivity extends Activity {
    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        if (state != null) return;
        Intent intent = new Intent(Intent.ACTION_CREATE_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("application/pdf");
        intent.putExtra(Intent.EXTRA_TITLE, getIntent().getStringExtra("name"));
        try { startActivityForResult(intent, 72); }
        catch (Exception error) { complete("ERROR:No document save app is available."); }
    }
    @Override protected void onActivityResult(int request, int result, Intent data) {
        super.onActivityResult(request, result, data);
        if (request != 72) return;
        if (result != RESULT_OK || data == null || data.getData() == null) { complete(""); return; }
        Uri uri = data.getData();
        new Thread(() -> {
            try (OutputStream output = getContentResolver().openOutputStream(uri)) {
                PdfDownloads.copy(new File(getIntent().getStringExtra("path")), output);
            } catch (Exception error) {
                try { android.provider.DocumentsContract.deleteDocument(getContentResolver(), uri); } catch (Exception ignored) { }
                complete("ERROR:Unable to save the PDF. Check available storage and retry."); return;
            }
            complete(uri.toString());
        }, "LearnAble PDF save").start();
    }
    private void complete(String result) {
        new File(getIntent().getStringExtra("path")).delete();
        PdfDownloads.complete(this, getIntent().getStringExtra("receiver"), result);
        runOnUiThread(this::finish);
    }
}
