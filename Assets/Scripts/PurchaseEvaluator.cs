using UnityEngine;

// NPC 의 구매 의사결정 로직.
//
// 설계 원칙:
// - 순수 함수(의존성 없음) — 외부 상태에 기대지 않으므로 테스트/멀티플레이 서버 검증 친화적이다.
// - 결정론적 — 입력 RNG 인스턴스를 받아 쓰며, UnityEngine.Random 을 절대 사용하지 않는다.
// - Docs/02, 03 의 MBTI 경제 모델을 직접 수식으로 구현한다.
//
// 수식 개요 (Docs/03 §2.1 MBTI 4축 전부 반영):
//   1) price ratio = DisplayPrice / IdealPrice
//      - IdealPrice = basePrice × (1 + 0.5 × max(0, quality - 1))
//      - ratio 에 따른 기본 확률은 S-커브: 싸면 95%, 기본가 70%, 비싸면 10%.
//   2) 카테고리 보너스 = traitSN × (카테고리별 계수)
//      - Utility 는 S 성향(traitSN 음수)이 선호
//      - Luxury 는 N 성향(traitSN 양수)이 선호
//      - F형(traitTF > 0) 은 카테고리 선호를 15% 증폭 (감성적 구매)
//   3) 가격 민감도 페널티 = 과가격일 때 (ratio - 1) × priceSensitivity 만큼 차감
//      - T형(traitTF < 0) 은 민감도를 10% 강화 (냉정한 가격 판단)
//   4) E/I 충동 구매 보너스 = traitEI × 0.10 (E형 +10%, I형 -10%)
//   5) 최종확률 = clamp01(기본확률 + 카테고리 보너스 - 민감도 페널티 + 충동 보너스)
public static class PurchaseEvaluator
{
    public struct Result
    {
        public bool willBuy;
        public float probability;
        public string reason;  // 디버그용
    }

    public static Result Evaluate(NpcProfile profile, ShopSlot slot, System.Random rng)
    {
        Result r = new Result { willBuy = false, probability = 0f, reason = "" };

        if (slot == null || slot.IsEmpty)
        {
            r.reason = "빈 진열대";
            return r;
        }
        if (rng == null)
        {
            r.reason = "RNG 없음";
            return r;
        }

        ItemInstance inst = slot.currentItem;
        Item item = inst.data;
        if (item == null)
        {
            r.reason = "아이템 데이터 누락";
            return r;
        }

        // 1) 이상가 대비 책정가 비율
        float qualityBoost = 0.5f * Mathf.Max(0f, inst.quality - 1f);
        int idealPrice = Mathf.Max(1, Mathf.RoundToInt(item.basePrice * (1f + qualityBoost)));
        int displayPrice = Mathf.Max(1, slot.EffectiveDisplayPrice);
        float ratio = displayPrice / (float)idealPrice;

        // 2) 기본 확률 S-커브 (ratio → baseProb)
        float baseProb;
        if (ratio <= 0.7f)
            baseProb = 0.95f;
        else if (ratio <= 1.0f)
            baseProb = Mathf.Lerp(0.95f, 0.70f, (ratio - 0.7f) / 0.3f);
        else if (ratio <= 1.5f)
            baseProb = Mathf.Lerp(0.70f, 0.10f, (ratio - 1.0f) / 0.5f);
        else
            baseProb = 0.05f;

        // 3) MBTI 카테고리 선호 보너스
        float categoryBonus = 0f;
        if (profile != null)
        {
            switch (item.category)
            {
                case ItemCategory.Utility:
                    // traitSN 이 -1(S) 일수록 강한 보너스
                    categoryBonus = (-profile.traitSN) * 0.20f * profile.utilityConsumption;
                    break;
                case ItemCategory.Luxury:
                    // traitSN 이 +1(N) 일수록 강한 보너스
                    categoryBonus = profile.traitSN * 0.25f * profile.luxuryConsumption;
                    break;
                case ItemCategory.Processed:
                    // 가공품은 N 성향 소폭 선호
                    categoryBonus = profile.traitSN * 0.08f;
                    break;
                case ItemCategory.Raw:
                    // 원자재는 S 성향이 소폭 선호 (즉각적 활용 가치)
                    categoryBonus = (-profile.traitSN) * 0.05f;
                    break;
                case ItemCategory.Tool:
                    // Tool 카테고리는 진열 자체가 막혀있어야 하지만 안전망으로 0.
                    categoryBonus = 0f;
                    break;
            }
        }

        // 3-a) T/F 축: F형은 카테고리 보너스 증폭 (감성적 구매 성향)
        if (profile != null && Mathf.Abs(categoryBonus) > 0.001f)
        {
            // F형(+1): 선호 카테고리 보너스 15% 증폭 / T형(-1): 15% 억제
            categoryBonus *= (1f + 0.15f * profile.traitTF);
        }

        // 4) 가격 민감도 페널티 — 비싸면 sensitivity 가 높을수록 확률 하락
        float sensitivityPenalty = 0f;
        if (ratio > 1f)
        {
            float sensitivity = profile != null ? profile.priceSensitivity : 1f;
            sensitivityPenalty = (ratio - 1f) * 0.30f * sensitivity;

            // T/F 축: T형은 가격 민감도 10% 강화 (냉정한 판단)
            if (profile != null)
                sensitivityPenalty *= (1f + 0.10f * (-profile.traitTF));
        }

        // 5) E/I 충동 구매 보너스 — E형은 충동적, I형은 신중
        float impulseBias = 0f;
        if (profile != null)
            impulseBias = profile.traitEI * 0.10f;  // E(+1) → +10%, I(-1) → -10%

        float finalProb = Mathf.Clamp01(baseProb + categoryBonus - sensitivityPenalty + impulseBias);
        r.probability = finalProb;

        bool roll = rng.NextDouble() < finalProb;
        r.willBuy = roll;
        r.reason = string.Format(
            "ratio={0:F2} base={1:F2} catBonus={2:+0.00;-0.00} sensPen={3:-0.00} impulse={4:+0.00;-0.00} → p={5:F2} roll={6}",
            ratio, baseProb, categoryBonus, sensitivityPenalty, impulseBias, finalProb, roll ? "BUY" : "PASS");

        return r;
    }
}
