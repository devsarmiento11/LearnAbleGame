using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Only the document rendering surface is added inside the existing ViewPopup.
public sealed class ParentDocumentPreview : MonoBehaviour
{
    GameObject surface;
    GameObject zoomToolbar;
    Texture2D image;
    RenderTexture videoTexture;
    VideoPlayer video;
    ModulePdfScrollView pdf;
    int generation;

    public async Task Open(string path, TMP_Text style, CancellationToken token)
    {
        Close(); int version = generation;
        string mime = ParentDocumentFile.ContentType(path);
        if (mime == "application/msword" || mime.Contains("wordprocessingml"))
        {
            TeacherPdfPicker.Open(path); return;
        }
        gameObject.SetActive(true);
        surface = new GameObject("Document content", typeof(RectTransform));
        surface.transform.SetParent(transform, false); surface.transform.SetAsFirstSibling();
        var rect = (RectTransform)surface.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(25, 25); rect.offsetMax = new Vector2(-55, -55);
        Action<string> error = text =>
        {
            if (this == null || generation != version || surface == null) return;
            var label = Instantiate(style, surface.transform); label.gameObject.SetActive(true);
            label.richText = false; label.text = text; label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = 24;
            label.alignment = TextAlignmentOptions.Center;
        };
        if (mime == "application/pdf")
        {
            pdf = surface.AddComponent<ModulePdfScrollView>();
            pdf.Open(path, error);
            AddZoom(pdf.Scroll, style);
            return;
        }
        var display = new GameObject("Document display", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        display.transform.SetParent(surface.transform, false); display.raycastTarget = false;
        var fit = display.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        if (mime.StartsWith("image/", StringComparison.Ordinal))
        {
            byte[] bytes = await ModuleImageDecoder.Read(path, token);
            token.ThrowIfCancellationRequested();
            if (this == null || version != generation) return;
            image = ModuleImageDecoder.Texture(bytes); display.texture = image;
            Destroy(fit);
            fit.enabled = false;
            var viewport = (RectTransform)surface.transform;
            Canvas.ForceUpdateCanvases();
            display.rectTransform.anchorMin = display.rectTransform.anchorMax = display.rectTransform.pivot = new Vector2(0, 1);
            display.rectTransform.anchoredPosition = Vector2.zero;
            display.rectTransform.sizeDelta = new Vector2(viewport.rect.width,
                viewport.rect.width * image.height / image.width);
            surface.AddComponent<RectMask2D>();
            surface.AddComponent<Image>().color = Color.clear;
            var scroll = surface.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = display.rectTransform;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            AddZoom(scroll, style);
        }
        else
        {
            video = surface.AddComponent<VideoPlayer>();
            var audio = surface.AddComponent<AudioSource>(); audio.playOnAwake = false;
            video.playOnAwake = false; video.source = VideoSource.Url; video.url = new Uri(path).AbsoluteUri;
            video.renderMode = VideoRenderMode.RenderTexture; video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            video.controlledAudioTrackCount = 1; video.EnableAudioTrack(0, true); video.SetTargetAudioSource(0, audio);
            display.raycastTarget = true;
            var pause = display.gameObject.AddComponent<Button>(); pause.targetGraphic = display;
            pause.transition = Selectable.Transition.None;
            pause.onClick.AddListener(() => { if (video == null) return; if (video.isPlaying) video.Pause(); else video.Play(); });
            video.errorReceived += (player, message) => error("Unable to play this video on this device. " + message);
            video.prepareCompleted += player =>
            {
                if (this == null || version != generation) return;
                int width = Mathf.Clamp((int)player.width, 1, 1920);
                float ratio = player.width / (float)Math.Max(1u, player.height);
                videoTexture = new RenderTexture(width, Mathf.Clamp(Mathf.RoundToInt(width / ratio), 1, 1920), 0);
                videoTexture.Create(); player.targetTexture = videoTexture; display.texture = videoTexture;
                fit.aspectRatio = ratio; player.Play();
            };
            video.Prepare();
        }
    }

    void AddZoom(ScrollRect scroll, TMP_Text style)
    {
        var toolbar = new GameObject("Document zoom", typeof(RectTransform));
        var rect = (RectTransform)toolbar.transform;
        rect.SetParent(transform, false);
        rect.SetSiblingIndex(1);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(25, -6);
        toolbar.AddComponent<ParentDocumentZoom>().Initialize(scroll, style);
        // Keep controls outside the clipped viewport and clean them up with it.
        zoomToolbar = toolbar;
    }

    public void Close()
    {
        generation++;
        if (video != null) video.Stop();
        if (pdf != null) pdf.Release();
        if (surface != null) { surface.SetActive(false); Destroy(surface); }
        if (zoomToolbar != null) { zoomToolbar.SetActive(false); Destroy(zoomToolbar); zoomToolbar = null; }
        if (image != null) Destroy(image);
        if (videoTexture != null) { videoTexture.Release(); Destroy(videoTexture); }
        surface = null; image = null; videoTexture = null; video = null; pdf = null;
        gameObject.SetActive(false);
    }
    void OnDisable() { if (surface != null) Close(); }
    void OnDestroy() { Close(); }
}
