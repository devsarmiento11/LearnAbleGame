# LearnAble Input System migration — 6 September 2026

Project: E:\UnityProjects\LearnAble\LearnAbleGame

Active Input Handling remains **Input System Package (New)** (`activeInputHandler: 1`). Input System package 1.19.0 was already installed and was not changed.

## A–C. Scripts changed and replacements

| Script | Finding | Change | Editor mouse | Android touch |
|---|---|---|---|---|
| Assets/Scripts/DrawingManager.cs | Already migrated, but the mere presence of a touchscreen prevented mouse fallback | Only prioritize touch when pressed, pressed this frame, or released this frame. Preserve primaryTouch/Mouse press, hold and release. Use the drawing area's Canvas camera for screen conversion (null for overlay). | Supported by code | Supported by code |
| Assets/TextMesh Pro/Examples & Extras/Scripts/CameraController.cs | Input.GetAxis (Mouse X/Y/ScrollWheel), GetKey (Shift), GetKeyDown (I/F/S), GetMouseButton (0/1/2), mousePosition, touchCount, GetTouch, legacy Touch/TouchPhase, simulateMouseWithTouches | Null-safe Mouse delta, scroll, position and buttons; Keyboard keys with unchanged mappings; count pressed Touchscreen.touches and retain single-finger orbit/two-finger pinch. Suppress mouse during active touch. Remove obsolete mouse simulation setting. Preserve 0.1 axis sensitivity from InputManager.asset, including Windows platform-specific scroll normalization. | Orbit, pan, target selection, wheel and Shift shortcuts | Original orbit/pinch gestures |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextEventHandler.cs | Input.mousePosition in text hit testing | Shared touch-first pointer position with mouse fallback and a no-device guard. Selection events and camera logic retained. | Supported by code | Supported by code |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_A.cs | Input.mousePosition; GetKey for left/right Shift | Shared pointer position and null-safe Keyboard Shift checks. Character/word/link selection unchanged. | Supported, including Shift | Pointer selection; Shift still requires keyboard |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_B.cs | Same position/Shift calls, plus old calls in commented click examples | Same shared pointer/Shift handling. Commented click examples now use eventData.position. Existing event interfaces and camera logic retained. | Supported, including Shift | Pointer selection; Shift still requires keyboard |

Added `Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_ExamplePointer.cs` and its `.meta`: an internal helper used only by the three TMP examples. It checks touch press/release before mouse, handles missing devices, and centralizes Shift checks. Existing script GUIDs, class names, public/serialized fields and button methods were preserved.

## D. EventSystems/scenes

Changed only `Assets/TextMesh Pro/Examples & Extras/Scenes/12a - Text Interactions.unity`.

Replaced its StandaloneInputModule serialization with InputSystemUIInputModule, preserving the component fileID, EventSystem GameObject and every other scene object. Bound the installed package's DefaultInputActions (the same verified asset already used by MainMenu), including point, clicks, scroll, navigation, submit and cancel. Repeat delay/rate remain 0.5/0.1. These bindings include mouse and touchscreen input as well as keyboard navigation.

All 143 `.unity`/`.prefab` files were scanned, including recovery and TMP examples. Across them, all 104 EventSystems have exactly one enabled New Input System UI module on the same GameObject. No StandaloneInputModule or TouchInputModule remains. Required UI action references were checked for null bindings. The complete per-file inventory is in `InputMigrationBackup-20260906/Validation/scene-audit.csv`.

## E. Inspected, unchanged

All existing 103 C# scripts were scanned; the final total is 104 with the helper. The full inventory follows below. Only the five scripts above were edited.

Detailed interaction review left MatchManager.cs, RightDot.cs and LeftDot.cs unchanged: they already use New Input System polling or pointer events. Matching line creation, correct/wrong scoring, retained wrong lines and Undo were not changed. UIDrawing.cs already uses PointerEventData.position/pressEventCamera for tracing; drawing bounds, brush spacing, completion and Undo were left unchanged. DraggableWord.cs and DropZone.cs already use UI drag/drop events and were left unchanged.

