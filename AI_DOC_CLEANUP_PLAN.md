# AI_DOC_CLEANUP_PLAN.md

작성일: 2026-07-09
작성자: Claude (Fable 5) — 문서 감사 전용 세션
작업 범위: **md 파일 감사·분류·정리 계획 수립만 수행.** 코드/씬/에셋/파일 이동·삭제 없음. 이 파일 하나만 신규 생성.

조사 방법: `Library / Temp / Logs / obj / .git / Packages` 제외 후 프로젝트 전체 `.md` 스캔 → **42개 발견**, 전부 내용 확인(대형 파일은 헤더+구조 확인). 추가로 문서를 참조하는 스크립트/설정(`Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1`, `Automation/LoopEngineering/loop-policy.json`, `Assets/Scripts/` 주석)을 교차 확인했다.

기준 정체성 (이 계획의 판단 축):

> Project P.A.는 "낮에는 동물 마을을 만들고, 밤에는 그 마을의 유일한 잡화점을 운영하는 3D 코지 라이프 시뮬레이션". 구조적으로는 역-공급망 경영 시뮬 (자원 수집 → 가공 → 진열 → NPC 소비 → 판매 통계 → 상점 성장 → 마을 변화). 최상위 문서: `PROJECT_PA_CREATIVE_NORTH_STAR.md`.

위험도 정의:

- **상**: 이 문서를 현재 기준으로 그대로 믿고 코딩하면 정체성/안전규칙과 어긋남, 또는 이동 시 도구·참조가 깨짐.
- **중**: 일부 내용이 낡았거나 다른 문서와 겹쳐 혼란 유발 가능.
- **하**: 그대로 두어도 무해.

---

## 1. 전체 md 파일 목록

### 1-A. 루트 (17개)

| 현재 경로 | 문서 목적 | 카테고리 | 최신성 | 중복 여부 | 위험도 | 추천 처리 |
|---|---|---|---|---|---|---|
| `CLAUDE.md` | Claude용 프로젝트 규칙 (안전/문서화/Unity 규칙) | Agent Rules | 최신 (06-26) | `AGENTS.md`와 의도적 쌍 | 하 | **유지 (루트 고정)** |
| `AGENTS.md` | Codex용 에이전트 가이드 (사전점검/금지작업/검증) | Agent Rules | 최신 (06-26) | `CLAUDE.md`와 의도적 쌍 | 하 | **유지 (루트 고정)** |
| `README.md` | 빌드 실행법, 데모 루트, 조작법, 검증 현황 | Technical Architecture | 최신 (06-26) | 없음 | 하 | **유지** (218행의 `Docs/발표_개발현황보고서.md` 참조는 이동 시 갱신) |
| `PROJECT_PA_CREATIVE_NORTH_STAR.md` | **최상위 창작 기준.** 코지 낮-밤 상점 루프 정의, 마일스톤 1~4 | Project Identity | 최신 (06-21) | G5 (DESIGN_INTENT와 부분 중복) | 하 | **유지 — 대표 정체성 문서** |
| `PROJECT_PA_DESIGN_INTENT.md` | 역-공급망 백본 상세, 금지 변경 목록, T010 가드레일 | Project Identity | 최신 (북극성 하위로 명시됨) | G5 | 하 | **유지** (북극성과의 상하관계가 문서 안에 명시되어 있어 병합 불필요) |
| `PROJECT_PA_CURRENT_MILESTONE.md` | 마일스톤 1 정의 + 성공 기준 + CL-001~004 이력 | Scope / MVP | 최신 (06-26 갱신 중) | 없음 | 하 | **유지** |
| `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md` | 1.0 장기 계획 (10/30시간 구조, Day1~30, 플레이 필라) | Game Loop / Design | 최신 (06-20) | G2 (COMPLETION_PLAN과 중복) | 중 | **유지 — 장기계획 대표 문서** |
| `PROJECT_PA_COMPLETION_PLAN.md` | "완성된 게임" 정의 + 1.0 목표 (06-19 작성) | Game Loop / Design | 구버전 (MASTER PLAN이 사실상 대체) | G2 | 중 | **병합 → 아카이브** (완성 정의 §만 MASTER PLAN에 흡수) |
| `PROJECT_PA_CORE_SLICE_PLAN.md` | Day 1-3 최소 플레이 슬라이스 계획 | Scope / MVP | 최신 (06-25) | 없음 | 하 | **유지** |
| `PROJECT_PA_FULL_GAME_BACKLOG.md` | CN-001~003, FG-00x 백로그 (현황 포함) | Task Queue | 최신 (06-21, 갱신 중) | G3 (RELEASE_BACKLOG와 중복) | 중 | **유지 — 백로그 대표 문서** (RELEASE_BACKLOG의 ID 표 흡수) |
| `PROJECT_PA_RELEASE_BACKLOG.md` | CDN/IL/SPY/VC/NPC ID 체계의 원본 백로그 | Task Queue | 반쯤 구버전 (06-21 리프레임 주석만 추가됨) | G3 | **상** | **병합 → 아카이브** (⚠ CDN/IL/SPY/VC ID는 기능 문서들이 참조하므로 ID 표는 반드시 보존) |
| `PROJECT_PA_MIGRATION_PLAN.md` | Project_D 시각 참조 경계 + T010 이력 | Technical Architecture | 구버전 (06-19, 시각 이관 1차 완료) | 없음 | 중 | **유지 (당분간)** — CONTEXT_INDEX Art 스프린트가 참조. 시각 작업 종료 후 아카이브 |
| `PROJECT_PA_STATUS.md` | 상태 append 로그 (64KB) | Bug / Changelog / Logs | 최신 (계속 갱신) | G7 (SESSION_REPORT와 역할 겹침) | 중 | **유지 + 로테이션** (오래된 섹션은 아카이브로 절단) |
| `PROJECT_PA_TODO.md` | 스프린트별 체크리스트 (39KB) | Task Queue | 최신 (계속 갱신) | 없음 | 중 | **유지 + 완료 스프린트 절단** |
| `PROJECT_PA_SESSION_REPORT.md` | 세션별 작업 보고 append 로그 (57KB) | Handoff | 최신 (계속 갱신) | G7 | 중 | **유지 + 로테이션** |
| `PROJECT_PA_CRASH_REPORT_20260624.md` | D3D12 크래시 3연속 원인 조사 (상세) | Bug / Changelog / Logs | 구버전 (0625 리포트가 후속) | G4 | 하 | **아카이브** (0625가 최종 baseline 기록) |
| `PROJECT_PA_CRASH_REPORT_20260625.md` | D3D12 크래시 최종 판정 + D3D11 우회 baseline | Bug / Changelog / Logs | 최신 크래시 기준선 | G4 | **상 (이동 금지)** | **유지 (루트 고정)** — preflight 스크립트가 루트에서 `PROJECT_PA_CRASH_REPORT_*.md`를 glob |

