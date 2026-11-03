# Project P.A. — Reference Research

> 이 폴더는 **레퍼런스 게임 분석·연구 근거·개발 가설**을 보관하는 참고 지식 저장소다. 공식 게임 기획 Canon이나 실제 Unity 구현 상태를 대체하지 않는다.

## 문서 우선순위와 주의사항

1. **공식 요구사항:** `Docs/01_GAME_DESIGN/Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md` 및 관련 승인 Canon을 먼저 따른다.
2. **현행 구현 상태:** `Docs/00_CURRENT/CURRENT_STATE.md`, `CAPABILITY_REGISTRY.md`, `GAME_LOOP_MAP.md`, `INTEGRATION_QUEUE.md`와 **현재 로컬 코드/씬/검증 결과**를 확인한다.
3. **레퍼런스 연구:** 이 폴더는 게임플레이·미술·표현을 개선하는 판단 근거다. 연구 제안(H)은 신규 확정 계약이 아니다.
4. 연구에 기록된 GitHub 기준 HEAD `b121140ae6f5ca0f92078db5c2f75512dc8fc95a`은 **연구 당시의 스냅샷**이다. 최신 로컬 브랜치 및 미커밋 파일과 다를 수 있다.
5. 원작의 확인된 화면/측정(O/M/V/D), 해석(I), P.A. 가설(H), 미확인(U)을 혼동하지 않는다. **영상 프레임 간격을 실제 게임 입력 지연/내부 코드 수치로 간주하지 않는다.**
6. 연구상의 Q 시나리오는 실제 Unity GameView에서 플레이하고 기록하기 전까지 `PASS`로 취급하지 않는다. 레퍼런스 연구만으로 기존 기능을 재작성하거나 확장하지 않는다.

## 연구 묶음과 읽기 입구

| 묶음 | 읽기 시작 | 핵심 개발 자료 | 남은 한계 |
|---|---|---|---|
| **1차: Canon v2 전체 연구 (2026-10-11, 24편)** | [전체 연구 README](Project_PA_Canon_v2_Reference_Research/README.md) | [개발 지식 색인](Project_PA_Canon_v2_Reference_Research/CANON_V2_DEVELOPMENT_KNOWLEDGE.md), [작업별 책임/검수](Project_PA_Canon_v2_Reference_Research/02_design_knowledge/06_INTEGRATION_AND_WORK_PACKETS.md), [장면 근거](Project_PA_Canon_v2_Reference_Research/03_evidence/SCENE_LEDGER.md) | 짧은 공식 클립 중심, 원음·정확한 동작 타이밍 미확인 |
| **2차: Dinkum 벌목 (2026-10-12, 7편)** | [벌목 README](pa-dinkum-logging-deep-2026-10-12/DINKUM_LOGGING_README_2026-10-12.md) | [벌목 구현 지식](pa-dinkum-logging-deep-2026-10-12/PA_LOGGING_IMPLEMENTATION_KNOWLEDGE_2026-10-12.md), [장면/미확인](pa-dinkum-logging-deep-2026-10-12/DINKUM_LOGGING_CAPTURE_AND_GAPS_2026-10-12.md) | 접근→벌목→목재 지급의 HUD·원음 연속 영상 미확인, U-01 미종결 |
| **4차: ACNH NPC 생활감 (2026-10-12, 5편)** | [NPC README](pa-acnh-npc-life-2026-10-12/ACNH_NPC_LIFE_README_2026-10-12.md) | [NPC 구현 지식](pa-acnh-npc-life-2026-10-12/PA_NPC_LIFE_DESIGN_KNOWLEDGE_2026-10-12.md), [관찰 사례](pa-acnh-npc-life-2026-10-12/ACNH_NPC_LIFE_CASES_2026-10-12.md) | 단기 시각 행동 중심. 장소 선택·장기 스케줄·원음 의미 미확인 |

**3차 Dinkum 이동 예비조사**는 요약만 전달됐으며 이 묶음에는 원본 Markdown이 포함되지 않는다. 추후 원본을 받으면 별도 폴더로 추가한다. 5차 월드 비주얼 연구도 아직 포함되지 않는다.

## 작업별 최소 열람 경로

- **이동·도구·채집·획득:** 1차 `ACT-01/02` → 2차 Dinkum 벌목 구현 지식 → 해당 영상 근거 및 열린 질문 → 현행 코드 확인.
- **동료·주민·생산 행동:** 1차 `DAY-01`/책임 맵 → 4차 ACNH NPC 설계 지식 및 사례 → 현행 `NpcController`/`ProducerNpcController` 등 실제 코드 확인.
- **진열·가격·판매:** 1차 `COM-01` 관련 원작 분석과 개발 계약. 아직 독립 고정밀 추가 연구 없음.
- **월드·배치·환경:** 1차 `WORLD-01/PLC-01`을 참고하되 고정밀 환경 미술 연구는 아직 없음.
- **낮밤·시네마틱·UI:** 1차 `DAY-01/PRE-01` 및 검증 조건에서 필요한 문서만 선택.

## 자료 관리 원칙

- 새 연구 결과는 **독립 폴더**로 추가한다. 기존 Markdown은 원본 그대로 유지해 출처 추적과 상대 링크를 보존한다.
- `00_basis/CANON_V2_snapshot.md`, `IDENTITY_snapshot.md`는 연구 당시에 사용한 **역사적 보존본**이다. 프로젝트 공식 Canon보다 우선하지 않는다.
- 새 연구 패키지를 합칠 때 동명이인 문서를 덮어쓰거나 외부 영상/이미지/음원 원본을 연구 폴더에 무단 반입하지 않는다.
- 연구에서 제안한 기술적 수치·코드 구조는 반드시 최신 프로젝트에 맞춰 검증한다. 코드 변경은 별도 명시적 개발 작업으로 처리한다.

**사용 예시:** “현재 티켓과 직접 관련된 `Docs/01_GAME_DESIGN/ReferenceResearch/README.md` 및 연결된 연구 파일만 읽고, 현행 Canon·코드·검증과 비교하여 적용 여부를 판단하라.”
