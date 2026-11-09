"""Reuse the existing isolated packager for fresh, post-Claude UI baselines."""
import sys
sys.dont_write_bytecode = True
import argparse
import importlib.util
import json
import subprocess
from types import SimpleNamespace
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RUN = "Quality-20261005-001"
spec = importlib.util.spec_from_file_location(
    "existing_ui_packager", ROOT / "Tools/CodexParallel/DemoUI/package_ui.py")
pack = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pack)

TASKS = {
    "craft": ("Q1", "P6CraftingUI", "Assets/Scripts/CraftingUI.cs", "crafting-quality.patch"),
    "storage": ("Q2", "P6StorageUI", "Assets/Scripts/StorageUI.cs", "storage-quality.patch"),
    "companion": ("Q3", "P7CompanionSelection", "Assets/Scripts/Presentation/DepartureCompanionSelection.cs", "companion-quality.patch"),
    "price": ("Q4", "P8ShopPriceUI", "Assets/Scripts/UI/ShopPriceUI.cs", "price-quality.patch"),
}
pack.TASKS = {key: (ticket, live, patch)
              for key, (ticket, folder, live, patch) in TASKS.items()}


def paths(task):
    ticket, folder, live, patch = TASKS[task]
    stage = ROOT / "Automation/CodexParallel" / folder / RUN
    log = ROOT / "Logs/CodexParallel" / folder / RUN
    return ticket, live, stage, log, stage / patch


pack.paths = paths


def scoped_run(command, **kwargs):
    # A copied LF source must remain LF for an exact-byte isolated patch comparison.
    # Pin this invocation only; do not change repository or user Git configuration.
    if command[:2] == ["git", "apply"]:
        command = ["git", "-c", "core.autocrlf=false", *command[1:]]
    return subprocess.run(command, **kwargs)


pack.subprocess = SimpleNamespace(run=scoped_run, check_output=subprocess.check_output)


def verify(task, repair_lf=False):
    if repair_lf:
        ticket, live, stage, log, patch = paths(task)
        retry_stage = stage / "LFValidation"
        if retry_stage.exists():
            raise RuntimeError("LF repair already attempted; stop rather than repeat")
        for directory in ("Baseline", "Source"):
            copied = retry_stage / directory / Path(live).name
            copied.parent.mkdir(parents=True, exist_ok=True)
            copied.write_bytes((stage / directory / Path(live).name).read_bytes())
        pack.write_json(log / "mechanical-recovery.json", {
            "cause": "Git core.autocrlf converted isolated LF file to CRLF; normalized text matched exactly.",
            "correction": "Pin core.autocrlf=false for isolated invocation; retain failed PatchCheck; one fresh LFValidation directory.",
            "gameCodeChangedForCorrection": False,
        })
        pack.paths = lambda requested: (ticket, live, retry_stage, log, patch) if requested == task else paths(requested)
    pack.verify(task)


