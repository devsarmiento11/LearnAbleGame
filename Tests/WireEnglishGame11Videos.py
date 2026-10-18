"""Wire and verify Game11 using its own popup layout and the shared video setup."""
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

if __name__ == '__main__':
    setup.configure('EnglishGame11')
    register_scenes('EnglishGame11')
    setup.wire()
