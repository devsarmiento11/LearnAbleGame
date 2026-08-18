using UnityEngine;
using UnityEngine.EventSystems;

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
        // Mouse
        if (Input.GetMouseButtonDown(0))
            BeginDraw(Input.mousePosition);

        if (Input.GetMouseButton(0) && isDrawing)
            ContinueDraw(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            EndDraw();

        // Touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginDraw(touch.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDrawing)
                        ContinueDraw(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndDraw();
                    break;
            }
        }
    }

    void BeginDraw(Vector2 screenPos)
    {
        // Only allow drawing inside the drawing area
        if (!RectTransformUtility.RectangleContainsScreenPoint(drawingArea, screenPos))
            return;

        isDrawing = true;

        currentStroke = new GameObject("Stroke");
        currentStroke.transform.SetParent(strokeContainer, false);

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea,
            screenPos,
            null,
            out localPoint);

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
            null,
            out localPoint);

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

    void CreateBrush(Vector2 localPoint)
    {
        if (currentStroke == null)
            return;

        GameObject brush = Instantiate(brushPrefab, currentStroke.transform);

        RectTransform rect = brush.GetComponent<RectTransform>();
        rect.anchoredPosition = localPoint;
    }
}