def bundle():
    """Compile all four candidates together, including their Unity Editor branches."""
    stage = ROOT / "Automation/CodexParallel/DemoUIQuality" / RUN / "Compile"
    log = ROOT / "Logs/CodexParallel/DemoUI" / RUN
    stage.mkdir(parents=True, exist_ok=True)
    log.mkdir(parents=True, exist_ok=True)
    sources, tracked = [], {}
    for task in TASKS:
        ticket, live, task_stage, task_log, patch = paths(task)
        baseline = json.loads((task_log / "baseline.json").read_text(encoding="utf-8"))
        verification = json.loads((task_log / "verification.json").read_text(encoding="utf-8"))
        preservation = json.loads((task_log / "authority-preservation.json").read_text(encoding="utf-8"))
        source = task_stage / "Source" / Path(live).name
        if (verification["partialCompile"] != "PASS" or preservation["status"] != "PASS"
                or pack.sha(source) != verification["sourceSha256"]
                or pack.sha(patch) != verification["patchSha256"]):
            raise RuntimeError("Candidate changed or checks incomplete: " + task)
        sources.append(source)
        for entry in baseline["files"]:
            if entry["path"] in tracked and tracked[entry["path"]] != entry["sha256"]:
                raise RuntimeError("Two candidate baselines disagree: " + entry["path"])
            tracked[entry["path"]] = entry["sha256"]
    ref_log = paths("price")[3] / "compile-references.json"
    references = json.loads(ref_log.read_text(encoding="utf-8"))

    def drift():
        return {
            "live": [p for p, digest in tracked.items() if pack.sha(ROOT / p) != digest],
            "cache": [r["path"] for r in references if pack.sha(Path(r["path"])) != r["sha256"]],
        }

    before = drift()
    if before["live"] or before["cache"]:
        raise RuntimeError("Concurrent baseline/cache drift: " + str(before))
    compiler = Path("C:/Program Files/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll")
    rsp = stage / "compile.rsp"
    rsp.write_text("\n".join([
        "/nologo", "/target:library", "/langversion:9", "/nostdlib+", "/nowarn:0436",
        "/define:UNITY_EDITOR", '/out:"' + str(stage / "DemoUIQuality.dll") + '"',
        *['/reference:"' + r["path"] + '"' for r in references],
        *['"' + str(source) + '"' for source in sources],
    ]) + "\n", encoding="utf-8")
    result = subprocess.run(["dotnet", str(compiler), "@" + str(rsp)], cwd=stage,
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    (log / "bundle-compile.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    after = drift()
    good = result.returncode == 0 and not after["live"] and not after["cache"]
    pack.write_json(log / "bundle-verification.json", {
        "time": pack.timestamp(), "partialBundleCompile": "PASS" if good else "FAIL",
        "compilerExitCode": result.returncode, "define": "UNITY_EDITOR", "references": len(references),
        "sources": [{"path": str(p.relative_to(ROOT)), "sha256": pack.sha(p)} for p in sources],
        "liveFilesCompared": len(tracked), "liveDrift": after["live"], "cacheDrift": after["cache"],
        "liveApply": "NOT RUN", "unityCompile": "NOT RUN", "play": "NOT RUN",
        "gameView": "UNVERIFIED", "human": "UNVERIFIED",
    })
    if not good:
        raise RuntimeError("Bundle compile failed or baseline/cache drift; see dedicated logs")
    print(f"Bundle: four UI classes + UNITY_EDITOR partial compile PASS; {len(tracked)} live/API files unchanged")


def snapshot(task, dependencies):
    # Validate paths before the reused helper writes either source copy.
    ticket, live, stage, log, patch = paths(task)
    tracked = [live] + dependencies
    for name in tracked:
        if not (ROOT / name).is_file():
            raise RuntimeError("Missing baseline dependency: " + name)
    baseline = stage / "Baseline" / Path(live).name
    source = stage / "Source" / Path(live).name
    if baseline.exists() or source.exists():
        # Recover only an interrupted snapshot whose two copies are still exact live bytes.
        if (log / "baseline.json").exists() or not baseline.exists() or not source.exists():
            raise RuntimeError("Existing work: resume without overwriting")
        if baseline.read_bytes() != source.read_bytes() or baseline.read_bytes() != (ROOT / live).read_bytes():
            raise RuntimeError("Interrupted snapshot has diverged; stop")
        pack.write_json(log / "baseline.json", {
            "ticket": ticket, "snapshotTime": pack.timestamp(), "source": live,
            "branch": subprocess.check_output(["git", "branch", "--show-current"], cwd=ROOT, text=True).strip(),
            "head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
            "relatedDiff": subprocess.check_output(["git", "diff", "--", *tracked], cwd=ROOT, text=True, encoding="utf-8"),
            "files": [{"path": p, "sha256": pack.sha(ROOT / p)} for p in tracked],
            "snapshotRecovery": "Initial dependency path typo; source and baseline matched unchanged live bytes. Corrected once.",
        })
        print(f"{ticket}: interrupted snapshot recovered without source overwrite")
    else:
        pack.snapshot(task, dependencies)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["snapshot", "verify", "bundle"])
    parser.add_argument("task", choices=TASKS, nargs="?")
    parser.add_argument("dependencies", nargs="*")
    parser.add_argument("--repair-lf", action="store_true", help="One isolated LF/CRLF mechanical retry; retains previous output")
    args = parser.parse_args()
    if args.action == "bundle":
        bundle()
    elif not args.task:
        parser.error("snapshot/verify require a task")
    elif args.action == "snapshot":
        snapshot(args.task, args.dependencies)
    else:
        verify(args.task, args.repair_lf)
