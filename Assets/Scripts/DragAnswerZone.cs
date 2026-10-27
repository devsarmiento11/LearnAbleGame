using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DragAnswerZone : MonoBehaviour, IDropHandler
{
    public string acceptedAnswerId;
    public DragAnswerLevel manager;
    public DragAnswerChoice Occupant { get; internal set; }
    public bool IsCorrect => Occupant != null && !string.IsNullOrWhiteSpace(acceptedAnswerId)
        && string.Equals(Occupant.answerId, acceptedAnswerId, StringComparison.Ordinal);

    public void OnDrop(PointerEventData data)
    {
        if (manager == null || manager.IsFinished || data.pointerDrag == null) return;
        var choice = data.pointerDrag.GetComponent<DragAnswerChoice>();
        if (choice == null || !choice.IsDragging || choice.manager != manager) return;
        if (Occupant != null) Occupant.ReturnHome();
        choice.Accept(this);
    }
}
