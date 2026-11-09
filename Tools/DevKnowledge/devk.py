#!/usr/bin/env python3
"""Project P.A. 게임 개발 기술 지식 라이브러리 CLI (표준 라이브러리 + 선택적 Pillow).

정본:
  Docs/DevKnowledge/techniques/*.md   기술 노트(앞머리 YAML 비슷한 key: value + 본문). 사람이 읽고 git으로 남는 정본.
  Docs/DevKnowledge/devknowledge.sqlite  수집 기록(검색어·출처·증거·계정)과 기술 색인(FTS5). `index`로 md를 다시 읽는다.
  Logs/DevKnowledge/cache/<source>/      프레임·자막 등 원본 파생물(공개 저장소 밖, 분석 뒤 지워도 됨).

자주 쓰는 명령:
  python Tools/DevKnowledge/devk.py search "navmesh avoidance"      # 기술·출처 찾기(Claude Code/Codex 공용)
  python Tools/DevKnowledge/devk.py show <slug>                      # 기술 노트 경로·출처·검증
  python Tools/DevKnowledge/devk.py stats --topic shop-npc-movement  # 수집·분석·검증 성공률과 활용 가치
수집 기록용(탐색 작업자):
  query-add / source-add / source-set / evidence-add / frames / index / export
"""
import argparse, datetime as dt, hashlib, json, os, re, sqlite3, sys, urllib.parse, urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LIB = os.path.join(ROOT, "Docs", "DevKnowledge")
TECH_DIR = os.path.join(LIB, "techniques")
DB = os.path.join(LIB, "devknowledge.sqlite")
CACHE = os.path.join(ROOT, "Logs", "DevKnowledge", "cache")

SCHEMA = """
CREATE TABLE IF NOT EXISTS queries(id INTEGER PRIMARY KEY, topic TEXT, platform TEXT, query TEXT, origin TEXT,
  ran_at TEXT, results INTEGER DEFAULT 0, kept INTEGER DEFAULT 0, UNIQUE(topic, platform, query));
CREATE TABLE IF NOT EXISTS sources(id TEXT PRIMARY KEY, platform TEXT, url TEXT UNIQUE, title TEXT, author TEXT,
  published TEXT, duration_s INTEGER, kind TEXT, topic TEXT, query_id INTEGER, relevance REAL, quality REAL, score REAL,
  status TEXT, fail_reason TEXT, dup_of TEXT, notes TEXT, found_at TEXT);
CREATE TABLE IF NOT EXISTS evidence(id INTEGER PRIMARY KEY, source_id TEXT, kind TEXT, ref TEXT, summary TEXT,
  ok INTEGER, added_at TEXT);
CREATE TABLE IF NOT EXISTS accounts(id TEXT PRIMARY KEY, platform TEXT, handle TEXT, url TEXT, hits INTEGER DEFAULT 0,
  best_score REAL DEFAULT 0, promising INTEGER DEFAULT 0, note TEXT);
CREATE TABLE IF NOT EXISTS techniques(slug TEXT PRIMARY KEY, title TEXT, topic TEXT, status TEXT, pa_fit TEXT,
  value INTEGER, applied TEXT, md_path TEXT, updated TEXT);
CREATE TABLE IF NOT EXISTS technique_sources(slug TEXT, source_id TEXT, role TEXT, PRIMARY KEY(slug, source_id, role));
CREATE TABLE IF NOT EXISTS verifications(slug TEXT, ref_url TEXT, ref_kind TEXT, verdict TEXT, claim TEXT,
  PRIMARY KEY(slug, ref_url, claim));
CREATE VIRTUAL TABLE IF NOT EXISTS tech_fts USING fts5(slug, title, body, tokenize='unicode61');
"""

# 주제별 관련도 어휘(가중치). 새 주제는 여기 한 줄을 더한다.
TOPICS = {
    "shop-npc-movement": {
        "npc": 2, "navmesh": 3, "nav mesh": 3, "pathfinding": 2, "avoidance": 3, "crowd": 3, "steering": 3,
        "separation": 2, "rvo": 3, "orca": 3, "queue": 3, "line": 1, "shop": 3, "store": 2, "customer": 3,
        "shopper": 3, "browse": 2, "wander": 2, "idle": 2, "schedule": 2, "villager": 2, "townsfolk": 2,
        "behavior tree": 2, "state machine": 2, "utility ai": 2, "agent": 1, "unity": 1, "godot": 1, "unreal": 1,
        "cozy": 1, "life sim": 1, "simulation": 1, "look at": 2, "head look": 2, "smart object": 3,
        "point of interest": 2, "waypoint": 2, "flow field": 2, "natural": 1, "movement": 1, "ai": 1,
    },
}


