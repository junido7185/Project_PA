---
id: 16
title: "User-mandated local font was unloadable in the Figma MCP runtime; probe it before design planning"
status: open
type: open-source
skill: ["figma:figma-use-slides", "figma:figma-use"]
proposes_skill: []
target_file: []
siblings_checked: "figma plugin family: figma-use-slides (deck typography plan, Phase 1) and figma-use (canonical text recipe, Rule 8) both added; figma-use-figjam assumed to share the same runtime, so the same probe applies, but it is not added because FigJam text rarely carries a mandated brand font (assumed)"
area: "Phase 1 Design & Plan — shared constants / font-loading preamble"
date: 2026-10-05
session_context: "Graduation deck redesign in an existing Figma Slides file whose current text uses a locally installed Korean font"
parked_until:
resolved:
resolution:
reference:
---

**Issue:** The user mandated one font family, which is installed on their machine and already used in the existing deck. A type scale was planned around it. The first read-only `use_figma` probe then showed the font is absent from `listAvailableFontsAsync()`: 1,938 families were listed, all cloud fonts, none of them local. `loadFontAsync` failed for every style of both family names. As a result, existing text in that font cannot be edited through MCP, and no new text can be created in it. The plan had to be reopened after the user had already reviewed it.

**Suggested improvement:** In `figma-use-slides` Phase 1, step 2 ("Shared constants"), and in `figma-use` Rule 8, add one step: before committing a type plan, run a read-only probe. It calls `loadFontAsync` on every mandated family and style and checks `listAvailableFontsAsync` for families already used in the file. If the probe fails, report it as a blocker and offer options instead of substituting a font. One option is to build every text node on local text styles with a proxy font, so the owner can switch the family once per style in a client that has the font. Also note in the skill that local fonts installed on the user's machine may not be visible to the MCP runtime.

**Principle:** Before planning around an environment-dependent resource the user requires (a font, an asset, a plugin), verify that the execution runtime can actually use it. Availability on the user's machine does not prove availability to the agent.
