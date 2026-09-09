$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets/Scripts/ScoreSubjectFilter.cs')
foreach ($subject in @('English','Science','Math')) {
    foreach ($other in @('English','Science','Math')) {
        $result = [ScoreSubjectFilter]::Matches("${other}Game1Level5", $subject)
        if ($result -ne ($subject -eq $other)) { throw "Incorrect filter: $subject / $other" }
    }
    if (-not [ScoreSubjectFilter]::Matches("  $($subject.ToLower()) Game 2  ", $subject)) { throw 'Case/space handling failed' }
}
if ([ScoreSubjectFilter]::Subjects[0] -ne 'All Subjects') { throw 'Wrong default' }
foreach ($activity in @('EnglishGame1Level1','ScienceGame2Level5','MathGame1Level3','Legacy Activity')) {
    if (-not [ScoreSubjectFilter]::Matches($activity, 'All Subjects')) { throw 'All subjects hid an activity' }
}
if ([ScoreSubjectFilter]::Matches('MathematicalEnglish', 'Math')) { throw 'Unrelated prefix matched' }
Write-Output 'PASS: all-subject default, three subject filters, case/whitespace and unrelated activities.'
