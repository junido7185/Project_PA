from pathlib import Path
import re

ROOT = Path.cwd()
OUT = ROOT / "P4_ITEM_REGISTRY_DIAG.txt"
lines = []

def add(s=""):
    lines.append(str(s))

def rel(p):
    try:
        return str(p.relative_to(ROOT)).replace("\\", "/")
    except Exception:
        return str(p)

def read_text(p):
    try:
        return p.read_text(encoding="utf-8-sig", errors="replace")
    except Exception as e:
        return f"<READ ERROR: {e}>"

add("PROJECT P.A. — P4 ItemRegistry diagnostic")
add(f"ROOT={ROOT}")
add()

# 1) Exact continuation failure
add("===== CONTINUATION RESULT =====")
result = ROOT / "Logs/P4-R5/continuation-result.txt"
add(read_text(result) if result.exists() else "<missing>")
add()

# 2) Current ItemRegistry source + meta guid
registry = ROOT / "Assets/Scripts/ItemRegistry.cs"
registry_meta = ROOT / "Assets/Scripts/ItemRegistry.cs.meta"
add("===== CURRENT ItemRegistry.cs =====")
add(read_text(registry) if registry.exists() else "<missing>")
add()
add("===== ItemRegistry.cs.meta =====")
meta_text = read_text(registry_meta) if registry_meta.exists() else "<missing>"
add(meta_text)
guid_match = re.search(r"^guid:\s*([0-9a-fA-F]+)", meta_text, re.M)
registry_guid = guid_match.group(1) if guid_match else None
add(f"ItemRegistry GUID={registry_guid}")
add()

# 3) Locate likely Wood/Ore Item assets and show relevant YAML.
add("===== LIKELY WOOD / ORE ITEM ASSETS =====")
asset_hits = []
for p in (ROOT / "Assets").rglob("*.asset"):
    text = read_text(p)
    low = text.lower()
    # Broad enough to catch m_Name, itemName, references, etc.
    if ("wood" in low or "ore" in low):
        score = 0
        if re.search(r"(?im)^\s*itemName:\s*(Wood|Ore)\s*$", text): score += 10
        if re.search(r"(?im)^\s*m_Name:\s*.*(Wood|Ore).*$", text): score += 5
        if re.search(r"(?im)^\s*id:\s*3\s*$", text): score += 3
        if "producedItem:" in text: score += 2
        asset_hits.append((score, p, text))

if not asset_hits:
    add("<no .asset file containing Wood or Ore>")
else:
    for score, p, text in sorted(asset_hits, key=lambda x: (-x[0], rel(x[1]))):
        add(f"--- {rel(p)} | score={score} | under Resources={'/Resources/' in '/' + rel(p)} ---")
        m = p.with_suffix(p.suffix + ".meta")
        if m.exists():
            mm = re.search(r"^guid:\s*([0-9a-fA-F]+)", read_text(m), re.M)
            if mm:
                add(f"asset_guid={mm.group(1)}")
        for i, line in enumerate(text.splitlines(), 1):
            if any(k in line.lower() for k in ("wood", "ore", "itemname:", "id:", "produceditem:", "m_name:")):
                add(f"{i:04d}: {line}")
        add()

# 4) Inventory of Item assets below Resources, to verify Resources.LoadAll<Item>("Items") scope.
add("===== ASSETS UNDER Resources/Items =====")
res_items = ROOT / "Assets/Resources/Items"
if res_items.exists():
    for p in sorted(res_items.rglob("*.asset")):
        text = read_text(p)
        name = re.search(r"(?im)^\s*itemName:\s*(.+?)\s*$", text)
        iid = re.search(r"(?im)^\s*id:\s*(-?\d+)\s*$", text)
        add(f"{rel(p)} | itemName={name.group(1).strip() if name else '?'} | id={iid.group(1) if iid else '?'}")
else:
    add("<Assets/Resources/Items missing>")
add()

# 5) Find where ItemRegistry component is serialized in scenes/prefabs.
add("===== SERIALIZED ItemRegistry COMPONENT REFERENCES =====")
if registry_guid:
    refs = []
    for ext in ("*.unity", "*.prefab"):
        for p in (ROOT / "Assets").rglob(ext):
            text = read_text(p)
            if registry_guid in text:
                refs.append((p, text))
    if not refs:
        add("<no scene/prefab references ItemRegistry script GUID>")
    else:
        for p, text in refs:
            add(f"--- {rel(p)} ---")
            arr = text.splitlines()
            for i, line in enumerate(arr):
                if registry_guid in line:
                    start = max(0, i - 8)
                    end = min(len(arr), i + 35)
                    for j in range(start, end):
                        add(f"{j+1:05d}: {arr[j]}")
                    add()
else:
    add("<could not determine ItemRegistry GUID>")
add()

# 6) Find code that creates/assigns ItemRegistry or manipulates allItems.
add("===== CODE REFERENCES TO ItemRegistry / allItems =====")
code_hits = []
for p in (ROOT / "Assets").rglob("*.cs"):
    text = read_text(p)
    if ("ItemRegistry" in text or "allItems" in text):
        code_hits.append((p, text))
for p, text in sorted(code_hits, key=lambda x: rel(x[0])):
    add(f"--- {rel(p)} ---")
    for i, line in enumerate(text.splitlines(), 1):
        if ("ItemRegistry" in line or "allItems" in line or "LoadAll<Item>" in line):
            add(f"{i:04d}: {line}")
    add()

# 7) Current continuation source around post-load registry checks.
cont = ROOT / "Assets/Editor/PA_FirstProductionR5Continuation.cs"
add("===== CURRENT R5 CONTINUATION REGISTRY CHECKS =====")
if cont.exists():
    text = read_text(cont)
    for i, line in enumerate(text.splitlines(), 1):
        if ("ItemRegistry" in line or "full inventory restored" in line or "preserved P4 ready state" in line):
            add(f"{i:04d}: {line}")
else:
    add("<missing>")
add()

# 8) Save file inventory slots: prove what was serialized.
save = ROOT / "Logs/P4-R5/Save/departure_settlement.json"
add("===== PRESERVED SAVE QUICK CHECK =====")
if save.exists():
    text = read_text(save)
    add(f"path={rel(save)} bytes={save.stat().st_size}")
    add(f'contains "itemName":"Wood" = {text.count(chr(34)+"itemName"+chr(34)+":"+chr(34)+"Wood"+chr(34))}')
    add(f'contains "itemName":"Ore" = {text.count(chr(34)+"itemName"+chr(34)+":"+chr(34)+"Ore"+chr(34))}')
    # first few Wood contexts
    pos = 0
    shown = 0
    while shown < 3:
        pos = text.find('"itemName":"Wood"', pos)
        if pos < 0:
            break
        add(text[max(0,pos-120): min(len(text), pos+180)])
        pos += 1
        shown += 1
else:
    add("<missing preserved save>")

OUT.write_text("\n".join(lines), encoding="utf-8")
print(f"PASS: wrote {OUT}")
print(f"{OUT.stat().st_size} bytes")
