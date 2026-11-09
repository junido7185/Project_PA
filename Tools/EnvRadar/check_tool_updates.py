"""도구 업그레이드 점검기(읽기 전용).

지금 쓰는 도구의 설치 버전과 공개 저장소의 최신 버전을 비교하고,
watchlist.json 후보의 새 릴리스·라이선스 변경을 알려 준다.
아무것도 설치·변경하지 않는다. 조회 대상: npm 레지스트리, PyPI, GitHub API(비인증 60회/시간),
nodejs.org, download.blender.org, gyan.dev.

사용: python Tools/EnvRadar/check_tool_updates.py
결과: Logs/ToolRadar/radar-YYYYMMDD.md (+ state.json: 지난 점검 대비 '새로 나온 것' 표시용)
"""
import datetime
import json
import os
import re
import subprocess
import urllib.request

try:
    from packaging.version import Version
except ImportError:  # packaging이 없으면 문자열 비교로
    Version = None

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
HOME = os.path.expanduser('~')
OUT_DIR = os.path.join(ROOT, 'Logs', 'ToolRadar')
STATE = os.path.join(OUT_DIR, 'state.json')
UA = {'User-Agent': 'project-pa-tool-radar', 'Accept': 'application/json'}
BLENDER = r'C:\Users\sdjsd\Desktop\Unity\PA_ArtStaging\_tools\blender-4.5.14-windows-x64\blender.exe'
PY312 = r'C:\Users\sdjsd\AppData\Local\Programs\Python\Python312\python.exe'
_gh_local = os.path.join(os.environ.get('LOCALAPPDATA', ''), 'Programs', 'GitHub CLI', 'bin', 'gh.exe')
GH_EXE = _gh_local if os.path.exists(_gh_local) else 'gh'  # PATH가 옛 상태인 셸에서도 찾도록
errors = []
GH = {'token': None, 'limited': False}


def get(url, as_json=True):
    headers = dict(UA)
    if url.startswith('https://api.github.com/'):
        if GH['limited']:
            raise RuntimeError('GitHub 조회 한도 소진 — 건너뜀(gh auth login 하면 한도 5000회/시간)')
        if GH['token']:
            headers['Authorization'] = 'Bearer ' + GH['token']  # 메모리에서만 사용, 파일에 쓰지 않음
    req = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=20) as r:
            body = r.read().decode('utf-8', errors='ignore')
    except urllib.error.HTTPError as e:
        if e.code in (403, 429) and 'api.github.com' in url:
            GH['limited'] = True
        raise
    return json.loads(body) if as_json else body


def raw(repo, path):
    path = path[2:] if path.startswith('./') else path.lstrip('/')
    return get(f'https://raw.githubusercontent.com/{repo}/HEAD/{path}')


def remote_plugin_version(repo, name, local_market_dir):
    """마켓 저장소 HEAD의 실제 플러그인 버전(raw 파일이라 API 한도와 무관)."""
    mj = json.load(open(os.path.join(local_market_dir, '.claude-plugin', 'marketplace.json'), encoding='utf-8'))
    entry = next((p for p in mj.get('plugins', []) if p.get('name') == name), {})
    src = entry.get('source')
    if isinstance(src, dict) and src.get('url'):
        r2 = re.sub(r'^https://github.com/|\.git$', '', src['url'])
        return raw(r2, '.claude-plugin/plugin.json').get('version')
    if isinstance(src, str) and src not in ('./', '.'):
        try:
            v = raw(repo, src.rstrip('/') + '/.claude-plugin/plugin.json').get('version')
            if v:
                return v
        except Exception:
            pass
    remote = raw(repo, '.claude-plugin/marketplace.json')
    return next((p.get('version') for p in remote.get('plugins', []) if p.get('name') == name), None)


def safe(fn, label):
    try:
        return fn()
    except Exception as e:  # 조회 실패는 기록만 하고 계속
        errors.append(f'{label}: {type(e).__name__} {e}'[:200])
        return None


def run(cmd):
    try:
        return subprocess.run(cmd, capture_output=True, text=True, timeout=30, shell=isinstance(cmd, str)).stdout.strip()
    except Exception:
        return ''


