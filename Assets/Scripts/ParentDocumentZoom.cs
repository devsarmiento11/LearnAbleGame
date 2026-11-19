using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class ParentDocumentZoom : MonoBehaviour
{
    ScrollRect scroll;
    float zoom = 1f;
    float pinchDistance;

    public void Initialize(ScrollRect target, TMP_Text style)
    {
        scroll = target;
        scroll.horizontal = true;
        AddButton("−", 0, 1f / 1.25f, style);
        AddButton("+", 48, 1.25f, style);
    }

    void AddButton(string caption, float x, float factor, TMP_Text style)
    {
        var control = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = (RectTransform)control.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, 0);
        rect.sizeDelta = new Vector2(44, 44);
        var background = control.GetComponent<Image>();
        background.color = new Color(.32f, .20f, .12f);
        var label = Instantiate(style, rect, false);
        label.gameObject.SetActive(true);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.enableAutoSizing = false;
        label.fontSize = 28;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.text = caption;
        var button = control.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => Zoom(factor));
    }

    void Zoom(float factor)
    {
        if (scroll == null || scroll.content == null) return;
        var content = scroll.content;
        float next = Mathf.Clamp(zoom * factor, 1f, 4f);
        scroll.StopMovement();
        var center = scroll.viewport.TransformPoint(scroll.viewport.rect.center);
        var point = content.InverseTransformPoint(center);
        content.localScale = new Vector3(next, next, 1);
        content.position += center - content.TransformPoint(point);
        zoom = next;
    }

    void Update()
    {
        if (scroll == null) return;
        Vector2 first, second;
#if ENABLE_INPUT_SYSTEM
        var screen = Touchscreen.current;
        if (screen == null || screen.touches.Count < 2 ||
            !screen.touches[0].press.isPressed || !screen.touches[1].press.isPressed)
        { pinchDistance = 0; return; }
        first = screen.touches[0].position.ReadValue();
        second = screen.touches[1].position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount != 2) { pinchDistance = 0; return; }
        first = Input.GetTouch(0).position;
        second = Input.GetTouch(1).position;
#else
        return;
#endif
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
        var canvas = scroll.GetComponentInParent<Canvas>();
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        float distance = Vector2.Distance(first, second);
        if (pinchDistance > 1 && distance > 1 &&
            RectTransformUtility.RectangleContainsScreenPoint(scroll.viewport, (first + second) / 2, camera))
            Zoom(distance / pinchDistance);
        pinchDistance = distance;
#endif
    }
}
