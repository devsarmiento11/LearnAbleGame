using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Uses only the three authored panels and their existing Close/Download controls.
public sealed class StudentModulePreview : MonoBehaviour
{
    private Transform canvas;
    private StudentPdfDownload exporter;
    private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();
    private readonly Dictionary<Selectable, bool> backgroundControls = new Dictionary<Selectable, bool>();
    private TMP_Text textStyle, message;
    private GameObject panel, surface, blocker;
    private Button download;
    private Texture2D image;
    private ModulePdfScrollView pdf;
    private VideoPlayer video;
    private AudioSource videoAudio;
    private RenderTexture videoTexture;
    private CancellationTokenSource request;
    private string fileName;
    private string localPath;
    private int generation;
    private bool saving;
    public bool IsOpen => panel != null;
    public bool IsSaving => saving;

    public void Initialize(Transform sceneCanvas, StudentPdfDownload fileExporter, TMP_Text style)
    {
        canvas = sceneCanvas; exporter = fileExporter; textStyle = style;
        foreach (string name in new[] { "PdfPopUpScreen", "ImagePopUpScreen", "VideoPopUpScreen" })
        {
            Transform found = canvas.Find(name);
            if (found == null) throw new InvalidOperationException("Missing student module popup: " + name);
            var close = found.Find("CloseBtn").GetComponent<Button>();
            var save = found.Find("DownloadBtn").GetComponent<Button>();
            close.onClick = new Button.ButtonClickedEvent(); close.onClick.AddListener(Close);
            save.onClick = new Button.ButtonClickedEvent(); save.onClick.AddListener(Save);
            panels.Add(name, found.gameObject);
            found.gameObject.SetActive(false);
        }
    }

    public async void Open(StudentModuleStore store, StudentModuleFolder folder, TeacherPdfModule module)
    {
        if (IsOpen || saving) return;
        int version = ++generation;
        request = new CancellationTokenSource();
        CancellationToken token = request.Token;
        try
        {
            string mime = ModuleFile.ContentType(module.StoragePath);
            panel = panels[mime == "application/pdf" ? "PdfPopUpScreen" : mime.StartsWith("video/") ? "VideoPopUpScreen" : "ImagePopUpScreen"];
            ShowPanel();
            SetMessage("Loading " + module.Title + "...");
            var file = await store.Download(folder, module.Id, token,
                progress => { if (version == generation) SetMessage("Loading " + Mathf.FloorToInt(progress * 100) + "%"); });
            string path = file.Path;
            if (version != generation || token.IsCancellationRequested) { DeleteFile(path); return; }
            localPath = path;
            fileName = file.FileName;
            // Use the verified downloaded extension if the teacher replaced a file.
            mime = ModuleFile.ContentType(path);
            string correctPanel = mime == "application/pdf" ? "PdfPopUpScreen" : mime.StartsWith("video/") ? "VideoPopUpScreen" : "ImagePopUpScreen";
            if (panel != panels[correctPanel])
            {
                HideSurface(); panel.SetActive(false); panel = panels[correctPanel]; ShowPanel();
            }
            download.interactable = true;
            if (mime == "application/pdf")
            {
                pdf = surface.AddComponent<ModulePdfScrollView>();
                SetMessage("");
                pdf.Open(path, error => { if (version == generation) SetMessage(error); });
            }
            else if (mime.StartsWith("image/"))
            {
                byte[] bytes = await ModuleImageDecoder.Read(path, token);
                if (version != generation) { DeleteFile(path); return; }
                token.ThrowIfCancellationRequested();
                image = ModuleImageDecoder.Texture(bytes);
                var raw = Display(); raw.texture = image; raw.raycastTarget = false;
                Fit(raw.gameObject, image.width, image.height);
                SetMessage("");
            }
            else PrepareVideo(path, version);
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            if (this != null && version == generation)
            {
                SetMessage("Unable to preview this module. " + error.GetBaseException().Message);
                Debug.LogWarning("Student module preview: " + error.GetBaseException().Message);
            }
        }
    }

