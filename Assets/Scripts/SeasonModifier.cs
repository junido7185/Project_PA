using UnityEngine;

// 계절별 생산 속도 보정 — ProducerNpcController / SpecialistNpcController 가 참조하는 정적 헬퍼.
//
// 사용 방식:
//   float modifier = SeasonModifier.GetProductionModifier(specialty);
//   effectiveInterval /= modifier;  // 높을수록 빠름
//
// 기본 규칙 (Docs/05 §5 계절 시너지):
//   Spring : 농업 1.3× / 나머지 1.0×
//   Summer : 채광 1.2× / 농업 0.8× (더위)
//   Autumn : 벌목 1.2× / 가공 1.1× (수확기 시너지)
//   Winter : 전체 0.7× (혹한 페널티)
//
// 커스터마이징이 필요하면 ScriptableObject 로 데이터를 옮길 수 있으나 MVP 에서는 하드코딩.
public static class SeasonModifier
{
    /// <summary>현재 계절에서 specialty 의 생산 속도 배율을 반환한다 (1.0 = 기본).</summary>
    public static float GetProductionModifier(NpcSpecialty specialty)
    {
        if (GameClock.Instance == null) return 1f;
        return GetModifier(GameClock.Instance.CurrentSeason, specialty);
    }

    public static float GetModifier(Season season, NpcSpecialty specialty)
    {
        switch (season)
        {
            case Season.Spring:
                if (specialty == NpcSpecialty.Farmer) return 1.3f;
                return 1.0f;

            case Season.Summer:
                if (specialty == NpcSpecialty.Miner)  return 1.2f;
                if (specialty == NpcSpecialty.Fisher)  return 1.3f;  // 여름 성어기
                if (specialty == NpcSpecialty.Farmer)  return 0.8f;
                return 1.0f;

            case Season.Autumn:
                if (specialty == NpcSpecialty.Lumberjack) return 1.2f;
                if (specialty == NpcSpecialty.Fisher)      return 1.1f;  // 가을 풍어기
                if (NpcSpecialtyMapping.IsCraftingSpecialty(specialty)) return 1.1f;
                return 1.0f;

            case Season.Winter:
                return 0.7f;  // 전체 혹한 페널티

            default:
                return 1.0f;
        }
    }
}
