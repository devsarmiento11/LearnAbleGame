using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

// Owns a local PDF and renders only the pages currently visible in the popup.
public sealed class ModulePdfDocument : IDisposable
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject document;
    public ModulePdfDocument(string path) { document = new AndroidJavaObject("com.learnable.modules.ModulePreview", path); }
    public int Count => document.Call<int>("count");
    public float Ratio(int page) { return document.Call<float>("ratio", page); }
    public Texture2D Render(int page, int width)
    {
        width = Mathf.Min(width, Mathf.Max(1, Mathf.FloorToInt(2048f / Mathf.Max(1f, Ratio(page)))));
        return ModuleImageDecoder.Texture(document.Call<byte[]>("render", page, width));
    }
    public void Dispose()
    {
        if (document == null) return;
        try { document.Call("close"); } finally { document.Dispose(); document = null; }
    }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private IntPtr document;
    private GCHandle memory;
    private static bool initialized;
    public ModulePdfDocument(string path)
    {
        if (!initialized) { FPDF_InitLibrary(); initialized = true; }
        byte[] data = File.ReadAllBytes(path);
        memory = GCHandle.Alloc(data, GCHandleType.Pinned);
        document = FPDF_LoadMemDocument64(memory.AddrOfPinnedObject(), new UIntPtr((uint)data.Length), null);
        if (document == IntPtr.Zero) { Dispose(); throw new InvalidOperationException("Unable to open this PDF. It may be damaged or password protected."); }
    }
    public int Count => FPDF_GetPageCount(document);
    public float Ratio(int index)
    {
        IntPtr page = Page(index);
        try { return FPDF_GetPageHeightF(page) / FPDF_GetPageWidthF(page); }
        finally { FPDF_ClosePage(page); }
    }
    private IntPtr Page(int index)
    {
        IntPtr page = FPDF_LoadPage(document, index);
        if (page == IntPtr.Zero) throw new InvalidOperationException("Unable to read this PDF page.");
        return page;
    }
    public Texture2D Render(int index, int width)
    {
        IntPtr page = Page(index);
        IntPtr bitmap = IntPtr.Zero;
        try
        {
            float ratio = FPDF_GetPageHeightF(page) / FPDF_GetPageWidthF(page);
            width = Mathf.Min(width, Mathf.Max(1, Mathf.FloorToInt(2048f / Mathf.Max(1f, ratio))));
            int height = Mathf.Max(1, Mathf.RoundToInt(width * ratio));
            bitmap = FPDFBitmap_Create(width, height, 1);
            if (bitmap == IntPtr.Zero) throw new InvalidOperationException("Unable to allocate PDF preview.");
            FPDFBitmap_FillRect(bitmap, 0, 0, width, height, 0xffffffff);
            FPDF_RenderPageBitmap(bitmap, page, 0, 0, width, height, 0, 1);
            int stride = FPDFBitmap_GetStride(bitmap);
            byte[] bgra = new byte[stride * height];
            Marshal.Copy(FPDFBitmap_GetBuffer(bitmap), bgra, 0, bgra.Length);
            byte[] rgba = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int source = y * stride + x * 4, target = ((height - 1 - y) * width + x) * 4;
                    rgba[target] = bgra[source + 2]; rgba[target + 1] = bgra[source + 1];
                    rgba[target + 2] = bgra[source]; rgba[target + 3] = bgra[source + 3];
                }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(rgba); texture.Apply(false, true); return texture;
        }
        finally { if (bitmap != IntPtr.Zero) FPDFBitmap_Destroy(bitmap); FPDF_ClosePage(page); }
    }
    public void Dispose()
    {
        if (document != IntPtr.Zero) { FPDF_CloseDocument(document); document = IntPtr.Zero; }
        if (memory.IsAllocated) memory.Free();
    }
    private const string Library = "pdfium";
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern void FPDF_InitLibrary();
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern IntPtr FPDF_LoadMemDocument64(IntPtr data, UIntPtr length, string password);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern void FPDF_CloseDocument(IntPtr doc);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern int FPDF_GetPageCount(IntPtr doc);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern IntPtr FPDF_LoadPage(IntPtr doc, int index);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern void FPDF_ClosePage(IntPtr page);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern float FPDF_GetPageWidthF(IntPtr page);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern float FPDF_GetPageHeightF(IntPtr page);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern void FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, uint color);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern void FPDF_RenderPageBitmap(IntPtr bitmap, IntPtr page, int x, int y, int width, int height, int rotate, int flags);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern int FPDFBitmap_GetStride(IntPtr bitmap);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);
    [DllImport(Library, CallingConvention = CallingConvention.StdCall)] private static extern void FPDFBitmap_Destroy(IntPtr bitmap);
#else
    public ModulePdfDocument(string path) { throw new PlatformNotSupportedException("PDF previews are supported on Android and Windows."); }
    public int Count => 0;
    public float Ratio(int page) { return 1; }
    public Texture2D Render(int page, int width) { return null; }
    public void Dispose() { }
#endif
}
