using UnityEngine;
using UnityEngine.InputSystem;

public class DrawingManager : MonoBehaviour
{
    [Header("References")]
    public RectTransform drawingArea;
    public GameObject brushPrefab;
    public Transform strokeContainer;

    [Header("Drawing")]
    public float brushSpacing = 10f;

    private GameObject currentStroke;
    private Vector2 lastPoint;
    private bool isDrawing = false;

    void Update()
    {
        // PRIORITY: Touch first (Android)
        if (Touchscreen.current != null &&
            (Touchscreen.current.primaryTouch.press.isPressed ||
             Touchscreen.current.primaryTouch.press.wasPressedThisFrame ||
             Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                BeginDraw(touch.position.ReadValue());
            }

            if (touch.press.isPressed && isDrawing)
            {
                ContinueDraw(touch.position.ReadValue());
            }

            if (touch.press.wasReleasedThisFrame)
            {
                EndDraw();
            }

            return;
        }

        // FALLBACK: Mouse (Unity Editor / PC)
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                BeginDraw(Mouse.current.position.ReadValue());
            }

            if (Mouse.current.leftButton.isPressed && isDrawing)
            {
                ContinueDraw(Mouse.current.position.ReadValue());
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                EndDraw();
            }
        }
    }

    void BeginDraw(Vector2 screenPos)
    {
        // Only allow drawing inside the drawing area
        if (!RectTransformUtility.RectangleContainsScreenPoint(drawingArea, screenPos, GetUICamera()))
            return;

        isDrawing = true;

        currentStroke = new GameObject("Stroke");
        currentStroke.transform.SetParent(strokeContainer, false);

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea,
            screenPos,
            GetUICamera(),
            out localPoint
        );

        lastPoint = localPoint;

        CreateBrush(localPoint);
    }

    void ContinueDraw(Vector2 screenPos)
    {
        if (!isDrawing)
            return;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea,
            screenPos,
            GetUICamera(),
            out localPoint
        );

        if (Vector2.Distance(localPoint, lastPoint) >= brushSpacing)
        {
            CreateBrush(localPoint);
            lastPoint = localPoint;
        }
    }

    void EndDraw()
    {
        isDrawing = false;
    }

    private Camera GetUICamera()
    {
        Canvas canvas = drawingArea.GetComponentInParent<Canvas>();
        return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.worldCamera;
    }

    void CreateBrush(Vector2 localPoint)
    {
        if (currentStroke == null)
            return;

        GameObject brush = Instantiate(brushPrefab, currentStroke.transform);

        RectTransform rect = brush.GetComponent<RectTransform>();
        rect.anchoredPosition = localPoint;
    }
}