def now():
    return dt.datetime.now().strftime("%Y-%m-%d %H:%M")


def connect():
    os.makedirs(TECH_DIR, exist_ok=True)
    con = sqlite3.connect(DB)
    con.row_factory = sqlite3.Row
    con.executescript(SCHEMA)
    return con


# ───────── 출처 정규화·중복 ─────────
def canonical(url):
    """같은 콘텐츠를 같은 id로: YouTube 영상 id, Instagram shortcode, 그 밖은 쿼리·조각 뺀 URL 해시."""
    u = urllib.parse.urlparse(url.strip())
    host = u.netloc.lower().removeprefix("www.").removeprefix("m.")
    if host in ("youtube.com", "youtu.be", "music.youtube.com"):
        vid = None
        if host == "youtu.be":
            vid = u.path.strip("/").split("/")[0]
        else:
            q = urllib.parse.parse_qs(u.query)
            if "v" in q:
                vid = q["v"][0]
            m = re.match(r"/(shorts|embed|live)/([\w-]{11})", u.path)
            if m:
                vid = m.group(2)
        if vid:
            return "yt:" + vid, "https://www.youtube.com/watch?v=" + vid
    if host == "instagram.com":
        m = re.match(r"/(?:[\w.]+/)?(p|reel|reels|tv)/([\w-]+)", u.path)
        if m:
            return "ig:" + m.group(2), f"https://www.instagram.com/{'reel' if m.group(1).startswith('reel') else 'p'}/{m.group(2)}/"
    clean = urllib.parse.urlunparse((u.scheme or "https", host, u.path.rstrip("/"), "", "", ""))
    return "web:" + hashlib.sha1(clean.encode()).hexdigest()[:12], clean


def relevance(topic, text):
    words = TOPICS.get(topic, {})
    t = (text or "").lower()
    hit = sum(w for k, w in words.items() if k in t)
    return round(min(1.0, hit / 12.0), 2)


def norm_title(t):
    return re.sub(r"[^0-9a-z가-힣]+", " ", (t or "").lower()).strip()


def near_duplicate(con, sid, title, duration):
    """제목이 거의 같고 길이가 비슷한 다른 출처(재업로드·클립)."""
    nt = set(norm_title(title).split())
    if len(nt) < 3:
        return None
    for r in con.execute("SELECT id,title,duration_s FROM sources WHERE id<>? AND dup_of IS NULL", (sid,)):
        ot = set(norm_title(r["title"]).split())
        if not ot:
            continue
        jac = len(nt & ot) / len(nt | ot)
        close = duration is None or r["duration_s"] is None or abs((r["duration_s"] or 0) - duration) <= max(5, .05 * duration)
        if jac >= .8 and close:
            return r["id"]
    return None


# ───────── 명령 ─────────
def cmd_query_add(a):
    con = connect()
    con.execute("INSERT OR IGNORE INTO queries(topic,platform,query,origin,ran_at) VALUES(?,?,?,?,?)",
                (a.topic, a.platform, a.query, a.origin, now()))
    if a.results is not None or a.kept is not None:
        con.execute("UPDATE queries SET results=COALESCE(?,results), kept=COALESCE(?,kept), ran_at=? WHERE topic=? AND platform=? AND query=?",
                    (a.results, a.kept, now(), a.topic, a.platform, a.query))
    qid = con.execute("SELECT id FROM queries WHERE topic=? AND platform=? AND query=?", (a.topic, a.platform, a.query)).fetchone()[0]
    con.commit()
    print(qid)


