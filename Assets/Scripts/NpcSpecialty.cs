// NPC 의 전문 분야 — Docs/05 §4 "전문가 시너지" 에서 언급된 분업 체계.
//
// 설계 의도:
// - 생산형 NPC(ProducerNpcController) 와 전문직 NPC(SpecialistNpcController) 가
//   공통으로 참조할 수 있도록 enum 을 별도 파일로 분리한다.
// - Workbench 자동 가공 시스템(Task #19) 은 Specialty → WorkbenchType 매핑으로
//   "이 NPC 가 어느 작업대에서 일해야 하는가" 를 판정한다.
// - 마을 이벤트/해금 조건에서 "셰프 2명 이상 고용 시 XX 해금" 같은 집계에도 사용된다.
public enum NpcSpecialty
{
    None,           // 일반 주민 (마을 소비형)
    Farmer,         // 농부 — ProducerNpcController (채집형)
    Miner,          // 광부 — ProducerNpcController (채집형)
    Lumberjack,     // 벌목꾼 — ProducerNpcController (채집형)
    Chef,           // 요리사 — SpecialistNpcController, Kitchen 전담
    Blacksmith,     // 대장장이 — SpecialistNpcController, Forge 전담
    Tailor,         // 재봉사 — SpecialistNpcController, SewingTable 전담
    Carpenter,      // 목수 — SpecialistNpcController, BasicWorkbench 전담
    Fisher          // 어부 — ProducerNpcController (채집형)
}

// Specialty 와 WorkbenchType 사이의 매핑 — SpecialistNpcController 가
// "이 전문가가 어느 작업대에서 일할 수 있는가" 를 판정할 때 사용한다.
public static class NpcSpecialtyMapping
{
    /// <summary>Specialty 에 대응하는 WorkbenchType 을 반환한다. 대응 없음이면 None.</summary>
    public static WorkbenchType GetWorkbenchType(NpcSpecialty specialty)
    {
        switch (specialty)
        {
            case NpcSpecialty.Chef:       return WorkbenchType.Kitchen;
            case NpcSpecialty.Blacksmith: return WorkbenchType.Forge;
            case NpcSpecialty.Tailor:     return WorkbenchType.SewingTable;
            case NpcSpecialty.Carpenter:  return WorkbenchType.BasicWorkbench;
            default:                      return WorkbenchType.None;
        }
    }

    /// <summary>Specialty 가 전문 가공직(Workbench 기반)인지 여부.</summary>
    public static bool IsCraftingSpecialty(NpcSpecialty specialty)
    {
        return GetWorkbenchType(specialty) != WorkbenchType.None;
    }
}
