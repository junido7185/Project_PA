---
id: 4
title: "Canonical configuration readiness did not cover effective Claude settings"
status: open
type: internal
skill: [pa-demo-factory]
proposes_skill: []
target_file: [Tools/LoopEngineering/Test-ProjectPADemoEnvironment.py]
siblings_checked: "none; specific client-settings/doctor diagnostic coverage, no declared sibling family"
area: "effective model, tool rejection and MCP scope precedence"
date: 2026-10-02
session_context: "DEMO-ENV-003 Claude production prompt and settings review"
commands_verified: "none"
parked_until:
resolved:
resolution:
reference:
---

**Issue:** The shared-config doctor returned configurationReady while project local settings rejected Serena, user defaults selected Sonnet with per-model low effort, and user-local MCP registrations shadowed/duplicated project server names. A separate native CLI/settings cascade review exposed these facts. The user requested repairs, which were applied locally and in an explicit launch entrypoint; the diagnostic still intentionally covers only canonical base configuration.

**Suggested improvement:** A later skill review should make the diagnostic's scope explicit or add a bounded effective-client-settings check during environment setup. Compare actual settings precedence, local MCP names and per-model effort before claiming a complete production environment is ready. Avoid repeating this broad configuration review for every gameplay ticket.

**Principle:** A valid shared configuration is distinct from the configuration a client actually loads; overridden settings and connection health require evidence from the effective client.
