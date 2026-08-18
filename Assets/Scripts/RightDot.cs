using UnityEngine;
using UnityEngine.EventSystems;

public class RightDot : MonoBehaviour, IPointerEnterHandler
{
    public string id;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only allow the connection to end on a RIGHT dot
        if (Input.GetMouseButton(0))
        {
            MatchManager.Instance.EndConnection(this);
        }
    }
}