Firebase/plugin sources, microphone/speech, authentication, scores, grades, unlocking, PlayerPrefs, navigation, video, language and other game systems were not edited. The sibling workspace Assets folder contained no files. No project package or Player Settings edits were needed.

## F. Validation and uncertainty

- Compiled the complete Assembly-CSharp source set using Unity 6000.4.10f1's bundled C# compiler and existing Unity-generated Editor and Android response files. Both returned exit 0 with no diagnostics. Added the new helper explicitly to both checks. Android defines include UNITY_ANDROID and ENABLE_INPUT_SYSTEM, without ENABLE_LEGACY_INPUT_MANAGER.
- Compiler outputs were redirected into the backup/Validation folder. No Library, Temp, Logs, obj, PackageCache or generated plugin files were edited by this work.
- Checked namespaces, public declarations, scene module counts and final diffs. The sample scene diff contains only the input module fields.
- These were compiler and serialized-configuration checks, **not a Unity import, Play Mode test, Android build, or device test**. Actual mouse/touch interactions remain to be play-tested. No script was left unconverted due to uncertainty. Sample camera scroll feel still warrants a physical mouse check; its scaling follows the saved axis settings and the installed package's documented normalization.
- Suggested runtime checks: draw/trace press-drag-release with mouse and touch, including a computer with an idle touchscreen; matching correct/wrong connections and Undo; drag/drop and Undo; buttons/navigation; TMP text fields, dropdowns, sliders and scroll views; sample camera orbit/pinch/scroll.

## G. Final legacy search

No `UnityEngine.Input` calls or legacy input APIs remain in Assets C# sources, including TMP examples and commented examples. No legacy keyboard, mouse, touch, axis or button calls remain.

Broad substring searches still match legitimate `UnityEngine.InputSystem` namespaces, the new `UnityEngine.InputSystem.TouchPhase`, TMP field variables such as `passwordInput`, and the custom `LevelUnlockStorage.GetKey` method, which constructs a PlayerPrefs key. Those are unrelated to legacy input and remain unchanged. ProjectSettings/InputManager.asset remains as unused configuration; it does not enable the old backend.

## Backup and rollback

`InputMigrationBackup-20260906/Assets/` contains exact pre-edit copies of all six existing files changed by this task, including the user's already-modified DrawingManager. `baseline-status.txt` records pre-existing Git changes. Do not use a broad Git reset: this project had many unrelated changes before the migration.

To revert only this migration, copy those six backed-up files to their matching project paths and remove the newly added TMP_ExamplePointer.cs and TMP_ExamplePointer.cs.meta. Review subsequent edits before restoring. No existing `.meta` files were changed.

Compiler logs, response files and the inventories are in `InputMigrationBackup-20260906/Validation/`.

