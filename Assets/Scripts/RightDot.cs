using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RightDot : MonoBehaviour, IPointerEnterHandler
{
    public string id;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only finish the connection if the
        // player is still holding the pointer.
        if (IsPointerPressed())
        {
            if (MatchManager.Instance != null)
            {
                MatchManager.Instance.EndConnection(this);
            }
        }
    }

    // ==========================================
    // CHECK POINTER
    // Android Touch + Unity Editor Mouse
    // ==========================================

    private bool IsPointerPressed()
    {
        // Android / touchscreen
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }

        // Unity Editor / PC mouse
        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            return true;
        }

        return false;
    }
}