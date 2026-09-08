using UnityEngine;
using UnityEngine.InputSystem;

namespace TMPro
{
    // Shared only by the TMP hover examples; gameplay keeps its own input flow.
    internal static class TMP_ExamplePointer
    {
        public static bool TryGetPosition(out Vector2 position)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null &&
                (touchscreen.primaryTouch.press.isPressed ||
                 touchscreen.primaryTouch.press.wasPressedThisFrame ||
                 touchscreen.primaryTouch.press.wasReleasedThisFrame))
            {
                position = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            position = Vector2.zero;
            return false;
        }

        public static bool ShiftPressed => Keyboard.current != null &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
    }
}
