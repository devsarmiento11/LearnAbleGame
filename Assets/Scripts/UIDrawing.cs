using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIDrawing : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Drawing")]
    public RectTransform drawingArea;
    public GameObject brushPrefab;

    [Tooltip("Minimum distance between brush stamps.")]
    public float brushSpacing = 8f;

    [Header("Line Ends")]
    public RectTransform[] lineEnds;
    public float detectDistance = 30f;

    // Number of completed tracing lines
    public int completedLines = 0;

    private bool[] counted;

    private GameObject currentStroke;

    // Undo history
    private Stack<StrokeData> strokeHistory =
        new Stack<StrokeData>();

    // LineEnds completed by the current stroke
    private List<int> currentStrokeLineEnds =
        new List<int>();

    // Last brush position
    private Vector2 lastBrushPosition;

    private bool hasLastBrushPosition;


    // =========================================================
    // STROKE DATA
    // =========================================================

    private class StrokeData
    {
        public GameObject strokeObject;

        public List<int> completedLineEnds =
            new List<int>();
    }


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        counted = new bool[lineEnds.Length];
    }


    // =========================================================
    // GET SCORE
    // =========================================================

    public int GetCompletedLines()
    {
        return completedLines;
    }


    // =========================================================
    // START DRAWING
    // =========================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        // Make sure the pointer is actually inside DrawingArea
        if (!IsInsideDrawingArea(eventData.position,
            eventData.pressEventCamera))
        {
            return;
        }

        currentStroke = new GameObject("Stroke");

        currentStroke.transform.SetParent(
            drawingArea,
            false
        );

        currentStrokeLineEnds.Clear();

        hasLastBrushPosition = false;

        Draw(eventData);
    }


    // =========================================================
    // DRAG
    // =========================================================

    public void OnDrag(PointerEventData eventData)
    {
        if (currentStroke == null)
            return;

        Draw(eventData);
    }


    // =========================================================
    // END DRAWING
    // =========================================================

    public void OnPointerUp(PointerEventData eventData)
    {
        if (currentStroke == null)
            return;

        StrokeData data = new StrokeData();

        data.strokeObject = currentStroke;

        data.completedLineEnds.AddRange(
            currentStrokeLineEnds
        );

        strokeHistory.Push(data);

        currentStroke = null;

        currentStrokeLineEnds.Clear();

        hasLastBrushPosition = false;
    }


    // =========================================================
    // DRAW
    // =========================================================

    void Draw(PointerEventData eventData)
    {
        if (currentStroke == null)
            return;

        Vector2 pos;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            drawingArea,
            eventData.position,
            eventData.pressEventCamera,
            out pos))
        {
            return;
        }

        // -----------------------------------------------------
        // KEEP DRAWING INSIDE THE DRAWING AREA
        // -----------------------------------------------------

        Rect rect = drawingArea.rect;

        pos.x = Mathf.Clamp(
            pos.x,
            rect.xMin,
            rect.xMax
        );

        pos.y = Mathf.Clamp(
            pos.y,
            rect.yMin,
            rect.yMax
        );


        // -----------------------------------------------------
        // FIRST BRUSH
        // -----------------------------------------------------

        if (!hasLastBrushPosition)
        {
            CreateBrush(pos);

            lastBrushPosition = pos;
            hasLastBrushPosition = true;

            return;
        }


        // -----------------------------------------------------
        // DISTANCE FROM LAST BRUSH
        // -----------------------------------------------------

        float distance = Vector2.Distance(
            lastBrushPosition,
            pos
        );

        // Don't create another brush if we're too close
        if (distance < brushSpacing)
        {
            return;
        }


        // -----------------------------------------------------
        // CREATE BRUSHES ALONG THE PATH
        // -----------------------------------------------------

        Vector2 direction =
            (pos - lastBrushPosition).normalized;

        float remainingDistance = distance;

        Vector2 currentPosition =
            lastBrushPosition;

        while (remainingDistance >= brushSpacing)
        {
            currentPosition +=
                direction * brushSpacing;

            CreateBrush(currentPosition);

            remainingDistance -= brushSpacing;
        }

        lastBrushPosition = currentPosition;
    }


    // =========================================================
    // CREATE BRUSH
    // =========================================================

    void CreateBrush(Vector2 position)
    {
        GameObject brush =
            Instantiate(brushPrefab);

        brush.transform.SetParent(
            currentStroke.transform,
            false
        );

        RectTransform brushRect =
            brush.GetComponent<RectTransform>();

        brushRect.anchoredPosition = position;

        CheckLineEnds(brushRect);
    }


    // =========================================================
    // CHECK DRAWING AREA
    // =========================================================

    bool IsInsideDrawingArea(
        Vector2 screenPosition,
        Camera eventCamera)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            drawingArea,
            screenPosition,
            eventCamera
        );
    }


    // =========================================================
    // CHECK LINE ENDS
    // =========================================================

    void CheckLineEnds(RectTransform brush)
    {
        for (int i = 0; i < lineEnds.Length; i++)
        {
            if (lineEnds[i] == null)
                continue;

            // Already completed
            if (counted[i])
                continue;

            float distance = Vector2.Distance(
                brush.position,
                lineEnds[i].position
            );

            if (distance <= detectDistance)
            {
                counted[i] = true;

                completedLines++;

                // Remember which LineEnd this stroke completed
                if (!currentStrokeLineEnds.Contains(i))
                {
                    currentStrokeLineEnds.Add(i);
                }

                Debug.Log(
                    "Completed Line: " +
                    completedLines
                );

                // Hide LineEnd
                lineEnds[i].gameObject.SetActive(false);
            }
        }
    }


    // =========================================================
    // UNDO LAST STROKE
    // =========================================================

    public void UndoLastStroke()
    {
        if (strokeHistory.Count == 0)
        {
            Debug.Log("Nothing to undo.");
            return;
        }

        StrokeData lastStroke =
            strokeHistory.Pop();


        // Delete visual stroke
        if (lastStroke.strokeObject != null)
        {
            Destroy(lastStroke.strokeObject);
        }


        // Restore LineEnds
        foreach (int index in lastStroke.completedLineEnds)
        {
            if (index < 0 ||
                index >= lineEnds.Length)
            {
                continue;
            }

            if (!counted[index])
                continue;

            counted[index] = false;

            completedLines--;

            if (lineEnds[index] != null)
            {
                lineEnds[index]
                    .gameObject
                    .SetActive(true);
            }

            Debug.Log(
                "Restored LineEnd: " +
                index
            );
        }


        Debug.Log(
            "Undo complete. Current Lines: " +
            completedLines
        );
    }


    // =========================================================
    // CLEAR ALL
    // =========================================================

    public void ClearAll()
    {
        // Delete saved strokes
        while (strokeHistory.Count > 0)
        {
            StrokeData stroke =
                strokeHistory.Pop();

            if (stroke.strokeObject != null)
            {
                Destroy(stroke.strokeObject);
            }
        }


        // Delete current stroke
        if (currentStroke != null)
        {
            Destroy(currentStroke);

            currentStroke = null;
        }


        // Reset score
        completedLines = 0;


        // Reset LineEnds
        for (int i = 0;
            i < counted.Length;
            i++)
        {
            counted[i] = false;

            if (lineEnds[i] != null)
            {
                lineEnds[i]
                    .gameObject
                    .SetActive(true);
            }
        }


        currentStrokeLineEnds.Clear();

        hasLastBrushPosition = false;

        Debug.Log(
            "All drawing cleared."
        );
    }


    // =========================================================
    // GET DRAWING AREA
    // =========================================================

    public Transform GetStrokeContainer()
    {
        return drawingArea;
    }
}