### 1-B. Docs/ (20개)

| 현재 경로 | 문서 목적 | 카테고리 | 최신성 | 중복 여부 | 위험도 | 추천 처리 |
|---|---|---|---|---|---|---|
| `Docs/01_개요_및_정체성.md` | 초기 기획: 정체성/기획 의도/벤치마킹 | Project Identity | 구버전 (04-10) | G1 | **상** | **유지 + 상단 배너** ("현재 기준은 NORTH_STAR" 명시). §2 참조 |
| `Docs/02_경제_및_아이템_설계.md` | 역-공급망 4단계, ItemData/ItemInstance 설계 | Game Loop / Design | 유효 (경제 백본은 현행) | G1 | 하 | **유지** — 코드 주석이 `Docs/02 §` 인용. 이동·개명 금지 |
| `Docs/03_NPC_및_AI_시스템.md` | NavMesh+FSM, MBTI 행동 설계 | Game Loop / Design | 유효 | G1 | 하 | **유지** — 코드 주석 인용 (`DialogueData.cs` 등). 이동 금지 |
| `Docs/04_멀티플레이_및_지속성.md` | UGS Relay Host-Client, 클라우드 저장 | Technical Architecture | **보류된 기능** (현행: 싱글 우선) | G1 | **상** | **유지 + "Deferred" 배너** — `EconomyService.cs`가 "Docs/04 원칙" 인용하므로 이동 금지 |
| `Docs/05_성장_및_운영_시스템.md` | 티어/감사/친밀도/채용 설계 | Game Loop / Design | 유효 | G1 | 하 | **유지** — 코드 주석 다수 인용. 이동 금지 |
| `Docs/06_개발_로드맵_및_명세.md` | 16주 WBS, 요구사항 명세, 외부 에셋 권장 | Scope / MVP | 구버전 (04-10; MASTER PLAN이 대체) | G1, G8 | **상** | **유지 + 배너** (외부 에셋 권장 §3.2는 현행 안전규칙과 충돌함을 명시) |
| `Docs/07_개발일지.md` | 개발일지 append 로그 (191KB, 최신 항목 06-26) | Bug / Changelog / Logs | 최신 (매 세션 갱신 필수) | 없음 | 중 | **유지 + 로테이션** (연/분기 단위 분할 권장) |
| `Docs/08_아트_및_씬_구성_가이드.md` | 아트 톤, 3D/2D 발주 파이프라인, 씬 체크리스트 (110KB) | Technical Architecture | 유효 (05-08 갱신) | G6 (§10이 건물_진입 가이드와 중복) | 하 | **유지** — 코드 주석 인용 (`BuildingEntrance.cs` 등). 이동 금지 |
| `Docs/🏝️ 프로젝트 P A (Pioneer Assistance) 기획서 2d3ea077ba408051b7acd05ea8275ae6.md` | Notion 내보내기 원본 기획서 (01~06의 원천) | Archive Candidate | 구버전 (01~06으로 분할 완료) | **G1 대표 중복** | **상** | **아카이브 + 개명** (`PA_기획서_노션원본_2026-04.md`). 이미지 링크 이미 깨짐, 이모지/공백 파일명은 도구 처리 위험 |
| `Docs/기능_명세서.md` | 312개 항목 상세 기능 명세 (04-28 상태 기준) | Verification / Test | 명세는 유효, **상태표(✅🟡❌)는 낡음** | 없음 | 중 | **유지** + 상태열이 2026-04-28 기준임을 상단에 명시 |
| `Docs/발표_개발현황보고서.md` | 발표용 개발현황 보고 (04-28 시점 고정) | Handoff | 시점 고정 자료 | 없음 | 하 | **이동 → `Docs/Presentation/`** (README 218행 참조 갱신 필요) |
| `Docs/Project_PA_Week11_SpeakerScript.md` | 11주차 발표 대본 | Handoff | 시점 고정 자료 | 없음 | 하 | **이동 → `Docs/Presentation/`** |
| `Docs/Tripo3D_빌딩_생성_프롬프트.md` | Tripo3D 발주 프롬프트 시트 (복붙용) | Prompt Archive | 유효 (아트 발주 진행 중) | 없음 | 하 | **유지** |
| `Docs/UI_스프라이트_가이드.md` | UI 스프라이트 17종 + Nano Banana 프롬프트 | Prompt Archive | 유효 | 없음 | 하 | **유지** (`Docs/레퍼런스/` 이미지와 세트 — 함께 유지) |
| `Docs/건물_진입_씬_세팅_가이드.md` | 씬 세팅 절차 튜토리얼 (세션 17, 04-16) | Technical Architecture | 부분 구버전 | G6 | 중 | **유지** (08 §10의 상세 부록 역할. 08에서 링크로 연결 권장) |
| `Docs/AgentWorkflow/CONTEXT_INDEX.md` | **작업 유형별 컨텍스트 라우터** (스프린트별 필독 문서/검증기/금지사항) | Codex Workflow | 최신 (06-26) | 없음 | **상 (참조 허브)** | **유지 — 이동 계획 실행 시 이 파일의 경로 참조를 반드시 동기 갱신** |
| `Docs/CustomerPresentation/README.md` | SPY-002/003 구현 기록 (고객 성향 표시 레이어) | Verification / Test | 최신 (06-22) | 없음 | 하 | **유지** (⚠ 폴더명이 '발표자료'로 오해될 수 있음 — 문서 첫 줄에 기능 문서임이 명시돼 있어 개명은 선택) |
| `Docs/IslandLife/GATHERING_AND_SHOP_GATE.md` | IL-001+CDN-002 구현 기록 (낮 채집/밤 영업 게이트) | Verification / Test | 최신 (06-24) | 없음 | 하 | **유지** |
| `Docs/VillageCulture/VC-001A.md` | VC-001A 구현 기록 (판매 카테고리 → 다음날 마을 시각 변화) | Verification / Test | 최신 (06-26) | 없음 | 하 | **유지** |
| `Docs/VisualTargets/VISUAL_TARGETS.md` | 비주얼 목표 정의 (참조 이미지 사용 규칙) | Game Loop / Design | 유효 (06-19) | 없음 | 하 | **유지** |

