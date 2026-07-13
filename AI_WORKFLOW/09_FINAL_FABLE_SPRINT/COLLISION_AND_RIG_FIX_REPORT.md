# COLLISION_AND_RIG_FIX_REPORT — 접지/충돌/NPC 정비

작성: 2026-07-13 (Fable 5), 커밋 `f3ef51a`
근거: `PA_PhysicsAudit` 실측 (`Logs/Fable_VS_PhysicsAudit.log` → 수정 후 `Fable_VS_PhysicsAudit2.log`)

## 1. 원인 실측 (수정 전)

| 문제 | 실측 |
|---|---|
| 길 위 파묻힘 | 물리 지면(Ground MeshCollider) y=-0.5, 길 비주얼(Road_NS/EW y=0.03, Pavement_ShopPlaza y=0.04)은 콜라이더 없이 떠 있음 → 캐릭터는 -0.5에 서고 길 상면은 +0.055 → **0.48m 파묻힘** |
| 발 파묻힘(리깅 오프셋) | PlayerModel_C01 발(렌더러 최저점)이 캡슐 바닥보다 **0.15 아래** (localY -0.005) |
| 투명 벽 | 건물 BoxCollider 8개가 비주얼보다 과대 — 최악 B12_TradePort **10.0m vs 실제 3.6m** (좌우 3.2m씩 투명 벽) |
| NPC 파고듦 | 전 NPC stoppingDistance **0.2** → 카운터/서로에게 밀착 |

## 2. 적용한 수정 (`PA_VerticalSliceFixer`, 씬 백업 후 저장)

1. **길 정렬**: Road_NS/EW → y=-0.47, Pavement_ShopPlaza → y=-0.46 (지면 -0.5 바로 위, 절대 좌표라 멱등). 콜라이더가 없는 비주얼이므로 NavMesh 무영향.
2. **플레이어 발 정렬**: PlayerModel_C01 localY -0.005 → **+0.17** — 발이 캡슐 바닥 +0.02 지점. 수정 후 실측 feetY=0.175 (캡슐 바닥 0.153).
3. **콜라이더 축소 8개**: 각 메시의 **로컬 bounds 8모서리**를 메시→월드→콜라이더 로컬로 변환해 타이트한 로컬 AABB로 재설정. 회전 건물에서 월드 AABB 재변환이 이중으로 부풀던 1차 시도 실패를 교정했고, "축별로 절대 키우지 않음" 가드 추가. 대상: B10_Cottage_01/02/03/Static, B09_StorageShed(+Static), B12_TradePort(+Static). 수정 후 감사 **flagged=0**.
4. **NPC 정지 거리**: 8명 전원 stoppingDistance 0.2 → **0.75**.
5. **NavMesh 리베이크**: 콜라이더 축소로 풀린 보행 영역 반영. 수정 후 런타임 `agents=8/8, onMesh=8`, "Failed to create agent" 0건.

## 3. 검증

- 재감사: 파묻힘 원인 3종 모두 해소, 과대 콜라이더 0개.
- 회귀: FinalRoute(`paid=30G`) / DayNight(`sellableInventory=10`) / CoreSlice / SaveRoundTrip(v9) 전부 PASS — `Logs/Fable_VS_*.log`.
- Before/After (동일 플레이 카메라 2560x1440): `Logs/DemoViewShots/before_vslice_20260713_111149.png` → `after_vslice_20260713_112849.png` — 캐릭터가 길 위에 온전히 서고, 손님 2명이 카운터 앞에 머문다.

## 4. 남은 문제 (정직하게)

1. **보행 애니메이션 질감**: 접지/발 정렬은 수정했지만 Blend Tree·이동속도-애니메이션 비례·발 미끄러짐은 batch에서 판정 불가 — **사람이 에디터에서 걸어보고 판단 필요** (PlayerAnimator/C-01Avatar Humanoid 정상, applyRootMotion=false, PlayerFootIkStabilizer 존재).
2. NPC "둘러봄" 연출은 기존 FSM(Browsing) + 스테이징 수준 — 시선 처리(look-at)나 대기 모션 다양화는 미착수.
3. 길이 지면과 플러시가 되며 흙과의 명도 차가 줄어 경로 가독성이 소폭 감소 — 파버 톤 조정 후보.
4. 실내 상점 벽/바닥은 아직 primitive 구성 (기능 우선).
