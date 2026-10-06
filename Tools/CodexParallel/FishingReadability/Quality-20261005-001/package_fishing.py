"""Isolated source/patch/compile checks. Never writes Assets or controls Unity."""
import sys
sys.dont_write_bytecode = True
import argparse
import difflib
import importlib.util
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
RUN = "Quality-20261005-001"
STAGE = ROOT / "Automation/CodexParallel/FishingReadability" / RUN
LOG = ROOT / "Logs/CodexParallel/FishingReadability" / RUN
LIVE = "Assets/Scripts/FishingSpot.cs"
HELPER = "Assets/Scripts/Presentation/FirstDayFishingFloat.cs"
PATCH = STAGE / "fishing-readability.patch"
spec = importlib.util.spec_from_file_location("existing_packager", ROOT / "Tools/CodexParallel/DemoUI/package_ui.py")
pack = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pack)
pack.paths = lambda task: ("Q5_FISHING_READABILITY", LIVE, STAGE, LOG, PATCH)

LEGACY = """        var assets = FirstDayStudioAssets.Load();
        if (_bobber == null && assets != null && assets.fruit != null)
        {
            // 찌 모델 자산이 없어 작은 붉은 열매 모델을 찌로 쓴다(표시 전용).
            _bobber = FirstDayStudioAssets.Place(assets.fruit, null, _waterTarget, .16f);
            _bobber.name = "FishingBobber";
            foreach (var collider in _bobber.GetComponentsInChildren<Collider>()) collider.enabled = false;
        }
"""
REPLACEMENT = """        // Display-only float; existing fishing phases, timings and rewards remain authoritative.
        if (_bobber == null) _bobber = FirstDayFishingFloat.Create(this);
"""


def snapshot():
    if (ROOT / HELPER).exists():
        raise RuntimeError("A live float helper already exists; inspect it instead of creating a duplicate")
    dependencies = ["Assets/Scripts/EquipmentSystem.cs", "Assets/Scripts/Presentation/FirstDayStudioAssets.cs",
                    "Assets/Scripts/DayNightShopLoopController.cs", "Assets/Scripts/World/WorldGridService.cs",
                    "Automation/LoopEngineering/State/loop-state.json", "Docs/00_CURRENT/CODEX_HANDOFF.md",
                    "Assets/Scripts/CraftingUI.cs", "Assets/Scripts/StorageUI.cs",
                    "Assets/Scripts/Presentation/DepartureCompanionSelection.cs", "Assets/Scripts/UI/ShopPriceUI.cs"]
    for name in [LIVE, *dependencies]:
        if not (ROOT / name).is_file():
            raise RuntimeError("Missing baseline dependency: " + name)
    pack.snapshot("fishing", dependencies)
    source = STAGE / "Source/FishingSpot.cs"
    raw = source.read_bytes()
    terminator = "\r\n" if b"\r\n" in raw else "\n"
    old = LEGACY.replace("\n", terminator).encode("utf-8")
    new = REPLACEMENT.replace("\n", terminator).encode("utf-8")
    if raw.count(old) != 1:
        raise RuntimeError("Expected fruit stand-in block no longer matches; reconcile current source")
    source.write_bytes(raw.replace(old, new, 1))
    print("Q5: saved current baseline and changed only float construction")


