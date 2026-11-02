package com.learnable.modules;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.graphics.pdf.PdfRenderer;
import android.os.ParcelFileDescriptor;
import java.io.ByteArrayOutputStream;
import java.io.File;

// Decodes locally on the phone. Private student modules never go to a web viewer.
public final class ModulePreview implements AutoCloseable {
    private final PdfRenderer renderer;
    private final ParcelFileDescriptor descriptor;

    public ModulePreview(String path) throws Exception {
        descriptor = ParcelFileDescriptor.open(new File(path), ParcelFileDescriptor.MODE_READ_ONLY);
        try { renderer = new PdfRenderer(descriptor); }
        catch (Exception error) { descriptor.close(); throw error; }
    }
    public int count() { return renderer.getPageCount(); }
    public float ratio(int index) {
        try (PdfRenderer.Page page = renderer.openPage(index)) {
            return (float)page.getHeight() / page.getWidth();
        }
    }
    public byte[] render(int index, int width) throws Exception {
        try (PdfRenderer.Page page = renderer.openPage(index)) {
            int height = Math.max(1, Math.round(width * (float)page.getHeight() / page.getWidth()));
            Bitmap bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
            try {
                bitmap.eraseColor(Color.WHITE);
                page.render(bitmap, null, null, PdfRenderer.Page.RENDER_MODE_FOR_DISPLAY);
                return png(bitmap);
            } finally { bitmap.recycle(); }
        }
    }
    public static byte[] image(String path) throws Exception {
        BitmapFactory.Options bounds = new BitmapFactory.Options();
        bounds.inJustDecodeBounds = true;
        BitmapFactory.decodeFile(path, bounds);
        if (bounds.outWidth <= 0 || bounds.outHeight <= 0) throw new Exception("Unable to read this image.");
        BitmapFactory.Options options = new BitmapFactory.Options();
        options.inSampleSize = 1;
        while (Math.max(bounds.outWidth, bounds.outHeight) / options.inSampleSize > 2048)
            options.inSampleSize *= 2;
        Bitmap bitmap = BitmapFactory.decodeFile(path, options);
        if (bitmap == null) throw new Exception("Unable to read this image.");
        try { return png(bitmap); } finally { bitmap.recycle(); }
    }
    private static byte[] png(Bitmap bitmap) throws Exception {
        try (ByteArrayOutputStream bytes = new ByteArrayOutputStream()) {
            if (!bitmap.compress(Bitmap.CompressFormat.PNG, 100, bytes)) throw new Exception("Unable to render module.");
            return bytes.toByteArray();
        }
    }
    @Override public void close() throws Exception { try { renderer.close(); } finally { descriptor.close(); } }
}
