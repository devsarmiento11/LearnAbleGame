"""Wire and verify EnglishGame1 without changing any visual component."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SCENE = ROOT / 'Assets/Settings/Scenes/EnglishGame/EnglishGame1.unity'
GUID = '4fa6812ebcb741c5bba7e85ad5beeb86'

def blocks(text):
    return {int(m[2]): (int(m[1]), m[0]) for m in re.finditer(
        r'^--- !u!(\d+) &(\d+)\n.*?(?=^--- !u!|\Z)', text, re.M | re.S)}

def ref(text, key):
    return int(re.search(r'^  '+key+r': \{fileID: (\d+)\}', text, re.M)[1])

def inspect(text):
    data = blocks(text)
    names = {i: re.search(r'^  m_Name: (.*)', b, re.M)[1].strip(" '\r")
             for i, (t, b) in data.items() if t == 1}
    transforms = {i: ref(b, 'm_GameObject') for i, (t,b) in data.items() if t in (4,224)}
    parents = {ref(b,'m_GameObject'): transforms[ref(b,'m_Father')]
               for t,b in data.values() if t in (4,224) and ref(b,'m_Father') in transforms}
    def below(go, ancestor):
        while go:
            if go == ancestor: return True
            go = parents.get(go, 0)
        return False
    managers = {i: b for i,(t,b) in data.items() if t == 114 and
                'guid: 8edeba1ec197c944985bfbc9460d17b1' in b}
    return data, names, below, managers

def wire():
    text = SCENE.read_text(encoding='utf-8-sig')
    data, names, below, managers = inspect(text)
    if GUID in text:
        verify(text)
        return
    replacements = {}
    pairs = {}
    for i,b in managers.items():
        level = int(re.search(r'nextSceneName: EnglishGame1Level(\d)',b)[1])
        language = 'Tagalog' if 'Tagalog' in names[ref(b,'m_GameObject')] else 'English'
        panel = next(go for go,n in names.items() if n == f'VideoPopupPanelLevel{level}{language}')
        player = next(j for j,(t,v) in data.items() if t == 328 and below(ref(v,'m_GameObject'),panel))
        slider = next(j for j,(t,v) in data.items() if t == 114 and
                      'm_WholeNumbers:' in v and below(ref(v,'m_GameObject'),panel))
        fixed = b
        for key,value in [('videoPopupPanel',panel),('videoPlayer',player),('progressBar',slider)]:
            fixed = re.sub(r'('+key+r': \{fileID: )\d+',lambda m:m[1]+str(value),fixed)
        replacements[i] = fixed
        pairs[level,language] = i
    assert len(pairs) == 10, pairs
    # All existing button calls retain their intended destination level.
    levels = {i:int(re.search(r'nextSceneName: EnglishGame1Level(\d)',b)[1]) for i,b in managers.items()}
    for i,(t,b) in data.items():
        if 'm_MethodName: OpenVideo' not in b: continue
        def route(m):
            target = int(m[1])
            candidates = [int(re.search(r'Level(\d)',n)[1]) for go,n in names.items()
                          if re.fullmatch(r'Level\d(?:Panel|Btn)',n) and below(ref(b,'m_GameObject'),go)]
            level = candidates[0] if candidates else levels.get(target)
            if level is None:
                level = int(re.search(r'm_StringArgument: EnglishGame1Level(\d)',b)[1])
            return (f'm_Target: {{fileID: {2000000000+level}}}\n'
                    '        m_TargetAssemblyTypeName: LocalizedVideoPopup, Assembly-CSharp\n'
                    '        m_MethodName: OpenVideo')
        replacements[i] = re.sub(r'm_Target: \{fileID: (\d+)\}\n'
            r'        m_TargetAssemblyTypeName: VideoPopupManager, Assembly-CSharp\n'
            r'        m_MethodName: OpenVideo',route,b)
    for level in range(1,6):
        router = 2000000000+level
        assert router not in data
        english,tagalog = pairs[level,'English'],pairs[level,'Tagalog']
        go = ref(managers[english],'m_GameObject')
        replacements[go] = data[go][1].replace('  m_Layer:',f'  - component: {{fileID: {router}}}\n  m_Layer:',1)
        text += f'''--- !u!114 &{router}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::LocalizedVideoPopup
  english: {{fileID: {english}}}
  tagalog: {{fileID: {tagalog}}}
'''
    for i,b in replacements.items(): text = text.replace(data[i][1],b,1)
    verify(text)
    # Save the user's starting scene, including their uncommitted work.
    backup = ROOT / 'Tests/EnglishGame1.before-localized-videos.unity.txt'
    if not backup.exists(): backup.write_bytes(SCENE.read_bytes())
    SCENE.write_text(text,encoding='utf-8',newline='\n')

def verify(text):
    data,names,below,managers = inspect(text)
    assets = {}
    for p in (ROOT/'Assets/EnglishInstructionVideos').rglob('*.meta'):
        m = re.search(r'^guid: (\w+)',p.read_text(),re.M)
        if m: assets[m[1]] = p.name[:-5]
    for i,b in managers.items():
        level = int(re.search(r'nextSceneName: EnglishGame1Level(\d)',b)[1])
        language = 'Tagalog' if 'Tagalog' in names[ref(b,'m_GameObject')] else 'English'
        panel = ref(b,'videoPopupPanel')
        assert names[panel] == f'VideoPopupPanelLevel{level}{language}'
        player = data[ref(b,'videoPlayer')][1]
        assert below(ref(player,'m_GameObject'),panel)
        assert below(ref(data[ref(b,'progressBar')][1],'m_GameObject'),panel)
        clip = re.search(r'm_VideoClip: .*guid: (\w+)',player)[1]
        assert assets[clip] == f'EnglishLevel{level}Instruction{language}.mp4', (level,language,assets[clip])
        texture = re.search(r'm_TargetTexture: .*guid: (\w+)',player)[1]
        displays = [v for t,v in data.values() if t == 114 and 'm_Texture:' in v
                    and below(ref(v,'m_GameObject'),panel)]
        assert any(texture in v for v in displays), (level,language,'display texture')
        assert f'EnglishGame1Level{level}.unity' in (ROOT/'ProjectSettings/EditorBuildSettings.asset').read_text()
        print(f'PASS level {level} {language}: panel, player, clip, texture, progress and destination')
    routers = {i:b for i,(t,b) in data.items() if GUID in b}
    assert len(routers) == 5
    for b in routers.values():
        en,tg = ref(b,'english'),ref(b,'tagalog')
        assert en in managers and tg in managers
        assert re.search(r'nextSceneName: (.*)',managers[en])[1] == re.search(r'nextSceneName: (.*)',managers[tg])[1]
    calls = re.findall(r'm_Target: \{fileID: (\d+)\}\n        m_TargetAssemblyTypeName: LocalizedVideoPopup, Assembly-CSharp\n        m_MethodName: OpenVideo',text)
    assert calls and all(int(i) in routers for i in calls)
    assert 'm_TargetAssemblyTypeName: VideoPopupManager, Assembly-CSharp' not in text
    print(f'PASS {len(calls)} button calls route through five language selectors')
    backup = ROOT / 'Tests/EnglishGame1.before-localized-videos.unity.txt'
    if backup.exists():
        original = blocks(backup.read_text(encoding='utf-8-sig'))
        for i,(t,b) in original.items():
            after = data[i][1]
            if i in managers or 'm_MethodName: OpenVideo' in b: continue
            if t == 1:
                after = re.sub(r'^  - component: \{fileID: 200000000[1-5]\}\n','',after,flags=re.M)
            assert b == after, ('Unexpected visual/component change',i)
        print('PASS all original visual components and layout are unchanged')

if __name__ == '__main__': wire()
