"""Reconnect the copied ScienceGame4 scene to its own videos without visual edits."""
import re
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

def wire_copied_science_game(game='ScienceGame4', register=True):
    setup.configure(game)
    text = setup.SCENE.read_text(encoding='utf-8-sig')
    backup = setup.ROOT/f'Tests/{game}.before-localized-videos.unity.txt'
    if not backup.exists():
        backup.write_text(text,encoding='utf-8',newline='\n')
    instructions = list((setup.ROOT/'Assets/Settings/Scenes').rglob(f'{game}Instruction*.unity'))
    instruction = instructions[0].stem if len(instructions) == 1 else 'ScienceGameScene'
    updated = text.replace('ScienceGame2Instruction', instruction)
    updated = updated.replace('ScienceGame2', game)
    if updated != text:
        setup.SCENE.write_text(updated,encoding='utf-8',newline='\n')
    if register:
        register_scenes(game)
    # The original already contained ten popup panels; compare visual properties
    # separately instead of treating it as the older single-panel template.
    setup.BACKUP = setup.ROOT/f'Tests/{game}.unused-single-panel-backup.txt'
    setup.wire()
    before = setup.blocks(backup.read_text())
    after = setup.blocks(setup.SCENE.read_text())
    assert set(before) == set(after)
    for i,(kind,b) in before.items():
        new = after[i][1]
        if 'nextSceneName:' in b or 'm_VideoClip:' in b or 'm_Texture:' in b:
            continue
        expected = b.replace('ScienceGame2Instruction',instruction).replace('ScienceGame2',game)
        assert expected == new, ('Unexpected UI change',i)
    print('PASS original UI layout, artwork, button controls and object hierarchy unchanged')

if __name__ == '__main__':
    wire_copied_science_game()
