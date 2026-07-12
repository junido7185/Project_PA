# VISUAL_POLISH_REPORT — Visual Demo Integration Pass

작성: 2026-07-12 (Claude Fable 5)
목표: 새 시스템 추가 없이, 기존 구현·기존 에셋을 연결/배치/문구 정리해서 데모 화면이 "완성된 코지 상점 게임"으로 읽히게 만든다.

## 1. 기존 에셋/기능 중 재사용한 것 (수정 없이 그대로)

| 재사용 대상 | 역할 |
|---|---|
| `PA_MarketStall_Hub_Visual` (씬 배치됨) | 줄무늬 천막·목재 기둥·랜턴·상품 궤짝·가격표 4종·간판 — 화면 중앙 포컬 포인트 |
| `B11_PlazaFountain_Static`, `ShopPlaza` (씬 배치됨) | 광장 중심 연출의 앵커. 새 벤치가 이 분수를 둘러싼다 |
| `B09_StorageShed / B10_Cottage x3 / B12_TradePort` (씬 배치됨) | 마을 배경 밀도 |
| `Assets/Resources/Items/*.asset` 아이콘 15종 | 채집 포인트 위 아이템 아이콘 빌보드로 재사용 |
| HUD 전체 (MoneyHUD/ClockHUD/목표 패널/핫바/상호작용 프롬프트/말풍선/가격 UI/정산 요약) | 전부 기존 것. 문구만 정돈 |
| `CustomerArrivalController`, `PurchaseEvaluator` 피드백, `VillageCultureVisualController`(VC-001A) | 손님 연출·구매 반응·다음날 변화 — 이미 구현돼 있어 연결 확인만 수행 |
| `PrototypeWorldLabel`, VC-001A 런타임 사이드카 패턴 | 새 드레싱 컨트롤러가 같은 패턴을 그대로 따름 |

## 2. 새로 연결한 것

- `Assets/Scripts/DemoVisualDressingController.cs` (신규 1파일) — 런타임 코지 드레싱 사이드카.
  - 채집 포인트 5곳: 단색 큐브 → 나무 궤짝 받침 + 아이템 색 작물 3구 + **실제 아이템 아이콘 빌보드**.
  - 영업 간판: 큐브 단독 → 나무 기둥 + 걸이대 + 크림색 보드 트림 + 발광 랜턴.
  - 광장: 분수 둘레 벤치 3개, 상점~분수 동선 화단 4개(꽃 3색), 가로등 2개(따뜻한 발광), 상점 옆 궤짝 더미/통.
  - 전부 렌더러 전용(콜라이더 제거) → 이동/NavMesh/상호작용/검증기 무영향.
- `PA_RuntimeSceneBinder.cs` 등록 1줄 — 씬 파일 수정 없이 자동 부착.

## 3. Placeholder 로 보완한 것

- 벤치/화단/가로등/궤짝은 기존 팔레트(천막 주황·목재 갈색·크림)에 맞춘 primitive 조합이다. Nature Pack FBX 는 Resources 밖이라 런타임 로드가 불가능해 이번 패스에서는 쓰지 않았다(대체 계획: `VISUAL_GAP_AND_PLACEHOLDER_PLAN.md`).
- 채집 포인트의 "작물"은 아이템 색 구체 + 진짜 아이콘 빌보드 조합의 placeholder다.

## 4. 화면 완성도를 올린 포인트

1. **데모 루트 위 debug 큐브 소멸** — 플레이어가 걷는 동선(채집→진열→가격→판매)에 있던 원색 큐브 6개가 전부 "게임 오브젝트"로 읽힌다.
2. **HUD 언어 통일** — "Day Prep / Shop: OPEN / prep stock" 혼용 → "1일차 07:00 · 낮 준비 / 상점: 영업 중 (손님 구매 가능) / 채집 가능" 한국어 통일. 돈·시계·목표·핫바와 같은 톤.
3. **광장 중심 구도 완성** — 분수 둘레 벤치, 동선 가장자리 화단, 저녁 가로등 → 레퍼런스 인상(광장형 코지 상점)의 골격 재현.
4. **정산 요약 잘림 수복** — Village direction 섹션 추가 후 본문(410px)이 영역(360px)을 넘던 기존 문제를 요약 상태 본문 확장(418px)으로 해결. `PA_FinalPresentationReviewer` 재통과.

