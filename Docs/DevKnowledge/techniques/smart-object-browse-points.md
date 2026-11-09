---
slug: smart-object-browse-points
title: 물건이 '여기서 할 행동'을 알려 주는 스마트 오브젝트로 손님의 머무는 자리·자세를 만든다
topic: shop-npc-movement
status: unverified
pa_fit: adapt
value: 2
applied: partial: 부분(진열대 슬롯 예약·접근점·FaceCurrentSlot) — 자세·행동 다양화는 미적용
sources: web:052fc41832d3, web:ed7544699b2d
updated: 2026-11-09
---
# 스마트 오브젝트로 머무는 자리와 자세를 만든다

## 요약
NPC가 아무 데나 서는 대신, **물건(진열대·의자·창문)이 '여기 서서 이렇게 행동하라'는 자리와 동작을 들고 있게** 하면 가게 안 움직임이 목적 있어 보인다. 같은 의자라도 NPC마다 앉는 자세가 달라 살아 있는 느낌을 준다(FFXV 사례).

## 원리
- 관찰(GDC 2018 기사, 2차): FFXV는 Meta AI → Character AI → Animation AI 3층. 세계에 스마트 오브젝트가 많고, 같은 의자에 NPC마다 편하게/격식 있게 다르게 앉는다. **고르는 방법은 기사에 없음.**
- 관찰(논문): 'Smart Areas' — 영역 단위로 일상 행동을 붙이는 모듈식 접근(초록 수준만 확인).
- 추론: 진열대마다 '서는 점 + 바라볼 점 + 머무는 시간 범위 + 동작 후보'를 두고, 손님 성격으로 동작을 고르면 같은 자리라도 다르게 보인다.

## 코드 예제(설계 스케치, 미검증)
```csharp
public sealed class BrowsePoint : MonoBehaviour {
    public Transform stand, lookAt; public Vector2 lingerSeconds = new(2f, 5f);
    public string[] gestures = { "lean", "pick-up", "tilt-head" };
    public bool Claimed { get; private set; }
    public bool TryClaim() { if (Claimed) return false; Claimed = true; return true; }
    public void Release() => Claimed = false;
}
```

## 적용 조건
- 둘러볼 대상(진열대·벽 장식)이 고정되어 있고 NPC가 거기서 머무는 게임.

## 한계
- 1차 출처(GDC 발표 영상·슬라이드)를 아직 보지 못했다 → status unverified.
- 동작 다양화는 애니메이션 자산이 필요(P.A. 모션 작업은 보류 중이라 자세 추가는 지금 범위 밖).

## P.A. 적용
- 이미 비슷한 구조: `NpcController`가 진열대 슬롯을 예약(`approach` 접근점·`_hasApproachReservation`)하고 도착하면 `FaceCurrentSlot`으로 바라본다 → 스마트 오브젝트의 '자리+바라봄'은 있음.
- 남은 것: 머무는 시간·작은 동작 다양화(모션 보류와 충돌하지 않는 고개 돌림·몸 기울임 수준) — 후속.

## 검증
- [partial] talk: https://gamegrin.com/news/gdc-2018-bringing-life-to-the-npcs-of-final-fantasy-xv-using-ai — 기사 요약: 스마트 오브젝트·의자 자세 다양화(발표 원문 미확인)
- [partial] paper: https://www.scitepress.org/Papers/2014/49211/ — 영역 기반 일상 행동(초록 수준)

## 출처
- web:052fc41832d3 GameGrin GDC 2018 기사, web:ed7544699b2d SCITEPRESS 논문
