using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GlowOnClick : MonoBehaviour, IPointerClickHandler
{
    public Color normalColor = Color.white;
    public Color glowColor = new Color(1f, 1f, 0.4f);

    [Header("Correct Answer?")]
    public bool isCorrect;

    private Image image;
    private bool selected;

    public bool IsSelected => selected;
    public bool IsCorrect => isCorrect;

    void Start()
    {
        image = GetComponent<Image>();
        image.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // If already selected, allow deselecting
        if (selected)
        {
            Deselect();

            if (ScienceSelectionManager.Instance != null)
            {
                ScienceSelectionManager.Instance.RemoveSelection(this);
            }

            return;
        }

        // Ask manager if another selection is allowed
        if (ScienceSelectionManager.Instance != null)
        {
            if (!ScienceSelectionManager.Instance.CanSelect())
            {
                Debug.Log("Selection limit reached!");
                return;
            }
        }

        selected = true;
        image.color = glowColor;

        if (ScienceSelectionUndo.Instance != null)
        {
            ScienceSelectionUndo.Instance.RecordSelection(this);
        }

        if (ScienceSelectionManager.Instance != null)
        {
            ScienceSelectionManager.Instance.AddSelection(this);
        }
    }

    public void Deselect()
    {
        selected = false;

        if (image != null)
            image.color = normalColor;
    }
}