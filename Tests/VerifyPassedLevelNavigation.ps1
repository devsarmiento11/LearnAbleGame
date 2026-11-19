$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$stubs = @'
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace UnityEngine {
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) {} }
    public static class Application { public static bool Available = true; public static bool CanStreamedLevelBeLoaded(string s) { return Available; } }
    public static class Mathf {
        public static int Min(int a,int b) { return Math.Min(a,b); }
        public static int Max(int a,int b) { return Math.Max(a,b); }
        public static int Clamp(int v,int a,int b) { return Math.Max(a,Math.Min(v,b)); }
    }
    public class MonoBehaviour { public GameObject gameObject = new GameObject(); }
    public class GameObject { public Scene scene; public bool active; public void SetActive(bool v) { active=v; } }
    public static class Debug { public static void Log(string s) {} }
}
namespace UnityEngine.SceneManagement {
    public struct Scene { public string name; }
    public static class SceneManager { public static string Current; public static Scene GetActiveScene() { return new Scene {name=Current}; } }
}
public static class LearningDataStore { public static string CurrentUserId = "student-a"; }
public static class PassedNavigationHarness {
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run() {
        foreach (string subject in new [] {"English", "Math", "Science"}) {
            int count = subject == "English" ? 13 : subject == "Math" ? 8 : 9;
            for (int game=1; game<=count; game++) for (int level=1; level<=5; level++) {
                string menu=subject+"Game"+game;
                SceneManager.Current=menu+"Level"+level;
                Check(PassedLevelNavigation.Destination("ScoreScene")=="ScoreScene","Skipped results");
                SceneManager.Current="ScoreScene";
                Check(PassedLevelNavigation.ContinueDestination("SettingsScene")=="SettingsScene","Settings redirected");
                Check(PassedLevelNavigation.ContinueDestination("ModuleActivitiesScene")==menu,"Wrong continue destination");
                SceneManager.Current="SettingsScene";
                Check(PassedLevelNavigation.ContinueDestination("ScoreScene")=="ScoreScene","Settings return redirected");
                SceneManager.Current="ScoreScene";
                Check(PassedLevelNavigation.ContinueDestination("ModuleActivitiesScene")==menu,"Settings lost pending preview");
                var panels=new GameObject[5];
                for(int i=0;i<5;i++) panels[i]=new GameObject();
                object manager=Activator.CreateInstance(Type.GetType(subject+"GameManager"));
                var type=manager.GetType();
                ((MonoBehaviour)manager).gameObject.scene=new Scene { name=menu };
                type.GetField("levels").SetValue(manager,panels);
                type.GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(manager,null);
                for(int i=0;i<5;i++) Check(panels[i].active==(i==Math.Min(level,4)),"Wrong preview panel");
                Check(PassedLevelNavigation.ConsumePreview(menu,5)==0,"Request reused");
                type.GetMethod("PreviousLevel").Invoke(manager,null);
                Check(panels[Math.Min(level,4)-1].active,"Previous arrow out of sync");
                type.GetMethod("NextLevel").Invoke(manager,null);
                Check(panels[Math.Min(level,4)].active,"Next arrow out of sync");
            }
        }
        SceneManager.Current="EnglishGame2Level1";
        Check(PassedLevelNavigation.Destination("ScoreScene",false)=="ScoreScene","Failed attempt redirected");
        Check(PassedLevelNavigation.ConsumePreview("EnglishGame2",5)==0,"Failed attempt queued");
        PassedLevelNavigation.Destination("ScoreScene");
        LearningDataStore.CurrentUserId="student-b";
        Check(PassedLevelNavigation.ConsumePreview("EnglishGame2",5)==0,"Student request leaked");
        PassedLevelNavigation.Destination("ScoreScene");
        Check(PassedLevelNavigation.ConsumePreview("MathGame2",5)==0,"Activity request leaked");
        Application.Available=false;
        Check(PassedLevelNavigation.Destination("ScoreScene")=="ScoreScene","Missing menu ignored fallback");
        Application.Available=true;
        SceneManager.Current="EnglishGame2Level1";
        PassedLevelNavigation.Destination("ScoreScene");
        SceneManager.Current="ScoreSceneFailed";
        Check(PassedLevelNavigation.ContinueDestination("ModuleActivitiesScene")=="ModuleActivitiesScene","Failure advanced");
        PassedLevelNavigation.Reset();
        SceneManager.Current="ScoreScene";
        Check(PassedLevelNavigation.ContinueDestination("ModuleActivitiesScene")=="ModuleActivitiesScene","New attempt retained pending preview");
        foreach(string scene in new [] {"EnglishGame2", "SettingsScene", "EnglishGame2Level6", "MathGameScene"}) {
            SceneManager.Current=scene;
            Check(PassedLevelNavigation.Destination("ScoreScene")=="ScoreScene","Non-game scene redirected");
        }
    }
}
'@
$sources = @('PassedLevelNavigation','EnglishGameManager','MathGameManager','ScienceGameManager') | ForEach-Object {
    (Get-Content "$project/Assets/Scripts/$_.cs" -Raw) -replace '(?m)^using [^;]+;\s*', ''
}
Add-Type -TypeDefinition ($stubs + "`n" + ($sources -join "`n"))
[PassedNavigationHarness]::Run()
Write-Output 'PASS: actual navigation and three menu managers compiled; all 150 levels show results before the next preview; Settings, failure, arrows, Level 5, student isolation and fallback verified.'

