"""Wire and verify Game6 using its own popup layout and the shared video setup."""
import WireEnglishGame2Videos as setup

def register_scenes(game='EnglishGame6', require_levels=True):
    build = setup.ROOT/'ProjectSettings/EditorBuildSettings.asset'
    original = build.read_text(encoding='utf-8-sig')
    text = original
    scenes = [p for p in (setup.ROOT/'Assets/Settings/Scenes').rglob(f'{game}*.unity')
              if setup.re.fullmatch(game+r'(?:Level[1-5]|Instruction(?:Scene)?)?',p.stem)]
    assert len(scenes) in ((6, 7) if require_levels else (1, 2))
    for scene in sorted(scenes):
        path = scene.relative_to(setup.ROOT).as_posix()
        if f'    path: {path}\n' not in text:
            entry = f'  - enabled: 1\n    path: {path}\n    guid: {setup.guid(scene)}\n'
            text = text.replace('  m_configObjects:',entry+'  m_configObjects:',1)
    if text != original:
        backup = setup.ROOT/f'Tests/EditorBuildSettings.before-{game}-videos.asset.txt'
        if not backup.exists(): backup.write_text(original,encoding='utf-8')
        build.write_text(text,encoding='utf-8',newline='\n')

if __name__ == '__main__':
    setup.configure('EnglishGame6')
    register_scenes()
    setup.wire()
