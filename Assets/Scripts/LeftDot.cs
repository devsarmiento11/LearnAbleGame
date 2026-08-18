using UnityEngine;
using UnityEngine.EventSystems;

public class LeftDot : MonoBehaviour, IPointerDownHandler
{
    public string id;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Start a connection only from a LEFT dot
        MatchManager.Instance.StartConnection(this);
    }
}