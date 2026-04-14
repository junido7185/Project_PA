using System.Collections.Generic;
using UnityEngine;

// NPC 대사 풀 — ScriptableObject.
// Docs/03 §2.2 "MBTI T/F 축은 MVP 에서 대사 톤 분기에 사용" 을 구현하는 최소 단위.
//
// 사용 방식:
//   Assets > Create > P.A. System > Dialogue Data 로 에셋을 만들고
//   주제(topic) 별로 T형(논리)/F형(감성) 대사를 각각 여러 개씩 등록한다.
//   동일 에셋을 여러 NPC 가 공유해도 되고, "이 NPC 전용 대사" 를 위해 전용 에셋을 만들어도 된다.
//
// 설계 의도:
// - 대사는 코드가 아니라 데이터로 관리한다 → 기획자·작가가 코드를 건드리지 않고 대사 추가 가능.
// - "주제(DialogueTopic)" 는 상황 분기 키. 인사/잡담/상점/경제/친밀도/계절 등을 구분해 호출자가 원하는 풀만 꺼낼 수 있다.
// - T/F 분기는 "하나의 topic 에 두 개의 풀" 구조 — NpcProfile.traitTF 양/음에 따라 DialogueService 가 자동 선택.
// - 배열이 비어 있으면 반대 톤 풀에서 대체 선택한다 (DialogueService 쪽에서 처리).
// - 다국어 확장은 이번 스프린트 밖. 추후 `DialogueLocale` 필드 + 테이블로 확장 가능.

// 대사 주제 분류.
// 호출자 예:
//   - NpcDialogue.Interact → Greeting
//   - ShopSlot 구매 성공 → ShopBought
//   - FriendshipService 친밀도 상승 → FriendshipUp
//   - AuditService 감사 통과 → AuditPass 등 (향후)
public enum DialogueTopic
{
    Greeting,      // 처음 인사 / 재회 인사
    SmallTalk,     // 일상 잡담 (날씨·기분)
    ShopBrowse,    // 상점 진열대를 둘러보며 (내심)
    ShopBought,    // 구매 성공 직후 소감
    ShopTooExpensive, // "너무 비싸" 패스 직후
    FriendshipUp,  // 친밀도 단계 상승
    Economy,       // 마을 경제/티어 관련
    Idle           // 기본값 — 주제를 특정하지 않은 호출
}

[CreateAssetMenu(fileName = "New Dialogue Data", menuName = "P.A. System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("메타")]
    [Tooltip("이 에셋이 어떤 NPC/상황을 위한 것인지 메모. 런타임 로직에는 사용되지 않음.")]
    public string label = "공용 대사";

    [Header("주제별 대사 묶음 (T형/F형 각각)")]
    public List<TopicDialoguePool> topics = new List<TopicDialoguePool>();

    /// <summary>주어진 topic 의 풀을 반환한다. 없으면 null.</summary>
    public TopicDialoguePool FindTopic(DialogueTopic topic)
    {
        if (topics == null) return null;
        foreach (var pool in topics)
            if (pool != null && pool.topic == topic) return pool;
        return null;
    }
}

// 한 주제의 T/F 대사 풀.
[System.Serializable]
public class TopicDialoguePool
{
    public DialogueTopic topic;

    [Tooltip("T형(논리·건조) 대사 후보. traitTF < 0 인 NPC 가 우선 선택한다.")]
    [TextArea(1, 3)]
    public List<string> thinkingLines = new List<string>();

    [Tooltip("F형(감성·친근) 대사 후보. traitTF > 0 인 NPC 가 우선 선택한다.")]
    [TextArea(1, 3)]
    public List<string> feelingLines = new List<string>();
}
