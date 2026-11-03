# 제작 흐름

1. 현재 로컬 Git와 실행 세션, Handoff를 인수하고 관련 상태·기능·연결·큐만 읽는다.
2. 현재 티켓 안에서 플레이로 확인할 완결된 경험을 고른다(예: 해안 이동 중 수중 바위와 진로가 읽힘, 도구 선택→타격→획득). 보일 변화·소유 파일·끝을 확인할 기존 증거/도구를 알린다. 합의된 범위·취향은 기존 Canon/결정에서 찾고 다시 인터뷰하지 않는다.
3. Claude Code 총괄이 기존 LoopEngineering 런처로 제작·비평·수정·검증을 격리 worktree에 배정한다. Claude 총괄만 원본과 Unity를 소유하며 Codex는 검토·독립 지원이다.
4. 작업자 diff·원본 충돌을 검토하고 통합한다. 기존 권위와 로컬 자산을 재사용한다.
5. Unity compile → 관련 test → 실제 입력/GameView → 필요한 Windows 후보/성능을 검증한다.
6. 반복 탐색·실패·재작성은 해당 run의 실패와 기존 검사/도구부터 찾는다. 가장 작은 방법 변경을 현재 실제 작업에 쓰고 기존 같은 조건 결과와 비교한다. 원인·바꾼 접근·결과·한계는 기존 checkpoint/Handoff에 남긴다. 효과가 불명확하면 후보이며, 다음 다른 실제 작업의 적용 근거가 있어야 재사용 확인이다. 매 작업에 후보/검사를 증설하거나 승인·권한·owner·사용량·저장 보호를 자동 수정하지 않는다.
7. 승인 목표 안의 다음 작업을 이어간다. 종료 시 월별 결과 한 번 + 변경된 현재 사실 + 짧은 Handoff.

공통 보호/완료/기록 계약은 [AGENTS.md](../../AGENTS.md). 상세 자료는 [DOCS_INDEX](DOCS_INDEX.md)에서 필요할 때 읽는다. 작업별 문서와 스킬이 같은 인수/기록을 반복하지 않는다.

표현 판단이 필요하면 [현재 미술 검수 기준](../../Docs/01_GAME_DESIGN/Art/VISUAL_TARGETS.md#현재-대표-구역-검수--2026-10-21)과 [관련 결정 입구](../../Docs/01_GAME_DESIGN/References/README.md)만 선택한다. 물가는 DK-03의 원인 분리/비교 조건을, 실행 도구 문제는 기존 SESSION_RULES·해당 run 오류를 사용한다. 원본 매뉴얼 개정·owner 채택·후속 실제 사용은 각기 diff/결정/결과를 연결하며 Read 반환만으로 완료하지 않는다.

방법 출처: [writing-for-agents](https://github.com/mattpocock/skills/blob/main/skills/productivity/writing-for-agents/SKILL.md)의 조건부 문서 링크, [retro](https://github.com/mattpocock/skills/blob/main/skills/engineering/retro/SKILL.md)의 실제 실패·기존 검사 우선, [Harness²](https://arxiv.org/html/2610.14224v1)의 근거 비교를 P.A. 기존 절차에 맞게 선별했다. 외부 실행기·설정·hook·자동 commit·새 tracker는 도입하지 않는다.
