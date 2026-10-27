using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class DragAnswerChoice : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Tooltip("Match this ID to a drop zone's accepted answer ID. Independent of image and object name.")]
    public string answerId;
    public DragAnswerLevel manager;
    public DragAnswerZone Zone { get; internal set; }
    public bool IsDragging { get; private set; }
    RectTransform rect;
    CanvasGroup group;
    Transform home;
    Vector3 homePosition;
    int sibling;
    int pointer;
    Vector3 offset;
    bool accepted;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        home = transform.parent;
        homePosition = rect.localPosition;
        sibling = transform.GetSiblingIndex();
    }

    public void OnBeginDrag(PointerEventData data)
    {
        if (IsDragging || manager == null || manager.IsFinished || !manager.BeginMove()) return;
        pointer = data.pointerId;
        IsDragging = true;
        accepted = false;
        if (Zone != null) Zone.Occupant = null;
        Zone = null;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        rect.SetParent(canvas.transform, true);
        rect.SetAsLastSibling();
        group.blocksRaycasts = false;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)rect.parent,
            data.position, data.pressEventCamera, out var point)) offset = rect.position - point;
    }

    public void OnDrag(PointerEventData data)
    {
        if (!IsDragging || pointer != data.pointerId) return;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)rect.parent,
            data.position, data.pressEventCamera, out var point)) rect.position = point + offset;
    }

    internal void Accept(DragAnswerZone zone)
    {
        Zone = zone;
        zone.Occupant = this;
        rect.position = zone.transform.position;
        accepted = true;
    }

    // A placed image covers the zone, so forward replacement drops to that zone.
    public void OnDrop(PointerEventData data)
    {
        if (Zone != null) Zone.OnDrop(data);
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (!IsDragging || pointer != data.pointerId) return;
        if (!accepted) ReturnHome();
        IsDragging = false;
        group.blocksRaycasts = true;
        manager.EndMove();
    }

    internal void ReturnHome()
    {
        if (Zone != null) Zone.Occupant = null;
        Zone = null;
        rect.SetParent(home, true);
        rect.localPosition = homePosition;
        rect.SetSiblingIndex(sibling);
        group.blocksRaycasts = true;
    }
}
