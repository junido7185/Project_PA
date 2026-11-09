"""Read-only check before opening a provider; no model, Unity, or state writes."""
import json
from pathlib import Path
import sys
sys.dont_write_bytecode = True
from setup_pa import ALLOWED_ROOT, guard_windows_root, project_snapshot
try:
    root = ALLOWED_ROOT.resolve()
    guard_windows_root(root)
    state = project_snapshot(root)
    print(json.dumps({k: state[k] for k in ('branch', 'head', 'ticket', 'unity_owner', 'approval_status')}, ensure_ascii=False))
except Exception as exc:
    print('PREFLIGHT_BLOCKED: ' + str(exc), file=sys.stderr)
    raise SystemExit(2)