def newer(latest, current):
    if not latest or not current:
        return None
    a, b = str(latest).lstrip('v'), str(current).lstrip('v')
    if Version:
        try:
            return Version(a) > Version(b)
        except Exception:
            pass
    return a != b


def npm_latest(pkg):
    return safe(lambda: get(f'https://registry.npmjs.org/{pkg.replace("/", "%2F")}/latest')['version'], 'npm ' + pkg)


def pypi_latest(pkg):
    return safe(lambda: get(f'https://pypi.org/pypi/{pkg}/json')['info']['version'], 'pypi ' + pkg)


def gh_release(repo):
    return safe(lambda: get(f'https://api.github.com/repos/{repo}/releases/latest')['tag_name'], 'release ' + repo)


def gh_repo(repo):
    def f():
        d = get(f'https://api.github.com/repos/{repo}')
        return {'stars': d.get('stargazers_count'), 'pushed': (d.get('pushed_at') or '')[:10],
                'license': (d.get('license') or {}).get('spdx_id'), 'archived': d.get('archived'),
                'branch': d.get('default_branch')}
    return safe(f, 'repo ' + repo)


def gh_head(repo, branch='main'):
    return safe(lambda: get(f'https://api.github.com/repos/{repo}/commits/{branch}')['sha'], 'head ' + repo)


