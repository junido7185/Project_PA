# Project P.A. Architecture Decisions

최종 갱신: 2026-07-15

## ADR-001 — 실내 잡화점 해금은 Tier에서 파생한다

- 상태: 적용
- 결정: 별도 `interiorShopUnlocked` 저장 필드를 만들지 않고 `TierService.CurrentTier >= 1`을 실내 잡화점 해금의 원본으로 사용한다.
- 이유: Tier는 이미 v9 저장/로드 대상이며 단조 증가한다. 중복 저장 플래그는 불일치와 마이그레이션 위험만 늘린다.
- 결과: 로드 시 `ForceSetTier` 후 S4 프레젠테이션이 문·간판 상태를 조용히 재구성한다.

## ADR-002 — 기존 문 워프에 선택적 Tier 게이트를 추가한다

- 상태: 적용
- 결정: `BuildingEntrance`의 기존 워프를 교체하지 않고 기본값 0인 선택적 `requiredTier` 가드를 추가한다.
- 이유: 기존 외부/실내 양방향 워프, 페이드, 카메라 스냅을 그대로 보존하면서 Tier 0 조기 진입만 막을 수 있다.
- 결과: 다른 건물 문은 기존 동작 그대로이며, S4 컨트롤러가 외부 잡화점 문에만 런타임으로 Tier 1 요구를 설정한다.

## ADR-003 — 상점 진화는 런타임 사이드카로 연출한다

- 상태: 적용
- 결정: 메인 씬을 다시 저장하지 않고 `ShopEvolutionController`가 기존 `PA_StoreDoor_Out`과 `TierService.OnTierAdvanced`를 연결한다.
- 이유: 이미 검증된 실내 공간·Shop·슬롯·손님 FSM과 씬 직렬화를 보존한다.
- 결과: Tier 0 잠금 안내, Tier 1 간판/문 조명, 비차단 해금 패널만 추가된다. 경제·구매·NPC 판단은 변경하지 않는다.

## 보존 결정

- `PA_ShopLocator`의 지상/실내 Shop 구분 정책 유지.
- Day 1 튜토리얼의 상점 구매 예외 유지.
- 실내 손님은 `InteriorCustomerController`와 기존 `NpcController` 쇼핑 FSM을 계속 사용.
- 저장 스키마 v9, 메인 씬, 프리팹, 외부 패키지는 S4에서 변경하지 않음.
