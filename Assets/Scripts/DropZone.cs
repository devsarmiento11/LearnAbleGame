using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    public string correctWord;

    private bool isCorrect = false;

    public bool IsCorrect()
    {
        return isCorrect;
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableWord word = eventData.pointerDrag.GetComponent<DraggableWord>();

        if (word == null)
            return;

        RectTransform wordRect = word.GetComponent<RectTransform>();
        RectTransform zoneRect = GetComponent<RectTransform>();

        // Save for Undo
        ScienceUndoDraggableManager.Instance.RecordMove(word);

        // Snap word
        wordRect.anchoredPosition = zoneRect.anchoredPosition;

        // Check correctness
        isCorrect = word.gameObject.name == correctWord;

        if (isCorrect)
            Debug.Log("Correct");
        else
            Debug.Log("Wrong");
    }
}