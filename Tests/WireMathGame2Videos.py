"""Wire MathGame2 videos using each level's original popup appearance."""
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

if __name__ == '__main__':
    setup.configure('MathGame2')
    register_scenes('MathGame2')
    setup.wire()
