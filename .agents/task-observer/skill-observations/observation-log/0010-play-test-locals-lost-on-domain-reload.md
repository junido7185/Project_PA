---
id: 10
title: "Chained Play-mode test lost its locals across the Play-mode domain reload"
status: open
type: open-source
skill: [pa-demo-factory, project-pa-vertical-slice]
proposes_skill: []
target_file: []
siblings_checked: "no family registry; pa-demo-factory added (owns validator/evidence rules in Implement, play, repair); project-pa-vertical-slice added (its Play/Capture phases author the same Play tests); pa-visual-director excluded (assumed: review-only, writes no tests)"
area: "authoring Editor tests that enter/exit Play mode several times"
date: 2026-10-03
session_context: "P1_CURRENT_CONTINUE isolated save/continue tests (Unity Test Framework EditMode tests with EnterPlayMode/ExitPlayMode, run through the Unity MCP test tool)"
parked_until:
resolved:
resolution:
reference: "Logs/VisualQA/20261003-064301-P1_CURRENT_CONTINUE/LegacyV16Notice/result.txt"
---

**Issue:** A new EditMode UnityTest kept two file paths in local variables, yielded `ExitPlayMode`, then hashed those paths. With domain reload enabled, the framework restores only the enumerator position, so the locals came back null and two hash checks reported FAIL although the product behaviour (6 checks) passed. The same reload also left the MCP test-job tracker stuck at "running" (no progress after the reload); only the evidence file showed the real outcome. A previous test in the same file had already worked around this with a SessionState re-read, but nothing documented why.

**Suggested improvement:** In pa-demo-factory "Implement, play, repair" (and the vertical-slice Play/Capture phase), add one rule for Play-mode test authoring: every value needed after `EnterPlayMode`/`ExitPlayMode` is re-derived from SessionState, constants or files, never kept in a local; long Play tests write their own run-unique result file and are monitored through it, because the runner's job status may not survive the reload.

**Principle:** When a test runner resumes a coroutine across a process- or domain-level reset, treat everything not persisted outside the coroutine as lost, and monitor the run through an artefact the run itself writes rather than through the runner's in-memory status.
