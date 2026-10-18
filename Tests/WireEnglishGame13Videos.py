"""Wire and verify Game13 using its own popup layout and the shared video setup."""
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

if __name__ == '__main__':
    setup.configure('EnglishGame13')
    register_scenes('EnglishGame13')
    setup.wire()