### 1-C. Automation/LoopEngineering (4개)

| 현재 경로 | 문서 목적 | 카테고리 | 최신성 | 중복 여부 | 위험도 | 추천 처리 |
|---|---|---|---|---|---|---|
| `Automation/LoopEngineering/progress.md` | 루프 작업 append 로그 | Bug / Changelog / Logs | 최신 (06-26) | 없음 | 하 | **유지 (append-only 유지)** |
| `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md` | 더티 Git 144개 경로 분류 + 커밋 가이드 (1회성) | Handoff | 최신 — **아직 실행 안 된 커밋의 근거 문서** | 없음 | 중 | **유지** → 체크포인트 커밋 완료 후 아카이브 |
| `Automation/LoopEngineering/Tickets/LOOP-DRYRUN-001.md` | 드라이런 티켓 (완료, BLOCKED_BY_DIRTY_GIT 기록) | Task Queue | 완료 티켓 | 없음 | 하 | **유지** (티켓 이력은 Tickets/ 폴더에 누적) |
| `Automation/LoopEngineering/ticket-template.md` | 루프 티켓 템플릿 | Codex Workflow | 최신 | 없음 | 하 | **유지** |

### 1-D. Assets/ (1개)

| 현재 경로 | 문서 목적 | 카테고리 | 최신성 | 중복 여부 | 위험도 | 추천 처리 |
|---|---|---|---|---|---|---|
| `Assets/Jinxish/Drag & Drop Inventory & Hotbar Framework/readme.md` | 서드파티 에셋 설명서 | Unknown (외부 자산) | 해당 없음 | 없음 | **상 (이동 금지)** | **절대 이동/수정 금지** — Assets 내부, `.meta` 동반. Unity가 관리 |

