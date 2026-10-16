$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
Add-Type -Path @((Join-Path $project 'Assets/Scripts/ScoreSubjectFilter.cs'), (Join-Path $project 'Assets/Scripts/StudentScoreData.cs'))
function Assert-Equal($expected, $actual, $message) {
    if ($expected -ne $actual) { throw "$message : expected [$expected], got [$actual]" }
}
Assert-Equal 'EnglishGame2Level5' ([StudentScoreData]::NormalizeActivity(' english_game_02_level_05 ')) 'Activity identity'
foreach ($pair in @(@('EnglishGame2Level1','ScienceGame1Level1'), @('ScienceGame3Level5','MathGame1Level1'), @('MathGame2Level2','MathGame2Level10'), @('EnglishGame2Level5','EnglishGame10Level1'))) {
    if ([StudentScoreData]::CompareActivities($pair[0], $pair[1]) -ge 0) { throw "Incorrect order: $pair" }
}
$data = [System.Collections.Generic.Dictionary[string,object]]::new()
Assert-Equal $null ([StudentScoreData]::ReadPercentage($data)) 'Missing score stays unknown'
$data['correctAnswers'] = 2L
$data['totalItems'] = 3L
Assert-Equal 67 ([StudentScoreData]::ReadPercentage($data)) 'Percentage fallback'
$data['score'] = 0L
Assert-Equal 0 ([StudentScoreData]::ReadPercentage($data)) 'Real zero is included'
$data['score'] = '85.5'
Assert-Equal 86 ([StudentScoreData]::ReadPercentage($data)) 'Decimal rounding'
$data['score'] = 150L
Assert-Equal 100 ([StudentScoreData]::ReadPercentage($data)) 'Percentage bounds'
$data['timeUsedSeconds'] = -20L
Assert-Equal 0 ([StudentScoreData]::ReadSeconds($data)) 'Negative time'
$data['timeUsedSeconds'] = 3605L
Assert-Equal 3605 ([StudentScoreData]::ReadSeconds($data)) 'Duration'
# Compile the popup's actual aggregation class independently of Unity/Firebase.
$manager = Get-Content (Join-Path $project 'Assets/Scripts/TeacherScoreManager.cs') -Raw
$start = $manager.IndexOf('    private class ActivitySummary')
$end = $manager.IndexOf("`n}", $start)
$summarySource = $manager.Substring($start, $end - $start).Replace('private class ActivitySummary', 'public class ActivitySummary')
Add-Type -TypeDefinition ("using System;`n" + $summarySource)
$summary = [ActivitySummary]::new('EnglishGame1Level1')
$date = [DateTime]::SpecifyKind([DateTime]::Parse('2026-10-09'), [DateTimeKind]::Utc)
$summary.AddAttempt(85, 60, $date)
$summary.AddAttempt(0, 30, $date.AddDays(-1))
$summary.AddAttempt($null, 20, [DateTime]::MinValue)
Assert-Equal 3 $summary.Attempts 'All attempts counted'
Assert-Equal 2 $summary.ScoredAttempts 'Unknown score excluded from extrema'
Assert-Equal 0 $summary.LowestScore 'Actual zero included in lowest'
Assert-Equal 85 $summary.HighestScore 'Highest score'
Assert-Equal 110 $summary.TotalTimeUsedSeconds 'Time summed across attempts'
Assert-Equal $date $summary.LatestCompletion 'Latest completion independent of arrival order'
Write-Output 'PASS: subject/game/level order, canonical grouping, score fallback, missing/zero scores, rounding and duration.'
