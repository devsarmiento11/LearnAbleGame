package com.learnable.modules;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;
import java.io.File;
import java.io.FileNotFoundException;

// Grants read access only to a single temporary PDF explicitly opened by the user.
public final class PdfProvider extends ContentProvider {
    @Override public boolean onCreate() { return true; }
    private File resolve(Uri uri) throws FileNotFoundException {
        try {
            String name = uri.getLastPathSegment();
            if (name == null || !name.matches("[a-f0-9]{32}\\.pdf")) throw new Exception();
            File root = new File(getContext().getCacheDir(), "module-pdfs").getCanonicalFile();
            File file = new File(root, name).getCanonicalFile();
            if (!root.equals(file.getParentFile()) || !file.isFile()) throw new Exception();
            return file;
        } catch (Exception error) { throw new FileNotFoundException("PDF unavailable"); }
    }
    @Override public String getType(Uri uri) { return "application/pdf"; }
    @Override public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        if (!"r".equals(mode)) throw new FileNotFoundException("Read only");
        return ParcelFileDescriptor.open(resolve(uri), ParcelFileDescriptor.MODE_READ_ONLY);
    }
    @Override public Cursor query(Uri uri, String[] projection, String selection, String[] args, String order) {
        String[] columns = projection == null ? new String[]{OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE} : projection;
        MatrixCursor cursor = new MatrixCursor(columns);
        try {
            File file = resolve(uri); Object[] row = new Object[columns.length];
            for (int i = 0; i < columns.length; i++) {
                if (OpenableColumns.DISPLAY_NAME.equals(columns[i])) row[i] = "module.pdf";
                else if (OpenableColumns.SIZE.equals(columns[i])) row[i] = file.length();
            }
            cursor.addRow(row);
        } catch (FileNotFoundException ignored) { }
        return cursor;
    }
    @Override public Uri insert(Uri uri, ContentValues values) { throw new UnsupportedOperationException(); }
    @Override public int update(Uri uri, ContentValues values, String selection, String[] args) { throw new UnsupportedOperationException(); }
    @Override public int delete(Uri uri, String selection, String[] args) { throw new UnsupportedOperationException(); }
}
