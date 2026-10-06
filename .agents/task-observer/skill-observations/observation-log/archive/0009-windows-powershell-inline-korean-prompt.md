---
id: 9
title: "Windows PowerShell misdecoded the inline Korean preparation prompt"
status: actioned
type: internal
skill: []
proposes_skill: []
target_file: [Tools/LoopEngineering/Start-ProjectPAClaudeDemo.ps1]
siblings_checked: "Launcher artifact only; no skill change proposed. Compared the existing preparation CheckOnly and native command path; no matching existing encoding observation."
area: "Windows script decoding and native prompt delivery"
date: 2026-10-03
session_context: "User pasted mojibake from the Claude preparation session launched through powershell.exe."
parked_until:
resolved: 2026-10-03
resolution: "Moved Korean preparation text to an explicit UTF-8 Markdown read, added CheckOnly Korean/decoder reporting, and verified exact complete native argument delivery on Windows PowerShell 5.1/codepage 949 with a local capture executable. Real Claude and Unity were not started; approval hash was unchanged."
reference: "Logs/ClaudeLauncherEncoding-20261003-060607-335/result.json"
---

**Issue:** The BOM-less UTF-8 .ps1 held a Korean here-string. Windows PowerShell 5.1 decoded its source using codepage949, producing the user's corrupted prompt before native invocation. The previous AST/CheckOnly checks passed because they did not exercise that string. ParseFile under the actual host reproduced the corruption.

**Suggested improvement:** Keep non-ASCII instructions in explicitly decoded UTF-8 artifacts and verify the complete native argument with a capture program when validating a multilingual launcher. Do not start a paid model or gameplay to test transport. The native test initially saw both stub and installed Claude candidates; its PATH was isolated once before rerunning the failed check.

**Principle:** Syntax and readiness checks do not establish correct text transport across the actual host's source-decoding and process-argument boundaries.
