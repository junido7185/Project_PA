from pathlib import Path

src = Path(__file__).with_name("PA_FirstProductionR5Continuation.cs")
dst = Path.cwd() / "Assets/Editor/PA_FirstProductionR5Continuation.cs"

if not src.exists():
    raise SystemExit(f"ERROR: missing {src}")
if not dst.parent.exists():
    raise SystemExit("ERROR: run from Project_PA root.")

content = src.read_bytes()
if dst.exists():
    if dst.read_bytes() == content:
        print("UNCHANGED: continuation validator already installed.")
        raise SystemExit(0)
    raise SystemExit("ERROR: continuation validator exists with different content; no overwrite.")

dst.write_bytes(content)
print(f"PASS: installed {dst}")
