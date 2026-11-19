using UnityEngine;
using UnityEngine.UI;

/// <summary>Accepts pointer input on the menu's intentionally mirrored artwork.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(GraphicRaycaster))]
public sealed class ThirdQuarterEnglishMenuInput : MonoBehaviour
{
    private void Awake()
    {
        // Level3Panel/Gamebtn faces backwards (Y = 180). Preserve that visual
        // setup while allowing the existing Button and OnClick to receive input.
        GetComponent<GraphicRaycaster>().ignoreReversedGraphics = false;
    }
}
