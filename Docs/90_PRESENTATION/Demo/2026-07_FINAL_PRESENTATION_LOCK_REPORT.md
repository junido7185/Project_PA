# FINAL_PRESENTATION_LOCK_REPORT — Visual Demo Integration Pass v3

작성: 2026-07-13 (Codex)

## 최종 확정

- 판정: **조건부 발표용**
- Final Locked Screenshot: `Logs/DemoViewShots/after_locked_20260713_002356.png`
- 기준: 실제 추적 카메라, UI 포함, 2560×1440, Day 1 15:42, D3D11 batchmode
- 메인 씬·프리팹·저장 스키마·경제/NPC 코어는 수정하지 않았다.

## v3에서 실제 구현한 내용

- 조명: `DayNightVisual`이 시간대별 Trilight 앰비언트를 구동하고, 캡처 도구가 `ForceSet` 뒤 15시 조명을 즉시 재적용한다. 15시대 청회색 화면을 따뜻한 오후 톤으로 보정했다.
- 실모델: 기존 Nature Pack 소품을 에디터 베이커로 콜라이더·MonoBehaviour 없는 Resources 프리팹으로 만들고, 나무·수풀·꽃·풀·밀·바위·통나무·그루터기·의자를 런타임 드레싱에 사용했다.
- 광장 드레싱: 광장 플레이트·판매 데크·러그·파빙·화단·가로등·쇼케이스·분수 주변 소품을 실제 카메라 프레임 기준으로 재배치했다.
- NPC 손님 연출: 기존 `NpcController` FSM과 `NavMeshAgent`를 재사용해 Day 1에 NPC 2명을 판매대 전면에 스테이징했다.
- UI 통일: `ClockHUD` 폭을 좌측 정보 컬럼에 맞추고 좌우 HUD 배경 투명도를 0.55로 통일했다.
- 캡처: NPC 도착 대기 시간을 확보하고 조명/렌더 진단을 추가했다.

## 수정·생성 파일

- `Assets/Editor/PA_DemoViewCapture.cs`
- `Assets/Editor/PA_DemoPropBaker.cs` + `.meta`
- `Assets/Scripts/DayNightVisual.cs`
- `Assets/Scripts/DemoVisualDressingController.cs`
- `Assets/Scripts/UI/ClockHUD.cs`
- `Assets/Scripts/UI/MoneyHUD.cs`
- `Assets/Resources/PA_DemoProps/**`
- 본 폴더의 최종 보고 문서 5종과 프로젝트 종료 기록 문서

## 검증 결과

| 검증 | Exit | 결과 | 핵심 증거 | 로그 |
|---|---:|---|---|---|
| 런타임 컴파일 | 0 | PASS | 오류 0, 기존 CS8785 경고 1 | 콘솔 실행 결과 |
| 에디터 컴파일 | 0 | PASS | 오류 0, 기존 CS8785/CS0414 경고 2 | 콘솔 실행 결과 |
| Final Demo Route | 0 | PASS | BreadLoaf 진열, 30G 구매, 피드백 70% | `Logs/Fable_V3_FinalDemoRoute.log` |
| Day/Night Shop Loop | 0 | PASS | Day 1/2 페이즈·영업 게이트, sellableInventory=10 | `Logs/Fable_V3_DayNightShopLoop.log` |
| Customer Panel Layout | 0 | PASS | 패널 화면 내 배치·HUD/핫바 비겹침 | `Logs/Fable_V3_PanelLayout.log` |
| Core Slice Playability | 0 | PASS | 기본 HUD, F10 ON/OFF, 디버그 표시 기본 OFF | `Logs/Fable_V3_CoreSlicePlayability.log` |
| Final Presentation | 0 | PASS | 5장 캡처, 요약 본문 410/418 | `Logs/Fable_V3_FinalPresentation.log` |

신규 장식은 런타임에서 모든 Collider를 제거하며, 베이크된 프리팹도 Collider와 MonoBehaviour를 제거한다. 따라서 플레이어 이동과 NavMesh를 물리적으로 막지 않는다.

## 캡처 이슈 기록

첫 `after_final2` 실행은 에셋 재임포트/셰이더 컴파일 직후 런타임 생성 머티리얼이 검게 캡처됐다. 로그에 셰이더 오류는 없었고, 캐시 준비 후 코드 수정 없이 한 번 재실행한 `after_locked`에서는 재현되지 않았다. `after_final2`는 최종 증거로 사용하지 않는다.

## 남은 한계와 최종 판정

- 기능적 완성도: **높음** — 이동, 대화, 진열, 가격 확정, NPC 구매, 30G 증가, 정산, Day/Night 게이트가 검증됐다.
- 시각적 완성도: **중간** — 따뜻한 조명·식생·손님 스테이징은 개선됐지만 중앙 상점의 primitive 박스 실루엣이 강하다.
- 발표 가독성: **중상** — 상단 목표와 좌우 HUD는 읽히지만 좌측 컬럼이 넓고 하단 스마트폰 UI가 별도 톤이다.
- 레퍼런스 이미지와 남은 격차: 정식 상점 메시, 일관된 UI 스킨, 애니메이션·환경 밀도에서 차이가 남는다.
- 지금 상태에서 가장 큰 약점: 상점이 기능 중심 오브젝트로는 읽히지만 완성된 잡화점 건물/부스로는 충분히 읽히지 않는다.
- 사람이 확인해야 할 단 하나의 핵심: Unity Editor 실제 Game View가 Final Locked Screenshot과 동일하게 렌더링되고 한국어 폰트·UI 겹침·검은 머티리얼 재현이 없는지 1회 확인.

따라서 냉정한 최종 판정은 **조건부 발표용**이다.
