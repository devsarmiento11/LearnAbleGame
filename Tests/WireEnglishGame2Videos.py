"""Build localized EnglishGame2 popups from its unchanged existing popup layout."""
from pathlib import Path
import re
import struct
import uuid
from WireLocalizedEnglishVideos import blocks, ref, inspect, GUID

ROOT = Path(__file__).resolve().parents[1]
GAME = 'EnglishGame2'
REQUIRE_DESTINATIONS = True
SCENE = ROOT/f'Assets/Settings/Scenes/EnglishGame/{GAME}.unity'
VIDEOS = ROOT/f'Assets/EnglishInstructionVideos/{GAME}'
BACKUP = ROOT/f'Tests/{GAME}.before-localized-videos.unity.txt'

def configure(game):
    global GAME, SCENE, VIDEOS, BACKUP
    assert re.fullmatch(r'(?:English|Science)Game\d+', game)
    GAME = game
    matches = list((ROOT/'Assets/Settings/Scenes').rglob(f'{GAME}.unity'))
    assert len(matches) == 1, matches
    SCENE = matches[0]
    folder = 'ScienceInsctructionvideos' if GAME.startswith('Science') else 'EnglishInstructionVideos'
    VIDEOS = ROOT/f'Assets/{folder}/{GAME}'
    BACKUP = ROOT/f'Tests/{GAME}.before-localized-videos.unity.txt'

def guid(path):
    return re.search(r'^guid: (\w+)',Path(str(path)+'.meta').read_text(),re.M)[1]

def duration(path):
    raw = path.read_bytes()
    i = raw.index(b'mvhd')
    if raw[i+4] == 0:
        scale, ticks = struct.unpack('>II',raw[i+16:i+24])
    else:
        scale, ticks = struct.unpack('>IQ',raw[i+24:i+36])
    return ticks/scale

def replace_ref(b, key, value):
    return re.sub(r'('+key+r': \{fileID: )\d+',lambda m:m[1]+str(value),b)

def remap(b, ids):
    b = re.sub(r'(^--- !u!\d+ &)(\d+)',lambda m:m[1]+str(ids.get(int(m[2]),int(m[2]))),b,flags=re.M)
    return re.sub(r'(fileID: )(\d+)',lambda m:m[1]+str(ids.get(int(m[2]),int(m[2]))),b)

def refresh_existing_assets(text):
    data,names,below,managers = inspect(text)
    template = (ROOT/'Assets/EnglishInstructionVideos/EnglishGame1/EnglishLevel1InstructionEnglish.renderTexture').read_text()
    original = text
    for manager,b in managers.items():
        match = re.fullmatch(r'VideoPopupPanelLevel(\d+)(English|Tagalog)',names[ref(b,'videoPopupPanel')])
        assert match
        level,language = match.groups()
        clip = VIDEOS/f'Level{level}{language}.mp4'
        texture = clip.with_suffix('.renderTexture')
        player_id = ref(b,'videoPlayer')
        player = data[player_id][1]
        if not texture.exists():
            texture.write_text(re.sub(r'^  m_Name: .*',f'  m_Name: {texture.stem}',template,flags=re.M),encoding='utf-8',newline='\n')
        if not Path(str(texture)+'.meta').exists():
            old_guid = uuid.uuid4().hex
            Path(str(texture)+'.meta').write_text(f'fileFormatVersion: 2\nguid: {old_guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 8400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',encoding='utf-8',newline='\n')
        updated = re.sub(r'm_VideoClip: .*',f'm_VideoClip: {{fileID: 32900000, guid: {guid(clip)}, type: 3}}',player)
        updated = re.sub(r'm_TargetTexture: .*',f'm_TargetTexture: {{fileID: 8400000, guid: {guid(texture)}, type: 2}}',updated)
        text = text.replace(player,updated,1)
        seconds = max(18,duration(clip)*2+0.15)
        text = text.replace(b,re.sub(r'totalTime: .*',f'totalTime: {seconds:.3f}',b),1)
        panel = ref(b,'videoPopupPanel')
        for t,image in data.values():
            if 'm_Texture:' in image and below(ref(image,'m_GameObject'),panel):
                updated = re.sub(r'm_Texture: .*',f'm_Texture: {{fileID: 8400000, guid: {guid(texture)}, type: 2}}',image)
                text = text.replace(image,updated,1)
    if text != original:
        backup = ROOT/f'Tests/{GAME}.before-video-asset-refresh.unity.txt'
        if not backup.exists(): backup.write_text(original,encoding='utf-8',newline='\n')
    return text

