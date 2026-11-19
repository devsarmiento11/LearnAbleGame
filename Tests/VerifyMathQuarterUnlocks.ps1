$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$build = Get-Content "$project/ProjectSettings/EditorBuildSettings.asset" -Raw
$enabled = @{}
foreach ($entry in [regex]::Matches($build, '- enabled: 1\s+path: ([^\r\n]+)\s+guid: (\w+)')) {
    $enabled[$entry.Groups[1].Value] = $entry.Groups[2].Value
}
$scenes = @(Get-ChildItem "$project/Assets/Settings/Scenes/MathUnlock2ndQuarterFolder/*.unity") +
    @(Get-ChildItem "$project/Assets/Settings/Scenes/MathUnlock3rdQuarterFolder/*.unity")
if ($scenes.Count -ne 8) { throw 'Expected eight quarter unlock scenes.' }
$scriptGuids = @{}
foreach ($meta in Get-ChildItem "$project/Assets/Scripts" -Recurse -Filter *.cs.meta) {
    $guid = [regex]::Match((Get-Content $meta.FullName -Raw), '(?m)^guid: (\w+)').Groups[1].Value
    $scriptGuids[$guid] = $meta.FullName.Substring(0, $meta.FullName.Length - 5)
}
foreach ($scene in $scenes) {
    $text = Get-Content $scene.FullName -Raw
    $path = $scene.FullName.Substring($project.Length + 1).Replace('\', '/')
    $guid = [regex]::Match((Get-Content ($scene.FullName + '.meta') -Raw), '(?m)^guid: (\w+)').Groups[1].Value
    if ($enabled[$path] -ne $guid) { throw "Scene missing, disabled or wrong GUID: $path" }
    $blocks = @{}
    foreach ($block in [regex]::Matches($text, '(?ms)^--- !u!\d+ &(-?\d+)\r?\n(.*?)(?=^--- !u!|\z)')) {
        $blocks[$block.Groups[1].Value] = $block.Groups[2].Value
    }
    foreach ($call in [regex]::Matches($text, '(?s)- m_Target: \{fileID: (\d+)\}.*?m_MethodName: (\w+).*?m_CallState: (\d+)')) {
        $target = $blocks[$call.Groups[1].Value]
        if (-not $target) { throw "Missing callback target in $path" }
        $scriptGuid = [regex]::Match($target, 'm_Script: .*?guid: (\w+)').Groups[1].Value
        $source = $scriptGuids[$scriptGuid]
        $method = $call.Groups[2].Value
        if (-not $source -or (Get-Content $source -Raw) -notmatch "\b$method\s*\(") {
            throw "Missing callback script/method $method in $path"
        }
        if ($call.Groups[3].Value -eq '0') { throw "Disabled callback in $path" }
    }
    foreach ($link in [regex]::Matches($text, 'm_StringArgument: (\S+)')) {
        $name = $link.Groups[1].Value
        if (-not @($enabled.Keys | Where-Object { [IO.Path]::GetFileNameWithoutExtension($_) -eq $name }).Count) {
            throw "Unreachable navigation destination $name in $path"
        }
    }
    if ($scene.BaseName -match '^MathUnlockGame([3-8])$') {
        $number = [int]$Matches[1]
        $quarter = if ($number -le 5) { '2nd' } else { '3rd' }
        $game = Get-Content "$project/Assets/Settings/Scenes/${quarter}QuarterMathGame/MathGame$number.unity" -Raw
        $activity = [regex]::Match($text, '(?m)^  activityId: (.+)\r?$').Groups[1].Value.Trim()
        $gameIds = @([regex]::Matches($game, '(?m)^  activityId: (.+)\r?$') | ForEach-Object { $_.Groups[1].Value.Trim() })
        if ($gameIds.Count -ne 4 -or @($gameIds | Where-Object { $_ -ne $activity }).Count) {
            throw "Gameplay lock IDs disagree with $activity"
        }
        foreach ($level in 2..5) {
            if ($text -notmatch "(?s)m_MethodName: RequestUnlock\s+m_Mode: 3\s+m_Arguments:.*?m_IntArgument: $level\b") {
                throw "Missing unlock request for level $level in $path"
            }
        }
        $manager = @($blocks.Values | Where-Object { $_ -match 'activityId:' })
        if ($manager.Count -ne 1) { throw "Expected one unlock manager in $path" }
        $popup = [regex]::Match($manager[0], 'confirmationPopup: \{fileID: (\d+)\}').Groups[1].Value
        if (-not $blocks[$popup]) { throw "Missing confirmation popup in $path" }
        $levelEntries = [regex]::Matches($manager[0], '(?s)- levelNumber: (\d+)\s+buttonImage: \{fileID: (\d+)\}\s+lockedSprite: \{fileID: (-?\d+), guid: (\w+), type: 3\}\s+unlockedSprite: \{fileID: (-?\d+), guid: (\w+), type: 3\}')
        if (($levelEntries | ForEach-Object { $_.Groups[1].Value }) -join ',' -ne '1,2,3,4,5') { throw "Invalid level images in $path" }
        foreach ($entry in $levelEntries) {
            if (-not $blocks[$entry.Groups[2].Value]) { throw "Missing level image in $path" }
        }
        foreach ($method in @('ConfirmUnlock','CancelUnlock','LockLevels2To5')) {
            if ($text -notmatch "m_MethodName: $method\b") { throw "Missing $method button in $path" }
        }
    }
}
Write-Output 'PASS: all eight scenes enabled with matching GUIDs; navigation and button callbacks connected; all six activities match gameplay lock IDs and levels 1-5.'

$stubs = Get-Content "$PSScriptRoot/MathQuarterUnlockHarness.cs" -Raw
$sources = @('LevelUnlockStorage','UnlockLevelManager','LevelLockController') | ForEach-Object {
    Get-Content "$project/Assets/Scripts/LockUnlock/$_.cs" -Raw
}
Add-Type -TypeDefinition ($stubs + "`n" + (($sources | ForEach-Object { $_ -replace '(?m)^using [^;]+;\s*', '' }) -join "`n"))
[MathQuarterUnlockHarness]::Run()
Write-Output 'PASS: actual unlock scripts compiled; confirm, cancel, repeated clicks, invalid requests, saved state, relock, sprite refresh and gameplay button locks verified using an isolated in-memory Unity substitute.'
