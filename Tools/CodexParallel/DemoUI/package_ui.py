"""Staged UI packaging only. Never applies a patch to live Assets or runs Unity."""
import argparse
import datetime
import difflib
import hashlib
import json
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
RUN = "CodeReady-20261005-001"
TASKS = {
    "P6CraftingUI": ("C1", "Assets/Scripts/CraftingUI.cs", "crafting-ui.patch"),
    "P6StorageUI": ("C2", "Assets/Scripts/StorageUI.cs", "storage-ui.patch"),
    "P7CompanionSelection": ("C3", "Assets/Scripts/Presentation/DepartureCompanionSelection.cs", "companion-selection.patch"),
    "P8ShopPriceUI": ("C4", "Assets/Scripts/UI/ShopPriceUI.cs", "shop-price-ui.patch"),
}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def timestamp():
    return datetime.datetime.now(datetime.timezone(datetime.timedelta(hours=9))).isoformat()

def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

def paths(task):
    ticket, live, patch = TASKS[task]
    stage = ROOT / "Automation/CodexParallel" / task
    log = ROOT / "Logs/CodexParallel" / task / RUN
    return ticket, live, stage, log, stage / patch

def snapshot(task, dependencies):
    ticket, live, stage, log, patch = paths(task)
    baseline = stage / "Baseline" / Path(live).name
    source = stage / "Source" / Path(live).name
    if baseline.exists() or source.exists():
        raise RuntimeError("Existing staged work: resume it; do not overwrite")
    for path in (baseline, source):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes((ROOT / live).read_bytes())
    tracked = [live] + dependencies
    write_json(log / "baseline.json", {
        "ticket": ticket, "snapshotTime": timestamp(), "source": live,
        "branch": subprocess.check_output(["git", "branch", "--show-current"], cwd=ROOT, text=True).strip(),
        "head": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
        "relatedDiff": subprocess.check_output(["git", "diff", "--", *tracked], cwd=ROOT, text=True, encoding="utf-8"),
        "files": [{"path": p, "sha256": sha(ROOT / p)} for p in tracked],
    })
    print(f"{ticket}: baseline/source saved; SHA256 {sha(baseline)}")

def verify(task):
    ticket, live, stage, log, patch = paths(task)
    baseline = stage / "Baseline" / Path(live).name
    source = stage / "Source" / Path(live).name
    manifest = json.loads((log / "baseline.json").read_text(encoding="utf-8"))
    drift = [f["path"] for f in manifest["files"] if sha(ROOT / f["path"]) != f["sha256"]]
    if drift:
        raise RuntimeError(f"Live baseline/API drift; stop and reconcile: {drift}")
    old = baseline.read_bytes().decode("utf-8-sig")
    new = source.read_bytes().decode("utf-8-sig")
    # Preserve the original line terminators in context/addition lines for git apply.
    diff = "".join(difflib.unified_diff(old.splitlines(True), new.splitlines(True),
                                       fromfile="a/" + live, tofile="b/" + live))
    patch.write_bytes(diff.encode("utf-8"))
    check_root = stage / "PatchCheck"
    target = check_root / live
    if target.exists():
        raise RuntimeError("PatchCheck already exists; use a new verification directory on retry")
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(baseline.read_bytes())
    prefix = check_root.relative_to(ROOT).as_posix()
    command = ["git", "apply", "--no-index", "--whitespace=nowarn", "--directory=" + prefix]
    dry = subprocess.run(command + ["--check", str(patch)], cwd=ROOT, capture_output=True, text=True)
    if dry.returncode:
        raise RuntimeError("Isolated patch dry-run failed: " + dry.stderr)
    applied = subprocess.run(command + [str(patch)], cwd=ROOT, capture_output=True, text=True)
    if applied.returncode or target.read_bytes() != source.read_bytes():
        raise RuntimeError("Isolated patch application did not reproduce staged source")

    # Direct Roslyn invocation: no MSBuild/restore, installs, shared output, or fake stubs.
    refs = []
    for node in ET.parse(ROOT / "Assembly-CSharp.csproj").iter("HintPath"):
        p = Path(node.text.replace("\\", "/"))
        p = p if p.is_absolute() else ROOT / p
        if not p.exists():
            raise RuntimeError("Missing local compiler reference: " + str(p))
        refs.append(p.resolve())
    refs.append((ROOT / "Library/ScriptAssemblies/Assembly-CSharp.dll").resolve())
    refs = list(dict.fromkeys(refs))
    compiler = Path("C:/Program Files/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll")
    if not compiler.exists():
        write_json(log / "verification.json", {"patch": "PASS", "partialCompile": "UNVERIFIED", "reason": "Installed Roslyn not found"})
        return
    build = stage / "Compile"
    build.mkdir(parents=True, exist_ok=True)
    reference_manifest = [{"path": str(p), "sha256": sha(p), "mtime": p.stat().st_mtime} for p in refs]
    write_json(log / "compile-references.json", reference_manifest)
    rsp = build / "compile.rsp"
    # CS0436 is expected when compiling an improved existing class against cached game APIs.
    rsp.write_text("\n".join([
        "/nologo", "/target:library", "/langversion:9", "/nostdlib+", "/nowarn:0436",
        '/out:"' + str(build / (task + ".dll")) + '"',
        *['/reference:"' + str(p) + '"' for p in refs], '"' + str(source) + '"',
    ]) + "\n", encoding="utf-8")
    result = subprocess.run(["dotnet", str(compiler), "@" + str(rsp)], cwd=build,
                            capture_output=True, text=True, encoding="utf-8", errors="replace")
    (log / "compile.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    drift = [f["path"] for f in manifest["files"] if sha(ROOT / f["path"]) != f["sha256"]]
    reference_drift = [f["path"] for f in reference_manifest if sha(Path(f["path"])) != f["sha256"]]
    write_json(log / "verification.json", {
        "time": timestamp(), "patchDryRun": "PASS", "isolatedPatchBytes": "PASS",
        "baselineSha256": sha(baseline), "sourceSha256": sha(source), "patchSha256": sha(patch),
        "partialCompile": "PASS" if result.returncode == 0 else "FAIL", "compilerExitCode": result.returncode,
        "compiler": str(compiler), "references": len(refs), "liveDrift": drift, "cacheDrift": reference_drift,
        "liveApply": "NOT RUN", "unityCompile": "NOT RUN", "play": "NOT RUN", "gameView": "UNVERIFIED", "human": "UNVERIFIED",
    })
    if result.returncode or drift or reference_drift:
        raise RuntimeError("Compile failed or concurrent baseline/cache drift; see dedicated logs")
    print(f"{ticket}: isolated patch reproduced bytes; partial compile PASS ({len(refs)} local references)")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("action", choices=["snapshot", "verify"])
    parser.add_argument("task", choices=TASKS)
    parser.add_argument("dependencies", nargs="*")
    args = parser.parse_args()
    if args.action == "snapshot":
        snapshot(args.task, args.dependencies)
    else:
        verify(args.task)
