"""Build a local read-only metadata probe and record actual Unity source identity."""
import argparse
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
RUN = "Apply-20261005-001"
TOOL = Path(__file__).resolve().parent
BUILD = ROOT / "Automation/CodexParallel/DemoUIApply" / RUN / "PdbProbe"
LOG = ROOT / "Logs/CodexParallel/DemoUIApply" / RUN


def build():
    output = BUILD / "ReadPdbChecksums.dll"
    if output.exists():
        raise RuntimeError("Probe already built; use inspect without rebuilding")
    BUILD.mkdir(parents=True, exist_ok=True)
    LOG.mkdir(parents=True, exist_ok=True)
    refs = sorted(Path("C:/Program Files/dotnet/packs/Microsoft.NETCore.App.Ref/10.0.12/ref/net10.0").glob("*.dll"))
    if not refs:
        raise RuntimeError("Installed SDK reference set unavailable; no restore/download")
    response = BUILD / "compile.rsp"
    response.write_text("\n".join([
        "/nologo", "/target:exe", "/langversion:latest", "/nostdlib+", '/out:"' + str(output) + '"',
        *['/reference:"' + str(p) + '"' for p in refs], '"' + str(TOOL / "ReadPdbChecksums.cs") + '"',
    ]) + "\n", encoding="utf-8")
    result = subprocess.run(["dotnet", "C:/Program Files/dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll", "@" + str(response)],
                            cwd=BUILD, capture_output=True, text=True, encoding="utf-8")
    (LOG / "pdb-probe-compile.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    if result.returncode:
        raise RuntimeError("Probe compile failed; see dedicated log")
    output.with_suffix(".runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
        "tfm": "net10.0", "framework": {"name": "Microsoft.NETCore.App", "version": "10.0.12"}}}) + "\n", encoding="utf-8")
    print("Read-only PDB probe built from installed SDK; no package restore or Unity action")


def inspect(record, sources):
    destination = LOG / (record + "-pdb-identity.json")
    if destination.exists():
        raise RuntimeError("Identity observation already exists; do not overwrite")
    result = subprocess.run(["dotnet", str(BUILD / "ReadPdbChecksums.dll"), str(ROOT),
        str(ROOT / "Library/ScriptAssemblies/Assembly-CSharp.dll"),
        str(ROOT / "Library/ScriptAssemblies/Assembly-CSharp.pdb"), *sources],
        cwd=BUILD, capture_output=True, text=True, encoding="utf-8")
    destination.write_text(result.stdout, encoding="utf-8")
    if result.returncode:
        raise RuntimeError("Compiled source identity not established; inspect the observation and current compile state")
    value = json.loads(result.stdout)
    print("PDB source identity PASS:", len(value["documents"]), "documents; actual DLL/PDB linked")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["build", "inspect"])
    parser.add_argument("record", nargs="?")
    parser.add_argument("sources", nargs="*")
    args = parser.parse_args()
    if args.action == "build":
        build()
    elif not args.record or not args.sources:
        parser.error("inspect requires a fresh record and relative sources")
    else:
        inspect(args.record, args.sources)