def cmd_source_add(a):
    con = connect()
    sid, url = canonical(a.url)
    rel = a.relevance if a.relevance is not None else relevance(a.topic, f"{a.title} {a.notes or ''}")
    qual = a.quality if a.quality is not None else .5
    score = round(.6 * rel + .4 * qual, 2)
    old = con.execute("SELECT id,status FROM sources WHERE id=?", (sid,)).fetchone()
    if old:
        print(f"{sid} 이미 있음({old['status']}) — 같은 콘텐츠로 합침")
        return
    dup = near_duplicate(con, sid, a.title, a.duration)
    status = "duplicate" if dup else (a.status or "found")
    con.execute("""INSERT INTO sources(id,platform,url,title,author,published,duration_s,kind,topic,query_id,relevance,quality,score,
                   status,dup_of,notes,found_at) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
                (sid, a.platform, url, a.title, a.author, a.published, a.duration, a.kind, a.topic, a.query_id, rel, qual, score,
                 status, dup, a.notes, now()))
    if a.author:
        acc = f"{a.platform}:{a.author}"
        con.execute("INSERT OR IGNORE INTO accounts(id,platform,handle,url) VALUES(?,?,?,?)", (acc, a.platform, a.author, a.author_url))
        con.execute("UPDATE accounts SET hits=hits+1, best_score=MAX(best_score,?), promising=CASE WHEN MAX(best_score,?)>=.6 THEN 1 ELSE promising END WHERE id=?",
                    (score, score, acc))
    con.commit()
    print(f"{sid} score={score} rel={rel} qual={qual} status={status}" + (f" dup_of={dup}" if dup else ""))


def cmd_source_set(a):
    con = connect()
    sid = a.id if ":" in a.id else canonical(a.id)[0]
    sets, vals = [], []
    for col in ("status", "fail_reason", "notes", "title", "author", "published", "kind"):
        v = getattr(a, col)
        if v is not None:
            sets.append(f"{col}=?"); vals.append(v)
    if a.duration is not None:
        sets.append("duration_s=?"); vals.append(a.duration)
    if not sets:
        return
    con.execute(f"UPDATE sources SET {','.join(sets)} WHERE id=?", (*vals, sid))
    con.commit()
    print(sid, "updated")


def cmd_evidence_add(a):
    con = connect()
    sid = a.source if ":" in a.source else canonical(a.source)[0]
    con.execute("INSERT INTO evidence(source_id,kind,ref,summary,ok,added_at) VALUES(?,?,?,?,?,?)",
                (sid, a.kind, a.ref, a.summary, 1 if a.ok else 0, now()))
    con.commit()
    print("evidence +1", sid, a.kind, "ok" if a.ok else "FAIL")


def cmd_frames(a):
    """공개 이미지 URL(유튜브 썸네일 프레임·게시물 이미지)을 캐시에 받고 한 장짜리 모음 그림을 만든다."""
    sid = a.source if ":" in a.source else canonical(a.source)[0]
    out = os.path.join(CACHE, sid.replace(":", "_"))
    os.makedirs(out, exist_ok=True)
    urls = list(a.urls)
    if sid.startswith("yt:") and not urls:
        vid = sid[3:]
        # YouTube가 공개로 제공하는 자동 프레임(영상 약 1/4·1/2·3/4 지점)과 대표 썸네일.
        urls = [f"https://i.ytimg.com/vi/{vid}/{n}.jpg" for n in ("hqdefault", "hq1", "hq2", "hq3")]
    saved = []
    for i, u in enumerate(urls):
        p = os.path.join(out, f"frame_{i}.jpg")
        try:
            req = urllib.request.Request(u, headers={"User-Agent": "Mozilla/5.0 (ProjectPA DevKnowledge research)"})
            with urllib.request.urlopen(req, timeout=20) as r, open(p, "wb") as f:
                f.write(r.read())
            saved.append(p)
        except Exception as e:
            print("frame fail", u, e)
    sheet = None
    if saved:
        try:
            from PIL import Image
            ims = [Image.open(p).convert("RGB") for p in saved]
            w = 480
            ims = [im.resize((w, int(im.height * w / im.width))) for im in ims]
            h = max(im.height for im in ims)
            cols = 2
            rows = (len(ims) + cols - 1) // cols
            canvas = Image.new("RGB", (w * cols, h * rows), (20, 20, 20))
            for k, im in enumerate(ims):
                canvas.paste(im, ((k % cols) * w, (k // cols) * h))
            sheet = os.path.join(out, "sheet.jpg")
            canvas.save(sheet, quality=88)
        except Exception as e:
            print("sheet fail", e)
    print(json.dumps({"source": sid, "saved": len(saved), "requested": len(urls), "sheet": sheet}, ensure_ascii=False))


def cmd_meta(a):
    """YouTube 공식 oEmbed로 제목·채널을 채운다(다운로드 없음)."""
    con = connect()
    sid = a.source if ":" in a.source else canonical(a.source)[0]
    row = con.execute("SELECT url FROM sources WHERE id=?", (sid,)).fetchone()
    if not row:
        print("없음:", sid); return
    api = "https://www.youtube.com/oembed?format=json&url=" + urllib.parse.quote(row["url"], safe="")
    try:
        with urllib.request.urlopen(urllib.request.Request(api, headers={"User-Agent": "Mozilla/5.0"}), timeout=20) as r:
            d = json.loads(r.read().decode("utf-8"))
        con.execute("UPDATE sources SET title=?, author=? WHERE id=?", (d.get("title"), d.get("author_name"), sid))
        acc = f"youtube:{d.get('author_name')}"
        con.execute("INSERT OR IGNORE INTO accounts(id,platform,handle,url) VALUES(?,?,?,?)", (acc, "youtube", d.get("author_name"), d.get("author_url")))
        con.commit()
        print(json.dumps({"source": sid, "title": d.get("title"), "author": d.get("author_name"), "author_url": d.get("author_url")}, ensure_ascii=False))
    except Exception as e:
        print(json.dumps({"source": sid, "error": str(e)}, ensure_ascii=False))


def parse_md(path):
    text = open(path, encoding="utf-8").read()
    meta, body = {}, text
    m = re.match(r"---\n(.*?)\n---\n(.*)", text, re.S)
    if m:
        body = m.group(2)
        for line in m.group(1).splitlines():
            if ":" in line and not line.startswith(" "):
                k, v = line.split(":", 1)
                meta[k.strip()] = v.strip()
    return meta, body


def cmd_index(a):
    """기술 노트(md)를 DB 기술 표·FTS·출처/검증 연결로 다시 읽는다. md가 정본."""
    con = connect()
    con.execute("DELETE FROM techniques"); con.execute("DELETE FROM technique_sources")
    con.execute("DELETE FROM verifications"); con.execute("DELETE FROM tech_fts")
    n = 0
    for fn in sorted(os.listdir(TECH_DIR)):
        if not fn.endswith(".md"):
            continue
        path = os.path.join(TECH_DIR, fn)
        meta, body = parse_md(path)
        slug = meta.get("slug") or fn[:-3]
        con.execute("INSERT INTO techniques VALUES(?,?,?,?,?,?,?,?,?)",
                    (slug, meta.get("title"), meta.get("topic"), meta.get("status"), meta.get("pa_fit"),
                     int(meta.get("value", "0") or 0), meta.get("applied"), os.path.relpath(path, ROOT).replace("\\", "/"), meta.get("updated")))
        for role in ("sources", "demonstrated_by"):
            for s in filter(None, (x.strip() for x in meta.get(role, "").split(","))):
                con.execute("INSERT OR IGNORE INTO technique_sources VALUES(?,?,?)", (slug, s, role))
        # 본문의 "- [verdict] kind: url — claim" 줄을 검증으로 읽는다.
        for vm in re.finditer(r"^- \[(confirmed|partial|contradicted|not_found)\] (official_doc|source_code|paper|talk): (\S+) — (.+)$", body, re.M):
            con.execute("INSERT OR REPLACE INTO verifications VALUES(?,?,?,?,?)", (slug, vm.group(3), vm.group(2), vm.group(1), vm.group(4)))
        con.execute("INSERT INTO tech_fts VALUES(?,?,?)", (slug, meta.get("title", ""), body))
        n += 1
    con.commit()
    print(f"indexed {n} techniques")


def cmd_search(a):
    con = connect()
    q = a.text.strip()
    print(f"# 기술 ({q})")
    terms = " OR ".join(f'"{t}"' for t in re.findall(r"[\w가-힣]+", q)) or '""'
    rows = con.execute("""SELECT t.slug,t.title,t.status,t.pa_fit,t.value,t.md_path,snippet(tech_fts,2,'[',']','…',12) s
                          FROM tech_fts JOIN techniques t USING(slug) WHERE tech_fts MATCH ? ORDER BY rank LIMIT ?""", (terms, a.limit)).fetchall()
    for r in rows:
        print(f"- {r['slug']} [{r['status']}/fit:{r['pa_fit']}/value:{r['value']}] {r['title']}\n    {r['md_path']}\n    …{r['s']}")
    if not rows:
        print("- (없음)")
    print(f"# 출처 ({q})")
    like = f"%{q.lower()}%"
    for r in con.execute("""SELECT id,platform,status,score,title,url FROM sources WHERE lower(title) LIKE ? OR lower(notes) LIKE ?
                            ORDER BY score DESC LIMIT ?""", (like, like, a.limit)):
        print(f"- {r['id']} [{r['platform']}/{r['status']}/{r['score']}] {r['title']}\n    {r['url']}")


def cmd_show(a):
    con = connect()
    t = con.execute("SELECT * FROM techniques WHERE slug=?", (a.slug,)).fetchone()
    if not t:
        print("없음:", a.slug); return
    print(dict(t))
    for r in con.execute("SELECT ts.role,s.id,s.url,s.title FROM technique_sources ts LEFT JOIN sources s ON s.id=ts.source_id WHERE ts.slug=?", (a.slug,)):
        print(" source", r["role"], r["id"], r["url"] or "", r["title"] or "")
    for r in con.execute("SELECT * FROM verifications WHERE slug=?", (a.slug,)):
        print(" verify", r["verdict"], r["ref_kind"], r["ref_url"], "—", r["claim"])


def stats(con, topic):
    where, args = ("WHERE topic=?", (topic,)) if topic else ("", ())
    rows = con.execute(f"SELECT platform,status,COUNT(*) n FROM sources {where} GROUP BY platform,status", args).fetchall()
    per = {}
    for r in rows:
        per.setdefault(r["platform"], {})[r["status"]] = r["n"]
    out = {"platforms": {}}
    for p, d in per.items():
        found = sum(d.values()) - d.get("duplicate", 0)
        attempted = found - d.get("found", 0) - d.get("skipped", 0)
        ok_collect = d.get("collected", 0) + d.get("analyzed", 0)
        out["platforms"][p] = {"found": found, "duplicates": d.get("duplicate", 0), "attempted": attempted,
                               "collected_or_analyzed": ok_collect, "analyzed": d.get("analyzed", 0), "failed": d.get("failed", 0),
                               "collect_rate": round(ok_collect / attempted, 2) if attempted else None,
                               "analyze_rate": round(d.get("analyzed", 0) / attempted, 2) if attempted else None}
    ev = con.execute(f"""SELECT e.kind, SUM(e.ok) ok, COUNT(*) n FROM evidence e JOIN sources s ON s.id=e.source_id {where.replace('topic','s.topic')}
                         GROUP BY e.kind""", args).fetchall()
    out["evidence"] = {r["kind"]: f"{r['ok']}/{r['n']}" for r in ev}
    tw, ta = ("WHERE topic=?", (topic,)) if topic else ("", ())
    techs = con.execute(f"SELECT * FROM techniques {tw}", ta).fetchall()
    ver = {r["slug"]: r["n"] for r in con.execute("SELECT slug, COUNT(*) n FROM verifications WHERE verdict IN ('confirmed','partial') GROUP BY slug")}
    def applied(t):  # applied 앞머리 첫 단어: yes | partial | measuring | no
        return ((t["applied"] or "no").split(":")[0].split()[0]).lower()
    out["techniques"] = {"total": len(techs), "verified": sum(1 for t in techs if t["status"] == "verified"),
                         "with_official_or_code_check": sum(1 for t in techs if ver.get(t["slug"])),
                         "pa_fit_direct": sum(1 for t in techs if t["pa_fit"] == "direct"),
                         "applied_yes": sum(1 for t in techs if applied(t) == "yes"),
                         "applied_partial": sum(1 for t in techs if applied(t) == "partial"),
                         "measuring": sum(1 for t in techs if applied(t) == "measuring"),
                         "value_avg": round(sum(t["value"] or 0 for t in techs) / len(techs), 2) if techs else None}
    q = con.execute(f"SELECT COUNT(*) n, SUM(results) r, SUM(kept) k FROM queries {where}", args).fetchone()
    out["queries"] = {"count": q["n"], "results": q["r"], "kept": q["k"]}
    # 탐색 단계: 플랫폼별 검색 수, 결과가 나온 검색 수, 남긴 후보 수.
    for r in con.execute(f"SELECT platform, COUNT(*) n, SUM(results>0) hit, SUM(kept) k FROM queries {where} GROUP BY platform", args):
        d = out["platforms"].setdefault(r["platform"], {})
        d.update({"queries": r["n"], "queries_with_results": r["hit"], "kept_from_search": r["k"],
                  "discovery_rate": round(r["hit"] / r["n"], 2) if r["n"] else None})
    out["accounts_promising"] = [r["id"] for r in con.execute("SELECT id FROM accounts WHERE promising=1 ORDER BY best_score DESC")]
    return out


def cmd_stats(a):
    print(json.dumps(stats(connect(), a.topic), ensure_ascii=False, indent=1))


def cmd_export(a):
    """출처 목록 SOURCES.md를 DB에서 다시 쓴다(grep용)."""
    con = connect()
    lines = ["# 출처 원장 (자동 생성 — `devk.py export`)", "",
             "| id | 플랫폼 | 상태 | 점수 | 제목 | 작성자 | 링크 | 메모 |", "|---|---|---|---|---|---|---|---|"]
    for r in con.execute("SELECT * FROM sources ORDER BY topic, score DESC"):
        title = (r["title"] or "").replace("|", "/")[:80]
        note = (r["fail_reason"] or r["notes"] or "").replace("|", "/").replace("\n", " ")[:90]
        lines.append(f"| {r['id']} | {r['platform']} | {r['status']}{' →'+r['dup_of'] if r['dup_of'] else ''} | {r['score']} | {title} | {r['author'] or ''} | {r['url']} | {note} |")
    open(os.path.join(LIB, "SOURCES.md"), "w", encoding="utf-8").write("\n".join(lines) + "\n")
    print("wrote SOURCES.md", len(lines) - 4, "rows")


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("query-add"); s.add_argument("--topic", required=True); s.add_argument("--platform", required=True)
    s.add_argument("--query", required=True); s.add_argument("--origin", default="seed"); s.add_argument("--results", type=int); s.add_argument("--kept", type=int)
    s.set_defaults(f=cmd_query_add)
    s = sub.add_parser("source-add"); s.add_argument("--url", required=True); s.add_argument("--platform", required=True); s.add_argument("--topic", required=True)
    s.add_argument("--title", default=""); s.add_argument("--author"); s.add_argument("--author-url"); s.add_argument("--published"); s.add_argument("--duration", type=int)
    s.add_argument("--kind", default="video"); s.add_argument("--query-id", type=int); s.add_argument("--relevance", type=float); s.add_argument("--quality", type=float)
    s.add_argument("--status"); s.add_argument("--notes"); s.set_defaults(f=cmd_source_add)
    s = sub.add_parser("source-set"); s.add_argument("id")
    for col in ("status", "fail_reason", "notes", "title", "author", "published", "kind"):
        s.add_argument("--" + col.replace("_", "-"), dest=col)
    s.add_argument("--duration", type=int); s.set_defaults(f=cmd_source_set)
    s = sub.add_parser("evidence-add"); s.add_argument("source"); s.add_argument("--kind", required=True); s.add_argument("--ref", default="")
    s.add_argument("--summary", default=""); s.add_argument("--ok", type=int, default=1); s.set_defaults(f=cmd_evidence_add)
    s = sub.add_parser("frames"); s.add_argument("source"); s.add_argument("urls", nargs="*"); s.set_defaults(f=cmd_frames)
    s = sub.add_parser("meta"); s.add_argument("source"); s.set_defaults(f=cmd_meta)
    sub.add_parser("index").set_defaults(f=cmd_index)
    s = sub.add_parser("search"); s.add_argument("text"); s.add_argument("--limit", type=int, default=8); s.set_defaults(f=cmd_search)
    s = sub.add_parser("show"); s.add_argument("slug"); s.set_defaults(f=cmd_show)
    s = sub.add_parser("stats"); s.add_argument("--topic"); s.set_defaults(f=cmd_stats)
    sub.add_parser("export").set_defaults(f=cmd_export)
    a = p.parse_args()
    a.f(a)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
