# MathGame2Level1 check and fixes

Concept confirmed from the instruction scene and level data: find the missing number in the sequence, hold the microphone and say six (or a recognized numeric 6), release, reveal the six sprite, then press Done. One correct answer scores 100; unsolved scores 0.

Scene checks: missing number 6, reveal Image, six sprite subasset, result label, microphone pointer-down/up callbacks, Done callback, New Input System UI module and success/failure scene build entries are assigned. No scene/Inspector references were changed.

Changed Assets/Scripts/SpeechGameManager.cs:
- Reset the activity-recording state on entry. This level has no GameTimer to perform that reset, so later attempts could previously skip recording.
- Track microphone hold during asynchronous permission requests; do not start recording after release or after leaving the activity.
- Cancel the owned recognition session when the component/scene is disabled and ignore late results.
- Wait for a pending final transcript before Done computes the score; guard repeated submission.
- Handle missing recognition service and null missing-number entries during setup/checking.

Added Assets/Editor/SpeechGameManagerEditor.cs and its meta. The installed speech plugin's Editor implementation always emits Hello world, without recognizing microphone audio. In Play Mode select SpeechGameManager, type six or another transcript in Test transcript, and click Test spoken answer. This runs the existing answer callback. Android continues using the native speech plugin. The helper is Editor-only and does not ship in Android builds.

Validation: complete gameplay C# compilation passed using the existing Unity Editor and Android compiler configurations; the new custom inspector also compiled. Thirteen deterministic checks against the actual SpeechGameManager source passed with Unity/speech/scoring dependencies simulated: initial hidden state, wrong answer, sixteen rejection, six/numeric-six acceptance and reveal, 100/0 scoring routes, repeat/retry recording, recognition errors, delayed permission, pending Done and scene-exit cleanup. These do not replace Unity Play Mode or physical Android microphone testing. Android service availability, audio recognition accuracy and actual touch/raycast behavior remain device-test items.

The lifecycle fixes affect all scenes using the shared SpeechGameManager. Existing fields/methods, score threshold, number-to-word mappings and scene names remain intact. No Firebase rules or other pending grade-save changes were published.

Backup: MathGame2FixBackup/SpeechGameManager.cs is the exact pre-edit source. Restore it and remove the new custom inspector plus its meta to revert this task only. BehaviorTests.cs and behavior-results.txt record the isolated checks.
