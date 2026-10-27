$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$scenePath = Join-Path $root 'Assets/Settings/Scenes/2ndQuarterMathGame/MathGame3Level1.unity'
$scene = [IO.File]::ReadAllText($scenePath).Replace("`r`n", "`n")
$backup = Join-Path $PSScriptRoot 'MathGame3Level1.before-drag-answers.unity.txt'
if (!(Test-Path $backup)) { [IO.File]::WriteAllText($backup, $scene) }
$guids = @{
    DragAnswerChoice = 'ade873f88ec64a4f93c1ac1a18beb701'
    DragAnswerZone = 'ade873f88ec64a4f93c1ac1a18beb702'
    DragAnswerLevel = 'ade873f88ec64a4f93c1ac1a18beb703'
}
foreach ($name in $guids.Keys) {
    $meta = "fileFormatVersion: 2`nguid: $($guids[$name])`n"
    [IO.File]::WriteAllText((Join-Path $root "Assets/Scripts/$name.cs.meta"), $meta)
}
function Component($id, $go, $name, $fields) {
    return @"
--- !u!114 &$id
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $go}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $($guids[$name]), type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::$name
$fields

"@
}
if ($scene.Contains('Assembly-CSharp::DragAnswerChoice')) { throw 'Scene already wired.' }
$added = ''
$items = @(@(553672429,2100000001,'less'), @(1075697495,2100000002,'greater'), @(570022949,2100000003,'equal'), @(28852968,2100000004,'less'))
foreach ($item in $items) {
    $go = $item[0]; $id = $item[1]; $answer = $item[2]
    $pattern = '(?s)(--- !u!1 &' + $go + '\nGameObject:.*?  m_Component:\n)'
    $scene = [regex]::Replace($scene, $pattern, "`$1  - component: {fileID: $id}`n")
    if ($id -eq 2100000004) { $name = 'DragAnswerZone'; $field = 'acceptedAnswerId' }
    else { $name = 'DragAnswerChoice'; $field = 'answerId' }
    $added += Component $id $go $name "  ${field}: $answer`n  manager: {fileID: 1970100295}"
}
$fields = @"
  choices:
  - {fileID: 2100000001}
  - {fileID: 2100000002}
  - {fileID: 2100000003}
  dropZones:
  - {fileID: 2100000004}
  timer: {fileID: 1023773924}
  passingScore: 50
  scoreSceneName: ScoreScene
  failedScoreSceneName: ScoreSceneFailed
"@
$manager = Component 1970100295 1970100294 'DragAnswerLevel' $fields
$scene = [regex]::Replace($scene, '(?s)--- !u!114 &1970100295\n.*?(?=--- !u!)', $manager)
$scene = $scene.Replace('m_TargetAssemblyTypeName: ScienceLevelManager, Assembly-CSharp', 'm_TargetAssemblyTypeName: DragAnswerLevel, Assembly-CSharp')
$scene = $scene.Replace('m_Target: {fileID: 2086744991}', 'm_Target: {fileID: 1970100295}')
$scene = $scene.Replace('m_TargetAssemblyTypeName: ScienceUndoDraggableManager, Assembly-CSharp', 'm_TargetAssemblyTypeName: DragAnswerLevel, Assembly-CSharp')
# Disable the unused legacy undo component without changing any UI object.
$scene = [regex]::Replace($scene, '(?s)(--- !u!114 &2086744991\n.*?m_Enabled:) 1', '$1 0')
$scene = $scene.Replace('--- !u!1660057539', $added + '--- !u!1660057539')
if (!$scene.Contains('&2100000001' + "`n")) { $scene += $added }
[IO.File]::WriteAllText($scenePath, $scene)
$buildPath = Join-Path $root 'ProjectSettings/EditorBuildSettings.asset'
$build = [IO.File]::ReadAllText($buildPath)
if (!$build.Contains('2ndQuarterMathGame/MathGame3Level1.unity')) {
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'EditorBuildSettings.before-MathGame3-drag.asset.txt'), $build)
    $build = $build.Replace('  m_configObjects:', "  - enabled: 1`n    path: Assets/Settings/Scenes/2ndQuarterMathGame/MathGame3Level1.unity`n    guid: a4d9db7124d6a0445ab3ebca6f4583d3`n  m_configObjects:")
    [IO.File]::WriteAllText($buildPath, $build)
}
Write-Output 'Wired three choices, one zone, Done, Undo, timer, and build scene.'
