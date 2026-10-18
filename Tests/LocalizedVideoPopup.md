EnglishGame1 video localization
==============================

Keep the existing UI and VideoPopupManager playback behavior. Each level button
calls LocalizedVideoPopup.OpenVideo, which reads LanguageManager at click time
and delegates to that level's English or Tagalog VideoPopupManager. The Settings
toggle already saves LearnAbleLanguage across scene changes and app restarts.

For future games: add one LocalizedVideoPopup to an always-active manager object
per level, assign its english and tagalog managers, and connect that level's
buttons to the router's OpenVideo method. Each language manager must reference
its own popup panel, child player, progress slider, and destination scene. Match
the player's render texture to the panel's RawImage. Keep play-on-awake disabled.
Do not change visual layout when applying this pattern to other games.

The original VideoPopupManager intentionally retains its existing looping video
and 18-second instruction timer. This change does not alter that behavior.
The selected video's audio is decoded but muted throughout the first loop.
At the first loopPointReached callback it becomes audible for the second loop
and stays audible on subsequent loops. There is no startup fade or fixed delay.
This applies to all five levels in both languages without changing the UI.

Verification: run Tests/WireLocalizedEnglishVideos.py to check the saved wiring.
In Unity, test all five levels with Settings OFF (English), then ON (Tagalog),
including returning from Settings and restarting with the saved language.
Confirm one popup opens, video/audio plays smoothly, progress completes, and
the correct gameplay scene opens. Rapid repeated clicks should start one video.
Actual decoding smoothness must be checked in Play mode and on the target device.

EnglishGame2 uses the same LocalizedVideoPopup script and second-loop audio.
Its ten popup panels duplicate EnglishGame2's original popup geometry and style.
Each has its own player, slider and render texture. Render textures live beside
the clips in Assets/EnglishInstructionVideos/EnglishGame2 and use Game1's
1280 x 720 render texture settings. Existing level button artwork is unchanged.
Intro duration is at least 18 seconds, extended to twice the clip duration plus
0.15 seconds when needed, so the silent first pass and audible second pass fit.
Run Tests/WireEnglishGame2Videos.py to validate its connections and unchanged UI.
In Play mode, check all five levels in both languages, including Level5, where
audio begins after its approximately 18.25-second first loop. Confirm each level
opens its own gameplay scene after the intro. Scripts are already attached.

EnglishGame3 follows the same setup, using its original VideoPanel as the layout
template. Its ten render textures are in Assets/EnglishInstructionVideos/EnglishGame3.
Every level's English and Tagalog buttons use the shared LocalizedVideoPopup,
including its silent first loop and audible second loop. Level3's approximately
16.07-second clips have a 32.288-second intro so the audible pass finishes.
Validate with Tests/WireEnglishGame3Videos.py. The shared scene setup supports
different English game numbers through configure(), retaining each game's own UI.

EnglishGame4 uses the same language selection and second-loop audio behavior.
Its ten render textures are in Assets/EnglishInstructionVideos/EnglishGame4,
with the same settings as Game1. Its original VideoPanel supplies the unchanged
popup appearance. All clips fit both passes within the 18-second intro duration.
Run Tests/WireEnglishGame4Videos.py to validate all five levels in both languages,
the nine existing video button calls, destinations, and preservation of the UI.

EnglishGame5 uses the same shared scripts, saved language selection, and audio
starting at the second loop. Its original Video Panel supplies the unchanged
popup appearance. All ten render textures are beside the clips in
Assets/EnglishInstructionVideos/EnglishGame5 with Game1's render texture settings.
Both passes fit within the 18-second intro for all clips, including Level3 Tagalog.
Run Tests/WireEnglishGame5Videos.py to check all five levels in both languages,
the 73 existing video button calls, scene references and unchanged UI properties.

EnglishGame6 uses its own Video Panel layout in 2ndQuarterEnglishGame and the
same shared language router and second-loop audio. Ten render textures live in
Assets/EnglishInstructionVideos/EnglishGame6 with Game1's settings. All clips
fit both passes in the 18-second intro. Its copied EnglishGame5 destinations,
instruction link and progress key now point to EnglishGame6. The six game scenes
and instruction scene are registered in EditorBuildSettings so loading works.
Run Tests/WireEnglishGame6Videos.py to validate its ten videos and 15 video buttons.