def verify():
    baseline = json.loads((LOG / "baseline.json").read_text(encoding="utf-8"))
    def drift():
        return [entry["path"] for entry in baseline["files"] if pack.sha(ROOT / entry["path"]) != entry["sha256"]]
    if drift() or (ROOT / HELPER).exists():
        raise RuntimeError("Live baseline drift or a concurrent float helper; reconcile first")
    before = (STAGE / "Baseline/FishingSpot.cs").read_bytes()
    after = (STAGE / "Source/FishingSpot.cs").read_bytes()
    terminator = "\r\n" if b"\r\n" in before else "\n"
    expected = before.replace(LEGACY.replace("\n", terminator).encode("utf-8"),
                              REPLACEMENT.replace("\n", terminator).encode("utf-8"), 1)
    if after != expected:
        raise RuntimeError("Changes extend beyond the approved float construction block")
    pack.write_json(LOG / "authority-preservation.json", {
        "status": "PASS", "scope": "Every existing source byte outside the float construction block is unchanged.",
        "preserved": ["phase transitions", "bite window/random wait", "cast/bite/reward audio", "shore search",
                      "fish shadow", "reach/tool checks", "cancel/restore", "full bag retry", "durability",
                      "Inventory-first reward", "public P9 motion APIs", "non-demo fishing path"],
        "runtimeMeaning": "Source preservation only; not gameplay execution."
    })
    helper = STAGE / "Source/FirstDayFishingFloat.cs"
    segments = []
    for old, new, destination in [(before, after, LIVE), (b"", helper.read_bytes(), HELPER)]:
        segments.append("".join(difflib.unified_diff(
            old.decode("utf-8-sig").splitlines(True), new.decode("utf-8-sig").splitlines(True),
            fromfile="a/" + destination if old else "/dev/null", tofile="b/" + destination)))
    PATCH.write_bytes("".join(segments).encode("utf-8"))
    check_root = STAGE / "PatchCheck"
    if check_root.exists():
        raise RuntimeError("Previous verification exists; do not overwrite or repeat completed checks")
    target = check_root / LIVE
    target.parent.mkdir(parents=True)
    target.write_bytes(before)
    command = ["git", "-c", "core.autocrlf=false", "apply", "--no-index", "--whitespace=nowarn",
               "--directory=" + check_root.relative_to(ROOT).as_posix()]
    for args in [["--check"], []]:
        result = subprocess.run([*command, *args, str(PATCH)], cwd=ROOT, capture_output=True, text=True)
        if result.returncode:
            raise RuntimeError("Isolated patch failed: " + result.stderr)
    if target.read_bytes() != after or (check_root / HELPER).read_bytes() != helper.read_bytes():
        raise RuntimeError("Patch did not reproduce both source files exactly")

    # Reuse installed references discovered by the existing Q4 compiler check. No restore or Unity process.
    old_refs = json.loads((ROOT / "Logs/CodexParallel/P8ShopPriceUI" / RUN / "compile-references.json").read_text(encoding="utf-8"))
    references = [{"path": r["path"], "sha256": pack.sha(Path(r["path"]))} for r in old_refs]
    pack.write_json(LOG / "compile-references.json", references)
    compile_dir = STAGE / "Compile"
    compile_dir.mkdir()
    compiler = Path("C:/Program Files/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll")
    response = compile_dir / "compile.rsp"
    response.write_text("\n".join([
        "/nologo", "/target:library", "/langversion:9", "/nostdlib+", "/nowarn:0436", "/define:UNITY_EDITOR",
        '/out:"' + str(compile_dir / "FishingReadability.dll") + '"',
        *['/reference:"' + r["path"] + '"' for r in references],
        '"' + str(STAGE / "Source/FishingSpot.cs") + '"', '"' + str(helper) + '"',
    ]) + "\n", encoding="utf-8")
    result = subprocess.run(["dotnet", str(compiler), "@" + str(response)], cwd=compile_dir,
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    (LOG / "compile.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    cache_drift = [r["path"] for r in references if pack.sha(Path(r["path"])) != r["sha256"]]
    changed = drift()
    passed = result.returncode == 0 and not changed and not cache_drift
    pack.write_json(LOG / "verification.json", {
        "time": pack.timestamp(), "isolatedPatchBytes": "PASS", "authorityPreservation": "PASS",
        "partialCompile": "PASS" if passed else "FAIL", "compilerExitCode": result.returncode,
        "define": "UNITY_EDITOR", "references": len(references), "liveDrift": changed, "cacheDrift": cache_drift,
        "files": [{"path": p.relative_to(ROOT).as_posix(), "sha256": pack.sha(p)} for p in
                  [STAGE / "Baseline/FishingSpot.cs", STAGE / "Source/FishingSpot.cs", helper, PATCH]],
        "liveApply": "NOT RUN", "unityCompile": "NOT RUN", "play": "NOT RUN", "gameView": "UNVERIFIED",
        "steamReadiness": "UNVERIFIED"
    })
    if not passed:
        raise RuntimeError("Partial compile failed or concurrent drift; see dedicated logs")
    print("Q5: both patch files match bytes; source preservation and UNITY_EDITOR partial compile PASS")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["snapshot", "verify"])
    args = parser.parse_args()
    snapshot() if args.action == "snapshot" else verify()
