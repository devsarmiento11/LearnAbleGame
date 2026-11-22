"""Repair the current MathGame5 menu without reverting user UI edits."""
import re
import WireEnglishGame2Videos as setup
from WireEnglishGame6Videos import register_scenes

def repair():
    setup.configure('MathGame5')
    original = setup.SCENE.read_text(encoding='utf-8-sig')
    backup = setup.ROOT/'Tests/MathGame5.before-accuracy-repair.unity.txt'
    if not backup.exists():
        backup.write_text(original,encoding='utf-8',newline='\n')
    text = setup.refresh_existing_assets(original)
    data,names,below,managers = setup.inspect(text)
    routers = {int(re.search(r'nextSceneName: MathGame5Level(\d)',
                    managers[setup.ref(b,'english')])[1]): i
               for i,(kind,b) in data.items() if setup.GUID in b}
    assert set(routers) == set(range(1,6))
    counts = {i:0 for i in routers}
    for i,(kind,b) in data.items():
        if 'm_MethodName: OpenVideo' not in b: continue
        go = setup.ref(b,'m_GameObject')
        levels = [int(re.search(r'Level(\d)',name)[1]) for p,name in names.items()
                  if re.fullmatch(r'Level\dPanel',name) and below(go,p)]
        assert len(levels)==1, (i,levels)
        level = levels[0]
        fixed = re.sub(r'm_Target: \{fileID: \d+\}(\n        m_TargetAssemblyTypeName: LocalizedVideoPopup)',
                       lambda m:f'm_Target: {{fileID: {routers[level]}}}'+m[1],b)
        fixed = re.sub(r'm_StringArgument: MathGame5Level\d',f'm_StringArgument: MathGame5Level{level}',fixed)
        text = text.replace(b,fixed,1)
        counts[level] += 1
    assert all(counts.values())
    text = text.replace('m_StringArgument: 2ndQuarterScienceGameScene',
                        'm_StringArgument: 2ndQuarterMathGameScene')
    register_scenes('MathGame5')
    setup.BACKUP = setup.ROOT/'Tests/MathGame5.unused-accuracy-backup.txt'
    setup.verify(text)
    old,new = setup.blocks(original),setup.blocks(text)
    assert set(old)==set(new)
    for i,(kind,b) in old.items():
        after = new[i][1]
        for pattern in (r'^  totalTime: .*',r'^  m_VideoClip: .*',
                        r'^  m_TargetTexture: .*',r'^  m_Texture: .*',
                        r'^      - m_Target: .*',r'^          m_StringArgument: .*'):
            b = re.sub(pattern,'',b,flags=re.M)
            after = re.sub(pattern,'',after,flags=re.M)
        assert b==after,('Unexpected visual/component change',i)
    if text!=original:
        setup.SCENE.write_text(text,encoding='utf-8',newline='\n')
    print('PASS each level button opens its own language router; Back uses the Math menu')
    print('PASS current user UI, transforms, artwork, locks and object hierarchy unchanged')

if __name__=='__main__':
    repair()
