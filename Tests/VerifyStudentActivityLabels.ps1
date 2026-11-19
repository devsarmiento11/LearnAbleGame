$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $project 'Assets/Scripts/StudentActivityLabels.cs')
function Assert-Equal($expected, $actual, $message) {
    if ($expected -cne $actual) { throw "$message : expected [$expected], got [$actual]" }
}
$titles = @(
    "Let's Trace Vertical Lines", 'Connecting Vertical Dots',
    'Dividing the Pizza in Half', 'Missing Parts', 'Drawing Shapes',
    "Let's Trace Horizontal Lines", 'Connecting Horizontal Dots',
    'Dividing the Pizza in Half', 'Missing Parts', 'Color Shapes',
    "Let's Trace Curved Lines", 'Trace the Egg', "Let's Trace Curved Lines"
)
for ($game = 1; $game -le 13; $game++) {
    foreach ($level in @(1, 2, 3, 4, 5, 10)) {
        $activity = "EnglishGame${game}Level${level}"
        Assert-Equal $titles[$game - 1] ([StudentActivityLabels]::EnglishTitle($activity)) $activity
        Assert-Equal "$level" ([StudentActivityLabels]::Level($activity)) "$activity level"
        $legacy = " english_game_0${game}_level_0${level} "
        Assert-Equal $titles[$game - 1] ([StudentActivityLabels]::EnglishTitle($legacy)) "$activity legacy title"
        Assert-Equal "$level" ([StudentActivityLabels]::Level($legacy)) "$activity legacy level"
    }
    Assert-Equal '-' ([StudentActivityLabels]::Level("EnglishGame$game")) 'Game number is not a level'
}
foreach ($activity in @('ScienceGame1Level3', 'MathGame2Level3', 'EnglishGame14Level3', 'EnglishGame0Level3', 'OtherEnglishGame1Level3')) {
    Assert-Equal $null ([StudentActivityLabels]::EnglishTitle($activity)) 'Unmapped activity keeps original fallback'
    Assert-Equal '3' ([StudentActivityLabels]::Level($activity)) 'Other activity level preserved'
}
Assert-Equal $null ([StudentActivityLabels]::EnglishTitle($null)) 'Missing activity title'
Assert-Equal '-' ([StudentActivityLabels]::Level($null)) 'Missing activity level'
$scienceTitles = @(
    'Drag The Correct Answer', 'What Does Body Need?', 'Math It!',
    'Food or Not Food', 'Good,Bad For Health?', 'Do You Do These?',
    'My Morning Routine', 'Caring For Environment', 'Help The Environment'
)
for ($game = 1; $game -le 9; $game++) {
    foreach ($level in @(1, 2, 3, 4, 5, 10)) {
        $activity = "ScienceGame${game}Level${level}"
        Assert-Equal $scienceTitles[$game - 1] ([StudentActivityLabels]::ScienceTitle($activity)) $activity
        Assert-Equal "$level" ([StudentActivityLabels]::Level($activity)) "$activity level"
        $legacy = " science_game_0${game}_level_0${level} "
        Assert-Equal $scienceTitles[$game - 1] ([StudentActivityLabels]::ScienceTitle($legacy)) "$activity legacy title"
        Assert-Equal "$level" ([StudentActivityLabels]::Level($legacy)) "$activity legacy level"
    }
    Assert-Equal $scienceTitles[$game - 1] ([StudentActivityLabels]::ScienceTitle("ScienceGame$game")) 'Title without level'
    Assert-Equal '-' ([StudentActivityLabels]::Level("ScienceGame$game")) 'Science game number is not a level'
}
foreach ($activity in @($null, 'ScienceGame0Level1', 'ScienceGame10Level1', 'EnglishGame1Level1', 'MathGame1Level1', 'OtherScienceGame1Level1')) {
    Assert-Equal $null ([StudentActivityLabels]::ScienceTitle($activity)) 'Unknown Science title preserves fallback'
}
$manager = Get-Content (Join-Path $project 'Assets/Scripts/StudentProfileTabManager.cs') -Raw
$mathTitles = @(
    'Draw Above the Cake', 'Find The Missing Number', "Let's Count!",
    'Comparing Numbers', "Let's Compare Numbers!", 'Match The Coins',
    'Match The Money', 'How Much?'
)
for ($game = 1; $game -le 8; $game++) {
    foreach ($level in @(1, 2, 3, 4, 5, 10)) {
        $activity = "MathGame${game}Level${level}"
        Assert-Equal $mathTitles[$game - 1] ([StudentActivityLabels]::MathTitle($activity)) $activity
        Assert-Equal "$level" ([StudentActivityLabels]::Level($activity)) "$activity level"
        $legacy = " math_game_0${game}_level_0${level} "
        Assert-Equal $mathTitles[$game - 1] ([StudentActivityLabels]::MathTitle($legacy)) "$activity legacy title"
        Assert-Equal "$level" ([StudentActivityLabels]::Level($legacy)) "$activity legacy level"
    }
    Assert-Equal $mathTitles[$game - 1] ([StudentActivityLabels]::MathTitle("MathGame$game")) 'Math title without level'
    Assert-Equal '-' ([StudentActivityLabels]::Level("MathGame$game")) 'Math game number is not a level'
}
foreach ($activity in @($null, 'MathGame0Level1', 'MathGame9Level1', 'EnglishGame1Level1', 'ScienceGame1Level1', 'OtherMathGame1Level1')) {
    Assert-Equal $null ([StudentActivityLabels]::MathTitle($activity)) 'Unknown Math title preserves fallback'
}
if ($manager -notmatch 'StudentActivityLabels.MathTitle\(name\)') { throw 'Math titles are not connected' }
if ($manager -notmatch 'StudentActivityLabels.ScienceTitle\(name\)') { throw 'Science titles are not connected' }
if ($manager -notmatch 'SetText\(row, "ActivityText", GetActivityName\(record.Name\)\)' -or
    $manager -notmatch 'SetText\(row, "LevelText", GetLevel\(record.Name\)\)') { throw 'Recent activity label bindings missing' }
Write-Output 'PASS: all 13 English, 9 Science, and 8 Math titles, per-level labels, legacy names, unknown activities, and recent activity bindings.'
