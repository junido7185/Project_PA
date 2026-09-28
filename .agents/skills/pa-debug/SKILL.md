---
name: pa-debug
description: Diagnose one bounded Project P.A. bug and identify only the first proven broken edge without modifying files.
---

Follow AGENTS.md and the active CODEX_HANDOFF.

Treat existing Human Play evidence, logs, screenshots, and video findings
as authoritative for behavior already observed.

Tool order:
1. Serena symbol navigation
2. relevant existing code/docs
3. Context7 only when an external Unity/API fact is required
4. Unity MCP only when runtime, scene, or serialized state cannot be
   determined reliably otherwise

With Serena:
- prefer find_symbol and get_symbols_overview
- use find_referencing_symbols only when the relationship is necessary
- do not perform broad repository exploration

Do not modify files.

Stop once the FIRST concrete broken edge is established.

Report only:

ROOT CAUSE
EVIDENCE
FILES REQUIRED
MINIMUM FIX