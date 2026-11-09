---
slug: slot-based-queue
title: 줄 서기는 회피에 맡기지 말고 '줄 칸' 목적지를 순번으로 나눠 준다
topic: shop-npc-movement
status: unverified
pa_fit: adapt
value: 1
applied: no
sources: web:a6b974a9164f, web:8e3311ccfd83, web:535592c103cf
updated: 2026-11-09
---
# 줄 서기는 '줄 칸' 목적지를 순번으로 나눠 준다

## 요약
계산대·인기 진열대 앞에 손님이 몰릴 때 모두 같은 목적지로 보내면 회피끼리 밀고 맴돈다. 줄 방향과 간격을 정해 **i번째 손님 목적지 = 줄 머리 + 방향 × 간격 × i** 로 주고, 앞사람이 빠지면 모두 한 칸씩 당긴다.

## 원리
- 관찰(Roblox 포럼, 엔진 다름): 에이전트 표와 축 방향 고정 간격, 인덱스에서 목적지를 다시 계산하는 함수로 줄을 만든다.
- 관찰(Unity 포럼, 2차): 좁은 문에서 같은 우선순위끼리 멈춤 → 한쪽 우선순위를 올리라는 답. 한 지점 주위 군중은 속도를 조금씩 다르게 하고 미리 간격을 두면 간단히 보기 좋다. 점유 검사로 자리를 찾게 했더니 제자리에서 서로 맴돌았다는 보고.
- 추론: 줄 칸 목적지는 회피를 '마지막 보정'으로만 쓰게 해 밀림·맴돌기를 줄인다. 공식 문서 근거는 없다(설계 패턴).

## 코드 예제(설계 스케치)
```csharp
public sealed class QueueLine : MonoBehaviour {
    public Transform head; public float spacing = .9f; readonly List<NavMeshAgent> _line = new();
    public void Join(NavMeshAgent a) { _line.Add(a); Refresh(); }
    public void Leave(NavMeshAgent a) { _line.Remove(a); Refresh(); }
    void Refresh() { for (int i = 0; i < _line.Count; i++) _line[i].SetDestination(head.position - head.forward * spacing * i); }
}
```

## 적용 조건 / 한계
- 고정된 줄 자리(계산대)가 있을 때. 줄이 통로를 막지 않게 방향을 정해야 한다.
- 검증: 공식 문서·공개 Unity 코드로 확인하지 못함 → status unverified.

## P.A. 적용
- 지금 P.A. 손님은 진열대 슬롯에서 바로 사고(계산대 없음) 슬롯 예약으로 한 자리 한 명이라 줄이 생기지 않는다. 계산대·인기 진열대 대기를 넣을 때 후보.

## 검증
- [not_found] official_doc: https://docs.unity.com/en-us/engine/current/script-reference/unityengine/ai/navmeshagent/avoidancepriority — 줄 서기 패턴은 공식 API 문서 범위 밖(우선순위 의미만 확인)

## 출처
- web:a6b974a9164f Roblox devforum, web:8e3311ccfd83·web:535592c103cf Unity 포럼
