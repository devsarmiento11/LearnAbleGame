using System.Collections.Generic;
using UnityEngine;

public class ScienceSelectionUndo : MonoBehaviour
{
    public static ScienceSelectionUndo Instance;

    private Stack<GlowOnClick> history = new Stack<GlowOnClick>();

    void Awake()
    {
        Instance = this;
    }

    public void RecordSelection(GlowOnClick item)
    {
        history.Push(item);
    }

    public void Undo()
    {
        if (history.Count == 0)
            return;

        GlowOnClick item = history.Pop();

        if (item != null)
            item.Deselect();
    }
}