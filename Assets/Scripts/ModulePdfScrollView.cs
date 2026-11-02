using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Only visible pages retain textures, even for long teacher-uploaded PDFs.
public sealed class ModulePdfScrollView : MonoBehaviour
{
    private ModulePdfDocument document;
    private ScrollRect scroll;
    private RectTransform pages;
    private readonly List<RawImage> images = new List<RawImage>();
    private readonly List<float> tops = new List<float>();
    private readonly List<float> heights = new List<float>();
    private Action<string> error;
    private bool ready;

    public void Open(string path, Action<string> onError)
    {
        error = onError;
        document = new ModulePdfDocument(path);
        if (document.Count <= 0) throw new InvalidOperationException("This PDF has no pages.");
        scroll = gameObject.AddComponent<ScrollRect>();
        scroll.viewport = (RectTransform)transform;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40;
        gameObject.AddComponent<RectMask2D>();
        var hit = gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
        pages = new GameObject("PDF Pages", typeof(RectTransform)).GetComponent<RectTransform>();
        pages.SetParent(transform, false);
        pages.anchorMin = pages.anchorMax = pages.pivot = new Vector2(0, 1);
        scroll.content = pages;
        StartCoroutine(BuildPages());
    }

    private IEnumerator BuildPages()
    {
        Canvas.ForceUpdateCanvases();
        float width = ((RectTransform)transform).rect.width;
        float top = 0;
        int count = document.Count;
        for (int i = 0; i < count; i++)
        {
            float height;
            try { height = width * Mathf.Clamp(document.Ratio(i), .05f, 30f); }
            catch (Exception exception) { error(exception.Message); yield break; }
            var image = new GameObject("Page " + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(pages, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(0, 1);
            image.rectTransform.anchoredPosition = new Vector2(0, -top);
            image.rectTransform.sizeDelta = new Vector2(width, height);
            image.color = Color.white; image.raycastTarget = false;
            images.Add(image); tops.Add(top); heights.Add(height);
            top += height + 12;
            pages.sizeDelta = new Vector2(width, top);
            if (i % 20 == 0) { DrawVisible(); yield return null; }
        }
        ready = true;
        DrawVisible();
    }

    private void LateUpdate() { if (ready) DrawVisible(); }

    private void DrawVisible()
    {
        if (document == null || pages == null) return;
        float start = pages.anchoredPosition.y;
        float end = start + scroll.viewport.rect.height;
        // Render at most one new page per frame, keeping scroll and Close responsive.
        bool rendered = false;
        for (int i = 0; i < images.Count; i++)
        {
            bool visible = tops[i] + heights[i] >= start - 80 && tops[i] <= end + 80;
            if (!visible && images[i].texture != null)
            {
                Destroy(images[i].texture); images[i].texture = null;
            }
            else if (visible && images[i].texture == null && !rendered)
            {
                try { images[i].texture = document.Render(i, 1200); }
                catch (Exception exception) { ready = false; error(exception.Message); return; }
                rendered = true;
            }
        }
    }

    public void Release()
    {
        StopAllCoroutines(); ready = false;
        if (document != null) { document.Dispose(); document = null; }
        foreach (var image in images) if (image != null && image.texture != null) Destroy(image.texture);
        images.Clear(); tops.Clear(); heights.Clear();
    }
    private void OnDestroy() { Release(); }
}
