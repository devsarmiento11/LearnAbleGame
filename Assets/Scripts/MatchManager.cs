using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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


    // =====================================================
    // AWAKE
    // =====================================================

    void Awake()
    {
        Instance = this;
    }


    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        // Only draw temporary line while a connection
        // is currently being made.
        if (currentLine == null ||
            currentLeft == null)
        {
            return;
        }


        // Get pointer position using NEW INPUT SYSTEM
        Vector2 pointerPosition;

        if (!TryGetPointerPosition(
                out pointerPosition))
        {
            return;
        }


        RectTransform leftRect =
            currentLeft.GetComponent<RectTransform>();


        Camera uiCamera = GetUICamera();


        // LEFT DOT screen position
        Vector2 startScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                leftRect.position
            );


        Vector2 start;
        Vector2 end;


        // Convert LEFT dot to Canvas position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            startScreen,
            uiCamera,
            out start
        );


        // Convert touch/mouse position to Canvas position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            pointerPosition,
            uiCamera,
            out end
        );


        DrawLine(
            start,
            end
        );
    }


    // =====================================================
    // GET POINTER POSITION
    // Mouse in Editor + Touch on Android
    // =====================================================

    private bool TryGetPointerPosition(
        out Vector2 pointerPosition)
    {
        // ==========================================
        // TOUCHSCREEN / ANDROID
        // ==========================================

        if (Touchscreen.current != null)
        {
            var touch =
                Touchscreen.current.primaryTouch;

            if (touch.press.isPressed)
            {
                pointerPosition =
                    touch.position.ReadValue();

                return true;
            }
        }


        // ==========================================
        // MOUSE / UNITY EDITOR
        // ==========================================

        if (Mouse.current != null)
        {
            pointerPosition =
                Mouse.current.position.ReadValue();

            return true;
        }


        pointerPosition = Vector2.zero;

        return false;
    }


    // =====================================================
    // GET CORRECT CAMERA FOR UI
    // =====================================================

    private Camera GetUICamera()
    {
        if (canvas == null)
            return null;

        // Screen Space Overlay doesn't need a camera
        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }


    // =====================================================
    // START CONNECTION
    // =====================================================

    public void StartConnection(
        LeftDot left)
    {
        if (left == null)
            return;


        // Make sure this is actually inside
        // the LEFT container.
        if (leftDotsContainer != null &&
            !left.transform.IsChildOf(
                leftDotsContainer))
        {
            Debug.LogWarning(
                left.name +
                " is not inside the Left Dots container!"
            );

            return;
        }


        // Don't allow another connection
        // while dragging.
        if (currentLine != null)
        {
            Debug.Log(
                "A connection is already in progress."
            );

            return;
        }


        // The same LEFT answer can only
        // be connected once.
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

    public void EndConnection(
        RightDot right)
    {
        if (currentLeft == null ||
            currentLine == null)
        {
            return;
        }


        if (right == null)
            return;


        // Make sure target is actually
        // inside RIGHT container.
        if (rightDotsContainer != null &&
            !right.transform.IsChildOf(
                rightDotsContainer))
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


        Camera uiCamera =
            GetUICamera();


        Vector2 startScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                leftRect.position
            );


        Vector2 endScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                rightRect.position
            );


        Vector2 start;
        Vector2 end;


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            startScreen,
            uiCamera,
            out start
        );


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            endScreen,
            uiCamera,
            out end
        );


        // Draw final line
        DrawLine(
            start,
            end
        );


        // Check whether IDs match
        bool isCorrect =
            currentLeft.id == right.id;


        // Lock this LEFT answer.
        // Correct or wrong, it can only
        // be used again after Undo.
        connectedLeftDots.Add(
            currentLeft
        );


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

            // Wrong line stays visible.
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

    private void DrawLine(
        Vector2 start,
        Vector2 end)
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
            Debug.Log(
                "Nothing to undo."
            );

            return;
        }


        MatchData last =
            history.Pop();


        // If last connection was correct,
        // remove one correct point.
        if (last.correct)
        {
            correctMatches--;
        }


        // Allow LEFT answer to be selected again.
        if (last.left != null)
        {
            connectedLeftDots.Remove(
                last.left
            );
        }


        // Remove line from screen.
        if (last.line != null)
        {
            Destroy(
                last.line.gameObject
            );
        }


        Debug.Log(
            "Undo! Total Correct = " +
            correctMatches
        );
    }


    // =====================================================
    // CLEAN INSTANCE
    // =====================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}