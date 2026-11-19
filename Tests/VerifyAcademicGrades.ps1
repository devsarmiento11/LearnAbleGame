$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
Add-Type -Path (Join-Path $project 'Assets/Scripts/AcademicGradeReport.cs')

function Assert-Equal($expected, $actual, $message) {
    if ($expected -ne $actual) { throw "$message : expected [$expected], got [$actual]" }
}

$profile = [System.Collections.Generic.Dictionary[string,object]]::new()
$report = [AcademicGradeReport]::new($profile)
Assert-Equal $null ($report.GeneralAverage()) 'Empty report average'
Assert-Equal '' ([AcademicGradeReport]::Format($report.Grade('Math', '1ST'))) 'Missing quarter stays blank'
Assert-Equal '' ([AcademicGradeReport]::Remark($null)) 'Missing grade has no remark'

$grades = [System.Collections.Generic.Dictionary[string,object]]::new()
$grades['ENGLISH_1ST'] = 80L
$grades['ENGLISH_2ND'] = 90L
$grades['MATH_1ST'] = 100L
$profile['academicGrades'] = $grades
$profile['teacherRemark'] = 'Keep practicing.'
$report = [AcademicGradeReport]::new($profile)
Assert-Equal 85 ($report.SubjectAverage('English')) 'Entered-quarter average'
Assert-Equal 92.5 ($report.GeneralAverage()) 'Subjects have equal weight despite different quarter counts'
Assert-Equal 'Keep practicing.' $report.TeacherRemark 'Teacher note'
Assert-Equal $null ($report.SubjectAverage('Science')) 'Ungraded subject stays blank'

$grades['ENGLISH_1ST'] = 70L
Assert-Equal 80 ($report.SubjectAverage('English')) 'Editing replaces the old quarter'
Assert-Equal 100 ($report.Grade('Math', '1ST')) 'Other subject preserved'
$grades['SCIENCE_1ST'] = 0L
Assert-Equal 0 ($report.SubjectAverage('Science')) 'Zero is an entered grade'
Assert-Equal 'Failed' ([AcademicGradeReport]::Remark($report.SubjectAverage('Science'))) 'Zero fails'
Assert-Equal 60 ($report.GeneralAverage()) 'Zero included in general average'

foreach ($boundary in @(
    @(74.99, 'Failed'), @(75, 'Good'), @(84.99, 'Good'),
    @(85, 'Very Good'), @(89.99, 'Very Good'), @(90, 'Excellent'),
    @(95, 'Excellent'), @(100, 'Excellent')
)) {
    Assert-Equal $boundary[1] ([AcademicGradeReport]::Remark([decimal]$boundary[0])) "Boundary $($boundary[0])"
}
$profile['firstName'] = 'Mary Jane'
$profile['middleName'] = 'Santos'
$profile['lastName'] = 'Dela Cruz'
$profile['name'] = 'Mary Jane Santos Dela Cruz'
Assert-Equal 'Mary Jane Dela Cruz' ([AcademicGradeReport]::StudentName($profile, '')) 'Compound names retain first and last names only'
Assert-Equal $null ([AcademicGradeReport]::new([System.Collections.Generic.Dictionary[string,object]]::new()).GeneralAverage()) 'Different student does not inherit grades'

Assert-Equal '1ST,2ND,3RD' ([AcademicGradeReport]::Quarters -join ',') 'Only three supported quarters'
$threeQuarterGrades = [System.Collections.Generic.Dictionary[string,object]]::new()
$threeQuarterProfile = [System.Collections.Generic.Dictionary[string,object]]::new()
$threeQuarterProfile['academicGrades'] = $threeQuarterGrades
$threeQuarterReport = [AcademicGradeReport]::new($threeQuarterProfile)
foreach ($subject in [AcademicGradeReport]::Subjects) {
    $threeQuarterGrades["$($subject.ToUpperInvariant())_4TH"] = 100L
}
Assert-Equal $null ($threeQuarterReport.GeneralAverage()) 'Legacy fourth-quarter grades alone leave report blank'
foreach ($subject in [AcademicGradeReport]::Subjects) {
    $threeQuarterGrades["$($subject.ToUpperInvariant())_1ST"] = 70L
    $threeQuarterGrades["$($subject.ToUpperInvariant())_2ND"] = 80L
    $threeQuarterGrades["$($subject.ToUpperInvariant())_3RD"] = 90L
    Assert-Equal 80 ($threeQuarterReport.SubjectAverage($subject)) 'Final average uses quarters one to three'
    Assert-Equal $null ($threeQuarterReport.Grade($subject, '4TH')) 'Legacy fourth quarter is not displayed'
}
Assert-Equal 80 ($threeQuarterReport.GeneralAverage()) 'General average excludes legacy fourth quarter'