Scroll normalization reference: [Unity InputSettings documentation](https://docs.unity.cn/Packages/com.unity.inputsystem@1.11/api/UnityEngine.InputSystem.InputSettings.html); verified against installed 1.19.0 InputSettings.cs.

## Full script inventory

All files listed were audited for legacy input. The five scripts in the table were modified; TMP_ExamplePointer.cs was added; all other scripts were left unchanged.
- Assets\Editor\AutoFocusGameViewOnPlay.cs
- Assets\Firebase\FirebaseApp\Internal\AssemblyInfo.cs
- Assets\Firebase\FirebaseApp\Internal\FirebaseInterops.cs
- Assets\Firebase\FirebaseApp\Internal\HttpHelpers.cs
- Assets\Scripts\AcademicGradeReport.cs
- Assets\Scripts\AdminDashboardManager.cs
- Assets\Scripts\AudioManager.cs
- Assets\Scripts\BackToMenu.cs
- Assets\Scripts\BrushCollision.cs
- Assets\Scripts\ButtonClickSound.cs
- Assets\Scripts\CharacterSelectionManager.cs
- Assets\Scripts\DraggableWord.cs
- Assets\Scripts\DrawingManager.cs
- Assets\Scripts\DropZone.cs
- Assets\Scripts\EnglishGameManager.cs
- Assets\Scripts\EnglishLevel1Manager.cs
- Assets\Scripts\EnglishLevel2Manager.cs
- Assets\Scripts\FirebaseManager.cs
- Assets\Scripts\FirestoreProfileLogin.cs
- Assets\Scripts\GameTimer.cs
- Assets\Scripts\GlowOnClick.cs
- Assets\Scripts\GradesSceneManager.cs
- Assets\Scripts\GradeStudentRowUI.cs
- Assets\Scripts\InputGradeManager.cs
- Assets\Scripts\LanguageManager.cs
- Assets\Scripts\LanguageToggle.cs
- Assets\Scripts\LearningDataStore.cs
- Assets\Scripts\LeftDot.cs
- Assets\Scripts\LocalizedText.cs
- Assets\Scripts\LoginSession.cs
- Assets\Scripts\MatchManager.cs
- Assets\Scripts\MatchUndo.cs
- Assets\Scripts\MathGameManager.cs
- Assets\Scripts\MicToggle.cs
- Assets\Scripts\ModuleManager.cs
- Assets\Scripts\ModulePageManager.cs
- Assets\Scripts\MusicManager.cs
- Assets\Scripts\MusicToggle.cs
- Assets\Scripts\MusicVolume.cs
- Assets\Scripts\NewEmptyCSharpScript.cs
- Assets\Scripts\PlaySceneManager.cs
- Assets\Scripts\RightDot.cs
- Assets\Scripts\SceneLoader.cs
- Assets\Scripts\ScienceGameManager.cs
- Assets\Scripts\ScienceLevelManager.cs
- Assets\Scripts\ScienceMatchingManager.cs
- Assets\Scripts\ScienceSelectionManager.cs
- Assets\Scripts\ScienceSelectionUndo.cs
- Assets\Scripts\ScienceUndoDraggableManager.cs
- Assets\Scripts\ScoreCelebration.cs
- Assets\Scripts\ScoreDetailRowUI.cs
- Assets\Scripts\ScoreManager.cs
- Assets\Scripts\ScoreSceneManager.cs
- Assets\Scripts\SFXToggle.cs
- Assets\Scripts\SFXVolume.cs
- Assets\Scripts\SpeechGameManager.cs
- Assets\Scripts\StudentGradesView.cs
- Assets\Scripts\StudentProfileTabManager.cs
- Assets\Scripts\StudentScoreRowUI.cs
- Assets\Scripts\TeacherScoreManager.cs
- Assets\Scripts\TracingLevelManager.cs
- Assets\Scripts\TracingUndoManager.cs
- Assets\Scripts\TryAgain.cs
- Assets\Scripts\UIButton.cs
- Assets\Scripts\UIDrawing.cs
- Assets\Scripts\VideoPopupManager.cs
- Assets\Scripts\LockUnlock\LevelLockController.cs
- Assets\Scripts\LockUnlock\LevelUnlockStorage.cs
- Assets\Scripts\LockUnlock\UnlockLevelManager.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\Benchmark01_UGUI.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\Benchmark01.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\Benchmark02.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\Benchmark03.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\Benchmark04.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\CameraController.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\ChatController.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\DropdownSample.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\EnvMapAnimator.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\ObjectSpin.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\ShaderPropAnimator.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\SimpleScript.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\SkewTextExample.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TeleType.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TextConsoleSimulator.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TextMeshProFloatingText.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TextMeshSpawner.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_DigitValidator.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_ExamplePointer.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_ExampleScript_01.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_FrameRateCounter.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_PhoneNumberValidator.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_TextEventCheck.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_TextEventHandler.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_TextInfoDebugTool.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_TextSelector_A.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_TextSelector_B.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMP_UiFrameRateCounter.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\TMPro_InstructionOverlay.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\VertexColorCycler.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\VertexJitter.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\VertexShakeA.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\VertexShakeB.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\VertexZoom.cs
- Assets\TextMesh Pro\Examples & Extras\Scripts\WarpTextExample.cs

