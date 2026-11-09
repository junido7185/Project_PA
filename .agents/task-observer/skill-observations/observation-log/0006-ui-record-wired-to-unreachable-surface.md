---
id: 6
title: "UI record counted as connected while its tab had no player navigation path"
status: open
type: open-source
skill: [pa-demo-factory, project-pa-vertical-slice]
proposes_skill: []
target_file: []
siblings_checked: "no family registry; pa-demo-factory added (acceptance table row Report/UI); project-pa-vertical-slice added (Phase 4 Observe GameView judges UI); pa-visual-director not read this session (assumed)"
area: "Report/UI acceptance — reachability of a UI surface"
date: 2026-10-03
session_context: "2026-10-06 demo-video shot planning (P7 fixture citation); Opening Demo D4 review: toast history was rendered into the phone Feed tab, but the phone home hides every legacy tab button except the Shop tile"
parked_until:
resolved:
resolution:
reference:
---

**Issue:** A previous implementation of the Canon rule "missed objectives are reviewable on the
phone" appended toast history to a card inside the phone's Feed tab component. The component
renders correctly when its tab is opened by API, so a component-level check passes. In the
product, the phone home screen hides every legacy tab button except the Shop tile, so the player
has no way to open the Feed tab. The gap was only found by reading the navigation code path
from the real key (P) through the home screen to the target tab.

**Suggested improvement:** In pa-demo-factory's acceptance table (Report/UI row) and the
vertical-slice visual gate, require the evidence for any UI surface to start from the player's
real entry input (key/button) and reach the surface through visible navigation; opening a panel
by API must be labeled QA-only and cannot count as "connected".

**Additional instance (2026-10-06, demo-video planning):** The P7 companion-growth run grants the upgraded tool with the fixture `FIXTURE 개선 곡괭이 +1 (crafting verified in P6)`. No result.txt or EVIDENCE.md under Logs/ shows that item being crafted by real input in P6 or anywhere else. The gift step itself is real input, but the only route to the item in real play is unproven, so a recorded playthrough may be unable to reach a scene the presentation describes. Same shape as above: a fixture shortcut justified by a citation nobody checked. Point to add: a fixture that skips a step must name a run log where that step passed by real input, and the acceptance pass must open that log.

**Principle:** A UI surface is connected only when the player can reach it from a real input
through visible navigation; rendering correctly when opened programmatically proves content, not
reachability.
