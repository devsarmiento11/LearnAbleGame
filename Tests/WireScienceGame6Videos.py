"""Connect ScienceGame6 to its own localized videos while preserving its UI."""
from WireScienceGame4Videos import wire_copied_science_game
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

if __name__ == '__main__':
    setup.REQUIRE_DESTINATIONS = False
    wire_copied_science_game('ScienceGame6', register=False)
    missing = [f'ScienceGame6Level{level}' for level in range(1,6)
               if not list((setup.ROOT/'Assets/Settings/Scenes').rglob(f'ScienceGame6Level{level}.unity'))]
    print('MISSING gameplay scenes: '+', '.join(missing))
    register_scenes('ScienceGame6', require_levels=not missing)
