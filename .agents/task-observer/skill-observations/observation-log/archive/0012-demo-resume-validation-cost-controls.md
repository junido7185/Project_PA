---
id: 12
date: 2026-10-04
title: 'Demo resume prompt lacked explicit validation and context cost controls'
status: actioned
resolved: 2026-10-04
resolution: 'Contract cost section covers evidence applicability and rerun triggers, context/output limits, affected checks, captures/references, polling and builds; resume deduplicated. UTF8/content and contract-preservation checks passed; approval/Handoff hashes unchanged. Actual usage remains unmeasured.'
skill: []
target_file: 'AI_WORKFLOW/03_TASKS/CLAUDE_OPENING_DEMO_EXECUTION_PROMPT.md'
proposes_skill: []
siblings_checked: 'Two prompt documents are targets; no target skill and no workspace family registry.'
session_context: 'Docs/04_DEVELOPMENT_LOG/2026-10.md — CLAUDE-COST-CONTROL-001'
---

The user asked whether usage savings and excessive validation were fully considered. The prior resume prompt prohibited repeating P1 and referenced retry limits, but did not define when successful checks need rerunning, how prior evidence remains applicable, or how repeated document/memory reads, broad validators, captures and builds should be limited.

Ordinary prompt work added one shared cost-control section to the existing execution contract: evidence reuse requires matching scope and a verified unchanged baseline; rechecks need changes, failure or missing acceptance evidence; validation stays within the affected scope; retry ceilings are not quotas; P2 checks share the appropriate Play flows; broad delivery checks stay at P11; output, polling, references and build repetition are bounded. Required product input, GameView, core checks and human review remain intact.

The P2 resume prompt was shortened by referring to existing contract rules instead of repeating them. Model Opus/max and gameplay approval were preserved. This is a workflow refinement, not a measured usage reduction; actual Claude consumption must not be asserted from character counts.
