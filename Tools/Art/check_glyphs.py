"""대사·UI 글자 누락 검사(전시 화면에서 글자가 □로 깨지는 사고 방지).

Assets/Scripts 아래 C# 문자열 리터럴에 쓰인 모든 글자를 모아,
1) 한글 폰트 TTF(Jalnan2)와 이모지 폰트의 cmap(글리프 표)에 있는지,
2) TMP 폰트 에셋(Jalnan2_SDF)의 정적 글자표·동적 모드 여부를
확인한다. 외부 라이브러리 없이 TTF cmap(format 4/12)을 직접 읽는다(fontTools가 있으면 그쪽도 교차 확인).

사용: python Tools/Art/check_glyphs.py [--json out.json]
"""
import glob
import json
import os
import re
import struct
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FONTS = {
    'Jalnan2': os.path.join(ROOT, 'Assets', 'Fonts', 'Jalnan2TTF.ttf'),
    'PA_Emoji': os.path.join(ROOT, 'Assets', 'Fonts', 'PA_Emoji_Src.ttf'),
}
TMP_ASSET = os.path.join(ROOT, 'Assets', 'Fonts', 'Jalnan2_SDF.asset')
LITERAL = re.compile(r'(?:\$@|@\$|\$|@)?"((?:[^"\\\n]|\\.)*)"')


def cmap_codepoints(path):
    with open(path, 'rb') as f:
        data = f.read()
    num_tables = struct.unpack('>H', data[4:6])[0]
    cmap_off = None
    for i in range(num_tables):
        tag, _, off, _ = struct.unpack('>4sIII', data[12 + 16 * i: 28 + 16 * i])
        if tag == b'cmap':
            cmap_off = off
    if cmap_off is None:
        raise ValueError('cmap 없음: ' + path)
    n = struct.unpack('>H', data[cmap_off + 2: cmap_off + 4])[0]
    points = set()
    for i in range(n):
        plat, enc, sub = struct.unpack('>HHI', data[cmap_off + 4 + 8 * i: cmap_off + 12 + 8 * i])
        base = cmap_off + sub
        fmt = struct.unpack('>H', data[base: base + 2])[0]
        if fmt == 4:
            seg2 = struct.unpack('>H', data[base + 6: base + 8])[0]
            segs = seg2 // 2
            ends = struct.unpack('>%dH' % segs, data[base + 14: base + 14 + seg2])
            starts_off = base + 16 + seg2
            starts = struct.unpack('>%dH' % segs, data[starts_off: starts_off + seg2])
            deltas = struct.unpack('>%dh' % segs, data[starts_off + seg2: starts_off + 2 * seg2])
            ro_off = starts_off + 2 * seg2
            ros = struct.unpack('>%dH' % segs, data[ro_off: ro_off + seg2])
            for s in range(segs):
                for c in range(starts[s], ends[s] + 1):
                    if c == 0xFFFF:
                        continue
                    if ros[s] == 0:
                        gid = (c + deltas[s]) & 0xFFFF
                    else:
                        addr = ro_off + 2 * s + ros[s] + 2 * (c - starts[s])
                        gid = struct.unpack('>H', data[addr: addr + 2])[0]
                        if gid:
                            gid = (gid + deltas[s]) & 0xFFFF
                    if gid:
                        points.add(c)
        elif fmt == 12:
            ngroups = struct.unpack('>I', data[base + 12: base + 16])[0]
            for g in range(ngroups):
                sc, ec, sg = struct.unpack('>III', data[base + 16 + 12 * g: base + 28 + 12 * g])
                points.update(range(sc, ec + 1))
    return points


