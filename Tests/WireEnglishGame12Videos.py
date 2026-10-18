"""Wire and verify Game12 using its own popup layout and the shared video setup."""
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

if __name__ == '__main__':
    setup.configure('EnglishGame12')
    register_scenes('EnglishGame12')
    setup.wire()