---

## 2. 현재 정체성과 충돌하는 문서

**결론 먼저: "감성 서사 게임 / 기억 보관 게임 / 어두운 철학적 경영 게임" 방향의 문서는 발견되지 않았다.** 충돌은 전부 "초기 기획(순수 관리자 + 멀티플레이 + 외부 에셋)" 계열이며, 성격상 정체성 반전이 아니라 **버전 낙후**다.

| 파일 | 충돌 내용 | 처리 제안 |
|---|---|---|
| `Docs/01_개요_및_정체성.md` | §2.1 "노동은 NPC에게 위임, 플레이어는 관리자" **단독** 강조 → 현행 정체성은 낮 생활(채집·낚시·광질·농사)이 핵심 재미의 절반. §2.3 크로스플랫폼/Co-op → 현행 싱글 우선과 불일치 | 이동 금지(코드 §인용). 상단에 배너 추가: "이 문서는 2026-04 초기 기획 원본. 현재 기준은 `PROJECT_PA_CREATIVE_NORTH_STAR.md` (낮 생활 + 밤 상점). 멀티플레이는 보류." |
| `Docs/04_멀티플레이_및_지속성.md` | UGS Relay 멀티플레이 전체가 현행 "single-player-first" 방향과 충돌 (삭제된 게 아니라 **보류**) | 이동 금지(`EconomyService.cs`가 Docs/04 인용). 상단 "DEFERRED — 1.0 이후 검토" 배너 추가 |
| `Docs/06_개발_로드맵_및_명세.md` | §3.2 외부 에셋 권장(DOTween, Odin Inspector, Joystick Pack, Synty) → `CLAUDE.md`/`AGENTS.md`의 "외부 패키지 금지" 규칙과 정면 충돌. 16주 WBS는 MASTER PLAN이 대체 | 배너 추가: "로드맵은 `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md`가 대체. §3.2 외부 에셋 권장은 현행 안전규칙상 무효." |
| `Docs/🏝️ ... 기획서 2d3ea077....md` (Notion 원본) | 01~06과 같은 초기 기획 전체 (Factorio식 자동화 강조 포함). 최신 문서와 이중 진실(source-of-truth 분열) 유발 | **아카이브** (§6 참고). 역사 원본으로만 보존 |
| `PROJECT_PA_RELEASE_BACKLOG.md` | 충돌이라기보다 **위험한 중복**: 06-19 시점 백로그가 "Todo"로 남아 있어, 이미 구현 완료된 항목(CDN-001~003 등)을 Codex가 다시 구현하려 들 수 있음 | FULL_GAME_BACKLOG로 병합 후 아카이브 (§3 참고) |

---

## 3. 중복 문서 목록

| 중복 그룹 | 대표 문서 후보 | 합칠/정리할 문서 | 이유 |
|---|---|---|---|
| **G1. 초기 기획서** | `Docs/01~06` (분할본) | Notion 원본 `Docs/🏝️ ... 2d3ea077....md` → 아카이브 | 01~06이 동일 내용의 정리본. 원본은 이미지 링크가 깨져 있고 파일명이 위험함 |
| **G2. 장기 계획** | `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md` | `PROJECT_PA_COMPLETION_PLAN.md`의 "완성 정의"·"제출 데모 vs 1.0" 섹션을 MASTER PLAN에 흡수 후 아카이브 | 둘 다 1.0 정의를 다루며 MASTER PLAN이 더 새 프레임(06-20). CONTEXT_INDEX Build 스프린트 참조를 MASTER PLAN으로 교체 필요 |
| **G3. 백로그** | `PROJECT_PA_FULL_GAME_BACKLOG.md` | `PROJECT_PA_RELEASE_BACKLOG.md`의 카테고리 표(CDN/IL/SPY/VC/NPC/…)를 **ID 그대로** FULL_GAME_BACKLOG에 "레거시 ID 레지스트리" 섹션으로 이식 후 아카이브 | 기능 문서들(`GATHERING_AND_SHOP_GATE.md`=IL-001+CDN-002, `CustomerPresentation/README.md`=SPY-002/003, `VC-001A.md`)이 이 ID들을 참조. ID 유실 시 이력 추적 불가 |
| **G4. 크래시 리포트** | `PROJECT_PA_CRASH_REPORT_20260625.md` (루트 유지) | `PROJECT_PA_CRASH_REPORT_20260624.md` → 아카이브 | 0625가 최종 판정 + D3D11 baseline 기록. preflight는 "최신 1개"만 필요 |
| **G5. 정체성 문서** | `PROJECT_PA_CREATIVE_NORTH_STAR.md` | `PROJECT_PA_DESIGN_INTENT.md` — **병합하지 않음** | 상하관계가 양쪽 문서에 명시돼 있고 역할이 다름(북극성=방향, 디자인 의도=금지선/백본 상세). 병합하면 참조가 대량으로 깨짐 |
| **G6. 건물 진입 가이드** | `Docs/08_아트_및_씬_구성_가이드.md` §10 | `Docs/건물_진입_씬_세팅_가이드.md`는 상세 부록으로 유지 | 08 §10이 요약, 별도 가이드가 절차 상세. 삭제·병합보다 상호 링크가 안전 |
| **G7. 상태/세션 로그** | 역할 분리로 해소 (병합 아님) | `PROJECT_PA_STATUS.md` = "현재 상태 스냅샷", `PROJECT_PA_SESSION_REPORT.md` = "세션별 이력" 으로 역할 재선언 + 둘 다 오래된 섹션 로테이션 | 두 파일 모두 append 로그화되어 57~64KB. 역할 경계를 문서 상단에 명시하면 중복 기록이 줄어듦 |
| **G8. 로드맵** | `PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md` + `PROJECT_PA_CURRENT_MILESTONE.md` | `Docs/06_개발_로드맵_및_명세.md`는 배너만 추가(이동 금지) | 16주 WBS는 이미 지난 시점. 명세 §1은 기능_명세서와 함께 여전히 참조 가치 있음 |