def wire():
    text = SCENE.read_text(encoding='utf-8-sig')
    if GUID in text:
        refreshed = refresh_existing_assets(text)
        verify(refreshed)
        if refreshed != text: SCENE.write_text(refreshed,encoding='utf-8',newline='\n')
        return
    original = text
    # Some later games were copied from earlier scenes. Keep progress and the
    # instruction/back destination associated with this game as well.
    text = re.sub(r'(^  gameScene: )EnglishGame\d+',lambda m:m[1]+GAME,text,flags=re.M)
    instructions = list((ROOT/'Assets/Settings/Scenes').rglob(f'{GAME}Instruction*.unity'))
    if len(instructions) == 1:
        text = re.sub(r'(m_StringArgument: )EnglishGame\d+Instruction(?:Scene)?\b',
                      lambda m:m[1]+instructions[0].stem,text)
    data,names,below,managers = inspect(text)
    panels = {ref(b,'videoPopupPanel') for b in managers.values()}
    assert len(panels) == 1, 'Expected the existing shared video popup'
    panel = next(iter(panels))
    subtree = {i:b for i,(t,b) in data.items() if
               (t == 1 and below(i,panel)) or ('m_GameObject:' in b and below(ref(b,'m_GameObject'),panel))}
    root_transform = next(i for i,b in subtree.items() if data[i][0] == 224 and ref(b,'m_GameObject') == panel)
    parent = ref(subtree[root_transform],'m_Father')
    player = next(i for i in subtree if data[i][0] == 328)
    slider = next(i for i,b in subtree.items() if 'm_WholeNumbers:' in b)
    image = next(i for i,b in subtree.items() if 'm_Texture:' in b)
    en_managers = {int(re.search(r'nextSceneName: (?:English|Science)Game\d+Level(\d)',b)[1]):i for i,b in managers.items()}
    assert len(en_managers) == 5
    replacements, additions, new_roots = {}, [], []
    ids_used = set(data)
    next_id = 3000000000
    def allocate():
        nonlocal next_id
        next_id += 1
        assert next_id not in ids_used
        ids_used.add(next_id)
        return next_id
    routers = {level:allocate() for level in range(1,6)}
    template = (ROOT/'Assets/EnglishInstructionVideos/EnglishGame1/EnglishLevel1InstructionEnglish.renderTexture').read_text()
    for level in range(1,6):
        english = en_managers[level]
        tagalog = allocate()
        go = ref(managers[english],'m_GameObject')
        replacements[go] = data[go][1].replace('  m_Layer:',
            f'  - component: {{fileID: {tagalog}}}\n  - component: {{fileID: {routers[level]}}}\n  m_Layer:',1)
        for language,manager in [('English',english),('Tagalog',tagalog)]:
            clip = VIDEOS/f'Level{level}{language}.mp4'
            assert clip.exists() and Path(str(clip)+'.meta').exists()
            texture = clip.with_suffix('.renderTexture')
            if not texture.exists():
                texture.write_text(re.sub(r'^  m_Name: .*',f'  m_Name: {texture.stem}',template,flags=re.M),encoding='utf-8',newline='\n')
                Path(str(texture)+'.meta').write_text(f'''fileFormatVersion: 2
guid: {uuid.uuid4().hex}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 8400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
''',encoding='utf-8',newline='\n')
            mapping = {i:i for i in subtree} if (level,language)==(1,'English') else {i:allocate() for i in subtree}
            if (level,language)!=(1,'English'): new_roots.append(mapping[root_transform])
            for i,b in subtree.items():
                b = remap(b,mapping)
                if i == panel:
                    b = re.sub(r'^  m_Name: .*',f'  m_Name: VideoPopupPanelLevel{level}{language}',b,flags=re.M)
                    b = re.sub(r'^  m_IsActive: \d', '  m_IsActive: 0',b,flags=re.M)
                if i == player:
                    b = re.sub(r'm_VideoClip: .*',f'm_VideoClip: {{fileID: 32900000, guid: {guid(clip)}, type: 3}}',b)
                    b = re.sub(r'm_TargetTexture: .*',f'm_TargetTexture: {{fileID: 8400000, guid: {guid(texture)}, type: 2}}',b)
                    b = b.replace('m_PlayOnAwake: 1','m_PlayOnAwake: 0').replace('m_Looping: 0','m_Looping: 1')
                    b = b.replace('m_ControlledAudioTrackCount: 0','m_ControlledAudioTrackCount: 1')
                    b = re.sub(r'^  m_EnabledAudioTracks:.*','  m_EnabledAudioTracks: 01',b,flags=re.M)
                if i == image:
                    b = re.sub(r'm_Texture: .*',f'm_Texture: {{fileID: 8400000, guid: {guid(texture)}, type: 2}}',b)
                if mapping[i] == i: replacements[i] = b
                else: additions.append(b)
            b = remap(managers[english],{english:manager})
            b = re.sub(r'nextSceneName: (?:English|Science)Game\d+Level\d',f'nextSceneName: {GAME}Level{level}',b)
            for key,value in [('videoPopupPanel',mapping[panel]),('videoPlayer',mapping[player]),('progressBar',mapping[slider])]:
                b = replace_ref(b,key,value)
            # Preserve Game1's 18-second minimum, extending long intros to include
            # one complete audible pass after the silent warmup loop.
            seconds = max(18, duration(clip)*2 + 0.15)
            b = re.sub(r'totalTime: .*',f'totalTime: {seconds:.3f}',b)
            if language == 'English': replacements[english] = b
            else: additions.append(b)
        additions.append(f'''--- !u!114 &{routers[level]}
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
''')
    pb = data[parent][1]
    pb = pb.replace('  m_Father:', ''.join(f'  - {{fileID: {i}}}\n' for i in new_roots)+'  m_Father:',1)
    replacements[parent] = pb
    for i,(t,b) in data.items():
        if 'm_MethodName: OpenVideo' not in b: continue
        candidates = {int(re.search(r'Level(\d)',n)[1]) for go,n in names.items()
                      if re.fullmatch(r'Level\d(?:Panel|Btn)',n) and below(ref(b,'m_GameObject'),go)}
        args = set(map(int,re.findall(r'm_StringArgument: (?:English|Science)Game\d+Level(\d)',b)))
        assert len(candidates or args)==1,(i,candidates,args)
        level = next(iter(candidates or args))
        routed = re.sub(r'm_Target: \{fileID: \d+\}\n'
            r'        m_TargetAssemblyTypeName: VideoPopupManager, Assembly-CSharp\n'
            r'        m_MethodName: OpenVideo',f'm_Target: {{fileID: {routers[level]}}}\n'
            '        m_TargetAssemblyTypeName: LocalizedVideoPopup, Assembly-CSharp\n'
            '        m_MethodName: OpenVideo',b)
        replacements[i] = re.sub(r'm_StringArgument: (?:English|Science)Game\d+Level\d',f'm_StringArgument: {GAME}Level{level}',routed)
    for i,b in replacements.items(): text = text.replace(data[i][1],b,1)
    # Components must precede Unity's final SceneRoots document.
    root_marker = '--- !u!1660057539 '
    position = text.index(root_marker) if root_marker in text else len(text)
    text = text[:position]+''.join(additions)+text[position:]
    if not BACKUP.exists(): BACKUP.write_text(original,encoding='utf-8',newline='\n')
    verify(text)
    SCENE.write_text(text,encoding='utf-8',newline='\n')