def core_rows():
    rows = []  # (도구, 현재, 최신, 업그레이드?, 방법)
    cur = run('claude --version').split(' ')[0]
    lat = npm_latest('@anthropic-ai/claude-code')
    rows.append(('Claude Code CLI', cur, lat, newer(lat, cur), '`claude update` (데스크탑 앱은 앱 자체 업데이트)'))

    # 플러그인: 설치 커밋 vs 마켓 저장소 최신 커밋
    ip = json.load(open(os.path.join(HOME, '.claude', 'plugins', 'installed_plugins.json'), encoding='utf-8'))
    km = json.load(open(os.path.join(HOME, '.claude', 'plugins', 'known_marketplaces.json'), encoding='utf-8'))
    for key, entries in ip.get('plugins', {}).items():
        name, market = key.split('@', 1)
        e = entries[0]
        mk = km.get(market, {})
        src = mk.get('source', {})
        repo = src.get('repo') or (re.sub(r'^https://github.com/|\.git$', '', src.get('url', '')) or None)
        cur = e.get('version')
        if not repo:
            rows.append((f'플러그인 {name}', cur, '-', None, '로컬 플러그인(직접 관리)'))
            continue
        lat = safe(lambda: remote_plugin_version(repo, name, mk.get('installLocation', '')), 'plugin ' + key)
        semver = bool(re.match(r'^\d+\.\d+', str(cur or ''))) and bool(re.match(r'^\d+\.\d+', str(lat or '')))
        flag = newer(lat, cur) if semver else (None if not lat else lat[:12] != str(cur)[:12])
        auto = ' (자동 업데이트 켜짐)' if mk.get('autoUpdate') else ''
        rows.append((f'플러그인 {name}', cur, lat or '?', flag, f'`claude plugin update {key}`{auto}'))

    # Unity MCP 패키지(#main 추적) vs 최신 릴리스
    lock = safe(lambda: json.load(open(os.path.join(ROOT, 'Packages', 'packages-lock.json'), encoding='utf-8')), 'packages-lock')
    pkg = (lock or {}).get('dependencies', {}).get('com.coplaydev.unity-mcp', {})
    pj = None
    cache = os.path.join(ROOT, 'Library', 'PackageCache')
    if os.path.isdir(cache):
        for d in os.listdir(cache):
            if d.startswith('com.coplaydev.unity-mcp'):
                pj = safe(lambda: json.load(open(os.path.join(cache, d, 'package.json'), encoding='utf-8'))['version'], 'unity-mcp package.json')
    lat = gh_release('CoplayDev/unity-mcp')
    rows.append(('MCP for Unity(에디터 패키지)', f"{pj or '?'} (#main {str(pkg.get('hash', ''))[:7]})", lat, newer(lat, pj),
                 '전시 뒤: manifest를 `#v<버전>` 태그로 고정해 갱신(단일 Unity writer가 에디터에서)'))
    lat = pypi_latest('mcpforunityserver')
    rows.append(('MCP for Unity 서버(PyPI)', '에디터 창이 띄움', lat, None, '에디터 패키지와 같은 버전으로 맞춤'))

    lat = pypi_latest('serena-agent')
    rows.append(('Serena(serena-agent)', 'uvx 캐시(고정 안 함)', lat, None, '`uvx --refresh --from serena-agent serena --help` 한 번 실행하면 최신으로 갱신'))

    cur = run('uv --version').split(' ')[1:2]
    cur = cur[0] if cur else ''
    lat = gh_release('astral-sh/uv')
    rows.append(('uv', cur, lat, newer(lat, cur), '`uv self update`'))

    cur = run('node --version').lstrip('v')
    def node_lts():
        idx = get('https://nodejs.org/dist/index.json')
        major = cur.split('.')[0]
        same = next((r['version'] for r in idx if r['version'].lstrip('v').split('.')[0] == major), None)
        lts = next((r['version'] for r in idx if r.get('lts')), None)
        return same, lts
    same, lts = safe(node_lts, 'node') or (None, None)
    rows.append(('Node.js', cur, f'{same} (최신 LTS {lts})', newer(same, cur), '`winget upgrade OpenJS.NodeJS.LTS`'))

    cur = re.sub(r'\.windows.*', '', run('git --version').replace('git version ', ''))
    lat = gh_release('git-for-windows/git')
    lat_clean = re.sub(r'\.windows.*|^v', '', lat or '')
    rows.append(('Git for Windows', cur, lat_clean, newer(lat_clean, cur), '`winget upgrade Git.Git`'))

    m = re.search(r'ffmpeg version (\S+)', run('ffmpeg -version'))
    cur = (m.group(1) if m else '').split('-')[0]
    lat = safe(lambda: get('https://www.gyan.dev/ffmpeg/builds/release-version', as_json=False).strip(), 'ffmpeg')
    rows.append(('ffmpeg', cur, lat, newer(lat, cur), '`winget upgrade Gyan.FFmpeg`'))

    m = re.search(r'blender-([\d.]+)-', BLENDER)
    cur = m.group(1) if m else ''
    def blender():
        idx = get('https://download.blender.org/release/', as_json=False)
        majors = sorted(set(re.findall(r'Blender(\d+\.\d+)/', idx)), key=lambda v: [int(x) for x in v.split('.')])
        line = '.'.join(cur.split('.')[:2])
        sub = get(f'https://download.blender.org/release/Blender{line}/', as_json=False)
        patches = sorted(set(re.findall(r'blender-(%s\.\d+)-windows-x64\.zip' % re.escape(line), sub)), key=lambda v: [int(x) for x in v.split('.')])
        return (patches[-1] if patches else None), (majors[-1] if majors else None)
    patch, major = safe(blender, 'blender') or (None, None)
    rows.append(('Blender 포터블', cur, f'{patch} (최신 계열 {major})', newer(patch, cur), '같은 LTS 계열의 새 포터블 zip을 `PA_ArtStaging/_tools`에 나란히 풀고 경로만 교체'))

    for mod in ('fonttools', 'pillow'):
        cur = run([PY312, '-c', f"import importlib.metadata as m;print(m.version('{mod}'))"])
        lat = pypi_latest(mod)
        rows.append((f'Python {mod}', cur, lat, newer(lat, cur), f'`{PY312} -m pip install -U {mod}`'))

    cur = run([GH_EXE, '--version']).split('\n')[0].replace('gh version ', '').split(' ')[0]
    lat = gh_release('cli/cli')
    rows.append(('gh CLI', cur or '미설치', lat, newer(lat, cur) if cur else None, '`winget upgrade GitHub.cli`'))

    npm_list = safe(lambda: json.loads(run('npm ls -g --depth=0 --json') or '{}').get('dependencies', {}), 'npm ls') or {}
    for pkg in ('odiff-bin', 'hanspell', 'jsfxr', '@mermaid-js/mermaid-cli'):
        cur = (npm_list.get(pkg) or {}).get('version')
        lat = npm_latest(pkg)
        rows.append((f'npm {pkg}', cur or '미설치', lat, newer(lat, cur) if cur else None, f'`npm install -g {pkg}@latest`'))
    return rows


