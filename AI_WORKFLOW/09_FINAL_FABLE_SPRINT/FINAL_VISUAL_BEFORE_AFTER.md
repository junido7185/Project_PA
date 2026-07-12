# FINAL_VISUAL_BEFORE_AFTER — 실제 Game View 비교

작성: 2026-07-13 (Codex)

## 캡처 경로

| 단계 | 경로 |
|---|---|
| Before | `Logs/DemoViewShots/before_20260712_164931.png` |
| v2 After | `Logs/DemoViewShots/after5_20260712_223953.png` |
| 조명 보정 | `Logs/DemoViewShots/after6_lighting_20260713_000017.png` |
| 실모델 적용 | `Logs/DemoViewShots/after7_assets_20260713_000527.png` |
| 광장/NPC 좌표 | `Logs/DemoViewShots/after8_20260713_000847.png` |
| HUD 보정 전 | `Logs/DemoViewShots/after_final_20260713_001146.png` |
| 실패 캡처(사용 금지) | `Logs/DemoViewShots/after_final2_20260713_002245.png` |
| **Final Locked** | `Logs/DemoViewShots/after_locked_20260713_002356.png` |

Before부터 Final Locked까지 실제 추적 카메라, UI 포함, 2560×1440을 유지했다. v3 캡처는 Day 1 15:42로 고정됐다.

## 실제 변화

- 조명: 청회색·어두운 15시 화면에서 크림/주황 계열의 따뜻한 오후로 바뀌었다. 그림자는 유지돼 깊이가 사라지지 않았다.
- 바닥·광장: 갈색 맨땅 비중을 광장 플레이트와 경로 구획으로 줄였고, 화단·가로등·식생을 실제 카메라 안으로 이동했다.
- 상점: 슬롯 행·쇼케이스·가격판·소품 밀도가 증가했다. 다만 중앙의 큰 직육면체 실루엣은 아직 primitive로 읽힌다.
- NPC: Final Locked에서 NPC 2명이 카운터 전면에 서서 손님 대기/쇼핑 장면으로 읽힌다.
- UI: ClockHUD 폭을 340으로 맞추고 좌우 HUD 투명도를 통일했다. 화면 중앙을 가리는 모달은 없다.
- 실모델: 나무·수풀·꽃·풀·밀·바위·통나무·그루터기·개구리 의자가 primitive 일부를 대체했다.

## 아직 남은 차이

- 정식 상점 건물/부스 메시와 애니메이션이 없어 레퍼런스의 완제품 인상에는 못 미친다.
- 좌측 정보 컬럼은 화면 면적을 많이 차지한다.
- 하단 분홍 스마트폰 UI는 목재·크림 HUD 팔레트와 일치하지 않는다.
- 식생은 배치됐지만 환경 전체의 밀도와 레이어링은 제한적이다.
