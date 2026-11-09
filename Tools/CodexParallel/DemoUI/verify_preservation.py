"""Bounded source preservation review; never a gameplay/visual PASS."""
import json
import re
from pathlib import Path
from package_ui import ROOT, RUN, TASKS, paths, sha, timestamp, write_json

def method(text, name, occurrence=0):
    matches = list(re.finditer(r"^    (?:public |private |static |internal |protected )*(?:[\w<>\[\]]+) " +
                              re.escape(name) + r"\([^\n]*\)\s*\{", text, re.M))
    start = matches[occurrence].end() - 1
    depth = 0
    for i in range(start, len(text)):
        # The reviewed methods have balanced interpolation braces and no unmatched brace literals.
        depth += (text[i] == "{") - (text[i] == "}")
        if depth == 0:
            return text[start:i + 1]
    raise ValueError("Unterminated method: " + name)

def public_surface(text):
    # Existing public/serialized fields, method signatures and properties, excluding expression bodies.
    declarations = []
    for line in text.splitlines():
        stripped = line.strip()
        if not stripped.startswith("public "):
            continue
        declarations.append(stripped.split("=>")[0].split("{")[0].strip())
    return declarations

UNCHANGED = {
    "P6CraftingUI": ["Awake", "Start", "OnDestroy", "ToggleUI", "Close", "CloseForOtherPanel",
                     "EnsureOpenState", "CloseConflictingPanels", "CaptureCursor", "RestoreCursor",
                     "PassesWorkbenchFilter", "IsUnlocked", "HasAllIngredients", "ContextMatches"],
    "P6StorageUI": ["Awake", "Start", "Update", "OnDestroy", "CloseBox", "CloseForOtherPanel",
                    "CloseConflictingPanels", "CaptureCursor", "RestoreCursor", "GetSelectedHotbarSlot"],
    "P7CompanionSelection": ["Register", "SceneLoaded", "Start", "Update", "OnDestroy", "OpenAfterCertification",
                             "OpenSelection", "Toggle", "HideSelection", "RestoreConfirmedSelection", "Panel", "Label"],
    "P8ShopPriceUI": ["Awake", "Open", "Close", "AdjustPrice", "OnConfirm", "OnRetrieve", "SetCanvasActive"],
}

def remove_status(body):
    return re.sub(r"^\s*SetStatus\([^\n]*\);\n?", "", body, flags=re.M)

report = {"time": timestamp(), "scope": "Static preservation and baseline review only; no game execution", "tasks": {}}
for task in TASKS:
    ticket, live, stage, log, patch = paths(task)
    old = (stage / "Baseline" / Path(live).name).read_text(encoding="utf-8-sig")
    new = (stage / "Source" / Path(live).name).read_text(encoding="utf-8-sig")
    checks = []
    def check(name, condition):
        checks.append({"check": name, "pass": bool(condition)})
    check("Public field/API declarations preserved", public_surface(old) == public_surface(new))
    for name in UNCHANGED[task]:
        check(name + " body unchanged", method(old, name) == method(new, name))
    if task in ("P6CraftingUI", "P6StorageUI"):
        overloaded = "Close" if task == "P6CraftingUI" else "CloseBox"
        check(overloaded + "(bool) unchanged", method(old, overloaded, 1) == method(new, overloaded, 1))
    if task == "P6StorageUI":
        for name, old_name, new_name in [
            ("OnClickTakeItem", "string itemName = inst.data.itemName;", "string itemName = ItemDisplayName.For(inst.data);"),
            ("OnClickStoreItem", "string itemName = heldInst.data.itemName;", "string itemName = ItemDisplayName.For(heldInst.data);"),
        ]:
            baseline_body = remove_status(method(old, name))
            staged_body = remove_status(method(new, name)).replace(new_name, old_name)
            check(name + " transaction exact after removing display-only changes", baseline_body == staged_body)
    if task == "P7CompanionSelection":
        baseline_confirm = re.sub(r"^\s*_count.text =[^\n]*\n", "", method(old, "Confirm"), flags=re.M)
        staged_confirm = re.sub(r"^\s*_count.text =[^\n]*\n", "", method(new, "Confirm"), flags=re.M)
        staged_confirm = re.sub(r"^\s*if \(_departureLabel != null\)[^\n]*\n", "", staged_confirm, flags=re.M)
        check("Confirm state/event exact after display-only changes", baseline_confirm == staged_confirm)
    if task == "P8ShopPriceUI":
        before = method(old, "ConfigureTutorialPriceDrag")
        after = method(new, "ConfigureTutorialPriceDrag")
        for line in ["rect.sizeDelta", "rect.anchoredPosition"]:
            pattern = r"^\s*" + re.escape(line) + r"[^\n]*\n"
            before = re.sub(pattern, "", before, flags=re.M)
            after = re.sub(pattern, "", after, flags=re.M)
        check("Slider range/listener/certification exact after layout-only changes", before == after)
    manifest = json.loads((log / "baseline.json").read_text(encoding="utf-8"))
    drift = [f["path"] for f in manifest["files"] if sha(ROOT / f["path"]) != f["sha256"]]
    check("Live source and related API/data SHA256 unchanged", not drift)
    verification = json.loads((log / "verification.json").read_text(encoding="utf-8"))
    check("Current source matches compiled/applied source", sha(stage / "Source" / Path(live).name) == verification["sourceSha256"])
    check("Patch matches verified patch", sha(patch) == verification["patchSha256"])
    passed = all(c["pass"] for c in checks)
    report["tasks"][ticket] = {"pass": passed, "checks": checks, "drift": drift}
    print(ticket, "preservation PASS" if passed else "preservation FAIL", f"({len(checks)} checks)")
write_json(ROOT / "Logs/CodexParallel/DemoUI" / RUN / "preservation.json", report)
if not all(t["pass"] for t in report["tasks"].values()):
    raise SystemExit("Preservation failure: inspect dedicated report before continuing")
