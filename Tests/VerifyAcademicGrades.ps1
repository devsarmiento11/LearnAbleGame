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

# Verify both Unity scenes have the shared view and all report labels wired by name.
foreach ($scene in @('StudentGradesScene', 'StudentProfileScene')) {
    $source = Get-Content (Join-Path $project "Assets/Settings/Scenes/$scene.unity") -Raw
    if ($source -notmatch 'guid: 3afde229f5ba4c938bd45d91c9c080f3') { throw "Missing grade view in $scene" }
    $ids = [regex]::Matches($source, '(?m)^--- !u!\d+ &(\d+)') | ForEach-Object { $_.Groups[1].Value }
    if (($ids | Select-Object -Unique).Count -ne $ids.Count) { throw "Duplicate scene IDs in $scene" }
    $names = [regex]::Matches($source, '(?m)^  m_Name: (.+)') | ForEach-Object { $_.Groups[1].Value.Trim().Trim("'").Trim() }
    foreach ($subject in @('English', 'Math', 'Science')) {
        foreach ($field in @('First', 'Second', 'Third', 'Fourth', 'Final', 'Remarks')) {
            if ($names -notcontains "$subject${field}Text") { throw "Missing $subject${field}Text in $scene" }
        }
    }
    if ($names -notcontains 'TeacherRemarkText') { throw "Missing teacher note in $scene" }
}
Write-Output 'PASS: grade calculations, missing values, zero, edits, boundaries, names, student isolation, and scene bindings.'
