"""Compare complete original authority/transaction methods, including all overloads."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RUN = "Quality-20261005-001"
TASKS = {
    "craft": ("P6CraftingUI", "CraftingUI.cs", [
        "TryCraftRecipe", "BuildFailureMessage", "BuildLockReason", "GetRootLabel",
        "IsUnlocked", "HasAllIngredients", "ContextMatches", "PassesWorkbenchFilter",
        "CompareRecipes", "GetWorkbenchLabel", "Close", "CloseForOtherPanel",
        "CloseConflictingPanels", "CaptureCursor", "RestoreCursor", "OnDestroy", "Start",
    ], ["craftingPanel", "slotParent", "slotPrefab", "lockedColor"]),
    "storage": ("P6StorageUI", "StorageUI.cs", [
        "OnClickTakeItem", "OnClickStoreItem", "MergeRoom", "Mergeable", "StoreWhole",
        "GetSelectedHotbarSlot", "CloseBox", "CloseForOtherPanel", "CloseConflictingPanels",
        "CaptureCursor", "RestoreCursor", "OnDestroy", "Update", "Start",
    ], ["uiPanel", "itemsParent", "slotPrefab", "titleText"]),
    "companion": ("P7CompanionSelection", "DepartureCompanionSelection.cs", [
        "Start", "Update", "OnDestroy", "SceneLoaded", "Register", "OpenAfterCertification",
        "OpenSelection", "Toggle", "Confirm", "HideSelection", "RestoreConfirmedSelection",
        "Panel", "Label",
    ], ["candidates", "font"]),
    "price": ("P8ShopPriceUI", "ShopPriceUI.cs", [
        "Awake", "Open", "Close", "AdjustPrice", "OnConfirm", "OnRetrieve", "SetCanvasActive",
    ], ["instance", "TutorialPriceDrag", "TutorialPriceDragged", "IsOpen", "ConfirmCount", "OnPriceConfirmed"]),
}


def methods(text, name):
    # Existing source convention: declarations and their closing brace have four spaces.
    # Compare bytes of the entire method, rather than reproducing its logic in a mock test.
    declaration = (r"(?m)^    (?:(?:public|private|protected|internal|static)\s+)*"
                   r"(?:void|bool|int|string|Color|InventorySlot|RectTransform|TextMeshProUGUI)\s+" + name + r"\([^\r\n]*")
    starts = list(re.finditer(declaration, text))
    bodies = []
    for start in starts:
        end = re.search(r"(?m)^    }\r?$", text[start.end():])
        if "=>" in start.group():
            end_index = text.find(";", start.start())
            bodies.append(text[start.start():end_index + 1])
        elif end:
            bodies.append(text[start.start():start.end() + end.end()])
        else:  # Expression-bodied helpers end in a semicolon before the next blank line.
            end_index = text.find(";", start.end())
            bodies.append(text[start.start():end_index + 1])
    return bodies


def verify(task):
    folder, filename, names, fields = TASKS[task]
    stage = ROOT / "Automation/CodexParallel" / folder / RUN
    old = (stage / "Baseline" / filename).read_bytes().decode("utf-8-sig")
    new = (stage / "Source" / filename).read_bytes().decode("utf-8-sig")
    checks = []
    for name in names:
        before, after = methods(old, name), methods(new, name)
        checks.append({"method": name, "overloads": len(before),
                       "unchanged": bool(before) and before == after})
    for name in fields:
        pattern = r"(?m)^    public [^\r\n]*\b" + name + r"\b[^\r\n]*"
        before, after = re.findall(pattern, old), re.findall(pattern, new)
        checks.append({"publicDeclaration": name, "unchanged": bool(before) and before == after})
    if task == "price":
        before = methods(old, "ConfigureTutorialPriceDrag")[0]
        after = methods(new, "ConfigureTutorialPriceDrag")[0]
        marker = "        TutorialPriceDrag = root.GetComponent<Slider>();"
        checks.append({"sliderAuthority": "range/wholeNumbers/handle/listener/tutorial flag",
                       "unchanged": marker in before and marker in after
                       and before[before.index(marker):] == after[after.index(marker):]})
        deltas = "new[] { -10, -1, 1, 10 }"
        checks.append({"priceSteps": [-10, -1, 1, 10],
                       "unchanged": old.count(deltas) == new.count(deltas) == 1})
    passed = all(check["unchanged"] for check in checks)
    log = ROOT / "Logs/CodexParallel" / folder / RUN / "authority-preservation.json"
    log.write_text(json.dumps({"status": "PASS" if passed else "FAIL", "checks": checks,
                              "meaning": "Source preservation only; no Unity or gameplay execution."},
                             ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"{task}: {sum(c['unchanged'] for c in checks)}/{len(checks)} source preservation comparisons")
    if not passed:
        raise SystemExit(1)


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("task", choices=TASKS)
    verify(parser.parse_args().task)
