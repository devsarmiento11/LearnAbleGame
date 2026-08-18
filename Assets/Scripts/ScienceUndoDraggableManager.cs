using System.Collections.Generic;
using UnityEngine;

public class ScienceUndoDraggableManager : MonoBehaviour
{
    public static ScienceUndoDraggableManager Instance;

    private Stack<DraggableWord> history = new Stack<DraggableWord>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RecordMove(DraggableWord word)
    {
        history.Push(word);
    }

    public void UndoLastMove()
    {
        if (history.Count == 0)
            return;

        DraggableWord word = history.Pop();
        word.ResetPosition();
    }
}