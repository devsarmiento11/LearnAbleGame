using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps the existing score viewport full width when its rows overflow.</summary>
public static class ScoreTableScroll
{
    public static void Configure(ScrollRect scroll)
    {
        if (scroll == null || scroll.viewport == null) return;

        // The scene's invisible scrollbar is as wide as the table. ExpandViewport
        // subtracts that width when All Subjects needs scrolling, clipping every row.
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scroll.viewport.anchorMin = Vector2.zero;
        scroll.viewport.anchorMax = Vector2.one;
        scroll.viewport.offsetMin = Vector2.zero;
        scroll.viewport.offsetMax = Vector2.zero;

        // Transparent scrollbar artwork must not intercept touch drags over rows.
        if (scroll.verticalScrollbar != null)
            foreach (Graphic graphic in scroll.verticalScrollbar.GetComponentsInChildren<Graphic>(true))
                if (graphic.color.a == 0f) graphic.raycastTarget = false;
    }
}
