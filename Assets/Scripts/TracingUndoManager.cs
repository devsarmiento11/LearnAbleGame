using UnityEngine;

public class TracingUndoManager : MonoBehaviour
{
    public UIDrawing drawing;

    public void Undo()
    {
        if (drawing == null)
        {
            Debug.LogWarning("UIDrawing is not assigned!");
            return;
        }

        drawing.UndoLastStroke();
    }
}