def verify(text):
    data,names,below,managers = inspect(text)
    assert len(managers)==10
    build = (ROOT/'ProjectSettings/EditorBuildSettings.asset').read_text()
    for level in range(1,6):
        for language in ('English','Tagalog'):
            panel = next(i for i,n in names.items() if n == f'VideoPopupPanelLevel{level}{language}')
            mb = next(b for b in managers.values() if ref(b,'videoPopupPanel')==panel)
            player = data[ref(mb,'videoPlayer')][1]
            assert below(ref(player,'m_GameObject'),panel)
            assert below(ref(data[ref(mb,'progressBar')][1],'m_GameObject'),panel)
            clip = VIDEOS/f'Level{level}{language}.mp4'
            texture = clip.with_suffix('.renderTexture')
            assert guid(clip) in player and guid(texture) in player
            assert any(guid(texture) in b for t,b in data.values() if 'm_Texture:' in b and below(ref(b,'m_GameObject'),panel))
            assert 'm_PlayOnAwake: 0' in player and 'm_Looping: 1' in player
            assert f'nextSceneName: {GAME}Level{level}' in mb
            if REQUIRE_DESTINATIONS:
                assert f'{GAME}Level{level}.unity' in build
            assert float(re.search(r'totalTime: (.+)',mb)[1]) >= duration(clip)*2
            assert 'm_IsActive: 0' in data[panel][1]
            print(f'PASS level {level} {language}: clip, texture, popup, player, slider, destination, two loops')
    routers = {i:b for i,(t,b) in data.items() if GUID in b}
    assert len(routers)==5
    for b in routers.values():
        en,tg = ref(b,'english'),ref(b,'tagalog')
        assert en in managers and tg in managers
        assert re.search('nextSceneName: (.*)',managers[en])[1]==re.search('nextSceneName: (.*)',managers[tg])[1]
    calls = re.findall(r'm_Target: \{fileID: (\d+)\}\n        m_TargetAssemblyTypeName: LocalizedVideoPopup, Assembly-CSharp\n        m_MethodName: OpenVideo',text)
    assert calls and all(int(i) in routers for i in calls)
    assert 'm_TargetAssemblyTypeName: VideoPopupManager, Assembly-CSharp' not in text
    # Every local scene reference resolves (asset references are external).
    for t,b in data.values():
        for m in re.finditer(r'\{fileID: (\d+)\}',b):
            assert int(m[1])==0 or int(m[1]) in data, ('Missing scene reference',m[1])
    if BACKUP.exists():
        old,old_names,old_below,old_managers = inspect(BACKUP.read_text())
        old_panel = ref(next(iter(old_managers.values())),'videoPopupPanel')
        for i,(t,b) in old.items():
            if i in old_managers or 'm_MethodName: OpenVideo' in b: continue
            after = data[i][1]
            if GAME == 'EnglishGame13' and 'UnityEngine.UI.GraphicRaycaster' in b:
                after = after.replace('m_IgnoreReversedGraphics: 0','m_IgnoreReversedGraphics: 1')
            if t==1:
                after = re.sub(r'^  - component: \{fileID: 3\d{9}\}\n','',after,flags=re.M)
                if GAME == 'EnglishGame13':
                    after = after.replace('  - component: {fileID: 4000000001}\n','')
                if i==old_panel:
                    after = re.sub(r'^  m_Name: .*','  m_Name: '+old_names[old_panel],after,flags=re.M)
                    after = re.sub(r'^  m_IsActive: \d',re.search(r'^  m_IsActive: \d',b,re.M)[0],after,flags=re.M)
            if t in (224,4): after = re.sub(r'^  - \{fileID: 3\d{9}\}\n','',after,flags=re.M)
            if t==328: continue
            if 'm_Texture:' in b:
                after = re.sub(r'^  m_Texture: .*',re.search(r'^  m_Texture: .*',b,re.M)[0],after,flags=re.M)
            if '  gameScene:' in b:
                after = re.sub(r'^  gameScene: .*',re.search(r'^  gameScene: .*',b,re.M)[0],after,flags=re.M)
            old_instruction = re.search(r'm_StringArgument: EnglishGame\d+Instruction(?:Scene)?\b',b)
            if old_instruction:
                after = re.sub(r'm_StringArgument: EnglishGame\d+Instruction(?:Scene)?\b',old_instruction[0],after)
            assert b==after,('Existing UI changed',i)
        print('PASS existing UI layout, graphics, navigation and other scripts unchanged')
    print(f'PASS {len(calls)} button calls and all local scene references')

if __name__=='__main__': wire()
