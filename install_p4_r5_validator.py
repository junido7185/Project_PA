from pathlib import Path
import sys

src = Path(__file__).with_name("PA_FirstProductionR5Checks.cs")
dst = Path.cwd() / "Assets/Editor/PA_FirstProductionR5Checks.cs"

if not src.exists():
    raise SystemExit(f"ERROR: missing source validator next to installer: {src}")
if not dst.parent.exists():
    raise SystemExit(f"ERROR: run from Project_PA root; missing {dst.parent}")

content = src.read_bytes()
if dst.exists():
    current = dst.read_bytes()
    if current == content:
        print("UNCHANGED: validator already installed.")
        raise SystemExit(0)
    raise SystemExit(
        "ERROR: Assets/Editor/PA_FirstProductionR5Checks.cs already exists with different content. "
        "No file was overwritten."
    )

dst.write_bytes(content)
print(f"PASS: installed {dst}")