## 5. 수정 파일 목록

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/DemoVisualDressingController.cs` (+`.meta`) | 신규 — 런타임 코지 드레싱 사이드카 |
| `Assets/Scripts/PA_RuntimeSceneBinder.cs` | 등록 1줄 추가 |
| `Assets/Scripts/DayNightShopLoopController.cs` | HUD 표시 문자열만 한국어화 (페이즈/게이트 로직 무변경) |
| `Assets/Scripts/DaytimeStockPrepPoint.cs` | 상호작용 프롬프트/라벨 문자열만 한국어화 |
| `Assets/Scripts/UI/PlayableDayScenarioController.cs` | 요약 상태 본문 영역만 확장 (810x360→810x418, 위치 보정) |
| `Assets/Editor/PA_DayNightShopLoopValidator.cs` | 성공 키워드 1줄 동기화 ("prepared"→"낮 준비 완료") |
| `Assembly-CSharp.csproj` | 신규 스크립트 include 1줄 |

씬/프리팹/저장 스키마/경제·NPC 코어: **무변경**.

## 6. 검증 결과 (2026-07-12, Editor 닫힘 + D3D11 batchmode)

- 컴파일: `dotnet build` 런타임/에디터 모두 0 오류 (기존 CS8785 경고만).
- `PA_DayNightShopLoopValidator` 통과 (`sellableInventory=10`) — `Logs/Fable_VisualPass_DayNightValidation.log`
- `PA_FinalDemoRouteValidator` 통과 (`stocked=BreadLoaf, paid=30G`) — `Logs/Fable_VisualPass_FinalRouteRegression.log`
- `PA_FinalPresentationReviewer` 통과 + 스크린샷 5장 — `Logs/FinalPresentation/20260712_161412/`
- `PA_GatheringShopReview` 캡처 5장 — `Logs/GatheringShopReview/20260712_161528/`
- `PA_CoreSlicePlayabilityValidator` 통과 — `Logs/Fable_VisualPass_CoreSliceRegression.log`
- `PA_LongPlayProgressionValidator` 통과 (`money=4633G` 기준선 불변) — `Logs/Fable_VisualPass_LongPlayRegression.log`
- 참고: 그동안 BLOCKED 였던 FinalDemoRoute/LongPlay 2종이 이번에 실제 실행·통과됨.

## 7. 남은 시각적 문제 (v1 기준 — v2 에서 대부분 해소, 아래 §8 참조)

1. ~~광장 전경 사람 확인 필요~~ → v2 에서 실제 플레이 카메라 캡처 툴로 대체.
2. ~~지면 갈색 맨땅~~ → v2 광장 베이스 플레이트로 해소.
3. Nature Pack 나무/수풀의 정식 배치는 씬 편집(에디터 툴) 승인 필요 — `VISUAL_GAP_AND_PLACEHOLDER_PLAN.md` 참조.
4. ClockHUD 와 페이즈 스트립의 시간 표기 이중화 — v2 에서 페이즈 스트립을 좌측 컬럼으로 이동해 겹침은 해소, 표기 통합은 후속.

---

# v2 — 실제 Game View 기준 재작업 (2026-07-12 저녁)

v1 은 검증기/마커 카메라 기준이라 실제 플레이 화면에서 불합격 판정을 받았다.
v2 의 판정 기준은 **같은 플레이 카메라·같은 해상도(2560x1440)·같은 시간대(Day 1 15:30)의 Before/After 스크린샷**이다.

## v2-1. Before/After 증거

- Before: `Logs/DemoViewShots/before_20260712_164931.png`
- After:  `Logs/DemoViewShots/after5_20260712_223953.png`
- 캡처 툴: `Assets/Editor/PA_DemoViewCapture.cs` (신규) — 온보딩 자동 종료 후 **실제 추적 카메라 그대로** UI 포함 캡처.

## v2-2. 실제로 고친 화면 문제

| Before 의 문제 | 수정 | After 에서 |
|---|---|---|
| 화면의 절반이 갈색 맨땅 (테스트맵 인상 최대 원인) | 런타임 광장 베이스 플레이트(30x30m, 석재 톤) + 판매 데크/러그/파빙/준비 매트 | 갈색 맨땅이 화면에서 사라지고 광장으로 읽힘 |
| 떠다니는 디버그 라벨 7종+ ("0. 플레이어 WASD…", "2. 판매대 슬롯 N", "P: 스마트폰", "보급품" 등 씬 저장 `Guide_*`) | `CoreSlicePresentationMode` 숨김 목록에 `Guide_` 접두사 + SUPPLY/PRICE/SALE 스테이징 라벨 추가 (F10 으로만 표시) | 상시 플로팅 텍스트가 채집 포인트 소형 라벨 2개 수준으로 감소 |
| 상단 중앙 3겹 UI (2줄 목표 + 페이즈 스트립 + 뒤의 간판) | 상단 중앙은 현재 단계 한 줄(780x44), 페이즈 스트립은 좌측 컬럼(시계 아래), **좌측 퀘스트 체크리스트 패널**(✓/▶/○ 6단계) 신설 | 좌측 정보 컬럼 + 중앙 한 줄 — 겹침 해소 |
| 핫바가 흰 박스 + 텍스트 | `Item.icon` 6종을 Free RPG Icons 스프라이트로 연결 (데이터만: 빵13/당근9/생선1/밀19/광석4/철괴5) | 핫바·쇼케이스·채집 포인트에 실제 아이콘 |
| 판매대 슬롯 = 원시 큐브 4개 | 슬롯별 나무 카운터+상판+앞면 가격판 드레싱 | 진열대 행으로 읽힘 |
| 상품이 화면에 없음 | 준비 구역 옆 쇼케이스 테이블 + 상품 5종(아이콘+작물) | 상점 앞 상품 5종 한눈에 |
| "MANAGEMENT HUB" 영어 간판 | 런타임 텍스트 교체 → "코지 잡화점" | 게임 내 간판으로 읽힘 |
| 채집 포인트 2줄 대형 라벨 + 영어 이름 | 한 줄 소형(0.95) + "텃밭 바구니/생산자 납품함" | 월드 소품 힌트 수준 |

## v2-3. 시행착오 기록 (재발 방지)

- **round 3 실패**: 바닥 구획을 `Shop.transform` 기준 방향으로 배치 → Shop 원점이 슬롯 부모라 방향이 무의미, 플레이트가 화면 밖(서쪽)에 생성됨.
- **round 4 수정**: `PlazaFrame` 실측 지오메트리 도입 — 판매대 행 방향(최원거리 슬롯 쌍) + 카운터 앞 방향(슬롯 중심→플레이어) + 기준 지면(레이캐스트, 물리 지면 y=-0.5 실측).
- **round 5 수정**: 물리 지면(-0.5)과 플레이트 두께 관계로 얇은 러그/파빙이 묻힘 → 오프셋 계층화(+0.03~0.055) 및 플레이트 깊이 24→30m(플레이어 뒤까지).
- 슬롯 필터: 거리 20m 필터는 상점 2개를 모두 포함해 데크가 거대해짐 → `shop.GetComponentsInChildren<ShopSlot>` 로 교정.

## v2-4. 검증 (2026-07-12 저녁, Editor 닫힘 + D3D11 batchmode)

- dotnet build 0 오류.
- `PA_FinalDemoRouteValidator` 통과 (`paid=30G`) — `Logs/Fable_VisualPass2_FinalRouteRegression.log`
- `PA_DayNightShopLoopValidator` 통과 (`sellableInventory=10`) — `Logs/Fable_VisualPass2_DayNightRegression.log`
- `PA_CustomerPanelLayoutValidator` 통과 (UI 이동 후 겹침 없음) — `Logs/Fable_VisualPass2_PanelLayoutRegression.log`

## v2-5. 아직 허접해 보이는 부분 (After 기준 냉정한 목록)

1. **조명이 어둑함** — 15:30 인데 화면 전반이 청회색. `DayNightVisual` 커브 조정은 시스템 수정이라 이번 범위에서 제외. 데모 리허설 시간대(오전) 선택 또는 커브 조정 검토 필요.
2. 러그/파빙 타일이 After 에서 뚜렷하게 안 읽힘 (어두운 조명 + 중앙 경로 메시에 일부 묻힘 추정) — 사람 확인 필요.
3. 소품이 전부 primitive 조합 — 나무/수풀 실모델 배치는 씬 편집 승인 필요.
4. 하단 중앙의 기존 씬 갈색 플랫폼(플레이트와 대비로 드러남) 정체 확인 필요.
5. 분수 구역(우상단)은 이번 카메라 프레임에서 부분만 보임 — 벤치 배치 품질은 사람 확인.