def used_characters():
    chars = {}
    for path in glob.glob(os.path.join(ROOT, 'Assets', 'Scripts', '**', '*.cs'), recursive=True):
        rel = os.path.relpath(path, ROOT)
        with open(path, encoding='utf-8', errors='ignore') as f:
            for ln, line in enumerate(f, 1):
                if line.lstrip().startswith('//'):
                    continue
                for m in LITERAL.finditer(line):
                    text = m.group(1)
                    text = re.sub(r'\{[^{}]*\}', '', text)  # 보간 구멍 제거
                    text = re.sub(r'\\[nrt"\\]', '', text)
                    text = re.sub(r'<[^<>]{1,40}>', '', text)  # TMP 리치 텍스트 태그 제거
                    for ch in text:
                        if ord(ch) < 0x20 or ch in ' ':
                            continue
                        chars.setdefault(ch, (rel, ln))
    return chars


def tmp_info(path):
    if not os.path.exists(path):
        return {'exists': False}
    with open(path, encoding='utf-8', errors='ignore') as f:
        text = f.read()
    mode = re.search(r'm_AtlasPopulationMode:\s*(\d+)', text)
    unicodes = {int(u) for u in re.findall(r'm_Unicode:\s*(\d+)', text)}
    fallback = re.findall(r'm_FallbackFontAssetTable:\s*\n((?:\s*-\s*\{.*\}\s*\n)*)', text)
    return {
        'exists': True,
        'populationMode': int(mode.group(1)) if mode else None,  # 0=정적, 1=동적
        'staticCharacters': len(unicodes),
        'unicodes': unicodes,
        'fallbacks': len(re.findall(r'fileID', fallback[0])) if fallback else 0,
    }


def main():
    chars = used_characters()
    covers = {name: cmap_codepoints(p) for name, p in FONTS.items() if os.path.exists(p)}
    tmp = tmp_info(TMP_ASSET)
    missing_any, missing_primary = [], []
    for ch, where in sorted(chars.items()):
        cp = ord(ch)
        in_primary = cp in covers.get('Jalnan2', set())
        in_any = any(cp in s for s in covers.values())
        if not in_primary:
            missing_primary.append((ch, cp, where, in_any))
        if not in_any:
            missing_any.append((ch, cp, where))
    hangul = [c for c in chars if 0xAC00 <= ord(c) <= 0xD7A3]
    print(f'사용 글자 {len(chars)}종(한글 음절 {len(hangul)}), 폰트: ' + ', '.join(f'{k} {len(v)}자' for k, v in covers.items()))
    if tmp.get('exists'):
        mode = {0: '정적', 1: '동적'}.get(tmp['populationMode'], str(tmp['populationMode']))
        print(f"TMP Jalnan2_SDF: 채움 모드 {mode}, 정적 글자표 {tmp['staticCharacters']}자, 대체 폰트 {tmp['fallbacks']}개")
        if tmp['populationMode'] == 0:
            notin = [c for c in chars if ord(c) not in tmp['unicodes'] and ord(c) in covers.get('Jalnan2', set())]
            print(f'  정적 아틀라스에 없는 사용 글자: {len(notin)}자 ' + ''.join(sorted(notin))[:200])
    print(f'Jalnan2에 없는 글자 {len(missing_primary)}종(이모지 폰트가 받는 것 포함):')
    for ch, cp, (rel, ln), in_any in missing_primary[:60]:
        print(f"  U+{cp:04X} '{ch}'  {'대체 폰트에 있음' if in_any else '어디에도 없음'}  예: {rel}:{ln}")
    print(f'어느 폰트에도 없는 글자: {len(missing_any)}종')
    if '--json' in sys.argv:
        out = sys.argv[sys.argv.index('--json') + 1]
        with open(out, 'w', encoding='utf-8') as f:
            json.dump({'used': len(chars), 'missingPrimary': [[c, cp, w, a] for c, cp, w, a in missing_primary],
                       'missingAny': [[c, cp, w] for c, cp, w in missing_any]}, f, ensure_ascii=False, indent=1)
    return 1 if missing_any else 0


if __name__ == '__main__':
    sys.exit(main())
