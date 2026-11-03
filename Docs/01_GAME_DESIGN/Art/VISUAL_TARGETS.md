# VISUAL_TARGETS.md

> **2026-10-18 현재 적용 주의:** 이 문서는 초기 이미지/시연 제안을 보존한다. 본문의 상시 좌측 objective panel, isometric/top-down 고정, 야외 부스 중심 구성, Project_D 자산 복사 허용처럼 읽히는 지시는 현재 실행 권위가 아니다. 현재 [Opening Demo Canon v2](../Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md) §7·§16~17·§22, 승인된 실제 카메라/플레이 경로와 AGENTS의 Project_D 읽기 전용 규칙을 따른다. 장면 판단은 [레퍼런스 결정 입구](../References/README.md)와 Style Grammar를 사용한다. 아래 과거 제안은 승인 없이 재활성화하지 않는다.


## 현재 대표 구역 검수 — 2026-10-21

현재 승인된 정상 카메라와 [Style Grammar](PROJECT_PA_ART_STYLE_GRAMMAR.md), [DK-03](../References/DINKUM_ACTION_WORLD.md#dk-03--자산-수보다-세계의-어휘와-공간-연결을-맞춘다)를 함께 사용한다. 아래 기준은 기존 미술 권위의 검수 관점이며 카메라·미술 방향을 새로 고정하지 않는다.

- **거리/실루엣:** 정상 게임 거리에서 캐릭터·채집 대상·진로와 큰 해안/수관 형태를 먼저 읽는다. 확대 crop은 원인 확인용이며 전체 화면의 가독성을 대신하지 않는다.
- **팔레트 역할:** 바다의 깊이·땅/길·식생·상호작용 대상이 밝기와 색의 역할을 나누는지 본다. 경계를 부드럽게 하면서 수중 바위/해초의 형태·색을 지우는 변경은 퇴행으로 구분한다.
- **낮/밤 조화:** 현재 대표 구역의 같은 카메라/시각/해상도/HUD·식생 조건을 맞춘다. 낮에서 좋아진 결과로 밤의 대상/문/가격 가독성을 수락하지 않는다. 밤·이동 영상이 없으면 해당 항목은 미검증이다.
- **이번 적용:** 보존된 v17c/v18b와 [r3 검수](../../../Logs/ParallelProduction/World-v17c-v18b-verifier-r3-20261021/HANDOFF.md)를 기준으로 수중 바위 소실/해초 halo를 먼저 다룬다. 셀 깊이 색 전이·지형 옆면·foam을 분리하고, shader 오류·화면 개선·사람 수락을 각각 기록한다. 같은 자료를 다시 제작하지 않는다.

출처는 [Game Studios art-bible](https://github.com/Donchitos/Claude-Code-Game-Studios/blob/main/.claude/skills/art-bible/SKILL.md)의 실루엣·색·환경 조화와 [GDAS vision](https://github.com/thedivergentai/Game-Dev-Agentic-Skills/blob/main/skills/godot-agent-vision/SKILL.md)의 필요한 화면 항목 선택이다. 엔진 중립적인 판단만 사용하며 Godot/Bevy 구현·도구·기본 수치·점수표는 이식하지 않는다. 아래 초기 시연 제안보다 현재 Canon/승인/카메라/실제 증거가 우선한다.

## Purpose

These images are visual target references for Project_PA.

They are not exact screenshots of the current build.
They are not assets to copy directly.
They define the intended direction for:

* market stall presentation
* cozy settlement atmosphere
* UI readability
* shop interaction clarity
* NPC placement
* presentation-quality scene composition

## Core Visual Direction

Project_PA should become a warm, cozy, low-poly management simulation game about helping a new settlement grow.

The final look should feel:

* cozy
* bright
* readable
* toy-like
* low-poly
* warm village market
* friendly but system-driven
* easy to understand for a professor or evaluator within 10 seconds

## Important Visual Goals

### 1. Market Stall as the Main Gameplay Object

The stall should be the center of the demo.

It should clearly show:

* product slots
* price tags
* displayed items
* player/NPC interaction point
* shop identity
* selling flow

The stall should feel more like a complete shop booth, not simple brown cubes.

Desired elements:

* wooden stall frame
* striped fabric awning
* product baskets or crates
* item display slots
* visible price labels
* small signboard
* lantern or small decorative prop
* clean interaction prompt

### 2. Project_PA Identity

Do not copy another commercial game directly.

Use the target images only as broad quality references.
Project_PA should keep its own identity:

* P.A. / Pioneer Assistance
* settlement helper theme
* first-day settlement support
* shop operation
* NPC friendship
* smartphone app
* tier / audit / growth systems

### 3. UI Style

The UI should be readable and consistent.

Target UI direction:

* warm dark translucent panels
* rounded corners
* yellow/gold highlight for money and selected item
* green confirmation buttons
* clear Korean text
* bottom hotbar
* top-left time/day panel
* top-right money/tier panel
* objective panel on the left
* compact interaction prompt near the player or object

### 4. Camera and Scene Composition

Use an isometric / top-down 3D view.

The final demo scene should clearly show:

* player
* market stall
* NPCs
* fountain or landmark
* paths
* shop area
* interaction target
* UI overlays

The camera should not be too zoomed out or too flat.
The player and stall should be readable at first glance.

### 5. Visual Polish Priority

Priority order:

1. Market stall readability
2. UI readability
3. NPC/player readability
4. Scene lighting
5. Ground/path/environment polish
6. Decorative props
7. Screenshot/presentation framing

## What Codex Should Do

Codex should use these images and notes as target direction for visual polish and scene staging.

Codex may:

* inspect the reference prototype project
* identify reusable user-created assets
* copy or migrate only safe user-created assets into Project_PA
* improve Project_PA scenes using existing assets
* create simple Unity primitives/materials if needed
* stage the market stall and NPCs more clearly
* improve UI layout and readability

Codex must not:

* copy copyrighted commercial assets
* use another game’s name, logo, UI text, or exact layout
* replace Project_PA’s core systems
* overwrite Project_PA scenes without backup or confirmation
* import large external packages without approval
* change gameplay logic unless required for integration
