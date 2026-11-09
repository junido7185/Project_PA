---
slug: navmesh-avoidance-prediction-time
title: 좁은 실내에서는 NavMesh 회피 예측 시간을 줄인다 (NavMesh.avoidancePredictionTime)
topic: shop-npc-movement
status: verified
pa_fit: direct
value: 1
applied: no: A/B(r185, 같은 빌드 2회씩) 개선 없음 — P.A. 기본 2.0 유지
sources: yt:dHYcio6fRI4, web:96eea6f11bef, web:824534fa83d8
updated: 2026-11-09
---
# 좁은 실내에서는 NavMesh 회피 예측 시간을 줄인다

## 요약
`NavMesh.avoidancePredictionTime`은 모든 NavMeshAgent가 **몇 초 뒤의 충돌까지 미리 피할지**를 정하는 전역 값이다(기본 2.0초). 클수록 멀리서부터 비켜 가서, 걷는 속도 2m/s면 약 4m 앞 상대를 보고 경로를 휜다. 10m 남짓한 가게 안에서는 방 반대편 손님 때문에 미리 꺾거나 서로 양보하다 머뭇거리는 "길을 막는" 움직임이 생길 수 있다. 값을 줄이면 가까워졌을 때만 비켜 간다.

## 원리 (검증된 사실 / 추론 구분)
- 사실(공식): 값이 클수록 충돌 경로의 에이전트가 더 일찍 피하기 시작한다. 단위 초, 기본 2.0, 조정 범위 0.5~5.0.
- 사실(영상 프레임): LlamAcademy의 회피 심층 영상 핵심 1번이 "LOWER avoidance time is typically better". 100명 원 시나리오 화면에 'Avoidance time 2 / Iterations 100 / Quality High' 표시.
- 사실(공개 코드): 같은 영상의 데모(MIT)가 이 값을 `Update()`에서 전역으로 넣는다 — 씬 전체에 한 번에 적용되는 값이다.
- 사실(공개 코드, 원리 근거): RVO2(상호 속도 장애물, Apache-2.0)의 같은 개념 `timeHorizon` 설명 — "클수록 더 일찍 다른 에이전트에 반응하지만, 속도를 고를 자유가 줄어든다". Unity 내부 회피가 RVO2와 같은 구현이라는 공식 근거는 없으므로 **원리 비유**로만 쓴다.
- 추론: 예측 시간이 짧을수록 회피가 늦고 급해질 수 있다. 빠른 에이전트·넓은 공간에서는 너무 줄이면 마지막 순간 급회피·겹침이 늘 수 있다. → 실내 측정으로 정한다.

## 코드 예제
```csharp
// 씬이 NavMesh를 만든 뒤 한 번(전역 static). 실내 위주 장면이면 0.5~1초부터 시험.
UnityEngine.AI.NavMesh.avoidancePredictionTime = 1f;
```

## 적용 조건
- 에이전트가 좁은 공간(가게·실내)에서 서로 스치는 장면. 속도 2m/s 안팎.
- 전역 값이라 야외 동행·관광객 등 **모든 에이전트**가 함께 바뀐다 → 야외 동선 회귀를 함께 본다.

## 한계
- 회피 알고리즘 자체(속도 표본 수·품질)는 바꾸지 않는다. 품질은 `ObstacleAvoidanceType`, 우선순위는 `avoidancePriority`.
- 영상의 나머지 핵심 4가지는 음성/자막 분석을 못 해 미확인(도구 설치 거절).

## P.A. 적용
- 현재: 게임 코드 어디에서도 설정하지 않음 → 기본 2.0초. 손님 속도는 원본 에이전트 값(기본 2m/s), 반경 `NpcPresentationNormalizer.AgentRadius`, 품질 High, 우선순위 35+순번%20.
- 측정 장치: `ExhibitionCaptureDirector` market 플랜 `CustomerFlow`(가게 안 꺾임 각속도 turnRate, 머뭇거림 hesitateSec, 멈춤 stuckSec, 0.6m 겹침 crowdPairs, 벽 통과·순간이동)로 같은 빌드에서 `-pa-qa-avoid-time 2 / 1 / 0.5` 비교.

## 실제 적용 측정 (2026-11-09, build r185, market 플랜, 손님 7·가게 안 이동 약 58초, 같은 저장·같은 판매 5건 296G)
| 예측 시간 | 꺾임 각속도(도/초) | 머뭇거림(초) | 멈춤(초) | 0.6m 겹침 | 문 통과 |
|---|---|---|---|---|---|
| 2.0 (기본) | 58 / 59 | 3.2 / 3.0 | 0.2 / 0.2 | 0 / 0 | 14 / 14 |
| 1.0 | 63 / 63 | 3.3 / 2.9 | 0.2 / 0.2 | 0 / 1 | 14 / 14 |
| 0.5 | 61 / 74 | 3.1 / 3.4 | 0.9 / 0.2 (최대 0.7) | 0 / 0 | 14 / 16 |

판정: 이 밀도(손님 7, 슬롯 예약으로 한 자리 한 명)에서는 줄여도 좋아지지 않았고 꺾임·멈춤이 조금 늘었다. 꺾임 대부분은 회피가 아니라 진열대 접근 경로의 모서리에서 나온다고 추정. **P.A.는 기본 2.0 유지**. 손님이 늘어 가게 안에서 마주치는 일이 잦아지면 다시 잰다(QA 인자 `-pa-qa-avoid-time <초>` 그대로 남김). 교훈: 영상의 "대체로 낮을수록 좋다"는 100명 밀집 시나리오 기준이라 저밀도 실내에는 그대로 옮기지 않는다.

## 검증
- [confirmed] official_doc: https://docs.unity.com/en-us/engine/current/script-reference/unityengine/ai/navmesh/avoidancepredictiontime — 기본 2.0초, 클수록 일찍 피함, 권장 조정 범위 0.5~5.0
- [confirmed] source_code: https://github.com/llamacademy/ai-series-part-32 — ScenarioManager.Update()에서 NavMesh.avoidancePredictionTime 전역 설정(데모 기본 2)
- [partial] source_code: https://github.com/snape/RVO2 — RVOSimulator.h timeHorizon: 클수록 일찍 반응·속도 선택 자유 감소(같은 원리, 다른 구현)
- [partial] talk: https://youtu.be/dHYcio6fRI4 — 영상 프레임의 핵심 1번 "lower avoidance time is typically better"(이유 설명은 음성이라 미확인)

## 출처
- yt:dHYcio6fRI4 LlamAcademy, "NavMeshAgent Avoidance in Depth - 5 Key Takeaways" (프레임 4장 분석)
- web:96eea6f11bef 공개 소스(MIT), web:824534fa83d8 Unity 공식 API