---

## 4. 최종 목표 디렉토리 구조

원칙: **루트의 핵심 문서와 Docs/01~08은 절대 이동하지 않는다** (코드 주석 §인용, preflight glob, CONTEXT_INDEX 참조 때문). 이동은 "참조가 없는 시점 고정 자료"와 "병합 완료된 구버전"만.

```
Project_PA/
├─ CLAUDE.md                              # Agent Rules (Claude)
├─ AGENTS.md                              # Agent Rules (Codex)
├─ README.md                              # 빌드/실행/데모
├─ AI_DOC_CLEANUP_PLAN.md                 # 본 계획 (실행 완료 후 99_ARCHIVE로)
│
│  # ── 정체성 · 계획 (항상 루트) ──
├─ PROJECT_PA_CREATIVE_NORTH_STAR.md      # 최상위 창작 기준
├─ PROJECT_PA_DESIGN_INTENT.md            # 금지선 + 역-공급망 백본
├─ PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md  # 장기 1.0 계획 (+완성 정의 흡수)
├─ PROJECT_PA_CURRENT_MILESTONE.md        # 현재 마일스톤
├─ PROJECT_PA_CORE_SLICE_PLAN.md          # Day 1-3 슬라이스
├─ PROJECT_PA_FULL_GAME_BACKLOG.md        # 통합 백로그 (+레거시 ID 레지스트리)
│
│  # ── 살아있는 기록 (항상 루트) ──
├─ PROJECT_PA_STATUS.md                   # 현재 상태 스냅샷 (로테이션)
├─ PROJECT_PA_TODO.md                     # 진행 중 체크리스트 (로테이션)
├─ PROJECT_PA_SESSION_REPORT.md           # 세션 이력 (로테이션)
├─ PROJECT_PA_MIGRATION_PLAN.md           # (시각 이관 작업 종료 시 아카이브 예정)
├─ PROJECT_PA_CRASH_REPORT_20260625.md    # 최신 크래시 baseline (preflight glob 대상 — 루트 고정)
│
├─ Docs/
│  ├─ 01~08_*.md                          # 번호 기획서 8종 — 위치·이름 동결 (코드 §인용)
│  ├─ 기능_명세서.md
│  ├─ Tripo3D_빌딩_생성_프롬프트.md
│  ├─ UI_스프라이트_가이드.md
│  ├─ 건물_진입_씬_세팅_가이드.md
│  ├─ 레퍼런스/                            # 참조 이미지 (그대로)
│  ├─ AgentWorkflow/CONTEXT_INDEX.md      # 컨텍스트 라우터
│  ├─ CustomerPresentation/README.md      # 기능 기록 (SPY-002/003)
│  ├─ IslandLife/GATHERING_AND_SHOP_GATE.md
│  ├─ VillageCulture/VC-001A.md
│  ├─ VisualTargets/VISUAL_TARGETS.md
│  └─ Presentation/                       # ★신설: 시점 고정 발표 자료
│     ├─ 발표_개발현황보고서.md
│     ├─ Project_PA_Week11_SpeakerScript.md
│     └─ (선택) Week11 pptx, 발표 스크립트 txt 등 비-md 발표 파일
│
├─ Automation/LoopEngineering/            # 현 구조 유지
│  ├─ progress.md / ticket-template.md / loop-policy.json / validator-registry.json
│  ├─ Tickets/LOOP-DRYRUN-001.md
│  └─ BASELINE_COMMIT_REVIEW.md           # (커밋 후 아카이브 예정)
│
└─ 99_ARCHIVE/
   └─ old_docs/
      ├─ PA_기획서_노션원본_2026-04.md      # 개명 후 이동
      ├─ PROJECT_PA_COMPLETION_PLAN.md     # 병합 후
      ├─ PROJECT_PA_RELEASE_BACKLOG.md     # ID 이식 후
      └─ PROJECT_PA_CRASH_REPORT_20260624.md
```

