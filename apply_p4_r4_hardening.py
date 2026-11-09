from pathlib import Path

ROOT = Path.cwd()

files = {
    "controller": ROOT / "Assets/Scripts/Presentation/FirstProductionController.cs",
    "validator": ROOT / "Assets/Editor/PA_FirstProductionSaveContractChecks.cs",
}

for name, path in files.items():
    if not path.exists():
        raise SystemExit(f"ERROR: missing {name}: {path}")

def read_normalized(path: Path):
    raw = path.read_bytes()
    newline = "\r\n" if b"\r\n" in raw else "\n"
    text = raw.decode("utf-8-sig").replace("\r\n", "\n").replace("\r", "\n")
    return text, newline

def encode_with_newline(text: str, newline: str):
    return text.replace("\n", newline).encode("utf-8")

controller, controller_nl = read_normalized(files["controller"])
validator, validator_nl = read_normalized(files["validator"])

old_clear = '''    public void ClearWorksitesForRestore()
    {
        foreach(var site in _sites.Values) if(site!=null) site.Binding.Unassign();
        if (Settlement != null && Settlement.Placement != null)
            foreach(string owner in _sites.Keys.ToArray())
                if(Settlement.Placement.TryGetPlacement(WorksiteId(owner),out _)) Settlement.Placement.TryRemove(WorksiteId(owner));
        _sites.Clear();
    }
'''

new_clear = '''    public void ClearWorksitesForRestore()
    {
        foreach(var site in _sites.Values) if(site!=null) site.Binding.Unassign();
        if (Settlement != null && Settlement.Placement != null)
        {
            var owners = new HashSet<string>(_sites.Keys);
            if (Settlement.Selection != null)
                foreach (string owner in Settlement.Selection.ConfirmedIds) owners.Add(owner);
            foreach(string owner in owners)
                if(Settlement.Placement.TryGetPlacement(WorksiteId(owner),out _)) Settlement.Placement.TryRemove(WorksiteId(owner));
        }
        _sites.Clear();
    }
'''

old_reject = '''            if(!result.Succeeded)
            {
                Debug.LogWarning("[VS-P4] Restore placement rejected: " + p.instanceId + " / " + result.Failure);
                return false;
            }
'''

new_reject = '''            if(!result.Succeeded)
            {
                Debug.LogWarning("[VS-P4] Restore placement rejected: " + p.instanceId + " / " + result.Failure);
                ClearWorksitesForRestore(); // remove any earlier placements created by this restore attempt
                return false;
            }
'''

old_validator = '''        // Spatial rejection is deliberately last: no global rollback is claimed or attempted.
        var spatial = p.CaptureState();
        spatial.worksites[0].placement.anchorX = s.Voyage.IslandGrid.Definition.Width;
        Check(FirstProductionController.IsValidSave(spatial, s.CaptureState()), "spatial invalidity deferred to placement authority");
        p.ClearWorksitesForRestore();
        Check(!p.RestoreState(spatial), "placement rejection returns false without throwing");
        Check(p.Sites.Count == 0, "rejection fabricates no replacement site/progress");
        Check(!_runtimeError, "runtime errors zero");
'''

new_validator = '''        // Spatial rejection is deliberately last: no global rollback is claimed or attempted.
        // Make the SECOND placement invalid so the first succeeds and rollback of this P4 restore attempt is proven.
        var spatial = p.CaptureState();
        spatial.worksites[1].placement.anchorX = s.Voyage.IslandGrid.Definition.Width;
        Check(FirstProductionController.IsValidSave(spatial, s.CaptureState()), "spatial invalidity deferred to placement authority");
        p.ClearWorksitesForRestore();
        Check(!p.RestoreState(spatial), "placement rejection returns false without throwing");
        Check(p.Sites.Count == 0 && Ids.All(owner => !s.Placement.TryGetPlacement(FirstProductionController.WorksiteId(owner), out _)),
            "second-placement rejection rolls back partial P4 restore");
        Check(!_runtimeError, "runtime errors zero");
'''

checks = [
    ("controller ClearWorksitesForRestore", controller, old_clear, new_clear),
    ("controller restore rejection", controller, old_reject, new_reject),
    ("validator spatial rejection", validator, old_validator, new_validator),
]

states = []
for label, text, old, new in checks:
    if new in text:
        states.append((label, "already"))
    elif old in text:
        states.append((label, "apply"))
    else:
        states.append((label, "missing"))

missing = [label for label, state in states if state == "missing"]
if missing:
    print("NO FILES WRITTEN.")
    for label, state in states:
        print(f"{state.upper():8} {label}")
    raise SystemExit("ERROR: expected source block not found; local code differs from the reviewed diff.")

new_controller = controller
new_validator_text = validator

if old_clear in new_controller:
    new_controller = new_controller.replace(old_clear, new_clear, 1)
if old_reject in new_controller:
    new_controller = new_controller.replace(old_reject, new_reject, 1)
if old_validator in new_validator_text:
    new_validator_text = new_validator_text.replace(old_validator, new_validator, 1)

if new_controller.count(new_clear) != 1:
    raise SystemExit("ERROR: ClearWorksitesForRestore target is not unique after patch.")
if new_controller.count(new_reject) != 1:
    raise SystemExit("ERROR: restore rejection target is not unique after patch.")
if new_validator_text.count(new_validator) != 1:
    raise SystemExit("ERROR: validator target is not unique after patch.")

files["controller"].write_bytes(encode_with_newline(new_controller, controller_nl))
files["validator"].write_bytes(encode_with_newline(new_validator_text, validator_nl))

print("PASS: P4-R4 hardening applied safely.")
for label, state in states:
    print(f"{('UNCHANGED' if state == 'already' else 'APPLIED'):9} {label}")
print("Changed paths:")
print(files["controller"])
print(files["validator"])
