$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
# An in-memory preferences adapter keeps these tests away from real accounts.
$preferences = @"
namespace UnityEngine {
    public static class PlayerPrefs {
        private static readonly System.Collections.Generic.Dictionary<string,int> Values = new System.Collections.Generic.Dictionary<string,int>();
        public static int SaveCount;
        public static int GetInt(string key, int fallback) { int value; return Values.TryGetValue(key, out value) ? value : fallback; }
        public static void SetInt(string key, int value) { Values[key] = value; }
        public static void Save() { SaveCount++; }
    }
}
"@
Add-Type -TypeDefinition ((Get-Content (Join-Path $project 'Assets/Scripts/AccountStartState.cs') -Raw) + $preferences)
function Assert($condition, $message) { if (-not $condition) { throw $message } }
Assert (-not [AccountStartState]::HasStarted('00123')) 'New account must start'
[AccountStartState]::MarkStarted('00123')
Assert ([AccountStartState]::HasStarted('00123')) 'Returning account must continue'
Assert (-not [AccountStartState]::HasStarted('123')) 'Leading zeros must identify a different account'
Assert (-not [AccountStartState]::HasStarted('second-account')) 'Another account must still start'
[AccountStartState]::MarkStarted('second-account')
Assert ([AccountStartState]::HasStarted('00123')) 'Switching accounts must preserve the first account'
Assert ([AccountStartState]::HasStarted(' 00123 ')) 'Trim incidental whitespace consistently'
[AccountStartState]::MarkStarted('')
Assert (-not [AccountStartState]::HasStarted('')) 'Signed-out session must not get a shared started flag'
Assert ([UnityEngine.PlayerPrefs]::SaveCount -eq 2) 'Both valid changes must be persisted'
$scene = Get-Content (Join-Path $project 'Assets/Settings/Scenes/PlayContinueScene.unity') -Raw
Assert ($scene.Contains('m_MethodName: StartOrContinue')) 'Button must use the new action'
Assert ($scene.Contains('label: {fileID: 244301221}')) 'Button text must be assigned'
$ids = [regex]::Matches($scene, '(?m)^--- !u!\d+ &(\d+)') | ForEach-Object { $_.Groups[1].Value }
Assert (($ids | Select-Object -Unique).Count -eq $ids.Count) 'Scene IDs must be unique'
Write-Output 'PASS: new/returning accounts, account isolation, leading zeros, persistence calls, signed-out behavior and scene wiring.'
