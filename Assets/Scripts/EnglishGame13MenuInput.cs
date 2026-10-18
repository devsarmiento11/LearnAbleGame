using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Initializes menu input without changing its layout or appearance.</summary>
[DefaultExecutionOrder(-100)]
public sealed class EnglishGame13MenuInput : MonoBehaviour
{
    private void Awake()
    {
        // The level artwork intentionally uses Y=180. Accept its back-facing
        // graphics as pointer targets without changing any transforms.
        foreach (GraphicRaycaster raycaster in GetComponentsInChildren<GraphicRaycaster>(true))
            raycaster.ignoreReversedGraphics = false;

        EventSystem events = FindAnyObjectByType<EventSystem>();
        if (events != null)
        {
            var input = events.GetComponent<InputSystemUIInputModule>();
            if (input != null)
            {
                // Recreate the UI action bindings for mouse, touch and keyboard.
                // This also avoids depending on stale serialized action references.
                input.AssignDefaultActions();
                input.enabled = true;
            }
        }

        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            // Popup graphics remain modal while video is playing.
            if (IsVideoPopup(graphic.transform))
                continue;

            // Keep graphics belonging to actual controls clickable; labels,
            // backgrounds and badges must never eat the menu's pointer events.
            graphic.raycastTarget = graphic.GetComponentInParent<Selectable>(true) != null;
        }

        // Preserve progression: use the real button for each existing lock,
        // rather than relying on an image to intercept clicks over its centre.
        foreach (LevelLockController level in GetComponentsInChildren<LevelLockController>(true))
            level.buttonsToLock = level.GetComponentsInChildren<Button>(true);

        // Bind the play buttons directly as well as keeping their inspector
        // callbacks. This survives stale serialized UnityEvent type bindings.
        // The router's existing guard prevents double playback on a click.
        var routers = FindObjectsByType<LocalizedVideoPopup>(FindObjectsInactive.Include);
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            int number = LevelNumber(button.transform);
            if (number == 0)
                continue;
            foreach (var router in routers)
            {
                if (router.gameObject.scene != gameObject.scene || router.english == null ||
                    router.english.nextSceneName != "EnglishGame13Level" + number)
                    continue;
                button.onClick.AddListener(router.OpenVideo);
                break;
            }
        }
    }

    private static int LevelNumber(Transform current)
    {
        while (current != null)
        {
            for (int level = 1; level <= 5; level++)
                if (current.name.Trim() == "Level" + level + "Panel")
                    return level;
            current = current.parent;
        }
        return 0;
    }

    private static bool IsVideoPopup(Transform current)
    {
        while (current != null)
        {
            if (current.name.StartsWith("VideoPopupPanel"))
                return true;
            current = current.parent;
        }
        return false;
    }
}
