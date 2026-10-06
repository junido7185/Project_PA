"""One-field ItemInstance metadata fix, isolated patch and real cached-reference compile."""
import sys
sys.dont_write_bytecode = True
import argparse
import importlib.util
from pathlib import Path
import subprocess
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[4]
RUN = "Quality-20261005-001"
STAGE = ROOT / "Automation/CodexParallel/InventoryDragIntegrity" / RUN
LOG = ROOT / "Logs/CodexParallel/InventoryDragIntegrity" / RUN
LIVE = "Assets/Scripts/InventorySlotUI.cs"
CONTEXT = "Assets/Scripts/DragContext.cs"
PATCH = STAGE / "drag-durability.patch"
spec = importlib.util.spec_from_file_location("existing_packager", ROOT / "Tools/CodexParallel/DemoUI/package_ui.py")
pack = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pack)
pack.paths = lambda task: ("Q6_DRAG_DURABILITY", LIVE, STAGE, LOG, PATCH)

LEGACY = """            quality = s.instance != null ? s.instance.quality : 1f,
            currentPrice = s.instance != null ? s.instance.currentPrice : 0
"""
REPLACEMENT = """            quality = s.instance != null ? s.instance.quality : 1f,
            currentPrice = s.instance != null ? s.instance.currentPrice : 0,
            durabilityUsed = s.instance != null ? s.instance.durabilityUsed : 0
"""


def scoped_run(command, **kwargs):
    if command[:2] == ["git", "apply"]:
        command = ["git", "-c", "core.autocrlf=false", *command[1:]]
    if command[0] == "dotnet":
        # SlotOwner is declared in InventorySlotUI. Compile the unchanged real DragContext
        # beside it so its fromOwner uses that enum instead of the cached assembly's enum.
        response = Path(command[-1][1:])
        with response.open("a", encoding="utf-8") as output:
            output.write('/define:UNITY_EDITOR\n"' + str(STAGE / "CompileInputs/DragContext.cs") + '"\n')
    return subprocess.run(command, **kwargs)


pack.subprocess = SimpleNamespace(run=scoped_run, check_output=subprocess.check_output)


def snapshot():
    dependencies = [CONTEXT, "Assets/Scripts/ItemInstance.cs", "Assets/Scripts/InventorySlot.cs",
                    "Assets/Scripts/World/ToolDurability.cs", "Assets/Scripts/Inventory.cs",
                    "Assets/Scripts/Hotbar.cs", "Assets/Scripts/HotbarUI.cs", "Assets/Scripts/InventoryUI.cs",
                    "Automation/LoopEngineering/State/loop-state.json", "Docs/00_CURRENT/CODEX_HANDOFF.md"]
    for name in [LIVE, *dependencies]:
        if not (ROOT / name).is_file():
            raise RuntimeError("Missing dependency: " + name)
    pack.snapshot("drag", dependencies)
    compile_input = STAGE / "CompileInputs/DragContext.cs"
    compile_input.parent.mkdir()
    compile_input.write_bytes((ROOT / CONTEXT).read_bytes())
    source = STAGE / "Source/InventorySlotUI.cs"
    raw = source.read_bytes()
    terminator = "\r\n" if b"\r\n" in raw else "\n"
    old = LEGACY.replace("\n", terminator).encode("utf-8")
    if raw.count(old) != 1:
        raise RuntimeError("Expected drag metadata initializer changed; reconcile first")
    source.write_bytes(raw.replace(old, REPLACEMENT.replace("\n", terminator).encode("utf-8"), 1))
    print("Q6: current baseline preserved; copied only durabilityUsed into existing drag instance")


def verify():
    before = (STAGE / "Baseline/InventorySlotUI.cs").read_bytes()
    after = (STAGE / "Source/InventorySlotUI.cs").read_bytes()
    terminator = "\r\n" if b"\r\n" in before else "\n"
    expected = before.replace(LEGACY.replace("\n", terminator).encode("utf-8"),
                              REPLACEMENT.replace("\n", terminator).encode("utf-8"), 1)
    context_unchanged = (STAGE / "CompileInputs/DragContext.cs").read_bytes() == (ROOT / CONTEXT).read_bytes()
    if after != expected or not context_unchanged:
        raise RuntimeError("Change exceeds the single metadata copy or compile dependency drifted")
    pack.write_json(LOG / "authority-preservation.json", {
        "status": "PASS", "allOtherSourceBytesUnchanged": True, "realCompileContextUnchanged": True,
        "changedField": "ItemInstance.durabilityUsed",
        "preserved": ["ItemInstance/Inventory/Hotbar authority", "drag quantity/split", "quality/price",
                      "slot swap/merge/cancel", "input subscription", "existing item labels", "Save schema/IO"],
        "unityRuntimeTest": "NOT RUN"
    })
    pack.verify("drag")
    print("Q6: one-field fix, exact patch bytes and UNITY_EDITOR partial compile PASS")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["snapshot", "verify"])
    args = parser.parse_args()
    snapshot() if args.action == "snapshot" else verify()
