$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$manager = Get-Content "$project/Assets/Scripts/TeacherScoreManager.cs" -Raw
function Extract-Block([string]$marker) {
    $start = $manager.IndexOf($marker)
    if ($start -lt 0) { throw "Missing $marker" }
    $brace = $manager.IndexOf('{', $start)
    $depth = 1
    $end = $brace + 1
    while ($depth -gt 0) {
        if ($manager[$end] -eq '{') { $depth++ }
        if ($manager[$end] -eq '}') { $depth-- }
        $end++
    }
    return $manager.Substring($start, $end - $start)
}
$methods = @('private void RebuildScoreRows', 'private static string GetText', 'private static DateTime GetDateTime', 'private class ActivitySummary') | ForEach-Object { Extract-Block $_ }
$source = @'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
public class DocumentSnapshot {
    public bool Exists = true;
    public Dictionary<string,object> Data;
    public Dictionary<string,object> ToDictionary() { return Data; }
}
public class Timestamp { public DateTime Value; public DateTime ToDateTime() { return Value; } }
public class TeacherTableHarness {
    string selectedSubject = "All Subjects";
    List<ActivitySummary> rows = new List<ActivitySummary>();
    string status;
    int scrollResets;
    void ClearScoreRows() { rows.Clear(); }
    void AddScoreRow(ActivitySummary summary) { rows.Add(summary); }
    void ShowScoreStatus(string message) { status=message; }
    void ResetScoreScrollToTop() { scrollResets++; }
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static DocumentSnapshot Attempt(string name, object score, object date) {
        return new DocumentSnapshot {Data = new Dictionary<string,object> {
            {"activityName",name}, {"score",score}, {"timeUsedSeconds",20L}, {"completedAt",date}
        }};
    }
    public static void Run() {
        var t=new TeacherTableHarness();
        var date=new DateTime(2026,11,16,0,0,0,DateTimeKind.Utc);
        var data=new [] {
            Attempt("EnglishGame1Level1",100L,new Timestamp{Value=date}),
            Attempt(" english_game_01_level_01 ",0L,"2026-11-15T00:00:00Z"),
            Attempt("ScienceGame1Level2",75L,date),
            Attempt("MathGame2Level3",50L,"invalid"),
            Attempt("Legacy Activity",null,null)
        };
        for(int repeat=0;repeat<10;repeat++) {
            foreach(string subject in new [] {"All Subjects","English","All Subjects","Science","Math","All Subjects"}) {
                t.selectedSubject=subject;
                t.RebuildScoreRows(data);
                Check(t.rows.Count==(subject=="All Subjects"?4:1),"Filter rows missing: "+subject);
                Check(t.status=="","False empty status");
                if(subject=="English" || subject=="All Subjects") {
                    var english=t.rows[0];
                    Check(english.Attempts==2 && english.LowestScore==0 && english.HighestScore==100,"Wrong aggregation");
                    Check(english.TotalTimeUsedSeconds==40 && english.LatestCompletion==date,"Wrong time/date");
                }
            }
        }
        t.selectedSubject="All Subjects";
        t.RebuildScoreRows(new DocumentSnapshot[0]);
        Check(t.rows.Count==0 && t.status=="No activity scores yet.","Empty student retained rows");
        t.RebuildScoreRows(new [] {data[0]});
        Check(t.rows.Count==1 && t.status=="","First load remained empty");
        t.selectedSubject="Math";
        t.RebuildScoreRows(new [] {data[0]});
        Check(t.rows.Count==0 && t.status=="No Math activity scores yet.","Empty subject status wrong");
        t.selectedSubject="All Subjects";
        t.RebuildScoreRows(new [] {data[0]});
        Check(t.rows.Count==1 && t.status=="","Returning to All Subjects lost English");
        Check(t.scrollResets==64,"Not every render resets scrolling");
    }
__METHODS__
}
'@
$source = $source.Replace('__METHODS__', ($methods -join "`n"))
$shared = @('ScoreSubjectFilter','StudentScoreData') | ForEach-Object {
    (Get-Content "$project/Assets/Scripts/$_.cs" -Raw) -replace '(?m)^using [^;]+;\s*', ''
}
Add-Type -TypeDefinition ($source + "`n" + ($shared -join "`n"))
[TeacherTableHarness]::Run()
Write-Output 'PASS: actual table aggregation/render selection tested across 60 repeated filter changes, empty/new students, first snapshot, mixed subjects, legacy names, zero scores and date formats.'
# Verify the real scene still supplies the existing controls and row bindings.
$scene = Get-Content "$project/Assets/Settings/Scenes/StudentsScoreScene.unity" -Raw
foreach ($field in @('content','studentRowPrefab','studentScorePopup','popupStudentNameText','popupStudentGradeText','scoreContent','scoreDetailRowPrefab')) {
    if ($scene -notmatch "(?m)^  ${field}: \{fileID: [1-9]") { throw "Missing scene binding: $field" }
}
$row = Get-Content "$project/Assets/Prefabs/ScoreDetailRow.prefab" -Raw
foreach ($field in @('activityText','levelText','lowestScoreText','highestScoreText','timeUsedText','attemptsText','dateText')) {
    if ($row -notmatch "(?m)^  ${field}: \{fileID: [1-9]") { throw "Missing row binding: $field" }
}
if ((Extract-Block 'private System.Collections.IEnumerator RefreshScoreLayoutAfterFrame') -notmatch 'yield return null;') { throw 'Missing deferred layout refresh' }
if ((Extract-Block 'private void RefreshScoreLayout') -notmatch 'Canvas.ForceUpdateCanvases\(\)') { throw 'Missing layout synchronization' }
Write-Output 'PASS: scene/prefab references intact; table schedules a layout refresh after Unity finishes the frame.'
