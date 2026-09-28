문서 라우팅

1. 기본 진입

Docs/README → 00_CURRENT 네 문서. AI_WORKFLOW는 작업 방법만 관리한다.

기본적으로 전체 문서 트리를 정독하지 않는다. 현재 작업에 직접 필요한 문서만 추가로 읽는다.

2. 기록 원칙

결과는 월별 이력에 한 번 작성한다. 상태·기능·실제 연결·다음 큐는 각각 00_CURRENT의 해당 문서만 갱신한다.

중간 진단·컴파일 재시도·Play 재시도마다 상태 문서를 반복 갱신하지 않는다. 작업이 PASS / PARTIAL / HARD BLOCKED 중 하나로 멈출 때 필요한 현재 문서만 한 번 갱신한다.

3. Vertical Slice Studio Mode

다음 중 하나가 사용자 지시나 승인된 작업 목표에 명시되면 player-facing 3D 작업에 한해 Studio Mode 라우팅을 추가한다.

VERTICAL SLICE STUDIO MODE

STUDIO MODE

Playable Island Rebase

GameView polish pass

Character feel pass

Presentation pass

Studio Mode 활성화 시 기본 진입 문서에 더해 아래만 읽는다.

AI_WORKFLOW/02_AGENT_RULES/VERTICAL_SLICE_STUDIO_MODE.md

AI_WORKFLOW/08_ASSET_CONTEXT/PROJECT_PA_ASSET_MANIFEST.md

.codex/skills/project-pa-vertical-slice/SKILL.md — Codex skill 로딩을 지원하는 환경에서 사용

해당 경험에 직접 관련된 구현/씬/프리팹/아트 문서

Docs/99_ARCHIVE/AI_STUDIO/PROJECT_PA_AI_STUDIO_PACK.md는 백업/확인용 통합본이다. 정상 Studio 작업 중에는 읽지 않는다. 활성 문서와 중복해 컨텍스트를 낭비하지 않는다.

Studio Mode는 월드·카메라·캐릭터·입력감·애니메이션·UI 피드백·VFX/SFX·레벨 드레싱·온보딩·player-facing presentation 작업에 사용한다.

SaveData schema, Economy 권위, 저장 마이그레이션, 네트워크, 대형 아키텍처 리팩터는 Studio Mode가 아니라 기존 bounded/safe workflow를 따른다.

4. 작업 유형별 추가 문서

작업

필요한 자료

게임 방향·콘텐츠

Canon, Campaign의 관련 승인·계약

기능·저장·월드

구현 참고와 Capability의 실제 코드·검증 근거

player-facing 3D / GameView

Studio Mode 3종 + Style Grammar + 직접 관련 구현/씬/프리팹만

아트

Style Grammar, 출처 입구

검증·크래시

검증 규칙, 관련 로그, 루트 최신 crash report

발표

발표 자료

과거 결정·실패

월별 이력, 보관 문서 중 해당 티켓만

5. 실행 원칙

실행 방법은 CONTEXT_INDEX. 전체 이력이나 과거 backlog를 매번 읽지 않는다.

Studio Mode에서는 AGENTS.md의 Vertical Slice Studio Mode — 플레이어 화면 제작 예외 규칙이 일반 bounded-ticket의 작업 단위·씬 대상·retry 범위보다 우선한다. 단, destructive Git/file operation, unrelated dirty overwrite, 기존 KEEP 권위 교체, incompatible SaveData migration 같은 HARD STOP은 그대로 유지한다.

최종 완료 판정은 작업 성격에 맞게 한다.

core/data 작업: compile + authority/integrity verification 중심

Studio 작업: compile/runtime 안정성 + 실제 Play + 실제 GameView 품질 게이트 중심