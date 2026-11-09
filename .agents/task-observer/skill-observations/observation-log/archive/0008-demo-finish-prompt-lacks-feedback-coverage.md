---
id: 8
title: "Demo finish prompt lacks explicit coverage of earlier gameplay feedback"
status: actioned
type: internal
skill: []
proposes_skill: []
target_file: [AI_WORKFLOW/03_TASKS/CLAUDE_OPENING_DEMO_EXECUTION_PROMPT.md]
siblings_checked: "Execution prompt artifact only; no skill change or new skill proposed. Compared the live prompt, Canon v2, pa-demo-factory and pa-demo-completion."
area: "feedback-to-ticket acceptance coverage"
date: 2026-10-03
session_context: "User supplied earlier feedback before restarting Claude; read-only comparison with current Canon and F1-F6 draft."
parked_until:
resolved: 2026-10-03
resolution: "Revised the existing execution prompt to P1-P11: continue first, explicit fishing/bug/crafting/placement/storage/companion/business acceptance, optional participation separate from validation, later scope decisions preserved, and actual reference/video comparison. Added a preparation-session entry requiring the user's actual approval message; no gameplay or approval was activated."
reference:
---

**Issue:** The F1-F6 draft emphasizes motion, action feedback, island presentation, first-day presentation, continue and delivery. Earlier user feedback and Canon also require the meaning of daytime preparation, representative tool crafting and compatible companion production, and usable optional fishing/bug activities. These do not have explicit acceptance coverage in the draft. The recent D1-D4 QA supplies placement and goods through fixtures and therefore cannot prove the gather/craft/companion-to-sale loop. The current roadmap also puts continue after motion although the supplied prior priority put continue first. This comparison does not establish that the underlying features are unimplemented.

**Suggested improvement:** Before approving a revised execution sequence, map each in-scope feedback requirement to one experience ticket, current evidence and a concrete player-input acceptance criterion. Keep optional player activities distinct from optional quality verification. Preserve later decisions (current-format restore, unsupported v16 notice with original preservation, Demo256, shallow specialization preview); separate larger fishing, weather and progression proposals rather than silently enabling them. This observation does not change the prompt or approval record.

**Principle:** A delivery sequence must cover the intended gameplay relationships as well as presentation and a reachable ending; optional participation does not make a shipped activity exempt from validation.
