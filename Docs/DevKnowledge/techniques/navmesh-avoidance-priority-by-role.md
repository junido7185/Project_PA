---
slug: navmesh-avoidance-priority-by-role
title: 회피 우선순위를 서로 다르게, 서 있는 NPC를 더 중요하게 (NavMeshAgent.avoidancePriority)
topic: shop-npc-movement
status: verified
pa_fit: direct
value: 2
applied: partial: 일부 적용됨(손님 35+순번%20, 동행 60, 대화 중 0) — 상태별(둘러보기 중) 우선순위는 미적용
sources: web:55a5d1870cbd, yt:dHYcio6fRI4, web:96eea6f11bef, web:557f43bede3a, web:8e3311ccfd83
updated: 2026-11-09
---
# 회피 우선순위를 서로 다르게, 서 있는 NPC를 더 중요하게

## 요약
같은 우선순위끼리는 서로를 똑같이 피하려 해 밀어내고 떨린다. 우선순위를 **사람마다 다르게** 두면 한쪽이 길을 내준다. 한 걸음 더: 진열대 앞에 서서 보는 손님·대화 중인 주민처럼 **멈춘 NPC를 더 중요(낮은 숫자)** 하게 두면 걷는 손님이 그 주위를 돌아가고, 서 있는 사람이 밀려나지 않는다.

## 원리
- 사실(공식): 범위 0~99, 0이 가장 중요, 기본 50. 회피할 때 자기보다 우선순위가 낮은(숫자가 큰) 에이전트는 무시한다 → 숫자가 작은 쪽은 비키지 않고, 큰 쪽이 비킨다.
- 사실(공개 코드): 회피 데모(MIT)에 `RandomizePriority`면 `avoidancePriority = Random.Range(0, 100)` 옵션.
- 관찰(포럼, 2차): ~150명이 한 점으로 갈 때 서로 밀어냄 → 같은 우선순위가 원인이라는 답, stoppingDistance도 확인하라는 답.
- 관찰(포럼, 2차, 2라운드 통합): 좁은 문에서 같은 우선순위 둘이 마주쳐 멈춤 → 한쪽 우선순위를 올리라는 답(같은 기법으로 합침).
- 추론: 멈춘 NPC는 회피 속도를 내지 못하므로(속도 0) 걷는 쪽이 피하는 게 자연스럽다 → 상태가 '머무름'일 때 숫자를 낮춘다.

## 코드 예제
```csharp
// 사람마다 다른 기본값(겹치지 않게), 머무는 동안은 더 중요하게.
agent.avoidancePriority = 35 + sequence % 20;          // 걷는 손님
void OnBrowseStart() => agent.avoidancePriority = 20;   // 진열대 앞에 서서 봄
void OnBrowseEnd()   => agent.avoidancePriority = 35 + sequence % 20;
```

## 적용 조건
- 같은 종류 NPC가 여럿 섞이는 실내. 서 있는 시간이 긴 행동(구경·대화·줄서기)이 있을 때 효과가 크다.

## 한계
- 우선순위가 너무 낮은(중요한) NPC가 좁은 문에 서 있으면 다른 에이전트가 영영 못 지나갈 수 있다 → 머무는 자리를 통로 밖에 둔다.
- 회피는 국소적이라 경로 자체를 바꾸지는 않는다(경로를 돌리려면 carving, 별도 노트).

## P.A. 적용
- 이미 적용: `CustomerArrivalController.ConfigureTouristAgent` 우선순위 35+순번%20(서로 다름), `DemoCompanionRoutine` 동행 60·대화 중 0(멈춘 쪽 가장 중요). 2026-11-04 측정에서 겹침 0·멈춤 0.2초.
- 남은 후보: `NpcController` 진열대 앞 둘러보기(`_arrivedAtSlot`) 동안 우선순위 낮추기 — 현재 손님 수(가게당 몇 명)와 측정 수치가 이미 낮아 효과 측정 뒤 결정.

## 검증
- [confirmed] official_doc: https://docs.unity.com/en-us/engine/current/script-reference/unityengine/ai/navmeshagent/avoidancepriority — 0이 가장 중요, 기본 50, 회피 때 우선순위 낮은 에이전트는 무시
- [confirmed] source_code: https://github.com/llamacademy/ai-series-part-32 — RandomizePriority 옵션으로 0~99 무작위 우선순위
- [partial] talk: https://forum.unity.com/threads/navmeshagents-are-pushing-eachother.356405/ — 같은 우선순위끼리 밀어낸다는 사용자 답(2차, 공식 아님)

## 출처
- web:55a5d1870cbd Unity 포럼, yt:dHYcio6fRI4·web:96eea6f11bef LlamAcademy 영상·코드, web:557f43bede3a Unity 공식 API
