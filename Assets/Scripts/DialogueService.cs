using UnityEngine;

// 대사 선택 단일 진입점 — 순수 헬퍼.
//
// 호출자는 "이 NPC(profile), 이 DialogueData, 이 topic" 만 넘기면
// 적절한 T/F 톤의 라인 하나를 돌려받는다.
//
// 톤 선택 규칙 (Docs/03 §2.2):
//   traitTF <= -0.2  → Thinking(T) 풀 우선
//   traitTF >=  0.2  → Feeling(F) 풀 우선
//   그 외 중립       → 두 풀을 합친 전체에서 선택
//   선호 풀이 비어 있으면 자동으로 반대 풀로 폴백.
//
// 결정론 원칙:
// - UnityEngine.Random 을 사용하지 않고 호출자가 넘긴 System.Random 을 사용한다.
// - rng 가 null 이면 단발성 System.Random() 을 만들어 사용 (대사 톤은 게임 상태에 영향 없음).
public static class DialogueService
{
    // 톤 분기 경계. 절대값이 이 이상이면 한쪽 풀만 본다.
    private const float TraitTFBoundary = 0.2f;

    /// <summary>
    /// profile 의 MBTI T/F 성향에 따라 data 의 topic 풀에서 대사 한 줄을 반환한다.
    /// 매칭되는 대사가 하나도 없으면 공란("") 을 반환한다.
    /// </summary>
    public static string GetLineFor(NpcProfile profile, DialogueData data, DialogueTopic topic, System.Random rng = null)
    {
        if (data == null) return string.Empty;

        TopicDialoguePool pool = data.FindTopic(topic);
        if (pool == null) return string.Empty;

        float tf = profile != null ? profile.traitTF : 0f;
        var thinking = pool.thinkingLines;
        var feeling  = pool.feelingLines;

        bool thinkingEmpty = thinking == null || thinking.Count == 0;
        bool feelingEmpty  = feeling  == null || feeling.Count  == 0;

        if (thinkingEmpty && feelingEmpty) return string.Empty;

        // 1) 강한 T 성향 — 사고 풀 우선
        if (tf <= -TraitTFBoundary && !thinkingEmpty)
            return PickOne(thinking, rng);

        // 2) 강한 F 성향 — 감정 풀 우선
        if (tf >= TraitTFBoundary && !feelingEmpty)
            return PickOne(feeling, rng);

        // 3) 중립 — 두 풀을 합쳐서 선택
        int total = (thinkingEmpty ? 0 : thinking.Count) + (feelingEmpty ? 0 : feeling.Count);
        int idx = NextIndex(rng, total);
        if (!thinkingEmpty && idx < thinking.Count) return thinking[idx];
        return feeling[idx - (thinkingEmpty ? 0 : thinking.Count)];
    }

    // -------- 내부 --------

    private static string PickOne(System.Collections.Generic.List<string> list, System.Random rng)
    {
        if (list == null || list.Count == 0) return string.Empty;
        return list[NextIndex(rng, list.Count)];
    }

    private static int NextIndex(System.Random rng, int count)
    {
        if (count <= 1) return 0;
        if (rng != null) return rng.Next(count);
        // rng 미제공 시 시스템 랜덤(무상태) — 대사는 게임플레이에 영향 없으므로 허용.
        return new System.Random().Next(count);
    }
}
