---
id: 15
title: "Evidence-first technical presentation workflow has no skill; status docs lagged code"
status: open
type: open-source
skill: []
proposes_skill: [evidence-first-tech-presentation]
target_file: []
siblings_checked: "none — no skill-families.md registry exists; proposes a new skill, no existing target"
area: "presentation production from a code repository (capstone/graduation talks)"
date: 2026-10-05
session_context: "Graduation presentation planning for a Unity project: evidence ledger before Figma deck redesign"
parked_until:
resolved:
resolution:
reference:
---

**Issue:** The user supplied a detailed, reusable methodology for building a technical talk from a codebase: evidence → verified claim → message → visual; per-claim reachability levels (code exists / connected / reachable in play / experienced by player); VERIFIED / PARTIAL / INTENT / UNVERIFIED classes; a "do not claim" column; and a professor cross-examination pass. No installed skill captures it. During the run, the project's own current-state documents were stale against the code in at least three places: a phone tab listed as unreachable had since been wired up, the player animation clip source had changed, and the newest validator result differed from the cited one. Only tracing the code and the latest run logs caught these.

**Suggested improvement:** Create a skill named `evidence-first-tech-presentation` with these parts:
- (1) A ledger template with an A–D reachability column and a "do not claim" column.
- (2) A rule that status docs are claims to verify, not evidence: anchor every claim to code plus the newest run log.
- (3) Mandatory disclosure checks: third-party and licence-less assets visible in chosen screenshots, AI-assisted authorship, and QA fixtures inside "automated E2E" claims.
- (4) A time budget in syllables per minute.
- (5) A final pass where a skeptical reviewer tries to disprove the claims.

**Additional instance (same session, production phase):** An image was reused from the old deck by a geometric rule ("the left-most image-filled node"). The rule picked a full-width tutorial capture instead of the island scene view, and the new caption described it as the island. The post-build screenshot caught the mismatch. Point to add to the skill: identify a reused asset by its name and by a rendered check, and write its caption only after seeing the image.

**User correction (same session, after delivery):** The deck used full-sentence assertion headlines, following the assertion-evidence research. The user rejected them for reading like an AI-made PPT. For Korean university talks, the user wants short noun-form titles, at most one short subtitle, and every claim or explanation moved into the speaker notes. Point to add to the skill: ask about, or default to, the audience's local deck convention for title style, and do not apply assertion-evidence headlines automatically.

**Additional instance (2026-10-06, external-examiner upgrade):** A separate adversarial-review subagent with repository access found three wrong claims in the main-deck copy that my own evidence ledger had passed. (1) "NPC is both customer and producer": in fact customers are tourists only and production is one miner, once. (2) The phone-tab defect was "found by automated check": the check had passed through an internal API, and an evidence review found the defect. (3) "Deposit is the single change point": all money changes go through the service, not through one method. Point to add to the skill: run an independent adversarial reviewer against code and logs before delivery, not only after the user asks.

**User correction (2026-10-06, contribution framing):** The deck had two problems. It gave main-talk time to how the automated checks worked and what their criteria were. It also introduced class names (ShopSlot, EconomyService) without saying what kind of thing each one is. The user also said the AI disclosure made the work read as "I just ran the AI", which hides a year of building the agent environment. That year covered rule docs, skills, validators, and adopting new tools as they appeared. Fix applied: verification internals moved to backup; the main talk shows results only. Each class name was given its kind on first mention (component / singleton service / static class). A dedicated slide shows the AI working environment, with dated git commits as evidence. Points to add to the skill: (a) when the author used AI agents, ask what the author built around them (harness, rules, review loop), and present that effort with dated repository evidence instead of a bare disclosure line; (b) give every code identifier a plain kind noun the first time it is said aloud; (c) keep test-method internals in backup unless the audience asks.

**Principle:** In a technical presentation, every on-slide claim needs an anchor to executable evidence of the current state, because summary documents drift from the code. Disclosure checks belong in the selection step: each chosen screenshot is checked for third-party or unlicensed content, and each automated-test claim is checked for fixtures that bypass the real path.
