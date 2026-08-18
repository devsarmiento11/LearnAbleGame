using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;

    [Header("UI")]
    public Canvas canvas;
    public Image linePrefab;

    [Header("Side Containers")]
    [Tooltip("Drag the LEFT Dots parent here.")]
    public RectTransform leftDotsContainer;

    [Tooltip("Drag the RIGHT Dots parent here.")]
    public RectTransform rightDotsContainer;

    private Image currentLine;
    private LeftDot currentLeft;

    private int correctMatches = 0;

    // Keeps track of LEFT dots that have already been connected.
    // Each LEFT answer can only be used once.
    private HashSet<LeftDot> connectedLeftDots =
        new HashSet<LeftDot>();

    private class MatchData
    {
        public Image line;
        public LeftDot left;
        public bool correct;
    }

    // Stores both correct and wrong connections
    // so Undo can remove them.
    private Stack<MatchData> history =
        new Stack<MatchData>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Draw the temporary line while dragging
        if (currentLine != null && currentLeft != null)
        {
            RectTransform leftRect =
                currentLeft.GetComponent<RectTransform>();

            Vector2 startScreen =
                RectTransformUtility.WorldToScreenPoint(
                    null,
                    leftRect.position
                );

            Vector2 start;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                startScreen,
                null,
                out start
            );

            Vector2 end;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                Input.mousePosition,
                null,
                out end
            );

            DrawLine(start, end);
        }
    }

    // =====================================================
    // START CONNECTION
    // =====================================================

    public void StartConnection(LeftDot left)
    {
        if (left == null)
            return;

        // Make sure this is actually inside the LEFT container
        if (leftDotsContainer != null &&
            !left.transform.IsChildOf(leftDotsContainer))
        {
            Debug.LogWarning(
                left.name +
                " is not inside the Left Dots container!"
            );

            return;
        }

        // Don't allow another connection while dragging
        if (currentLine != null)
        {
            Debug.Log(
                "A connection is already in progress."
            );

            return;
        }

        // IMPORTANT:
        // The same LEFT answer can only be connected once.
        if (connectedLeftDots.Contains(left))
        {
            Debug.Log(
                left.name +
                " has already been connected!"
            );

            return;
        }

        currentLeft = left;

        currentLine =
            Instantiate(
                linePrefab,
                canvas.transform
            );

        currentLine.transform.SetAsLastSibling();

        Debug.Log(
            "Started connection from: " +
            left.name
        );
    }

    // =====================================================
    // END CONNECTION
    // =====================================================

    public void EndConnection(RightDot right)
    {
        if (currentLeft == null || currentLine == null)
            return;

        if (right == null)
            return;

        // Make sure the target is actually inside
        // the RIGHT container.
        if (rightDotsContainer != null &&
            !right.transform.IsChildOf(rightDotsContainer))
        {
            Debug.LogWarning(
                right.name +
                " is not inside the Right Dots container!"
            );

            return;
        }

        RectTransform leftRect =
            currentLeft.GetComponent<RectTransform>();

        RectTransform rightRect =
            right.GetComponent<RectTransform>();

        Vector2 startScreen =
            RectTransformUtility.WorldToScreenPoint(
                null,
                leftRect.position
            );

        Vector2 endScreen =
            RectTransformUtility.WorldToScreenPoint(
                null,
                rightRect.position
            );

        Vector2 start;
        Vector2 end;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            startScreen,
            null,
            out start
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            endScreen,
            null,
            out end
        );

        // Draw the final line
        DrawLine(start, end);

        // Check if the IDs match
        bool isCorrect =
            currentLeft.id == right.id;

        // Lock this LEFT answer.
        // It doesn't matter whether the answer is
        // correct or wrong.
        connectedLeftDots.Add(currentLeft);

        // =================================================
        // CORRECT CONNECTION
        // =================================================

        if (isCorrect)
        {
            correctMatches++;

            Debug.Log(
                "CORRECT! " +
                currentLeft.id +
                " → " +
                right.id
            );

            Debug.Log(
                "Total Correct = " +
                correctMatches
            );
        }

        // =================================================
        // WRONG CONNECTION
        // =================================================

        else
        {
            Debug.Log(
                "WRONG! " +
                currentLeft.id +
                " → " +
                right.id
            );

            // IMPORTANT:
            // The wrong line stays visible.
        }

        // Store BOTH correct and wrong connections
        // so Undo can remove them.
        history.Push(
            new MatchData
            {
                line = currentLine,
                left = currentLeft,
                correct = isCorrect
            }
        );

        // Reset current connection
        currentLine = null;
        currentLeft = null;
    }

    // =====================================================
    // DRAW LINE
    // =====================================================

    void DrawLine(Vector2 start, Vector2 end)
    {
        if (currentLine == null)
            return;

        RectTransform rect =
            currentLine.rectTransform;

        Vector2 direction =
            end - start;

        rect.anchoredPosition =
            start;

        rect.sizeDelta =
            new Vector2(
                direction.magnitude,
                8f
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        rect.rotation =
            Quaternion.Euler(
                0,
                0,
                angle
            );
    }

    // =====================================================
    // GET CORRECT MATCHES
    // =====================================================

    public int GetCorrectMatches()
    {
        return correctMatches;
    }

    // =====================================================
    // UNDO
    // =====================================================

    public void Undo()
    {
        if (history.Count == 0)
        {
            Debug.Log("Nothing to undo.");
            return;
        }

        MatchData last =
            history.Pop();

        // If the last connection was correct,
        // remove one correct point.
        if (last.correct)
        {
            correctMatches--;
        }

        // Allow the LEFT answer to be selected again.
        if (last.left != null)
        {
            connectedLeftDots.Remove(last.left);
        }

        // Remove the line from the screen.
        if (last.line != null)
        {
            Destroy(last.line.gameObject);
        }

        Debug.Log(
            "Undo! Total Correct = " +
            correctMatches
        );
    }
}