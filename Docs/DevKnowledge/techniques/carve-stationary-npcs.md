---
slug: carve-stationary-npcs
title: 오래 서 있는 NPC는 잠시 NavMesh에 구멍을 깎아 다른 NPC가 경로부터 돌아가게 (NavMeshObstacle carving)
topic: shop-npc-movement
status: partial
pa_fit: adapt
value: 1
applied: no
sources: web:15c5f04aa039, web:522e0a53e340
updated: 2026-11-09
---
# 오래 서 있는 NPC는 잠시 NavMesh에 구멍을 깎는다

## 요약
회피(avoidance)는 부딪히기 직전에만 비키는 국소 반응이라, 진열대 앞에 오래 서 있는 손님 뒤로 다른 손님이 몰리면 비비거나 멈춘다. 서 있는 동안 그 자리를 **NavMeshObstacle(carve)** 로 바꾸면 다른 NPC는 처음부터 그 자리를 피해 **경로를 짠다**.

## 원리
- 사실(공식, AI Navigation 2.0): 깎기 켠 장애물은 정지해 있을 때 NavMesh에 구멍을 내고 경로 탐색이 그 주위로 돈다. 움직이는 동안은 구멍이 없고 에이전트는 충돌 회피로만 피한다. Carve Only Stationary가 성능상 보통 가장 좋다. 장애물 변경과 NavMesh 반영 사이에 한 프레임 지연.
- 관찰(Unreal 포럼, 2차 — 엔진 다름): 멈춘 에이전트 자리에 임시 구멍을 내 다른 에이전트가 경로로 돌아가게 하자는 답.
- 추론(미검증): 같은 오브젝트에서 NavMeshAgent와 깎는 장애물을 함께 켜면 자기 구멍에 갇힐 수 있어, 서 있는 동안 에이전트를 끄고 장애물을 켜는 식으로 **번갈아** 써야 한다. 공식 문서에서 이 경고를 직접 확인하지는 못했다.

## 코드 예제
```csharp
// 둘러보기 시작: 에이전트 끄고 장애물 켬(깎기). 끝: 반대로. 위치는 그대로.
void BeginLinger() { agent.enabled = false; obstacle.carving = true; obstacle.carveOnlyStationary = true; obstacle.enabled = true; }
void EndLinger()   { obstacle.enabled = false; agent.enabled = true; agent.Warp(transform.position); }
```

## 적용 조건
- 한 자리에 수 초 이상 머무는 NPC가 통로를 막는 장면. 머무름이 짧거나 잦으면 깎기 갱신 비용·한 프레임 지연 때문에 오히려 튈 수 있다.

## 한계
- 에이전트 끄고 켤 때 위치 동기화(`Warp`)와 경로 재요청이 필요. 다른 NPC 경로가 자주 다시 계산된다.
- P.A.는 섹터별 NavMesh를 2m 칸으로 굽고 장애물을 무시해 굽지만, 깎는 장애물은 실행 중에도 구멍을 낸다(가게 앞벽 막기에 이미 사용).

## P.A. 적용
- 지금 가게 손님 수·측정(멈춤 0.2초, 겹침 0)으로는 필요성이 낮다. 손님을 늘리거나 줄서기를 넣을 때 다시 검토.

## 검증
- [confirmed] official_doc: https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AboutObstacles.html — 정지 시 구멍·이동 중 회피만·Carve Only Stationary 성능·한 프레임 지연
- [not_found] official_doc: https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshObstacle.html — 같은 오브젝트의 에이전트와 깎는 장애물 동시 사용 경고는 이 페이지에 없음

## 출처
- web:15c5f04aa039 bugnet 블로그(2차), Unreal 포럼 답(검색 요약, 2차), web:522e0a53e340 Unity 공식 매뉴얼