# Verify both Unity scenes have the shared view and all report labels wired by name.
foreach ($scene in @('StudentGradesScene', 'StudentProfileScene')) {
    $source = Get-Content (Join-Path $project "Assets/Settings/Scenes/$scene.unity") -Raw
    if ($source -notmatch 'guid: 3afde229f5ba4c938bd45d91c9c080f3') { throw "Missing grade view in $scene" }
    $ids = [regex]::Matches($source, '(?m)^--- !u!\d+ &(\d+)') | ForEach-Object { $_.Groups[1].Value }
    if (($ids | Select-Object -Unique).Count -ne $ids.Count) { throw "Duplicate scene IDs in $scene" }
    $names = [regex]::Matches($source, '(?m)^  m_Name: (.+)') | ForEach-Object { $_.Groups[1].Value.Trim().Trim("'").Trim() }
    foreach ($subject in @('English', 'Math', 'Science')) {
        foreach ($field in @('First', 'Second', 'Third', 'Final', 'Remarks')) {
            if ($names -notcontains "$subject${field}Text") { throw "Missing $subject${field}Text in $scene" }
        }
    }
    if ($names -notcontains 'TeacherRemarkText') { throw "Missing teacher note in $scene" }
}

$inputScene = Get-Content (Join-Path $project 'Assets/Settings/Scenes/InputGradeScene.unity') -Raw
$profileScene = Get-Content (Join-Path $project 'Assets/Settings/Scenes/StudentProfileScene.unity') -Raw
if ($profileScene -match 'm_Name: [^\r\n]*Fourth') { throw 'Profile grade board still contains a fourth quarter' }
if ($profileScene -notmatch 'textRoot: \{fileID: 1944406618\}\s+useSelectedStudent: 0') {
    throw 'Profile grade view must use the signed-in student and GradesPanel root'
}
if ($profileScene -notmatch 'gradesPanel: \{fileID: 1944406617\}') { throw 'Profile grade tab lost its panel binding' }

# Compare the copied UI records with the source, preserving all visual properties
# and asset references while normalizing scene-local IDs and the panel parent.
$gradeScene = Get-Content (Join-Path $project 'Assets/Settings/Scenes/StudentGradesScene.unity') -Raw
$sourceCopies = [System.Collections.Generic.List[string]]::new()
$profileCopies = [System.Collections.Generic.List[string]]::new()
for ($sceneIndex = 0; $sceneIndex -lt 2; $sceneIndex++) {
    $sceneText = if ($sceneIndex -eq 0) { $gradeScene } else { $profileScene }
    $blocks = [regex]::Matches($sceneText, '(?ms)^--- !u!(\d+) &(\d+)\r?\n.*?(?=^--- !u!|\z)')
    $localIds = @{}
    foreach ($block in $blocks) { $localIds[$block.Groups[2].Value] = $true }
    foreach ($block in $blocks) {
        if ($sceneIndex -eq 1 -and ([long]$block.Groups[2].Value -lt 3000000000L -or $block.Groups[1].Value -eq '1660057539')) { continue }
        $normalized = [regex]::Replace($block.Value, '(?m)^(--- !u!\d+ &)\d+', '${1}LOCAL')
        $normalized = [regex]::Replace($normalized, '\{fileID: (\d+)\}', {
            param($match)
            if ($localIds.ContainsKey($match.Groups[1].Value)) { '{fileID: LOCAL}' } else { $match.Value }
        }).Replace("`r`n", "`n").TrimEnd()
        if ($sceneIndex -eq 0) { $sourceCopies.Add($normalized) } else { $profileCopies.Add($normalized) }
    }
}
Assert-Equal 148 $profileCopies.Count 'Complete updated profile grade layout'
foreach ($block in $profileCopies) {
    if (!$sourceCopies.Contains($block)) { throw 'Profile grade UI differs from the current student grade design' }
}
if ($inputScene -notmatch 'guid: c4c7d5ea441197d4d8af08e1fc8ed96f') { throw 'Missing input grade manager' }
foreach ($field in @('studentNameText', 'gradeLevelText', 'subjectDropdown', 'quarterDropdown', 'gradeInputField', 'teacherRemarkInputField', 'confirmationPopup')) {
    $binding = [regex]::Match($inputScene, "(?m)^  ${field}: \{fileID: (\d+)\}")
    if (!$binding.Success -or $binding.Groups[1].Value -eq '0' -or
        $inputScene -notmatch "(?m)^--- !u!\d+ &$($binding.Groups[1].Value)\r?$") {
        throw "Missing or broken input binding: $field"
    }
}
foreach ($method in @('OpenConfirmation', 'ConfirmSaveGrade', 'CancelConfirmation')) {
    if ($inputScene -notmatch "m_Target: \{fileID: 1241545492\}\s+m_TargetAssemblyTypeName: InputGradeManager, Assembly-CSharp\s+m_MethodName: $method") {
        throw "Missing grade button connection: $method"
    }
}
Write-Output 'PASS: three-quarter calculations, legacy data, missing values, zero, edits, boundaries, names, student isolation, scene bindings, and save/cancel buttons.'