EnglishGame7 has the same saved-language routing and silent-first-loop audio
using its own unchanged Video Panel layout. Ten textures live in
Assets/EnglishInstructionVideos/EnglishGame7 and match Game1's settings.
All clips fit both passes in the 18-second intro. Copied Game5 destinations,
instruction links and progress identifiers now target Game7. Its six game scenes
and instruction scene are registered for loading. Validate its ten videos and
15 existing video buttons with Tests/WireEnglishGame7Videos.py.

EnglishGame8 uses the same shared language routing and second-loop audio with
its unchanged Video Panel layout. Ten textures in
Assets/EnglishInstructionVideos/EnglishGame8 match Game1's settings. All clips
fit two passes within the 18-second intro. Copied Game5 destinations, progress
identifiers and instruction links now target Game8. Its six game scenes and
instruction scene are registered. Run Tests/WireEnglishGame8Videos.py to verify
all ten videos, 19 existing video button calls and preservation of the UI.

EnglishGame9 follows the same saved-language selection and second-loop audio
using its own unchanged Video Panel layout. Ten render textures live beside its
clips in Assets/EnglishInstructionVideos/EnglishGame9 with Game1's settings.
Every clip fits two passes within the 18-second intro. Copied Game5 destinations,
progress identifiers and instruction links now target Game9. Its six game scenes
and instruction scene are registered. Tests/WireEnglishGame9Videos.py validates
all ten videos, the 12 existing video button calls, destinations and unchanged UI.

EnglishGame10 uses the same saved-language selection and second-loop audio
with its original Video Panel appearance preserved. Its ten render textures in
Assets/EnglishInstructionVideos/EnglishGame10 match Game1's settings. All clips
fit both passes within the 18-second intro. Copied Game5 destinations, progress
identifiers and instruction links now target Game10. Its six game scenes and
instruction scene are registered. Tests/WireEnglishGame10Videos.py validates
the ten videos, ten existing video button calls, destinations and unchanged UI.

EnglishGame11 uses its unchanged Video Panel layout in 3rdQuarterEnglishGame,
the saved-language router and second-loop audio. Its ten existing render textures
are reused from Assets/EnglishInstructionVideos/EnglishGame11. All clips fit two
passes in the 18-second intro. Copied Game5 destinations, progress identifiers
and instruction links now target Game11. Its six game scenes and instruction
scene are registered. Tests/WireEnglishGame11Videos.py checks all ten videos,
five existing video buttons, destinations and unchanged UI properties.

EnglishGame12 uses the same saved-language router and second-loop audio with
its original Video Panel appearance preserved. Ten textures in
Assets/EnglishInstructionVideos/EnglishGame12 match Game1's settings. All clips
fit two passes within the 18-second intro. Copied Game5 destinations, progress
identifiers and instruction links now target Game12. Its six game scenes and
instruction scene are registered. Tests/WireEnglishGame12Videos.py validates
all ten videos, 165 existing video button calls, destinations and unchanged UI.

EnglishGame13 uses the same saved-language selection and second-loop audio
with its original Video Panel appearance preserved. Ten render textures in
Assets/EnglishInstructionVideos/EnglishGame13 match Game1's settings. All clips
fit two passes within the 18-second intro. Copied Game5 destinations, progress
identifiers and instruction links now target Game13. Its six game scenes and
instruction scene are registered. Tests/WireEnglishGame13Videos.py validates
all ten videos, five existing video button calls, destinations and unchanged UI.

EnglishGame13MenuInput initializes UI input on the menu Canvas, keeps decorative
graphics from intercepting clicks, assigns actual buttons to existing level-lock
controllers, and binds the five level buttons directly to their video routers.
It preserves the existing inspector callbacks, appearance, progression locks
and second-loop audio. Check the first level's play button in both languages,
then Next/Back and unlocked levels. The scene's original visual components have
not been edited; the fix is a single added behavior on its existing Canvas.
The menu Canvas raycaster also accepts reversed graphics (ignoreReversedGraphics
is false), because the button containers intentionally use a 180-degree Y
rotation. Keep that rotation: changing pointer filtering fixes the click without
changing the artwork, transforms, video behavior or level-lock requirements.

ScienceGame1 uses the same LocalizedVideoPopup and second-loop audio behavior,
with its own original Video Panel appearance preserved. Ten render textures in
Assets/ScienceInsctructionvideos/ScienceGame1 match EnglishGame1's settings.
The saved language selects the matching English or Tagalog panel for every level.
All clips fit two passes in the 18-second intro. Its game and instruction scenes
are registered for loading. Tests/WireScienceGame1Videos.py validates all ten
videos, 15 existing video button calls, destinations and unchanged UI properties.
