"""Apply one already verified independent candidate, preserving concurrent work."""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[4]
RUN = "Apply-20261005-001"
LOG = ROOT / "Logs/CodexParallel/DemoUIApply" / RUN
SNAPSHOT = ROOT / "Automation/CodexParallel/DemoUIApply" / RUN
TASKS = {
    "Q1": ("P6CraftingUI", "crafting-quality.patch", [("Assets/Scripts/CraftingUI.cs", "CraftingUI.cs")]),
    "Q2": ("P6StorageUI", "storage-quality.patch", [("Assets/Scripts/StorageUI.cs", "StorageUI.cs")]),
    "Q3": ("P7CompanionSelection", "companion-quality.patch", [("Assets/Scripts/Presentation/DepartureCompanionSelection.cs", "DepartureCompanionSelection.cs")]),
    "Q4": ("P8ShopPriceUI", "price-quality.patch", [("Assets/Scripts/UI/ShopPriceUI.cs", "ShopPriceUI.cs")]),
    "Q5": ("FishingReadability", "fishing-readability.patch", [("Assets/Scripts/FishingSpot.cs", "FishingSpot.cs"),
        ("Assets/Scripts/Presentation/FirstDayFishingFloat.cs", "FirstDayFishingFloat.cs")]),
    "Q6": ("InventoryDragIntegrity", "drag-durability.patch", [("Assets/Scripts/InventorySlotUI.cs", "InventorySlotUI.cs")]),
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def apply(ticket):
    folder, patch_name, files = TASKS[ticket]
    staged = ROOT / "Automation/CodexParallel" / folder / "Quality-20261005-001"
    evidence = ROOT / "Logs/CodexParallel" / folder / "Quality-20261005-001"
    baseline = json.loads((evidence / "baseline.json").read_text(encoding="utf-8"))
    validation = json.loads((evidence / "verification.json").read_text(encoding="utf-8"))
    authority = json.loads((evidence / "authority-preservation.json").read_text(encoding="utf-8"))
    if validation["partialCompile"] != "PASS" or authority["status"] != "PASS":
        raise RuntimeError("Candidate is not verified")
    witness = json.loads((LOG / (ticket + "-editor-before.json")).read_text(encoding="utf-8"))
    state = witness["data"]
    if time.time() * 1000 - state["observed_at_unix_ms"] > 20000:
        raise RuntimeError("Editor witness is stale; read the current instance again")
    if (state["editor"]["play_mode"]["is_playing"] or state["editor"]["play_mode"]["is_changing"]
            or state["compilation"]["is_compiling"] or state["assets"]["is_updating"] or state["tests"]["is_running"]):
        raise RuntimeError("Shared Editor is busy; do not interrupt Claude")
    if state["unity"]["instance_id"] != "Project_PA@9d3a8714c57d250f":
        raise RuntimeError("Unexpected editor instance")
    record = LOG / (ticket + "-apply.json")
    intent = LOG / (ticket + "-intent.json")
    if record.exists() or intent.exists():
        raise RuntimeError("An application record exists; inspect it instead of applying twice")
    baseline_hashes = {f["path"]: f["sha256"] for f in baseline["files"]}
    patch = staged / patch_name
    expected_patch = validation.get("patchSha256")
    if expected_patch is None:
        expected_patch = next(f["sha256"] for f in validation["files"] if f["path"].endswith("/" + patch_name))
    if sha(patch) != expected_patch:
        raise RuntimeError("Verified patch changed")
    changed = []
    for destination, name in files:
        source = staged / "Source" / name
        live = ROOT / destination
        if destination in baseline_hashes:
            if not live.is_file() or sha(live) != baseline_hashes[destination]:
                raise RuntimeError("Concurrent baseline drift: " + destination)
            saved = SNAPSHOT / "Before" / ticket / name
            saved.parent.mkdir(parents=True, exist_ok=True)
            if saved.exists():
                raise RuntimeError("Do not overwrite an earlier snapshot")
            saved.write_bytes(live.read_bytes())
        elif live.exists() or live.with_suffix(live.suffix + ".meta").exists():
            raise RuntimeError("Concurrent new-file collision: " + destination)
        expected_source = validation.get("sourceSha256") if len(files) == 1 else None
        if expected_source is None:
            expected_source = next(f["sha256"] for f in validation["files"] if f["path"].endswith("/Source/" + name))
        if sha(source) != expected_source:
            raise RuntimeError("Verified source changed: " + name)
        changed.append({"path": destination, "beforeSha256": baseline_hashes.get(destination), "expectedSha256": sha(source)})
    patch_paths = [line[6:].strip() for line in patch.read_text(encoding="utf-8").splitlines() if line.startswith("+++ b/")]
    if patch_paths != [p for p, _ in files]:
        raise RuntimeError("Patch changes paths beyond the declared scope")
    protected = ["Automation/LoopEngineering/State/loop-state.json", "Docs/00_CURRENT/CODEX_HANDOFF.md",
                 "Assets/Scripts/Presentation/FirstDayWorldPresentation.cs", "Assets/Scripts/World/WorldChunkTerrain.cs",
                 "Assets/Scripts/World/WorldIslandGenerator.cs", "Assets/Scripts/World/WorldNavigationService.cs"]
    protected_hashes = {p: sha(ROOT / p) for p in protected}
    index_path = Path(subprocess.check_output(["git", "rev-parse", "--git-path", "index"], cwd=ROOT, text=True).strip())
    if not index_path.is_absolute(): index_path = ROOT / index_path
    index_hash = sha(index_path)
    command = ["git", "-c", "core.autocrlf=false", "apply", "--whitespace=nowarn"]
    checked = subprocess.run([*command, "--check", str(patch)], cwd=ROOT, capture_output=True, text=True)
    if checked.returncode:
        raise RuntimeError("Current patch check failed: " + checked.stderr)
    write(intent, {"ticket": ticket, "time": datetime.datetime.now().astimezone().isoformat(),
                   "authorization": "Latest user goal explicitly requests actual application outside Claude's current work.",
                   "editorControl": "No Play/Stop/scene or routing changes; independent source application only.",
                   "files": changed, "protected": protected_hashes, "indexSha256": index_hash})
    applied = subprocess.run([*command, str(patch)], cwd=ROOT, capture_output=True, text=True)
    for item in changed:
        item["afterSha256"] = sha(ROOT / item["path"])
        item["matchesVerifiedCandidate"] = item["afterSha256"] == item["expectedSha256"]
    drift = [p for p, digest in protected_hashes.items() if sha(ROOT / p) != digest]
    passed = applied.returncode == 0 and all(f["matchesVerifiedCandidate"] for f in changed) and not drift and sha(index_path) == index_hash
    write(record, {"ticket": ticket, "time": datetime.datetime.now().astimezone().isoformat(),
                   "status": "APPLIED_WAITING_UNITY_COMPILE_PLAY" if passed else "STOP_RECONCILE",
                   "exitCode": applied.returncode, "output": applied.stdout + applied.stderr,
                   "files": changed, "protectedDrift": drift, "indexUnchanged": sha(index_path) == index_hash,
                   "unityCompile": "PENDING", "play": "NOT RUN", "gameView": "UNVERIFIED"})
    if not passed:
        raise RuntimeError("Apply result or concurrent preservation requires reconciliation; no automatic rollback")
    print(ticket + ": live source matches the verified candidate; Claude world/routing/Handoff/index preserved")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("ticket", choices=TASKS)
    apply(parser.parse_args().ticket)