---

## 5. 이동 계획

실행 순서 중요: **① 체크포인트 커밋 → ② 병합 편집 → ③ 이동 → ④ 참조 갱신** 순서를 지킬 것 (§8 위험 요소 참고).

| 현재 경로 | 이동 예정 경로 | 처리 이유 |
|---|---|---|
| `Docs/🏝️ 프로젝트 P A (Pioneer Assistance) 기획서 2d3ea077ba408051b7acd05ea8275ae6.md` | `99_ARCHIVE/old_docs/PA_기획서_노션원본_2026-04.md` | 01~06 분할본이 대체. 이모지+공백+해시 파일명은 스크립트/에이전트 처리 사고 위험. 참조하는 문서 없음(안전) |
| `PROJECT_PA_COMPLETION_PLAN.md` | `99_ARCHIVE/old_docs/` | "완성 정의" 섹션을 MASTER PLAN에 흡수한 **후에만** 이동. 이동 시 `CONTEXT_INDEX.md:144` (Build/Package 스프린트)를 MASTER PLAN으로 교체 |
| `PROJECT_PA_RELEASE_BACKLOG.md` | `99_ARCHIVE/old_docs/` | CDN/IL/SPY/VC/NPC ID 표를 FULL_GAME_BACKLOG에 이식한 **후에만** 이동 |
| `PROJECT_PA_CRASH_REPORT_20260624.md` | `99_ARCHIVE/old_docs/` | 0625 리포트가 최종. 루트 glob은 "최신 1개"만 필요하므로 구버전 제거는 오히려 안전성 향상 |
| `Docs/발표_개발현황보고서.md` | `Docs/Presentation/` | 시점 고정 발표 자료. 이동 시 `README.md:218` 참조 갱신 |
| `Docs/Project_PA_Week11_SpeakerScript.md` | `Docs/Presentation/` | 시점 고정 발표 대본. 참조 없음(안전) |
| `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md` | (이번 정리에서는 이동하지 않음) | 체크포인트 커밋이 실제로 이뤄진 뒤 다음 정리 때 아카이브 |
| `PROJECT_PA_MIGRATION_PLAN.md` | (이번 정리에서는 이동하지 않음) | `CONTEXT_INDEX.md:71`이 Art 스프린트 참조 중. 시각 이관 트랙 종료 후 아카이브 |

**이동하지 않는 것들 (명시적 동결):**

- `Docs/01~08` 번호 문서 전부 — 코드 주석이 `Docs/03 §2.2`, `Docs/05 §1`, `Docs/08 §…` 형식으로 인용 (프로젝트 주석 컨벤션).
- `PROJECT_PA_CRASH_REPORT_20260625.md` — `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1:86`이 루트 glob.
- `Assets/Jinxish/**/readme.md` — Unity 관리 영역, `.meta` 동반.
- CONTEXT_INDEX가 "Required"로 지정한 모든 루트 문서.

---

## 6. 아카이브 예정 파일

| 현재 경로 | 아카이브 이유 |
|---|---|
| `Docs/🏝️ ... 기획서 2d3ea077....md` | Notion 원본. 01~06으로 분할 정리 완료. 이미지 링크 깨짐. 파일명 위험. 초기 기획(순수 관리자/멀티플레이/자동화 중심)이 현행 정체성과 버전 충돌 |
| `PROJECT_PA_COMPLETION_PLAN.md` | MASTER_DEVELOPMENT_PLAN과 내용 중복 (둘 다 1.0 정의). 병합 후 이력 보존용 |
| `PROJECT_PA_RELEASE_BACKLOG.md` | FULL_GAME_BACKLOG와 백로그 이중화. 06-19 시점 "Todo" 상태가 낡아 재구현 유도 위험. ID 이식 후 이력 보존용 |
| `PROJECT_PA_CRASH_REPORT_20260624.md` | 0625 리포트로 대체된 중간 조사 기록 |
| (추후) `Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md` | 체크포인트 커밋 완료 후 1회성 문서 역할 종료 |
| (추후) `PROJECT_PA_MIGRATION_PLAN.md` | T010 계열 시각 이관 트랙 완전 종료 후 |

삭제 대상: **없음.** 전부 이동·보존만 제안한다.

참고 (md 외, 이번 범위 밖): `Docs/Memory_Recycler_Term_Project_Proposal_polished.docx`는 다른 과제의 파일로 보임 — 사용자 확인 후 처리 권장. `Docs/발표 스크립트*.txt`, `*.pptx`, `*.pdf`는 `Docs/Presentation/`으로 함께 모으면 좋으나 md 정리와 별건.

---

## 7. Codex가 읽어야 할 문서 순서

매 작업 공통 (항상, 이 순서대로):