    private void ShowPanel()
    {
        if (blocker == null)
        {
            blocker = new GameObject("Module modal input", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blocker.transform.SetParent(canvas, false);
            var rect = (RectTransform)blocker.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = Color.clear;
        }
        blocker.SetActive(true); blocker.transform.SetAsLastSibling();
        panel.SetActive(true); panel.transform.SetAsLastSibling();
        foreach (var control in canvas.GetComponentsInChildren<Selectable>(true))
        {
            if (control.transform.IsChildOf(panel.transform) || backgroundControls.ContainsKey(control)) continue;
            backgroundControls.Add(control, control.interactable); control.interactable = false;
        }
        var close = panel.transform.Find("CloseBtn").GetComponent<Button>(); close.interactable = true;
        download = panel.transform.Find("DownloadBtn").GetComponent<Button>(); download.interactable = localPath != null && !saving;
        surface = new GameObject("Module content", typeof(RectTransform));
        surface.transform.SetParent(panel.transform, false);
        surface.transform.SetAsFirstSibling();
        var content = (RectTransform)surface.transform;
        content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(24, 82); content.offsetMax = new Vector2(-62, -62);
        message = Instantiate(textStyle, panel.transform);
        message.name = "Module loading status"; message.gameObject.SetActive(true);
        var rectText = message.rectTransform;
        rectText.anchorMin = Vector2.zero; rectText.anchorMax = Vector2.one;
        rectText.offsetMin = new Vector2(30, 85); rectText.offsetMax = new Vector2(-68, -68);
        message.alignment = TextAlignmentOptions.Center; message.richText = false;
        message.enableAutoSizing = true; message.fontSizeMin = 14; message.fontSizeMax = 28;
        message.textWrappingMode = TextWrappingModes.Normal; message.color = Color.black;
        message.raycastTarget = false;
    }

    private void SetMessage(string value)
    {
        if (message == null) return;
        message.text = value; message.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private static void Fit(GameObject target, float width, float height)
    {
        var fitter = target.AddComponent<AspectRatioFitter>();
        fitter.aspectRatio = width / Mathf.Max(1, height);
        // Fit a child inside the already inset content rectangle.
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
    }

    private RawImage Display()
    {
        var raw = new GameObject("Module display", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
        raw.transform.SetParent(surface.transform, false);
        return raw;
    }

    private void PrepareVideo(string path, int version)
    {
        video = surface.AddComponent<VideoPlayer>();
        video.playOnAwake = false; video.source = VideoSource.Url;
        video.url = new Uri(path).AbsoluteUri;
        video.isLooping = true; video.waitForFirstFrame = true;
        videoAudio = surface.AddComponent<AudioSource>(); videoAudio.playOnAwake = false;
        videoAudio.spatialBlend = 0; videoAudio.volume = 1;
        video.audioOutputMode = VideoAudioOutputMode.AudioSource;
        video.controlledAudioTrackCount = 1; video.EnableAudioTrack(0, true); video.SetTargetAudioSource(0, videoAudio);
        video.renderMode = VideoRenderMode.RenderTexture;
        video.prepareCompleted += player =>
        {
            if (version != generation || player != video) return;
            int width = Mathf.Clamp((int)player.width, 1, 1920);
            int height = Mathf.Max(1, Mathf.RoundToInt(width * player.height / (float)Math.Max(1u, player.width)));
            videoTexture = new RenderTexture(width, Mathf.Min(height, 1920), 0);
            videoTexture.Create(); player.targetTexture = videoTexture;
            var raw = Display(); raw.texture = videoTexture; raw.raycastTarget = true;
            Fit(raw.gameObject, player.width, player.height);
            // Clicking the displayed video toggles pause without adding a UI button.
            var pause = raw.gameObject.AddComponent<Button>(); pause.transition = Selectable.Transition.None; pause.targetGraphic = raw;
            pause.onClick.AddListener(() => { if (video == null) return; if (video.isPlaying) video.Pause(); else video.Play(); });
            SetMessage(""); player.Play();
        };
        video.errorReceived += (player, error) =>
        {
            if (version == generation) SetMessage("This video's codec cannot be played on this device. You can download the original file.");
            Debug.LogWarning("Student video: " + error);
        };
        SetMessage("Preparing video..."); video.Prepare();
        StartCoroutine(VideoPreparationTimeout(video, version));
    }

    private IEnumerator VideoPreparationTimeout(VideoPlayer player, int version)
    {
        float deadline = Time.realtimeSinceStartup + 25f;
        while (version == generation && player != null && !player.isPrepared && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (version == generation && player != null && !player.isPrepared)
            SetMessage("Video preview could not start. You can download the original file or close and try again.");
    }

    private async void Save()
    {
        if (saving || localPath == null) return;
        saving = true;
        int version = generation;
        download.interactable = false;
        SetMessage("Saving module...");
        try
        {
            string result = await exporter.Save(localPath, fileName);
            if (version == generation) SetMessage(string.IsNullOrEmpty(result) ? "Save cancelled." : "Module downloaded.");
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { if (this != null && version == generation) SetMessage("Unable to save module. " + error.GetBaseException().Message); }
        finally
        {
            saving = false;
            if (this != null && version == generation && download != null) download.interactable = localPath != null;
        }
    }

    private void HideSurface()
    {
        if (video != null) { video.Stop(); video.targetTexture = null; video = null; }
        if (videoAudio != null) { videoAudio.Stop(); videoAudio = null; }
        if (pdf != null) { pdf.Release(); pdf = null; }
        if (image != null) { Destroy(image); image = null; }
        if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); videoTexture = null; }
        if (surface != null) { surface.SetActive(false); Destroy(surface); surface = null; }
        if (message != null) { Destroy(message.gameObject); message = null; }
    }

    public void Close()
    {
        ++generation;
        if (request != null) { request.Cancel(); request.Dispose(); request = null; }
        HideSurface();
        foreach (var popup in panels.Values) if (popup != null) popup.SetActive(false);
        panel = null; fileName = null; download = null;
        if (blocker != null) blocker.SetActive(false);
        foreach (var pair in backgroundControls) if (pair.Key != null) pair.Key.interactable = pair.Value;
        backgroundControls.Clear();
        DeleteFile(localPath); localPath = null;
    }
    private static void DeleteFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (IsOpen && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#else
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
    }
    private void OnDestroy() { Close(); }
}
