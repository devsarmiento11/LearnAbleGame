$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$scene = Get-Content "$project/Assets/Settings/Scenes/StudentsScoreScene.unity" -Raw
function Block($id) {
    [regex]::Match($scene, "(?ms)^--- !u!\d+ &$id\r?\n.*?(?=^--- !u!|\z)").Value
}
$scroll = Block '1871452526'
$parent = Block '1871452525'
$bar = Block '2145183683'
$parentWidth = [double][regex]::Match($parent, 'm_SizeDelta: \{x: ([\d.]+)').Groups[1].Value
$barWidth = [double][regex]::Match($bar, 'm_SizeDelta: \{x: ([\d.]+)').Groups[1].Value
$spacing = [double][regex]::Match($scroll, 'm_VerticalScrollbarSpacing: (-?[\d.]+)').Groups[1].Value
if ($scroll -notmatch 'm_VerticalScrollbarVisibility: 2' -or ($parentWidth - $barWidth - $spacing) -gt 4) {
    throw 'Expected original oversized-scrollbar regression setup has changed; inspect scene before updating test.'
}
Write-Output "Reproduced scene geometry: overflow reduces viewport from $parentWidth to $($parentWidth - $barWidth - $spacing) pixels under ExpandViewport."
$stubs = @'
using System;
using UnityEngine;
using UnityEngine.UI;
namespace UnityEngine {
    public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} public static Vector2 zero=>new Vector2(0,0); public static Vector2 one=>new Vector2(1,1); }
    public struct Color { public float a; }
    public class RectTransform { public Vector2 anchorMin,anchorMax,offsetMin,offsetMax; }
}
namespace UnityEngine.UI {
    public class Graphic { public Color color; public bool raycastTarget=true; }
    public class Scrollbar {
        public Graphic[] graphics;
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return (T[])(object)graphics; }
    }
    public class ScrollRect {
        public enum ScrollbarVisibility { Permanent,AutoHide,AutoHideAndExpandViewport }
        public ScrollbarVisibility verticalScrollbarVisibility=ScrollbarVisibility.AutoHideAndExpandViewport;
        public ScrollbarVisibility horizontalScrollbarVisibility=ScrollbarVisibility.AutoHideAndExpandViewport;
        public RectTransform viewport=new RectTransform();
        public Scrollbar verticalScrollbar;
    }
}
public static class ScoreScrollHarness {
    static void Check(bool ok,string message) {if(!ok) throw new Exception(message);}
    public static void Run(float width,float barWidth,float spacing) {
        var invisible=new Graphic{color=new Color{a=0}};
        var visible=new Graphic{color=new Color{a=1}};
        var scroll=new ScrollRect{verticalScrollbar=new Scrollbar{graphics=new []{invisible,visible}}};
        // Reproduce the oversized bar with 8 rows, then switch short/long lists.
        foreach(int count in new []{8,4,8,2,8,150,0,8}) {
            bool overflow=count*40f>241.4257f;
            float oldWidth=width-(overflow?barWidth+spacing:0);
            if(overflow) Check(oldWidth<4,"Failed to reproduce original collapse");
            ScoreTableScroll.Configure(scroll);
            bool expand=scroll.verticalScrollbarVisibility==ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            float actualWidth=width-(overflow&&expand?barWidth+spacing:0);
            Check(actualWidth==width,"All Subjects collapsed");
            Check(scroll.viewport.anchorMin.x==0 && scroll.viewport.anchorMax.x==1,"Viewport not stretched");
            Check(scroll.viewport.offsetMin.x==0 && scroll.viewport.offsetMax.x==0,"Viewport retained scrollbar inset");
            Check(!invisible.raycastTarget && visible.raycastTarget,"Scrollbar pointer handling incorrect");
        }
        ScoreTableScroll.Configure(null);
        ScoreTableScroll.Configure(new ScrollRect{viewport=null});
    }
}
'@
$source = (Get-Content "$project/Assets/Scripts/ScoreTableScroll.cs" -Raw) -replace '(?m)^using [^;]+;\s*', ''
Add-Type -TypeDefinition ($stubs + "`n" + $source)
[ScoreScrollHarness]::Run($parentWidth,$barWidth,$spacing)
$manager=Get-Content "$project/Assets/Scripts/TeacherScoreManager.cs" -Raw
if ($manager -notmatch 'ScoreTableScroll.Configure\(scoreContent.GetComponentInParent<ScrollRect>\(true\)\)' -or
    $manager -notmatch 'ScoreTableScroll.Configure\(scoreContent.GetComponentInParent<ScrollRect>\(\)\)') {
    throw 'Score table initialization/refresh not wired to the fix.'
}
Write-Output 'PASS: actual scroll helper compiled; viewport stays full width for 0/2/4/8/150 rows and repeated filter changes; invisible scrollbar no longer captures row drags. Initialization and refresh wired.'