1. `CLAUDE.md` 또는 `AGENTS.md` — 안전규칙·금지작업 (에이전트 종류에 맞는 쪽)
2. `Docs/AgentWorkflow/CONTEXT_INDEX.md` — 작업 유형 판별 후 최소 컨텍스트 선택
3. `PROJECT_PA_CREATIVE_NORTH_STAR.md` — 정체성 (게임플레이 관련 작업이면 필수)
4. `PROJECT_PA_DESIGN_INTENT.md` — 금지선 확인
5. `PROJECT_PA_CURRENT_MILESTONE.md` — 지금 무엇을 만드는 중인가
6. `PROJECT_PA_TODO.md` — 현재 스프린트의 미완 항목 (최신 섹션 위주)
7. `PROJECT_PA_STATUS.md` — 최근 상태 (최신 섹션만; 전체 정독 불필요)

작업 유형별 추가 (CONTEXT_INDEX의 스프린트 정의를 따름):

- 게임플레이/백로그 작업 → `PROJECT_PA_FULL_GAME_BACKLOG.md`, `PROJECT_PA_CORE_SLICE_PLAN.md`
- 경제/NPC → `Docs/02`, `Docs/03` (+ 관련 기능 기록: `Docs/CustomerPresentation/`, `Docs/IslandLife/`, `Docs/VillageCulture/`)
- 아트/씬/UI → `Docs/08`, `Docs/VisualTargets/VISUAL_TARGETS.md`, `Docs/UI_스프라이트_가이드.md`
- 아트 발주 → `Docs/Tripo3D_빌딩_생성_프롬프트.md`
- Unity 실행/검증 → 최신 `PROJECT_PA_CRASH_REPORT_*.md` + `Automation/LoopEngineering/validator-registry.json`
- 자동 루프 작업 → `Automation/LoopEngineering/loop-policy.json` → `progress.md` → `ticket-template.md` → 해당 티켓
- 작업 종료 시 갱신 → `PROJECT_PA_STATUS.md`, `PROJECT_PA_TODO.md`, `PROJECT_PA_SESSION_REPORT.md`, `Docs/07_개발일지.md` (+루프 작업이면 `progress.md`, `loop-state.json`)

---

## 8. 위험 요소

1. **코드 주석 ↔ Docs 번호 결합.** `Assets/Scripts/` 주석이 `Docs/03 §2.2`, `Docs/05 §1`, `Docs/08 §건물 진입` 형식으로 인용한다. `Docs/01~08`을 개명·이동하면 주석의 §인용이 전부 죽은 참조가 된다. → 번호 문서는 영구 동결.
2. **preflight 스크립트 glob.** `Tools/LoopEngineering/Invoke-ProjectPAPreflight.ps1:86`이 프로젝트 루트에서 `PROJECT_PA_CRASH_REPORT_*.md`를 찾는다. 최신 리포트(0625)를 루트 밖으로 옮기면 크래시 게이트 판정이 잘못된다. 구버전(0624) 이동은 안전.
3. **CONTEXT_INDEX 참조 동기화.** `CONTEXT_INDEX.md:71`(MIGRATION_PLAN), `:144`(COMPLETION_PLAN) 등 파일 경로가 하드코딩되어 있다. 문서 이동과 CONTEXT_INDEX 갱신은 **같은 커밋**에서 처리해야 한다. `CLAUDE.md`/`AGENTS.md`/`README.md:218`도 동일.
4. **더티 Git 상태.** 현재 워킹트리가 더티하고 loop-policy가 클린 baseline을 요구한다(`BLOCKED_BY_DIRTY_GIT` 기록 있음). 이 상태에서 문서를 대량 이동하면 baseline 검토(BASELINE_COMMIT_REVIEW)와 어긋나고 diff가 오염된다. → **반드시 사용자 승인 하에 체크포인트 커밋 후 이동.**
5. **백로그 ID 유실.** RELEASE_BACKLOG의 CDN/IL/SPY/VC/NPC ID는 기능 문서 3종과 TODO/일지가 참조하는 사실상의 ID 레지스트리다. 병합 시 ID 표를 통째로 보존하지 않으면 이력 추적이 끊긴다.
6. **append 로그의 절단.** `Docs/07_개발일지.md`(191KB), STATUS(64KB), SESSION_REPORT(57KB)는 로테이션 시 "잘라낸 부분을 아카이브 파일로 보존"해야 한다. 요약으로 대체하며 원문 삭제하는 방식은 금지 (CLAUDE.md의 기록 유지 규칙과 충돌).
7. **Notion export 파일명.** 이모지·공백·해시가 포함된 파일명은 셸 인용 실수로 오동작하기 쉽다. 이동 시 `git mv` + 따옴표 인용을 정확히 사용하고, 개명 후 경로만 이후 문서에서 사용할 것.
8. **Unity 관리 영역.** `Assets/` 내부 md는 `.meta`와 쌍이다. 절대 이동·삭제하지 않는다 (이번 계획에서 해당 파일은 Jinxish readme 1개뿐이며 동결).
9. **발표 자료의 비-md 짝 파일.** `발표_개발현황보고서.md`를 Presentation/으로 옮길 때 pptx·txt를 함께 옮기지 않으면 발표 자료가 두 곳으로 흩어진다. 함께 이동하거나 전부 보류할 것.