def watch_rows(prev):
    wl = json.load(open(os.path.join(os.path.dirname(__file__), 'watchlist.json'), encoding='utf-8'))
    rows, snap = [], {}
    for c in wl['candidates']:
        repo = c.get('repo')
        if not repo:
            rows.append((c['name'], c['verdict'], '-', '-', '-', '', c['why']))
            continue
        info = gh_repo(repo) or {}
        rel = gh_release(repo) if info else None
        old = prev.get(repo, {})
        snap[repo] = {'release': rel, 'license': info.get('license'), 'stars': info.get('stars')} if info else old
        notes = []
        if old and rel and old.get('release') != rel:
            notes.append(f"새 릴리스 {old.get('release')}→{rel}")
        if old and info.get('license') and old.get('license') != info.get('license'):
            notes.append(f"라이선스 변경 {old.get('license')}→{info.get('license')}")
        if info.get('archived'):
            notes.append('보관됨(개발 중단)')
        rows.append((c['name'], c['verdict'], rel or '-', info.get('license') or '?', info.get('pushed') or '?',
                     ', '.join(notes), c['why']))
    return rows, snap


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    tok = run([GH_EXE, 'auth', 'token'])  # gh 로그인이 돼 있으면 조회 한도 상향(메모리에서만)
    GH['token'] = tok if re.match(r'^[A-Za-z0-9_]{20,}$', tok or '') else None
    prev = json.load(open(STATE, encoding='utf-8')) if os.path.exists(STATE) else {}
    today = datetime.date.today().isoformat()
    core = core_rows()
    watch, snap = watch_rows(prev.get('watch', {}))
    up = [r for r in core if r[3]]
    news = [r for r in watch if r[5]]
    L = [f'# 도구 업그레이드 점검 {today}', '',
         f'업그레이드 가능 {len(up)}건 · 후보 변화 {len(news)}건 · 조회 실패 {len(errors)}건. 설치·변경은 하지 않았다.', '',
         '## 지금 쓰는 도구', '| 도구 | 현재 | 최신 | 업그레이드 | 방법 |', '| --- | --- | --- | --- | --- |']
    for name, cur, lat, flag, how in core:
        mark = '**있음**' if flag else ('-' if flag is None else '최신')
        L.append(f'| {name} | {cur} | {lat} | {mark} | {how} |')
    L += ['', '## 지켜보는 후보 (watchlist.json)', '| 후보 | 판정 | 최신 릴리스 | 라이선스 | 마지막 push | 지난 점검 대비 | 이유 |',
          '| --- | --- | --- | --- | --- | --- | --- |']
    for r in watch:
        L.append('| ' + ' | '.join(str(x) for x in r) + ' |')
    if errors:
        skipped = [e for e in errors if '한도 소진' in e]
        L += ['', '## 조회 실패', *[f'- {e}' for e in errors if '한도 소진' not in e]]
        if skipped:
            L.append(f'- GitHub 조회 한도 소진으로 {len(skipped)}건 건너뜀 — 1시간 뒤 다시 실행하거나 gh 로그인 뒤 실행')
    L += ['', '판정 기준: `Docs/00_CURRENT/TOOL_UPGRADE_POLICY.md`']
    out = os.path.join(OUT_DIR, f'radar-{today.replace("-", "")}.md')
    with open(out, 'w', encoding='utf-8') as f:
        f.write('\n'.join(L) + '\n')
    with open(STATE, 'w', encoding='utf-8') as f:
        json.dump({'date': today, 'watch': snap}, f, ensure_ascii=False, indent=1)
    print('\n'.join(L))
    print('\n저장:', out)


if __name__ == '__main__':
    main()
