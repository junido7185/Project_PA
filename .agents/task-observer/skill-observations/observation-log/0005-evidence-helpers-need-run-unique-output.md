---
id: 5
title: "Evidence helper wrote to a fixed folder that held the prior verdict's evidence"
status: open
type: open-source
skill: [pa-demo-factory, project-pa-vertical-slice]
proposes_skill: []
target_file: []
siblings_checked: "no family registry; pa-demo-factory added (owns acceptance evidence); project-pa-vertical-slice added (Phase 6 Capture produces the same evidence); pa-visual-director not checked in this session (assumed: review-only)"
area: "evidence capture before running an existing QA helper"
date: 2026-10-03
session_context: "Opening Demo D1-D4 resume: existing business Play helper wrote result.txt and PNGs to Logs/VisualQA/DemoCompletion-20260930/D2"
parked_until:
resolved:
resolution:
reference:
---

**Issue:** The existing D2 business Play helper used a constant output folder and
`File.WriteAllText(result.txt)` at start. Re-running it would have silently replaced the
2026-10-01 result and captures that the current state documents cite as the only evidence for
the earlier partial PASS. The canonical skill says "inspect a capture helper's side effects
before invoking it", which caught it only because the user's prompt also spelled out
"never overwrite past captures; patch fixed output paths first". Without that prompt line the
generic side-effect sentence does not name output-path collision.

**Suggested improvement:** In pa-demo-factory "Implement, play, repair" (and vertical-slice
Phase 6 Capture), add one concrete rule: before the first run of any evidence-producing helper,
confirm it writes to a run-unique folder (timestamped); if it uses a fixed path, change the path
first and record the new folder in the result. Optionally the doctor could flag helpers whose
registry entry names a fixed evidence path.

**Principle:** Evidence that justified an earlier verdict must be immutable; any tool that
produces evidence should write to a run-unique location, because a fixed path turns a re-run
into silent deletion of the record being re-checked.
