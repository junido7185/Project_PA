# UNITY_CODING_RULES — Unity C# 안전 규칙

작성: 2026-07-09
적용 대상: Project P.A.의 모든 C# 코드 작업. Unity 6000.3.2f1 / URP.

## 1. 직렬화·참조 보존 (가장 중요)

Unity에서 코드만 보고 안전하다고 판단하면 안 된다. 씬·프리팹·에셋이 코드에 바인딩되어 있다.

- **public/`[SerializeField]` 필드의 이름·타입 변경 금지.** 이름을 바꾸면 씬/프리팹에 저장된 값과 Inspector 연결이 조용히 끊긴다. 부득이하면 `[FormerlySerializedAs]`를 쓰되 사람 승인 후.
- **Inspector 연결 보존.** 기존 필드에 연결된 참조를 코드 쪽에서 제거·이름 변경하지 않는다.
- **씬 참조 보존.** 씬에 배치된 GameObject 이름·계층을 코드가 `Find`로 찾고 있는 경우가 있다 — 이름 기반 검색 대상은 변경 전에 전수 조사한다.
- **프리팹 참조 보존.** 프리팹이 쓰는 컴포넌트를 삭제·개명하지 않는다.
- 필드 삭제가 필요한 경우: 삭제 대신 `[Obsolete]` 표시 + 사람 승인 후 별도 태스크로 제거.

## 2. 임의 재작성 금지 시스템

아래 파일·시스템은 **수정 최소화, 재작성 금지**. 넓은 변경은 사람 승인 필요:

- `SaveManager` — 저장 스키마는 추가 확장만 (v7→v8 패턴처럼 버전 증가 + 마이그레이션). 필드 제거·의미 변경 금지
- `PlayerController` / `PlayerInteraction` / `PlayerInputHandler`
- `Inventory` (인벤토리/핫바 계열)
- `Shop` / `ShopSlot` / `ShopPriceUI` / `EconomyService` / `PurchaseEvaluator`
- `NpcController` 와 NPC FSM/스케줄 계열
- 씬 파일 (`Assets/Scenes/*.unity`) — 코드로도, 에디터 스크립트로도 임의 재작성 금지
- 메인 씬 경로: `Assets/Scenes/Prototype_FirstDay.unity` — Build Settings의 시작 씬. **덮어쓰기·교체 금지.** 새 씬이 필요하면 사용자 승인 후 별도 파일로.

## 3. 안전한 확장 패턴 (권장)

이 프로젝트에서 검증된 방식을 따른다:

- **런타임 사이드카**: 씬 직렬화를 건드리지 않고 런타임에 GameObject/컴포넌트를 생성·바인딩 (`PA_RuntimeSceneBinder`, `VillageCultureVisualController` 방식). 씬 참조 깨짐 위험이 없다.
- **읽기 전용 프레젠테이션 레이어**: 기존 판단 로직(예: `PurchaseEvaluator`)을 바꾸지 않고 결과를 보여주는 레이어만 추가 (SPY-002 방식).
- **기존 데이터 재사용**: 새 ItemData/프로필을 만들기 전에 기존 에셋으로 가능한지 먼저 검토 (IL-001 방식).
- **가드 1블록 추가**: 흐름 변경이 필요하면 기존 메서드를 갈아엎지 말고 진입점에 최소 가드만 추가.

## 4. 외부 의존성

- **외부 패키지·에셋 추가 금지** (DOTween, Odin, Joystick Pack 포함 — Docs/06 §3.2의 권장은 무효).
- `Packages/manifest.json` 수정 금지.
- Project_D(참조 프로토타입)에서 어떤 것도 복사·이식 금지.
- `Assets/Jinxish/` 등 기존 서드파티 에셋 내부 수정 금지.

## 5. 에디터/검증 도구

- 에디터 전용 코드는 `Assets/Editor/`에, `PA_` 접두사 관례를 따른다.
- 검증기는 `Automation/LoopEngineering/validator-registry.json`에 등록된 것을 우선 사용.
- **Unity Editor가 열려 있으면 같은 프로젝트에 batchmode 실행 금지.**
- 자동 Unity 실행은 D3D11 baseline(`-force-d3d11`)에서만. D3D12는 미승인 (근거: 루트 최신 크래시 리포트).

## 6. 코드 스타일 (프로젝트 관례)

- 주석은 한국어로, 설계 근거는 `Docs/§번호` 인용 형식 (예: `// Docs/03 §2.2 "MBTI T/F 축..."`).
- 로그 메시지에 이모지 접두사 관례 유지 (예: `Debug.Log("🚪 ...")`).
- 새 파일은 기존 파일의 네이밍(`PA_` 접두사는 에디터/바인더 계열)과 폴더 배치를 따른다.
- `.meta` 파일은 Unity가 생성하게 둔다. 스크립트를 새로 만들면 Unity가 meta를 만들 때까지 컴파일 검증(`dotnet build`)에 포함되지 않을 수 있음을 감안한다.

## 7. 컴파일 확인 방법

- 로컬 빠른 확인: `dotnet build Assembly-CSharp.csproj` (0 warnings / 0 errors 기준).
- Editor가 닫혀 있으면 Unity batchmode 컴파일 가능. 열려 있으면 Editor Console 확인을 요청하거나 보류 문서화.
