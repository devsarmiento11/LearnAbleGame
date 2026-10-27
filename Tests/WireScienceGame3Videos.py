"""Wire and verify ScienceGame3 with its own UI and shared localized video mechanics."""
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

if __name__ == '__main__':
    setup.configure('ScienceGame3')
    register_scenes('ScienceGame3')
    setup.wire()
