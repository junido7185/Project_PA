---
id: 3
title: "First-batch storage probe was missed again despite activation instruction"
status: open
type: internal
skill: [task-observer]
proposes_skill: []
target_file: [CLAUDE.md]
siblings_checked: "none; no declared family for the Task Observer session activation"
area: "enforcing activation before first tool call"
date: 2026-10-02
session_context: "DEMO-ENV-002 read-only runtime and ownership preflight; 2026-10-03 Codex D4 report review; 2026-10-03 Claude P1-P11 preparation session"
commands_verified: "none"
parked_until:
resolved:
resolution:
reference:
---

**Issue:** The session loaded Task Observer alone in its first call, then probed the pinned storage in the second batch and performed the header scan later. The explicit first-batch requirement existed in the loaded skill and project activation instruction. This repeats the execution failure covered by 0001; the project path correction there remains valid, but a prose instruction did not enforce call composition.

**Suggested improvement:** A review should consider a harness start hook that performs the pinned probe/header scan and injects its checkpoint before tool-using task execution. Do not add a third prose reminder as the sole remedy. No harness/skill changes were made in this task.

**Principle:** A repeated omitted prerequisite needs enforcement at the execution boundary; loading or repeating its written rule is insufficient.

**Additional instance (2026-10-03, Codex D4 report review):** The first tool call loaded the skill without the pinned storage probe; the guarded frontmatter scan ran only after task reads. The documented activation rule was present and loaded. This repeats the existing enforcement failure; the structural-hook recommendation remains pending. No skill or harness edit was made.

**Additional instance (2026-10-03, Claude P1-P11 preparation session):** The first call again loaded Task Observer alone; the pinned probe ran in the second batch together with the task reads, and the guarded scan after them. Protection in play: written (CLAUDE.md "Probe it in the first skill/tool batch"; skill Session Start step 1) and loaded (CLAUDE.md was in context before the first call; the skill text only after it) — no hook or checkpoint. Contributing wording, recorded as diagnosis rather than a proposed rewording: CLAUDE.md's "Before the first tool call ... invoke the task-observer skill" reads as a step that precedes every other call, competing with the same paragraph's batch requirement. The structural-hook recommendation stands; no skill, CLAUDE.md or harness edit was made.

**Additional instance (2026-10-04, prompt preparation after compaction):** The pre-compaction turn had run the protocol, but the resumed first batch reloaded Task Observer without the storage probe/frontmatter scan. The protocol was recovered after loading the compaction reference. Written skill guidance was present and reloaded; the summary's earlier-completion note was incorrectly treated as covering the resumed context. No enforcement or skill change was made.

**Additional instance (2026-10-06, Codex D0 demo polish):** Initial skill load omitted the pinned storage probe. The existing project-local anchor and frontmatter scan were recovered during preflight. Written instructions were available; no execution hook existed. No skill or harness changes made.