---

## 9. 다음 단계 실행 프롬프트 초안

아래 프롬프트를 다음 Fable/Codex 세션에 그대로 사용할 수 있다.

```text
너는 Unity 졸업작품 Project P.A.의 문서 정리 실행 담당이다.
루트의 AI_DOC_CLEANUP_PLAN.md 를 먼저 정독하고, 그 계획의 §5 이동 계획과 §8 위험 요소를 그대로 따른다.

안전 규칙 (위반 시 즉시 중단하고 보고):
- 코드/씬/프리팹/ScriptableObject/meta 파일 수정 금지.
- Assets, Packages, ProjectSettings, Library, Temp, Logs, obj, .git 수정 금지.
- 파일 삭제 금지. 모든 정리는 이동(git mv) 또는 문서 편집만.
- Docs/01~08 번호 문서는 개명·이동 금지 (코드 주석이 § 인용).
- PROJECT_PA_CRASH_REPORT_20260625.md 는 루트에서 이동 금지 (preflight glob).
- git push 금지, 커밋은 사용자 승인 후에만.

실행 순서:
0. git status 확인. 더티 상태면 먼저 사용자에게 체크포인트 커밋 승인을 받는다
   (Automation/LoopEngineering/BASELINE_COMMIT_REVIEW.md 의 분류를 근거로 사용).
   승인 전에는 어떤 이동도 하지 않는다.
1. [병합 1] PROJECT_PA_COMPLETION_PLAN.md 의 "Definition Of A Complete Game"과
   "Submission Demo vs Long-Term 1.0" 섹션을 PROJECT_PA_MASTER_DEVELOPMENT_PLAN.md 에
   출처 날짜와 함께 흡수한다.
2. [병합 2] PROJECT_PA_RELEASE_BACKLOG.md 의 카테고리 표(CDN/IL/SPY/VC/NPC 등 ID 전부)를
   PROJECT_PA_FULL_GAME_BACKLOG.md 하단에 "레거시 ID 레지스트리 (원본: RELEASE_BACKLOG 2026-06-19)"
   섹션으로 ID·상태 그대로 이식한다. ID는 하나도 빠뜨리지 않는다.
3. [배너] Docs/01, Docs/04, Docs/06 상단에 3줄 이내 안내 배너를 추가한다:
   "초기 기획 원본. 현재 기준은 PROJECT_PA_CREATIVE_NORTH_STAR.md" +
   (04: "멀티플레이는 DEFERRED") + (06: "외부 에셋 권장 §3.2는 현행 안전규칙상 무효").
   본문 내용은 수정하지 않는다.
4. [이동] 99_ARCHIVE/old_docs/ 와 Docs/Presentation/ 폴더를 만들고 AI_DOC_CLEANUP_PLAN.md §5 의
   표대로 git mv 한다. Notion 기획서는 PA_기획서_노션원본_2026-04.md 로 개명하며 이동한다.
5. [참조 갱신 — 이동과 같은 커밋에서]
   - Docs/AgentWorkflow/CONTEXT_INDEX.md: COMPLETION_PLAN 참조를 MASTER_DEVELOPMENT_PLAN 으로 교체.
   - README.md 218행 부근: 발표_개발현황보고서 경로를 Docs/Presentation/ 으로 교체.
6. [검증] grep 으로 이동한 옛 경로를 전체 md 에서 검색해 죽은 참조가 없는지 확인한다.
   (과거 로그 문서 STATUS/SESSION_REPORT/TODO/개발일지 안의 역사적 언급은 갱신하지 않고 남긴다.)
7. [기록] PROJECT_PA_STATUS.md, PROJECT_PA_TODO.md, PROJECT_PA_SESSION_REPORT.md,
   Docs/07_개발일지.md 에 이번 정리 내용을 기록한다.
8. 커밋 메시지 제안만 하고, 사용자 승인 후 커밋한다.

이번 세션에서 하지 않는 것:
- STATUS/SESSION_REPORT/개발일지 로테이션 (다음 티켓).
- BASELINE_COMMIT_REVIEW.md / MIGRATION_PLAN.md 아카이브 (조건 미충족).
- Docs 의 docx/pptx/pdf/txt 정리 (사용자 확인 필요).
```

---

## 완료 조건 자체 점검

- [x] 코드 수정 없음 — 조사와 본 문서 작성만 수행.
- [x] 실제 파일 이동 없음.
- [x] 삭제 없음 (제안만).
- [x] 신규 생성 파일은 `AI_DOC_CLEANUP_PLAN.md` 1개.
- [x] 모든 판단은 실제 파일 경로·행 번호 기준 (`CONTEXT_INDEX.md:71/:144`, `README.md:218`, `Invoke-ProjectPAPreflight.ps1:86` 등).
