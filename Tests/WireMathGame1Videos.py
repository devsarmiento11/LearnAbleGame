"""Wire MathGame1 videos using each level's original popup appearance."""
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

if __name__ == '__main__':
    setup.configure('MathGame1')
    register_scenes('MathGame1')
    setup.wire()
