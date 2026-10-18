"""Wire and verify ScienceGame1 with its own UI and shared localized video mechanics."""
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

if __name__ == '__main__':
    setup.configure('ScienceGame1')
    register_scenes('ScienceGame1')
    setup.wire()