$build = Get-Content "$project/ProjectSettings/EditorBuildSettings.asset" -Raw
$enabled = @{}
foreach ($m in [regex]::Matches($build, '- enabled: 1\s+path: ([^\r\n]+)')) {
    $enabled[[IO.Path]::GetFileNameWithoutExtension($m.Groups[1].Value)] = $true
}
$menus = @(Get-ChildItem "$project/Assets/Settings/Scenes" -Recurse -Filter *.unity | Where-Object { $_.BaseName -match '^(English|Math|Science)Game\d+$' })
if ($menus.Count -ne 30) { throw 'Expected 30 activity menus.' }
$handlers = @('TracingLevelManager','ScienceLevelManager','ScienceMatchingManager','ScienceSelectionManager','EnglishLevel1Manager','EnglishLevel2Manager','SpeechGameManager','DragAnswerLevel')
$handlerGuids = @($handlers | ForEach-Object {
    $source = Get-Content "$project/Assets/Scripts/$_.cs" -Raw
    if ($source -notmatch 'PassedLevelNavigation.Destination') { throw "Unrouted handler $_" }
    [regex]::Match((Get-Content "$project/Assets/Scripts/$_.cs.meta" -Raw), 'guid: (\w+)').Groups[1].Value
})
$missingBuildGames = @()
foreach ($menu in $menus) {
    if (!$enabled[$menu.BaseName]) { throw "Disabled menu $($menu.BaseName)" }
    $menuText = Get-Content $menu.FullName -Raw
    $managerGuids = @('EnglishGameManager','MathGameManager','ScienceGameManager') | ForEach-Object {
        [regex]::Match((Get-Content "$project/Assets/Scripts/$_.cs.meta" -Raw), 'guid: (\w+)').Groups[1].Value
    }
    if (!@($managerGuids | Where-Object { $menuText.Contains($_) }).Count) { throw "Missing menu manager: $($menu.BaseName)" }
    foreach ($level in 1..5) {
        $name = $menu.BaseName + 'Level' + $level
        if (!$enabled[$name]) { $missingBuildGames += $name }
        $path = Join-Path $menu.DirectoryName ($name + '.unity')
        $text = Get-Content $path -Raw
        if (!@($handlerGuids | Where-Object { $text.Contains($_) }).Count) { throw "No routed finish handler in $name" }
    }
}
Write-Output 'PASS: all 30 destination menus are enabled and use updated managers; all 150 game scenes have a routed completion handler.'
if ($missingBuildGames.Count) { Write-Output ('Existing gameplay scenes absent from build list: ' + ($missingBuildGames -join ', ')) }
